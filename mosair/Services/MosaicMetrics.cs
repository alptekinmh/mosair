using System;
using System.Collections.Generic;

namespace mosair.Services
{
    public readonly record struct MosaicQuality(double MeanDeltaE, double P95DeltaE, double EdgeMeanDeltaE, int Stones,
        double ChromaMeanDeltaE = 0, int ChromaPixels = 0, double EdgeKept = 1, double LightnessCorr = 1)
    {
        public override string ToString() =>
            $"stones={Stones} meanΔE={MeanDeltaE:F2} p95ΔE={P95DeltaE:F2} edgeΔE={EdgeMeanDeltaE:F2} " +
            $"chromaΔE={ChromaMeanDeltaE:F2}({ChromaPixels}px) edgesKept={EdgeKept:P1} Lcorr={LightnessCorr:F3}";
    }

    public static class MosaicMetrics
    {
        // Both arrays are BGR. Edge pixels = top 10% luminance gradient of the source.
        public static MosaicQuality Evaluate(byte[,,] src, byte[,,] mosaic, int R, int C)
        {
            var labCache = new Dictionary<int, (double L, double A, double B)>();
            (double L, double A, double B) Lab(byte r, byte g, byte b)
            {
                int key = (r << 16) | (g << 8) | b;
                if (!labCache.TryGetValue(key, out var v))
                {
                    v = ColorMatcher.RgbToLab(r, g, b);
                    labCache[key] = v;
                }
                return v;
            }

            int n = R * C;
            var de = new double[n];
            var srcL = new double[n];
            var mosL = new double[n];
            var used = new HashSet<int>();
            double chromaSum = 0; int chromaN = 0;
            for (int i = 0; i < R; i++)
                for (int j = 0; j < C; j++)
                {
                    var s = Lab(src[i, j, 2], src[i, j, 1], src[i, j, 0]);
                    var m = Lab(mosaic[i, j, 2], mosaic[i, j, 1], mosaic[i, j, 0]);
                    double dl = s.L - m.L, da = s.A - m.A, db = s.B - m.B;
                    de[i * C + j] = Math.Sqrt(dl * dl + da * da + db * db);
                    srcL[i * C + j] = s.L;
                    mosL[i * C + j] = m.L;
                    if (Math.Sqrt(s.A * s.A + s.B * s.B) > 30) { chromaSum += de[i * C + j]; chromaN++; }
                    used.Add((mosaic[i, j, 2] << 16) | (mosaic[i, j, 1] << 8) | mosaic[i, j, 0]);
                }

            double mean = 0;
            foreach (var d in de) mean += d;
            mean /= n;

            var sorted = (double[])de.Clone();
            Array.Sort(sorted);
            double p95 = sorted[Math.Min(n - 1, (int)Math.Ceiling(0.95 * n) - 1)];

            var grad = new double[n];
            for (int i = 0; i < R; i++)
                for (int j = 0; j < C; j++)
                {
                    double gx = srcL[i * C + Math.Min(C - 1, j + 1)] - srcL[i * C + Math.Max(0, j - 1)];
                    double gy = srcL[Math.Min(R - 1, i + 1) * C + j] - srcL[Math.Max(0, i - 1) * C + j];
                    grad[i * C + j] = Math.Sqrt(gx * gx + gy * gy);
                }
            var gSorted = (double[])grad.Clone();
            Array.Sort(gSorted);
            double gThresh = gSorted[(int)(0.9 * (n - 1))];
            double edgeSum = 0; int edgeN = 0;
            for (int p = 0; p < n; p++)
                if (grad[p] >= gThresh && grad[p] > 0) { edgeSum += de[p]; edgeN++; }

            int strong = 0, kept = 0;
            void Pair(int i1, int j1, int i2, int j2)
            {
                var a = Lab(src[i1, j1, 2], src[i1, j1, 1], src[i1, j1, 0]);
                var b = Lab(src[i2, j2, 2], src[i2, j2, 1], src[i2, j2, 0]);
                double dl = a.L - b.L, da = a.A - b.A, db = a.B - b.B;
                if (Math.Sqrt(dl * dl + da * da + db * db) < OptimalPaletteService.EdgeContrast) return;
                strong++;
                if (mosaic[i1, j1, 0] != mosaic[i2, j2, 0] || mosaic[i1, j1, 1] != mosaic[i2, j2, 1] ||
                    mosaic[i1, j1, 2] != mosaic[i2, j2, 2]) kept++;
            }
            for (int i = 0; i < R; i++)
                for (int j = 0; j < C; j++)
                {
                    if (j + 1 < C) Pair(i, j, i, j + 1);
                    if (i + 1 < R) Pair(i, j, i + 1, j);
                }

            return new MosaicQuality(mean, p95, edgeN > 0 ? edgeSum / edgeN : 0, used.Count,
                chromaN > 0 ? chromaSum / chromaN : 0, chromaN, strong > 0 ? kept / (double)strong : 1,
                Pearson(srcL, mosL));
        }

        private static double Pearson(double[] x, double[] y)
        {
            double mx = 0, my = 0;
            for (int i = 0; i < x.Length; i++) { mx += x[i]; my += y[i]; }
            mx /= x.Length; my /= y.Length;
            double sxy = 0, sxx = 0, syy = 0;
            for (int i = 0; i < x.Length; i++)
            {
                double dx = x[i] - mx, dy = y[i] - my;
                sxy += dx * dy; sxx += dx * dx; syy += dy * dy;
            }
            return sxx > 0 && syy > 0 ? sxy / Math.Sqrt(sxx * syy) : 1;
        }
    }
}
