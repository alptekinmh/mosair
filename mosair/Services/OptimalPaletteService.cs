using System;
using System.Collections.Generic;
using mosair.Models;

namespace mosair.Services
{
    public sealed class OptimalPaletteResult
    {
        // Indexed by stone count k (1..M); index 0 unused.
        public double[] MeanByK = Array.Empty<double>();
        public double[] P95ByK = Array.Empty<double>();
        public double[] P99ByK = Array.Empty<double>();
        // Share of strong source edges whose two sides still get different stones.
        public double[] EdgeKeptByK = Array.Empty<double>();
        public int CandidateCount;
        public int KOptimal;
        public int KKnee;
        public int KThreshold;
        // Stone count at which the first protected stone had to be removed (0 = never).
        public int KProtected;
        // Catalog indices (into the candidate list) in removal order; the last KOptimal survivors form the palette.
        public List<int> RemovalOrder = new();
        public List<int> Selected = new();
    }

    public static class OptimalPaletteService
    {
        public const double EdgeWeight = 2.0;
        // Lightness differences count this many times more than color differences when matching stones:
        // when the catalog cannot reproduce a hue, keeping light/dark structure is what keeps patterns readable.
        public static double LightnessWeight = 3.0;
        public const double MeanTolerance = 1.0;
        public const double P95Tolerance = 2.3;
        public const double P99Tolerance = 5.0;
        public const double ProtectDeltaE = 10.0;
        public const double MinProtectedShare = 0.001;
        public const int MinProtectedPixels = 4;
        public const double EdgeContrast = 12.0;
        public const double EdgeLambda = 1.0;
        public const double EdgeLossTolerance = 0.02;
        private const int HistBins = 2000;
        private const double HistStep = 0.1;

