using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mosair.Services
{
    // Google Drive project folder through a small Apps Script web app (Assets/mosair-drive.gs), the same way
    // the stock sheet is written: no Google sign-in in the app. Projects are sent and received gzip-compressed.
    public static class DriveService
    {
        public sealed class Config
        {
            public string FolderUrl { get; set; } = "";
            public string ScriptUrl { get; set; } = "";
        }

        // Folder: the project folder inside the Drive folder ("" for a .mos directly in it).
        // ImageId: the project's original image next to it (for the preview), "" when there is none.
        public sealed record DriveFile(string Id, string Name, string Folder, long Size, DateTime Modified, string ImageId);

        // Stored per user, like stock.json. Never put the folder link or the script URL in the repo or a build:
        // the script URL lets anyone write to and read from the folder.
        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mosair", "drive.json");

        // Opened Drive projects are kept here (they need a file on disk to open from).
        public static string CacheDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "mosair", "drive");

        private static readonly HttpClient Http = new(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        })
        { Timeout = TimeSpan.FromMinutes(10) };   // large projects

        public static Config LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                    return JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigPath)) ?? new Config();
            }
            catch (Exception) { }
            return new Config();
        }

        public static void SaveConfig(Config config)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        }

        // Accepts a folder link (…/drive/folders/<id>?…, …/open?id=<id>) or a bare folder ID.
        public static string FolderId(string folderUrl)
        {
            string text = (folderUrl ?? "").Trim();
            var m = Regex.Match(text, @"/folders/([A-Za-z0-9_-]+)");
            if (m.Success) return m.Groups[1].Value;
            m = Regex.Match(text, @"[?&]id=([A-Za-z0-9_-]+)");
            if (m.Success) return m.Groups[1].Value;
            return Regex.IsMatch(text, @"^[A-Za-z0-9_-]{10,}$") ? text : "";
        }

        public static bool IsConfigured(Config c) => FolderId(c.FolderUrl).Length > 0 && c.ScriptUrl.Trim().Length > 0;

        // The Apps Script code, shipped inside the app so the settings window can copy it.
        public static string ScriptCode()
        {
            var asm = typeof(DriveService).Assembly;
            foreach (string name in asm.GetManifestResourceNames())
                if (name.EndsWith("mosair-drive.gs", StringComparison.OrdinalIgnoreCase))
                {
                    using var s = asm.GetManifestResourceStream(name)!;
                    using var r = new StreamReader(s, Encoding.UTF8);
                    return r.ReadToEnd();
                }
            return "";
        }

        // Returns the folder's name (checks the link and the script).
        public static async Task<string> PingAsync(Config c)
        {
            using var doc = await PostAsync(c, new Dictionary<string, object> { ["action"] = "ping" });
            return doc.RootElement.TryGetProperty("folder", out var f) ? f.GetString() ?? "" : "";
        }

        // Like mosairPROJECT on the desktop: <folderName>/<name> inside the Drive folder, with the original image
        // next to it (sent when given; the script keeps an image that is already there).
        public static async Task SaveAsync(Config c, string folderName, string name, byte[] project,
            string? imageName = null, byte[]? image = null)
        {
            var payload = new Dictionary<string, object>
            {
                ["action"] = "save",
                ["folderName"] = folderName,
                ["name"] = name,
                ["data"] = Convert.ToBase64String(Gzip(project))
            };
            if (image != null && !string.IsNullOrEmpty(imageName))
            {
                payload["imageName"] = imageName;
                payload["image"] = Convert.ToBase64String(Gzip(image));
            }
            using var doc = await PostAsync(c, payload);
        }

        public static async Task<List<DriveFile>> ListAsync(Config c)
        {
            using var doc = await PostAsync(c, new Dictionary<string, object> { ["action"] = "list" });
            var files = new List<DriveFile>();
            if (doc.RootElement.TryGetProperty("files", out var arr))
                foreach (var f in arr.EnumerateArray())
                    files.Add(new DriveFile(
                        f.GetProperty("id").GetString() ?? "",
                        f.GetProperty("name").GetString() ?? "",
                        f.TryGetProperty("folder", out var folder) ? folder.GetString() ?? "" : "",
                        f.TryGetProperty("size", out var size) ? size.GetInt64() : 0,
                        f.TryGetProperty("modified", out var mod) && DateTime.TryParse(mod.GetString(), out var dt)
                            ? dt.ToLocalTime() : DateTime.MinValue,
                        f.TryGetProperty("imageId", out var img) ? img.GetString() ?? "" : ""));
            return files;
        }

        // Drive's own small previews (PNG) of the given image files; missing ones are left out.
        public static async Task<Dictionary<string, byte[]>> ThumbnailsAsync(Config c, IReadOnlyList<string> ids)
        {
            var result = new Dictionary<string, byte[]>();
            if (ids.Count == 0) return result;
            using var doc = await PostAsync(c, new Dictionary<string, object> { ["action"] = "thumbs", ["ids"] = ids });
            if (doc.RootElement.TryGetProperty("thumbs", out var thumbs))
                foreach (var t in thumbs.EnumerateObject())
                {
                    string b64 = t.Value.GetString() ?? "";
                    if (b64.Length > 0) result[t.Name] = Convert.FromBase64String(b64);
                }
            return result;
        }

        // The folder in the browser (the "Drive'da göster" button).
        public static string FolderWebUrl(Config c) => "https://drive.google.com/drive/folders/" + FolderId(c.FolderUrl);

        // Downloads a project, and the original image saved next to it, into the cache folder (same layout as on
        // Drive, so OpenProject finds the image beside the .mos) and returns the project's path.
        public static async Task<string> DownloadAsync(Config c, DriveFile file)
        {
            byte[] project = await GetAsync(c, file.Id, null);
            string dir = Path.Combine(CacheDir, SafeName(file.Folder.Length > 0 ? file.Folder : "_"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, SafeName(file.Name).Length > 0 ? SafeName(file.Name) : "drive.mos");
            await File.WriteAllBytesAsync(path, project);

            string? picture = PictureFileName(project);
            if (!string.IsNullOrEmpty(picture))
            {
                byte[] image = await GetAsync(c, file.Id, picture);
                if (image.Length > 0) await File.WriteAllBytesAsync(Path.Combine(dir, SafeName(picture)), image);
            }
            return path;
        }

        // The project file itself, or (imageName) the file of that name next to it; empty when there is none.
        private static async Task<byte[]> GetAsync(Config c, string fileId, string? imageName)
        {
            var payload = new Dictionary<string, object> { ["action"] = "get", ["fileId"] = fileId };
            if (imageName != null) payload["imageName"] = imageName;
            using var doc = await PostAsync(c, payload);
            string data = doc.RootElement.GetProperty("data").GetString() ?? "";
            return data.Length == 0 ? Array.Empty<byte>() : Gunzip(Convert.FromBase64String(data));
        }

        private static string SafeName(string name) => string.Concat(name.Split(Path.GetInvalidFileNameChars()));

        // The "PictureFileName" of a .mos (read without loading the whole JSON tree).
        private static string? PictureFileName(byte[] project)
        {
            try
            {
                var reader = new Utf8JsonReader(project, new JsonReaderOptions { AllowTrailingCommas = true });
                while (reader.Read())
                    if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 1 &&
                        reader.ValueTextEquals("PictureFileName"))
                    {
                        reader.Read();
                        return reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                    }
            }
            catch (JsonException) { }
            return null;
        }

        private static byte[] Gzip(byte[] data)
        {
            using var ms = new MemoryStream();
            using (var gz = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                gz.Write(data, 0, data.Length);
            return ms.ToArray();
        }

        private static byte[] Gunzip(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var gz = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gz.CopyTo(output);
            return output.ToArray();
        }

        private static async Task<JsonDocument> PostAsync(Config c, Dictionary<string, object> payload)
        {
            string folderId = FolderId(c.FolderUrl);
            if (folderId.Length == 0 || c.ScriptUrl.Trim().Length == 0)
                throw new InvalidOperationException(Loc.Get("DriveNotConfigured"));
            payload["folderId"] = folderId;
            string json = JsonSerializer.Serialize(payload);
            static bool IsHtml(string b) =>
                b.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) || b.Contains("<html", StringComparison.OrdinalIgnoreCase);

            // Like the stock script: Google sometimes answers with an HTML page for a moment; retry once.
            string body = "";
            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (attempt > 0) await Task.Delay(2000);
                var response = await Http.PostAsync(c.ScriptUrl.Trim(), new StringContent(json, Encoding.UTF8, "application/json"));
                body = await response.Content.ReadAsStringAsync();
                if (!IsHtml(body)) break;
            }
            if (IsHtml(body))
            {
                var title = Regex.Match(body, "<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                string detail = title.Success ? System.Net.WebUtility.HtmlDecode(title.Groups[1].Value).Trim() : "";
                throw new Exception(detail.Length > 0 ? Loc.Get("DriveErrDeploy") + "\n(" + detail + ")" : Loc.Get("DriveErrDeploy"));
            }

            JsonDocument doc;
            try { doc = JsonDocument.Parse(body); }
            catch (JsonException) { throw new Exception(Loc.Get("DriveErrDeploy")); }
            string status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
            if (status != "ok")
            {
                string message = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() ?? "" : body;
                doc.Dispose();
                throw new Exception(message.Length > 300 ? message[..300] : message);
            }
            return doc;
        }
    }
}
