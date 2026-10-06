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

        // ===== Stock-aware mosaic: stock on hand per stone =====

        // Weight of one mosaic stone. The sheet's "Kullanılacaklar (kg)" is exactly count × 3.3 g.
        public const double StoneWeightKg = 0.0033;

        public sealed class StoneStock
        {
            public int Id;            // "mos" column = catalog stone ID
            public string Code = "";  // "Kod", e.g. C125
            public string Name = "";  // "Öğe adı", e.g. Teos1 Yeşil; the same stone in other finishes shares it
            public double OnHandKg;   // "Bizdeki (kg)"
            // Stock already set aside by the sheet's other mosaic columns (counts × 3.3 g).
            public double OtherMosaicsKg;
            // What this mosaic may use: on hand minus the other mosaics' share.
            public double AvailableKg => OnHandKg - OtherMosaicsKg;
            public int Capacity => AvailableKg <= 0 ? 0 : (int)Math.Floor(AvailableKg / StoneWeightKg + 1e-9);
        }

        // Stones that have a row in the sheet (a "mos" number and a "Kod"). Stones without a row have no stock data.
        // projectName: this mosaic's own column, left out of the other mosaics' share (null = count every column).
        public static async Task<Dictionary<int, StoneStock>> FetchOnHandAsync(string sheetId, string? projectName = null) =>
            ParseOnHand(await FetchCsvAsync(sheetId), projectName);

        public static Dictionary<int, StoneStock> ParseOnHandCsv(string csv, string? projectName = null)
        {
            var rows = new List<string[]>();
            foreach (var line in csv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                rows.Add(ParseCsvLine(line));
            return ParseOnHand(rows, projectName);
        }

        private static Dictionary<int, StoneStock> ParseOnHand(List<string[]> rows, string? projectName)
        {
            var headers = rows[0];
            int mosCol = -1, kodCol = -1, nameCol = -1, bizdekiCol = -1, zoneStart = -1, zoneEnd = -1;
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i].Trim().ToLowerInvariant();
                if (h == "mos") mosCol = i;
                else if (h == "kod") kodCol = i;
                else if (nameCol < 0 && h.Contains("adı")) nameCol = i;
                if (bizdekiCol < 0 && h.Contains("bizdeki") && h.Contains("(kg)")) bizdekiCol = i;
                // Mosaic columns: same rule as the sheet's Apps Script (findMozaikZone) — after the last
                // "bizdeki" header up to the "13." header, skipping "#" and empty headers.
                if (h.Contains("bizdeki")) zoneStart = i + 1;
                if (h.Contains("13.")) zoneEnd = i;
            }
            if (mosCol < 0 || bizdekiCol < 0)
                throw new Exception(Loc.Fmt("StockErrColumns", "mos / Bizdeki (kg)", string.Join(", ", headers)));
            if (zoneStart > 0 && zoneEnd < 0) zoneEnd = zoneStart + 9;
            var zone = new List<int>();
            for (int c = Math.Max(zoneStart, 0); c < zoneEnd && c < headers.Length; c++)
            {
                string h = headers[c].Trim();
                if (h.Length == 0 || h == "#") continue;
                if (projectName != null && h == projectName.Trim()) continue;
                zone.Add(c);
            }

            var stock = new Dictionary<int, StoneStock>();
            for (int r = 1; r < rows.Count; r++)
            {
                var cols = rows[r];
                string Col(int c) => c >= 0 && c < cols.Length ? cols[c].Trim() : "";
                if (!int.TryParse(Col(mosCol), out int mos)) continue;
                string kod = Col(kodCol), name = Col(nameCol), bizdeki = Col(bizdekiCol);
                // Stones 68+ have no "Kod" in the sheet ("Taş 68" …) but do have a name and stock, so only rows with
                // nothing at all (the numbered filler rows below the catalog) are skipped.
                if (kod.Length == 0 && name.Length == 0 && bizdeki.Length == 0) continue;
                double others = 0;
                foreach (int c in zone) others += ParseTrNumber(Col(c)) ?? 0;
                stock[mos] = new StoneStock
                {
                    Id = mos, Code = kod, Name = name,
                    OnHandKg = ParseTrNumber(bizdeki) ?? 0,
                    OtherMosaicsKg = Math.Max(0, others) * StoneWeightKg
                };
            }
            return stock;
        }

        // Writes this mosaic's stone counts into its project column, without reading anything back
        // (used after the stock-aware fix, whose result is already known).
        public static Task WriteCountsAsync(string scriptUrl, string sheetId, string projectName, List<(int Id, int Count)> stones) =>
            PostAsync(scriptUrl, new Dictionary<string, object>
            {
                ["projectName"] = projectName,
                ["sheetId"] = sheetId,
                ["stones"] = stones.ConvertAll(s => new Dictionary<string, int> { ["mos"] = s.Id, ["count"] = s.Count })
            }, treatNotFoundAsError: false);

        // Sheet numbers use Turkish format: "1.027,00" = 1027.00, "15,00" = 15.0. Empty → null.
        public static double? ParseTrNumber(string s)
        {
            s = s.Trim();
            if (s.Length == 0) return null;
            string v = s.Replace(".", "").Replace(",", ".");
            return double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : null;
        }

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