        // src is BGR, as produced by ImageService.ToByteArray.
        public static OptimalPaletteResult Analyze(byte[,,] src, int R, int C, List<rgb> candidates,
            Action<int>? onProgress = null, GamutMapper? gamut = null)
        {
            int M = candidates.Count;
            var res = new OptimalPaletteResult { CandidateCount = M };
            if (M == 0) return res;

            double[] pixL = new double[R * C];
            var labCache = new Dictionary<int, (double L, double A, double B)>();
            var colorIndex = new Dictionary<int, int>();
            var uL = new List<double>(); var uA = new List<double>(); var uB = new List<double>();
            var uWeight = new List<double>(); var uCount = new List<int>();
            int[] pixU = new int[R * C];

            for (int i = 0; i < R; i++)
                for (int j = 0; j < C; j++)
                {
                    if (j == 0) WorkCancellation.Check();   // cancel button: once per row
                    byte b = src[i, j, 0], g = src[i, j, 1], r = src[i, j, 2];
                    int key = (r << 16) | (g << 8) | b;
                    if (!labCache.TryGetValue(key, out var lab))
                    {
                        lab = ColorMatcher.RgbToLab(r, g, b);
                        if (gamut != null) lab = gamut.Map(lab);
                        labCache[key] = lab;
                    }
                    pixL[i * C + j] = lab.L;
                    if (!colorIndex.TryGetValue(key, out int u))
                    {
                        u = uL.Count;
                        colorIndex[key] = u;
                        uL.Add(lab.L); uA.Add(lab.A); uB.Add(lab.B);
                        uWeight.Add(0); uCount.Add(0);
                    }
                    pixU[i * C + j] = u;
                }

            double[] edge = SobelMagnitude(pixL, R, C);
            double edgeRef = Percentile(edge, 0.98);
            if (edgeRef <= 1e-9) edgeRef = 1;
            for (int p = 0; p < R * C; p++)
            {
                int u = pixU[p];
                uWeight[u] += 1.0 + EdgeWeight * Math.Min(1.0, edge[p] / edgeRef);
                uCount[u]++;
            }

            int U = uL.Count;
            double[] cL = new double[M], cA = new double[M], cB = new double[M];
            for (int m = 0; m < M; m++)
            {
                var lab = ColorMatcher.RgbToLab(candidates[m].r, candidates[m].g, candidates[m].b);
                cL[m] = lab.L; cA[m] = lab.A; cB[m] = lab.B;
            }

            float[] dist = new float[U * M];
            int[] order = new int[U * M];
            System.Threading.Tasks.Parallel.For(0, U, u =>
            {
                WorkCancellation.Check();
                int baseIdx = u * M;
                var keys = new float[M];
                var idx = new int[M];
                for (int m = 0; m < M; m++)
                {
                    double dl = (uL[u] - cL[m]) * LightnessWeight, da = uA[u] - cA[m], db = uB[u] - cB[m];
                    float d = (float)Math.Sqrt(dl * dl + da * da + db * db);
                    dist[baseIdx + m] = d;
                    keys[m] = d;
                    idx[m] = m;
                }
                Array.Sort(keys, idx);
                Array.Copy(idx, 0, order, baseIdx, M);
            });

            bool[] alive = new bool[M];
            Array.Fill(alive, true);
            int[] best = new int[U];   // position in order[] of current best alive candidate
            int[] next = new int[U];   // position of second-best alive candidate (M if none)
            for (int u = 0; u < U; u++) { best[u] = 0; next[u] = M > 1 ? 1 : M; }

            double totalWeight = 0; long totalCount = 0;
            for (int u = 0; u < U; u++) { totalWeight += uWeight[u]; totalCount += uCount[u]; }

            // Strong source edges: neighbouring pixels that clearly differ and should stay different stones.
            var edgeA = new List<int>(); var edgeB = new List<int>(); var edgePenalty = new List<double>();
            void TryEdge(int p, int q)
            {
                int a = pixU[p], b = pixU[q];
                if (a == b) return;
                double dl = uL[a] - uL[b], da = uA[a] - uA[b], db = uB[a] - uB[b];
                double d = Math.Sqrt(dl * dl + da * da + db * db);
                if (d < EdgeContrast) return;
                edgeA.Add(a); edgeB.Add(b); edgePenalty.Add(EdgeLambda * d * d);
            }
            for (int i = 0; i < R; i++)
                for (int j = 0; j < C; j++)
                {
                    if (j == 0) WorkCancellation.Check();
                    if (j + 1 < C) TryEdge(i * C + j, i * C + j + 1);
                    if (i + 1 < R) TryEdge(i * C + j, (i + 1) * C + j);
                }
            int E = edgeA.Count;

            res.MeanByK = new double[M + 1];
            res.P95ByK = new double[M + 1];
            res.P99ByK = new double[M + 1];
            res.EdgeKeptByK = new double[M + 1];
            RecordCurve(res, M, U, M, dist, order, best, uWeight, uCount, totalWeight, totalCount);
            res.EdgeKeptByK[M] = EdgeKept(edgeA, edgeB, order, best, M);

            double[] cost = new double[M];
            int[] owned = new int[M];
            double[] localIncrease = new double[M];
            bool[] isProtected = new bool[M];
            int minProtectPixels = Math.Max(MinProtectedPixels, (int)Math.Ceiling(totalCount * MinProtectedShare));
            for (int k = M; k > 1; k--)
            {
                WorkCancellation.Check();
                Array.Clear(cost); Array.Clear(owned); Array.Clear(localIncrease);
                for (int u = 0; u < U; u++)
                {
                    int bm = order[u * M + best[u]];
                    owned[bm] += uCount[u];
                    if (next[u] >= M) { cost[bm] = double.MaxValue; continue; }
                    int nm = order[u * M + next[u]];
                    double dn = dist[u * M + nm], db = dist[u * M + bm];
                    localIncrease[bm] += uCount[u] * (dn - db);
                    if (cost[bm] != double.MaxValue)
                        cost[bm] += uWeight[u] * (dn * dn - db * db);
                }

                // Removing a stone that would merge a strong edge into a single stone also costs the edge.
                for (int e = 0; e < E; e++)
                {
                    int a = edgeA[e], b = edgeB[e];
                    int sa = order[a * M + best[a]], sb = order[b * M + best[b]];
                    if (sa == sb) continue;
                    if (next[a] < M && order[a * M + next[a]] == sb && cost[sa] != double.MaxValue)
                        cost[sa] += edgePenalty[e];
                    if (next[b] < M && order[b * M + next[b]] == sa && cost[sb] != double.MaxValue)
                        cost[sb] += edgePenalty[e];
                }

                // A stone is protected when it covers a visible area that would shift by a clearly visible amount.
                for (int m = 0; m < M; m++)
                    isProtected[m] = alive[m] && owned[m] >= minProtectPixels &&
                                     localIncrease[m] / owned[m] > ProtectDeltaE;

                int victim = -1;
                for (int m = 0; m < M; m++)
                {
                    if (!alive[m]) continue;
                    if (victim < 0) { victim = m; continue; }
                    if (isProtected[m] != isProtected[victim])
                    {
                        if (!isProtected[m]) victim = m;
                        continue;
                    }
                    if (cost[m] < cost[victim] || (cost[m] == cost[victim] && owned[m] < owned[victim]))
                        victim = m;
                }
                if (isProtected[victim] && res.KProtected == 0)
                    res.KProtected = k;
                alive[victim] = false;
                res.RemovalOrder.Add(victim);

                for (int u = 0; u < U; u++)
                {
                    int baseIdx = u * M;
                    if (order[baseIdx + best[u]] == victim)
                    {
                        best[u] = next[u];
                        next[u] = AdvanceAlive(order, baseIdx, best[u] + 1, M, alive);
                    }
                    else if (next[u] < M && order[baseIdx + next[u]] == victim)
                    {
                        next[u] = AdvanceAlive(order, baseIdx, next[u] + 1, M, alive);
                    }
                }

                RecordCurve(res, k - 1, U, M, dist, order, best, uWeight, uCount, totalWeight, totalCount);
                res.EdgeKeptByK[k - 1] = EdgeKept(edgeA, edgeB, order, best, M);
                onProgress?.Invoke((int)(100.0 * (M - k + 1) / M));
            }

            for (int m = 0; m < M; m++)
                if (alive[m]) res.RemovalOrder.Add(m);

            SelectK(res, M);
            for (int i = M - res.KOptimal; i < M; i++)
                res.Selected.Add(res.RemovalOrder[i]);
            return res;
        }

