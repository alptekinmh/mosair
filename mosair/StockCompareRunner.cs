using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using mosair.Models;
using mosair.Services;
using SkiaSharp;

namespace mosair;

// Usage: mosair --stockcompare <outDir> <widthCm> <stock.csv> <stockScale> <image1> [image2 ...]
// stock.csv is a local copy of the stock sheet (gviz CSV); stockScale multiplies every "Bizdeki (kg)" value so a
// shortage can be forced. Panels: original | Optimum | stock-aware | changed pixels.
public static class StockCompareRunner
{
    public static int Run(string[] args)
    {
        if (args.Length < 6)
        {
            Console.WriteLine("usage: --stockcompare <outDir> <widthCm> <stock.csv> <stockScale> <image...>");
            return 1;
        }
        var inv = CultureInfo.InvariantCulture;
        string outDir = args[1];
        double widthCm = double.Parse(args[2], inv);
        // MOSAIR_PROJECT: this mosaic's own sheet column, left out of the other mosaics' share (as after Stok Kontrol).
        string? project = Environment.GetEnvironmentVariable("MOSAIR_PROJECT");
        var stock = StockSheetService.ParseOnHandCsv(File.ReadAllText(args[3]), project);
        double scale = double.Parse(args[4], inv);
        Directory.CreateDirectory(outDir);
        ColorCatalogService.LoadDefaultCatalog();
        var options = new StockAwareOptions();

        var report = new StringBuilder();
        void Log(string s) { Console.WriteLine(s); report.AppendLine(s); }
        Log($"stones in sheet={stock.Count} stockScale={scale.ToString(inv)} widthCm={widthCm.ToString(inv)} " +
            $"tolerance={options.SimilarityTolerance.ToString(inv)} maxExtra={options.MaxExtraStones}");
        Log("");
        LogSimilarity(Log, stock);

        int? Cap(int id) => stock.TryGetValue(id, out var s)
            ? Math.Max(0, (int)Math.Floor((s.OnHandKg * scale - s.OtherMosaicsKg) / StockSheetService.StoneWeightKg + 1e-9)) : null;
        int? Unlimited(int id) => stock.ContainsKey(id) ? int.MaxValue / 4 : null;
        string? Family(int id) => stock.TryGetValue(id, out var s) ? s.Name : null;
        string Label(int id) => stock.TryGetValue(id, out var s) ? $"#{id} {s.Code} {s.Name}" : $"#{id} (stok kaydı yok)";
        string Kg(int pieces) => (pieces * StockSheetService.StoneWeightKg).ToString("0.00", inv);

        var summary = new StringBuilder();
        summary.AppendLine("image\tstones O/S\tmeanΔE O/S\tedgesKept O/S\tLcorr O/S\tover before/after\tsmall before/after\tmoved\tadded\tidentical\tchecks");

        for (int a = 5; a < args.Length; a++)
        {
            string path = args[a];
            string name = Path.GetFileNameWithoutExtension(path);
            MosaicEngine.Reset();
            if (MosaicEngine.LoadImage(path) == null) { Log($"[{name}] load failed"); continue; }
            MosaicEngine.CalculateDimensions(widthCm);
            int C = (int)MosaicEngine.width, R = (int)MosaicEngine.height;
            byte[,,] src = MosaicEngine.GetSourceStoneData();

            MosaicEngine.RunOptimal(InterpolationMethod.Area, null, prepareTextures: false);
            int k = MosaicEngine.LastOptimalResult!.KOptimal;
            var optData = (byte[,,])MosaicData.dataM3.Clone();
            var optIds = CopyIds(R, C);
            string optPalette = PaletteKey();
            var qOpt = MosaicMetrics.Evaluate(src, optData, R, C);

            options.MinUsage = StockAwareOptions.MinUsageFor(R * C);
            int smallBefore = MosaicData.arMB.SelectMany(l => l).Count(c => c.numOfPixel > 0 && c.numOfPixel < options.MinUsage);

            // Acceptance test: with enough stock and no minimum rule the step must not change anything.
            MosaicEngine.ApplyOptimalKWithStock(k, Unlimited, Family, new StockAwareOptions { MinUsage = 0 }, prepareTextures: false);
            bool identical = Same(optData, MosaicData.dataM3) && Same(optIds, CopyIds(R, C)) && optPalette == PaletteKey()
                             && !MosaicEngine.LastStockResult!.Changed;

            var sw = Stopwatch.StartNew();
            MosaicEngine.ApplyOptimalKWithStock(k, Cap, Family, options, prepareTextures: false);
            sw.Stop();
            var res = MosaicEngine.LastStockResult!;
            var stData = (byte[,,])MosaicData.dataM3.Clone();
            var qSt = MosaicMetrics.Evaluate(src, stData, R, C);
            int overBefore = res.CountBefore.Count(kv => res.Capacity.TryGetValue(kv.Key, out int c) && kv.Value > c);
            int smallAfter = res.CountAfter.Count(kv => kv.Value < options.MinUsage);
            bool checks = res.ShortIds.Count == 0 && smallAfter == 0;

            Log($"[{name}] {C}x{R} = {R * C} taş, Optimum k={k}");
            Log($"  Optimum      {qOpt}");
            Log($"  Stoğa göre   {qSt}  süre={sw.Elapsed.TotalMilliseconds:F0}ms");
            Log($"  stok yeterli olunca birebir aynı: {(identical ? "EVET" : "HAYIR")}; en az kullanım {options.MinUsage}: " +
                $"Optimum'da sınır altı taş {smallBefore}, sonra {smallAfter}; kontroller {(checks ? "GEÇTİ" : "KALDI")}");
            if (res.SmallRemovedIds.Count > 0)
                Log($"  az kullanıldığı için çıkarılan: {string.Join(", ", res.SmallRemovedIds.Select(id => $"{Label(id)} ({(res.CountBefore.TryGetValue(id, out int b) ? b : 0)})"))}");
            Log($"  stoğu aşan taş: önce {overBefore}, sonra {res.ShortIds.Count}; taşınan {res.MovedPixels} taş, " +
                $"taşınanların ortalama renk değişimi {res.MeanShift.ToString("F2", inv)}; arama seviyesi {res.Level} ({LevelText(res.Level)}); yeni taş türü: {(res.AddedIds.Count == 0 ? "yok" : string.Join(", ", res.AddedIds.Select(Label)))}");
            Log("  taş                                    | Optimum adet (kg) | stok kg (adet) | sonra adet (kg)");
            var ids = res.CountBefore.Keys.Union(res.CountAfter.Keys).OrderBy(x => x);
            foreach (int id in ids)
            {
                res.CountBefore.TryGetValue(id, out int before);
                res.CountAfter.TryGetValue(id, out int after);
                bool hasCap = res.Capacity.TryGetValue(id, out int cap);
                if (before == after && (!hasCap || before <= cap)) continue; // only rows that matter
                string capText = hasCap ? $"{(cap * StockSheetService.StoneWeightKg).ToString("0.00", inv)} ({cap})" : "-";
                string mark = hasCap && after > cap ? "  ← hâlâ yetmiyor" : "";
                Log($"  {Label(id),-38} | {before,6} ({Kg(before)}) | {capText,14} | {after,6} ({Kg(after)}){mark}");
            }
            foreach (var mv in res.Moves)
                Log($"    {Label(mv.FromId)} → {Label(mv.ToId)}: {mv.Count} taş");
            if (Environment.GetEnvironmentVariable("MOSAIR_USAGE") == "1")
                foreach (var kv in res.CountBefore.OrderByDescending(x => x.Value))
                    Log($"  USAGE\t{name}\t{kv.Key}\t{kv.Value}");
            Log($"  stok kaydı olmayan kullanılan taşlar: {(res.UnknownIds.Count == 0 ? "yok" : string.Join(", ", res.UnknownIds.Select(Label)))}");
            Log("");

            summary.AppendLine(string.Create(inv,
                $"{name}\t{qOpt.Stones}/{qSt.Stones}\t{qOpt.MeanDeltaE:F2}/{qSt.MeanDeltaE:F2}\t{qOpt.EdgeKept * 100:F1}/{qSt.EdgeKept * 100:F1}\t{qOpt.LightnessCorr:F3}/{qSt.LightnessCorr:F3}\t{overBefore}/{res.ShortIds.Count}\t{smallBefore}/{smallAfter}\t{res.MovedPixels}\t{res.AddedIds.Count}\t{(identical ? "yes" : "NO")}\t{(checks ? "ok" : "FAIL")}"));

            SavePanels(Path.Combine(outDir, $"{name}_stok.png"), R, C, optData, stData,
                ("Orijinal", src), ($"Optimum · {qOpt.Stones} taş", optData),
                ($"Stoğa göre · {qSt.Stones} taş", stData));
        }

        Log("SUMMARY");
        Log(summary.ToString());
        File.WriteAllText(Path.Combine(outDir, "rapor.txt"), report.ToString(), new UTF8Encoding(true));
        return 0;
    }

