using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace mosair.Services
{
    // Google Sheet stock integration, ported from the WPF app (WindowPicture stok* handlers + appsScript.js).
    // The sheet is read through its public gviz CSV export; writes go through the Apps Script web app.
    public static class StockSheetService
    {
        public sealed class Config
        {
            public string SheetId { get; set; } = "";
            public string ScriptUrl { get; set; } = "";
        }

        // Stored per user, outside the app folder: the macOS .app bundle is read-only.
        // Never ship these values in the repo or the builds — the script URL grants write access to the sheet.
        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mosair", "stock.json");

        private static readonly HttpClient Http = new(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        })
        { Timeout = TimeSpan.FromSeconds(60) };

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

        public static string SheetUrl(string sheetId) => "https://docs.google.com/spreadsheets/d/" + sheetId;

        // "stok çek": stock in kg per stone ID from the "Bizdeki (kg)" column.
        public static async Task<Dictionary<int, double>> FetchStockAsync(string sheetId)
        {
            var rows = await FetchCsvAsync(sheetId);
            var headers = rows[0];
            int mosCol = -1, bizdekiCol = -1;
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i].Trim().ToLowerInvariant();
                if (h == "mos") mosCol = i;
                else if (bizdekiCol < 0 && h.Contains("bizdeki") && h.Contains("(kg)")) bizdekiCol = i;
            }
            if (mosCol < 0 || bizdekiCol < 0)
                throw new Exception(Loc.Fmt("StockErrColumns", "mos / Bizdeki (kg)", string.Join(", ", headers)));

            var stock = new Dictionary<int, double>();
            foreach (var (mos, kg) in ReadNumberColumn(rows, mosCol, bizdekiCol))
                stock[mos] = stock.TryGetValue(mos, out double prev) ? prev + kg : kg;
            return stock;
        }

        // "stok kontrol": write this mosaic's stone counts into its column, then read back each stone's
        // "Tahmini Kalan" (estimated remaining) and "Bizdeki (kg)"; stones whose remaining went negative are short.
        public static async Task<CheckResult> CheckStockAsync(string scriptUrl, string sheetId, string projectName,
            List<(int Id, int Count)> stones)
        {
            var payload = new Dictionary<string, object>
            {
                ["projectName"] = projectName,
                ["sheetId"] = sheetId,
                ["stones"] = stones.ConvertAll(s => new Dictionary<string, int> { ["mos"] = s.Id, ["count"] = s.Count })
            };
            await PostAsync(scriptUrl, payload, treatNotFoundAsError: false);

            // Same short wait as WPF before reading back; gviz may still serve a slightly stale export.
            await Task.Delay(100);
            var rows = await FetchCsvAsync(sheetId);
            var headers = rows[0];
            int mosCol = -1, kalanCol = -1, bizdekiCol = -1;
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i].Trim().ToLowerInvariant();
                if (h == "mos") mosCol = i;
                else if (h.Contains("tahmini") && h.Contains("kalan")) kalanCol = i;
                else if (bizdekiCol < 0 && h.Contains("bizdeki") && h.Contains("(kg)")) bizdekiCol = i;
            }
            if (mosCol < 0 || kalanCol < 0)
                throw new Exception(Loc.Fmt("StockErrColumns", "mos / Tahmini Kalan", string.Join(", ", headers)));

            var result = new CheckResult();
            foreach (var (mos, kalan) in ReadNumberColumn(rows, mosCol, kalanCol))
            {
                result.Remaining[mos] = kalan;
                if (kalan < 0) result.ShortIds.Add(mos);
            }
            // As in WPF, "Bizdeki (kg)" is only taken from rows that have a remaining amount.
            if (bizdekiCol >= 0)
                foreach (var (mos, kg) in ReadNumberColumn(rows, mosCol, bizdekiCol))
                    if (result.Remaining.ContainsKey(mos)) result.OnHand[mos] = kg;
            return result;
        }

        public sealed class CheckResult
        {
            public HashSet<int> ShortIds { get; } = new();
            // "Tahmini Kalan" (stock left after the mosaics in the sheet) and "Bizdeki (kg)" (stock on hand), by stone ID.
            public Dictionary<int, double> Remaining { get; } = new();
            public Dictionary<int, double> OnHand { get; } = new();
        }

        // "stok temizle" (one column / all mosaic columns) and "stok ekle".
        public static Task ClearOneAsync(string scriptUrl, string sheetId, string projectName) =>
            PostAsync(scriptUrl, new Dictionary<string, object>
            {
                ["action"] = "clearOne", ["sheetId"] = sheetId, ["projectName"] = projectName
            }, treatNotFoundAsError: true);

        public static Task ClearAllAsync(string scriptUrl, string sheetId) =>
            PostAsync(scriptUrl, new Dictionary<string, object>
            {
                ["action"] = "clearAll", ["sheetId"] = sheetId
            }, treatNotFoundAsError: true);

        public static Task AddStockAsync(string scriptUrl, string sheetId) =>
            PostAsync(scriptUrl, new Dictionary<string, object>
            {
                ["action"] = "stokEkle", ["sheetId"] = sheetId
            }, treatNotFoundAsError: true);

        private static async Task<List<string[]>> FetchCsvAsync(string sheetId)
        {
            string url = $"https://docs.google.com/spreadsheets/d/{Uri.EscapeDataString(sheetId)}/gviz/tq?tqx=out:csv";
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
            string csv = await Http.GetStringAsync(url, cts.Token);
            var rows = new List<string[]>();
            foreach (var line in csv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                rows.Add(ParseCsvLine(line));
            if (rows.Count < 2) throw new Exception(Loc.Get("StockErrEmpty"));
            return rows;
        }

        private static IEnumerable<(int Mos, double Value)> ReadNumberColumn(List<string[]> rows, int mosCol, int valueCol)
        {
            for (int r = 1; r < rows.Count; r++)
            {
                var cols = rows[r];
                if (cols.Length <= Math.Max(mosCol, valueCol)) continue;
                if (!int.TryParse(cols[mosCol].Trim(), out int mos)) continue;
                string v = cols[valueCol].Trim().Replace(",", ".");
                if (v.Length == 0) continue;
                if (!double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double value)) continue;
                yield return (mos, value);
            }
        }

        private static async Task PostAsync(string scriptUrl, object payload, bool treatNotFoundAsError)
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await Http.PostAsync(scriptUrl, content);
            string body = await response.Content.ReadAsStringAsync();

            if (body.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) || body.Contains("<html", StringComparison.OrdinalIgnoreCase))
                throw new Exception(Loc.Get("StockErrDeploy"));

            string status = "", message = body;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("status", out var s)) status = s.GetString() ?? "";
                if (doc.RootElement.TryGetProperty("message", out var m)) message = m.GetString() ?? "";
            }
            catch (JsonException) { }

            // The script reports a missing column as status "ok" with a "...bulunamadi" message; WPF treats it as a failure.
            bool notFound = message.Contains("bulunamadi", StringComparison.OrdinalIgnoreCase) ||
                            message.Contains("bulunamadı", StringComparison.OrdinalIgnoreCase);
            if (status == "error" || (treatNotFoundAsError && notFound) || (status.Length == 0 && body.Contains("error")))
                throw new Exception(message.Length > 300 ? message[..300] : message);
        }

        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            bool inQuotes = false;
            var sb = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else sb.Append(c);
            }
            fields.Add(sb.ToString());
            return fields.ToArray();
        }
    }
}