        private static double EdgeKept(List<int> edgeA, List<int> edgeB, int[] order, int[] best, int M)
        {
            if (edgeA.Count == 0) return 1.0;
            int kept = 0;
            for (int e = 0; e < edgeA.Count; e++)
            {
                int a = edgeA[e], b = edgeB[e];
                if (order[a * M + best[a]] != order[b * M + best[b]]) kept++;
            }
            return kept / (double)edgeA.Count;
        }

        private static int AdvanceAlive(int[] order, int baseIdx, int from, int M, bool[] alive)
        {
            int p = from;
            while (p < M && !alive[order[baseIdx + p]]) p++;
            return p;
        }

        private static void RecordCurve(OptimalPaletteResult res, int k, int U, int M, float[] dist, int[] order,
            int[] best, List<double> uWeight, List<int> uCount, double totalWeight, long totalCount)
        {
            double sum = 0;
            var hist = new long[HistBins];
            for (int u = 0; u < U; u++)
            {
                double d = dist[u * M + order[u * M + best[u]]];
                sum += uWeight[u] * d;
                int bin = (int)(d / HistStep);
                if (bin >= HistBins) bin = HistBins - 1;
                hist[bin] += uCount[u];
            }
            res.MeanByK[k] = sum / totalWeight;

            res.P95ByK[k] = HistPercentile(hist, totalCount, 0.95);
            res.P99ByK[k] = HistPercentile(hist, totalCount, 0.99);
        }

        private static double HistPercentile(long[] hist, long totalCount, double q)
        {
            long target = (long)Math.Ceiling(totalCount * q), acc = 0;
            for (int bin = 0; bin < HistBins; bin++)
            {
                acc += hist[bin];
                if (acc >= target) return (bin + 1) * HistStep;
            }
            return HistBins * HistStep;
        }

        private static void SelectK(OptimalPaletteResult res, int M)
        {
            double eMin = res.MeanByK[M], p95Min = res.P95ByK[M], p99Min = res.P99ByK[M];

            res.KThreshold = M;
            for (int k = 1; k <= M; k++)
                if (res.MeanByK[k] <= eMin + MeanTolerance && res.P95ByK[k] <= p95Min + P95Tolerance
                    && res.P99ByK[k] <= p99Min + P99Tolerance
                    && res.EdgeKeptByK[k] >= res.EdgeKeptByK[M] - EdgeLossTolerance)
                {
                    res.KThreshold = k;
                    break;
                }

            // Kneedle on the normalized decreasing curve: knee maximizes (1 - x) - y.
            double eMax = res.MeanByK[1];
            res.KKnee = M;
            if (M > 1 && eMax > eMin)
            {
                double bestScore = double.MinValue;
                for (int k = 1; k <= M; k++)
                {
                    double x = (k - 1) / (double)(M - 1);
                    double y = (res.MeanByK[k] - eMin) / (eMax - eMin);
                    double score = 1 - x - y;
                    if (score > bestScore) { bestScore = score; res.KKnee = k; }
                }
            }

            res.KOptimal = Math.Max(res.KThreshold, res.KProtected);
        }

        internal static double[] SobelMagnitude(double[] L, int R, int C)
        {
            var mag = new double[R * C];
            for (int i = 0; i < R; i++)
                for (int j = 0; j < C; j++)
                {
                    double P(int y, int x) => L[Math.Clamp(y, 0, R - 1) * C + Math.Clamp(x, 0, C - 1)];
                    double gx = -P(i - 1, j - 1) - 2 * P(i, j - 1) - P(i + 1, j - 1)
                                + P(i - 1, j + 1) + 2 * P(i, j + 1) + P(i + 1, j + 1);
                    double gy = -P(i - 1, j - 1) - 2 * P(i - 1, j) - P(i - 1, j + 1)
                                + P(i + 1, j - 1) + 2 * P(i + 1, j) + P(i + 1, j + 1);
                    mag[i * C + j] = Math.Sqrt(gx * gx + gy * gy);
                }
            return mag;
        }

        internal static double Percentile(double[] values, double q)
        {
            var copy = (double[])values.Clone();
            Array.Sort(copy);
            int idx = (int)Math.Clamp(Math.Round(q * (copy.Length - 1)), 0, copy.Length - 1);
            return copy[idx];
        }
    }
}