    private static string LevelText(int level) => level switch
    {
        0 => "benzer taşlarla çözüldü",
        1 => "benzerlik sınırı 2 katına çıkarıldı",
        2 => "benzerlik sınırı 4 katına çıkarıldı",
        3 => "stoğu olan herhangi bir taş kullanıldı",
        _ => "yeni taş türü sınırı da aşıldı"
    };

    // For every stone with stock: its closest other stones in the substitute metric (ΔE), to judge the tolerance.
    private static void LogSimilarity(Action<string> log, Dictionary<int, StockSheetService.StoneStock> stock)
    {
        double W = new StockAwareOptions().LightnessWeight;
        var stones = MosaicData.arRGBAll.Where(s => stock.ContainsKey(s.ID)).ToList();
        var lab = stones.ToDictionary(s => s.ID, s => ColorMatcher.RgbToLab(s.r, s.g, s.b));
        double D(int x, int y)
        {
            var p = lab[x]; var q = lab[y];
            double dl = (p.L - q.L) * W, da = p.A - q.A, db = p.B - q.B;
            return Math.Sqrt(dl * dl + da * da + db * db);
        }
        log("RENK YAKINLIĞI (ΔE; aynı taşın diğer yüzeyleri * ile işaretli)");
        var family = new List<double>();
        foreach (var s in stones)
        {
            var near = stones.Where(t => t.ID != s.ID).Select(t => (t, d: D(s.ID, t.ID))).OrderBy(x => x.d).Take(3);
            var st = stock[s.ID];
            log($"  #{s.ID,-3} {st.Code,-6} {st.Name.Trim(),-28} RGB({s.r,3},{s.g,3},{s.b,3}) {st.OnHandKg,6:0.00}kg  →  " + string.Join("   ", near.Select(x =>
            {
                var o = stock[x.t.ID];
                bool same = string.Equals(o.Name.Trim(), st.Name.Trim(), StringComparison.OrdinalIgnoreCase);
                return $"{(same ? "*" : "")}#{x.t.ID} {o.Code} {x.d:F1}";
            })));
            foreach (var t in stones)
                if (t.ID > s.ID && string.Equals(stock[t.ID].Name.Trim(), st.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                    family.Add(D(s.ID, t.ID));
        }
        log("  aynı taşın farklı yüzeyleri (C cilalı, H honlu, AH):");
        foreach (var s in stones)
            foreach (var t in stones)
                if (t.ID > s.ID && string.Equals(stock[t.ID].Name.Trim(), stock[s.ID].Name.Trim(), StringComparison.OrdinalIgnoreCase))
                    log($"    #{s.ID} {stock[s.ID].Code} ↔ #{t.ID} {stock[t.ID].Code} {stock[s.ID].Name.Trim()}: {D(s.ID, t.ID):F1}");
        if (family.Count > 0)
        {
            family.Sort();
            log($"  aynı taşın yüzeyleri arası mesafe: en az {family[0]:F1}, ortanca {family[family.Count / 2]:F1}, en çok {family[^1]:F1} ({family.Count} çift)");
        }
        log("");
    }

    private static int[,] CopyIds(int R, int C)
    {
        var ids = new int[R, C];
        for (int i = 0; i < R; i++)
            for (int j = 0; j < C; j++) ids[i, j] = drl.dat[i, j, 3];
        return ids;
    }

    private static string PaletteKey() =>
        string.Join(";", MosaicData.arMB.SelectMany(l => l).Select(c => $"{c.ID}:{c.numOfPixel}:{c.u}"));

    private static bool Same(byte[,,] a, byte[,,] b) =>
        a.Length == b.Length && a.Cast<byte>().SequenceEqual(b.Cast<byte>());

    private static bool Same(int[,] a, int[,] b) =>
        a.Length == b.Length && a.Cast<int>().SequenceEqual(b.Cast<int>());

    private static void SavePanels(string file, int R, int C, byte[,,] before, byte[,,] after,
        params (string Title, byte[,,] Data)[] panels)
    {
        int cell = Math.Max(2, 420 / Math.Max(R, C));
        const int gap = 14, header = 30;
        int count = panels.Length + 1;
        int pw = C * cell;
        int w = count * pw + (count - 1) * gap;
        int h = header + R * cell;
        using var bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(bmp))
        {
            canvas.Clear(SKColors.White);
            using var paint = new SKPaint { IsAntialias = false };
            using var textPaint = new SKPaint { IsAntialias = true, Color = new SKColor(30, 30, 30) };
            using var font = new SKFont(SKTypeface.Default, 18);
            for (int p = 0; p < panels.Length; p++)
            {
                int ox = p * (pw + gap);
                canvas.DrawText(panels[p].Title, ox + 4, 21, font, textPaint);
                var data = panels[p].Data;
                for (int i = 0; i < R; i++)
                    for (int j = 0; j < C; j++)
                    {
                        paint.Color = new SKColor(data[i, j, 2], data[i, j, 1], data[i, j, 0]);
                        canvas.DrawRect(ox + j * cell, header + i * cell, cell, cell, paint);
                    }
            }
            // Changed stones in colour, unchanged ones faded to grey.
            int dx = panels.Length * (pw + gap);
            canvas.DrawText("Değişen taşlar", dx + 4, 21, font, textPaint);
            for (int i = 0; i < R; i++)
                for (int j = 0; j < C; j++)
                {
                    bool changed = before[i, j, 0] != after[i, j, 0] || before[i, j, 1] != after[i, j, 1] || before[i, j, 2] != after[i, j, 2];
                    if (changed) paint.Color = new SKColor(after[i, j, 2], after[i, j, 1], after[i, j, 0]);
                    else
                    {
                        byte grey = (byte)(200 + (before[i, j, 2] + before[i, j, 1] + before[i, j, 0]) / 3 / 5);
                        paint.Color = new SKColor(grey, grey, grey);
                    }
                    canvas.DrawRect(dx + j * cell, header + i * cell, cell, cell, paint);
                }
        }
        using var img = SKImage.FromBitmap(bmp);
        using var encoded = img.Encode(SKEncodedImageFormat.Png, 100);
        using var fs = File.Create(file);
        encoded.SaveTo(fs);
    }
}
