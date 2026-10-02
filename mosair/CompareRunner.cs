using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using mosair.Models;
using mosair.Services;
using SkiaSharp;

namespace mosair;

// Usage: mosair --compare <outDir> <widthCm> <image1> [image2 ...]
// Panels: original | WPF-style RunM3 | mosair RunM3 | Optimum.
public static class CompareRunner
{
    private sealed record Variant(string Title, byte[,,] Data, MosaicQuality Q, double Seconds);

    public static int Run(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("usage: --compare <outDir> <widthCm> <image...>");
            return 1;
        }
        string outDir = args[1];
        double widthCm = double.Parse(args[2], CultureInfo.InvariantCulture);
        Directory.CreateDirectory(outDir);
        ColorCatalogService.LoadDefaultCatalog();
        string? lw = Environment.GetEnvironmentVariable("MOSAIR_LW");
        if (!string.IsNullOrEmpty(lw))
            OptimalPaletteService.LightnessWeight = double.Parse(lw, CultureInfo.InvariantCulture);

        var report = new StringBuilder();
        void Log(string s) { Console.WriteLine(s); report.AppendLine(s); }
        Log($"catalog active stones={MosaicData.arRGB.Count} widthCm={widthCm.ToString(CultureInfo.InvariantCulture)}");
        Log($"optimum lightness weight={OptimalPaletteService.LightnessWeight.ToString(CultureInfo.InvariantCulture)}");
        Log("ΔE = perceptual color difference vs original (lower is better); edgesKept = strong source edges still separated (higher is better); Lcorr = light/dark structure correlation (closer to 1 is better)");

        var summary = new StringBuilder();
        summary.AppendLine("image\tstones W/M/O\tmeanΔE W/M/O\tedgesKept W/M/O\tLcorr W/M/O");
        var sums = new double[4, 3];
        int n = 0;

        for (int a = 3; a < args.Length; a++)
        {
            string path = args[a];
            string name = Path.GetFileNameWithoutExtension(path);
            MosaicEngine.Reset();
            if (MosaicEngine.LoadImage(path) == null) { Log($"[{name}] load failed"); continue; }
            MosaicEngine.CalculateDimensions(widthCm);
            int C = (int)MosaicEngine.width, R = (int)MosaicEngine.height;
            byte[,,] src = MosaicEngine.GetSourceStoneData();

            int totalPixels = R * C;
            int ew = (int)(0.1 * totalPixels) / 10;
            const int rgbInc = 10;
            int steps = (int)Math.Ceiling(256.0 / rgbInc);
            int target = Math.Clamp(Math.Max(2, ew * 10), 2, Math.Max(2, totalPixels));
            if (target >= steps * steps * steps) target = steps * steps * steps - 1;

            Variant RunOld(string title, bool wpf)
            {
                MosaicEngine.Reset();
                MosaicEngine.LoadImage(path);
                MosaicEngine.CalculateDimensions(widthCm);
                MosaicEngine.WpfStyleRemoval = wpf;
                var sw = Stopwatch.StartNew();
                MosaicEngine.RunM3(target, rgbInc, false, false, InterpolationMethod.Area, null, prepareTextures: false);
                sw.Stop();
                MosaicEngine.WpfStyleRemoval = false;
                var d = (byte[,,])MosaicData.dataM3.Clone();
                return new Variant(title, d, MosaicMetrics.Evaluate(src, d, R, C), sw.Elapsed.TotalSeconds);
            }

            var wpf = RunOld("WPF", true);
            var old = RunOld("mosair", false);

            MosaicEngine.Reset();
            MosaicEngine.LoadImage(path);
            MosaicEngine.CalculateDimensions(widthCm);
            var swN = Stopwatch.StartNew();
            MosaicEngine.RunOptimal(InterpolationMethod.Area, null, prepareTextures: false);
            swN.Stop();
            var res = MosaicEngine.LastOptimalResult!;
            var optData = (byte[,,])MosaicData.dataM3.Clone();
            var opt = new Variant("Optimum", optData, MosaicMetrics.Evaluate(src, optData, R, C), swN.Elapsed.TotalSeconds);

            Log($"[{name}] {C}x{R} = {totalPixels} stones (rgbM={target})");
            foreach (var v in new[] { wpf, old, opt })
                Log($"  {v.Title,-8} {v.Q}  time={v.Seconds:F2}s");
            Log($"  optimum: suggested k={res.KOptimal} (knee {res.KKnee}, threshold {res.KThreshold})");

            var vs = new[] { wpf, old, opt };
            for (int i = 0; i < 3; i++)
            {
                sums[0, i] += vs[i].Q.Stones;
                sums[1, i] += vs[i].Q.MeanDeltaE;
                sums[2, i] += vs[i].Q.EdgeKept;
                sums[3, i] += vs[i].Q.LightnessCorr;
            }
            n++;
            summary.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"{name}\t{wpf.Q.Stones}/{old.Q.Stones}/{opt.Q.Stones}\t{wpf.Q.MeanDeltaE:F2}/{old.Q.MeanDeltaE:F2}/{opt.Q.MeanDeltaE:F2}\t{wpf.Q.EdgeKept * 100:F1}/{old.Q.EdgeKept * 100:F1}/{opt.Q.EdgeKept * 100:F1}\t{wpf.Q.LightnessCorr:F3}/{old.Q.LightnessCorr:F3}/{opt.Q.LightnessCorr:F3}"));

            SavePanels(Path.Combine(outDir, $"{name}_karsilastirma.png"), R, C,
                ("Orijinal", src), ($"WPF · {wpf.Q.Stones} taş", wpf.Data),
                ($"mosair · {old.Q.Stones} taş", old.Data), ($"Optimum · {opt.Q.Stones} taş", opt.Data));
        }

        if (n > 0)
            summary.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"AVERAGE\t{sums[0, 0] / n:F1}/{sums[0, 1] / n:F1}/{sums[0, 2] / n:F1}\t{sums[1, 0] / n:F2}/{sums[1, 1] / n:F2}/{sums[1, 2] / n:F2}\t{sums[2, 0] / n * 100:F1}/{sums[2, 1] / n * 100:F1}/{sums[2, 2] / n * 100:F1}\t{sums[3, 0] / n:F3}/{sums[3, 1] / n:F3}/{sums[3, 2] / n:F3}"));

        Log("");
        Log("SUMMARY");
        Log(summary.ToString());
        File.WriteAllText(Path.Combine(outDir, "rapor.txt"), report.ToString(), new UTF8Encoding(true));
        return 0;
    }

    private static void SavePanels(string file, int R, int C, params (string Title, byte[,,] Data)[] panels)
    {
        int cell = Math.Max(2, 420 / Math.Max(R, C));
        const int gap = 14, header = 30;
        int pw = C * cell;
        int w = panels.Length * pw + (panels.Length - 1) * gap;
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
        }
        using var img = SKImage.FromBitmap(bmp);
        using var encoded = img.Encode(SKEncodedImageFormat.Png, 100);
        using var fs = File.Create(file);
        encoded.SaveTo(fs);
    }
}
