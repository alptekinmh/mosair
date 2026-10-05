using System;
using System.Collections.Generic;
using mosair.Models;

namespace mosair.Services
{
    public sealed class StockAwareOptions
    {
        // At most this many stone types that the mosaic did not use before may be brought in.
        public int MaxExtraStones = 2;
        // Colour difference used to judge substitutes: plain Lab distance (ΔE76) when 1. Optimum weighs lightness
        // ×3 to pick a palette, but for a substitute that would let a dark red stand in for black.
        public double LightnessWeight = 1.0;
        // Largest stone-to-stone colour difference (ΔE) for a substitute before the search has to widen.
        public double SimilarityTolerance = 10.0;
        // A move to another finish of the same stone (same name in the sheet) counts as this much cheaper.
        public double SameFamilyFactor = 0.85;
        // New stone types are only used when they make the total change at least this much smaller
        // (or when the palette alone cannot fit the stock).
        public double NewStoneGain = 0.8;
        // Substitute candidates considered per over-used stone (the closest ones), from the palette and from
        // the other stones separately.
        public int MaxCandidates = 8;
        // Source colours closer than this (ΔE) are moved together as one group; pixels are still ranked one by one.
        public double GroupStep = 2.0;
        // Stones this close (ΔE) to an over-used stone are practically the same colour ("twins", like the 48/49
        // case): they may always be brought in, without the NewStoneGain test.
        public double TwinTolerance = 3.0;
        // Stones used fewer times than this in the result are dropped (their pixels go to similar stones):
        // a handful of stones is not worth a colour change for the robot. 0 or 1 = off.
        public int MinUsage = 0;

        // Default minimum for a mosaic of the given size: 10 stones, or 0.05% of all stones on large mosaics
        // (78×78 → 10, 16 m² ≈ 111,000 stones → 56).
        public static int MinUsageFor(int totalStones) => Math.Max(10, (int)Math.Ceiling(totalStones * 0.0005));
    }

    public sealed class StockMove
    {
        public int FromId, ToId, Count;
    }

    public sealed class StockAwareResult
    {
        public bool Changed;
        // Pool index per pixel, row-major (i * C + j).
        public int[] Assignment = Array.Empty<int>();
        // Keyed by stone ID.
        public Dictionary<int, int> CountBefore = new(), CountAfter = new(), Capacity = new();
        public List<int> AddedIds = new();
        // Stones still used beyond their stock after the best possible rearrangement.
        public List<int> ShortIds = new();
        // Stones the mosaic uses that have no row in the stock sheet, so their stock could not be checked.
        public List<int> UnknownIds = new();
        // Stones dropped because they were used fewer than MinUsage times, and ones that could not be dropped.
        public List<int> SmallRemovedIds = new(), SmallKeptIds = new();
        public int MinUsage;
        public List<StockMove> Moves = new();
        public int MovedPixels;
        // Average extra colour distance of the moved pixels (Optimum metric).
        public double MeanShift;
        // How far the search had to widen: 0 = similar stones within the tolerance, 1 = 2×, 2 = 4×,
        // 3 = any stone with stock, 4 = any stone and more new stone types than MaxExtraStones.
        public int Level;
    }

    // Rearranges a finished mosaic so that no stone is used beyond its stock, with the smallest visible change.
    // Pixels of an over-used stone move to similar stones that still have stock. The amounts are chosen by a
    // min-cost flow (exact minimum of the total weighted colour change); flat areas move before edges, and within a
    // flat area the pixels whose surroundings are already closest to the substitute move first.
    public static class StockAwareAssigner
    {
        private const int Unknown = -1;

        // Stock limits plus the minimum-usage rule. A min-cost flow cannot price "using a stone at all", so the
        // rule is applied in rounds: every stone left with 0 < count < MinUsage gets capacity 0 and its pixels
        // are moved like any other excess; repeated until no new small stone appears. With no stone over its
        // stock and none under the minimum, the mosaic is returned unchanged.
        public static StockAwareResult SolveWithMinimum(byte[,,] src, int R, int C, int[] assign, List<rgb> pool,
            Func<int, int?> capacityOfId, Func<int, string?> familyOfId, StockAwareOptions opt, GamutMapper? gamut)
        {
            int N = R * C, M = pool.Count;
            var forced = new HashSet<int>(); // stone IDs that must give up all their pixels
            int[] current = assign;
            int level = 0;
            for (int round = 0; round < 64; round++)
            {
                var r = Solve(src, R, C, current, pool, id => forced.Contains(id) ? 0 : capacityOfId(id),
                    familyOfId, opt, gamut);
                level = Math.Max(level, r.Level);
                current = r.Assignment;
                if (opt.MinUsage <= 1) break;
                var counts = new int[M];
                for (int p = 0; p < N; p++) counts[current[p]]++;
                bool added = false;
                for (int m = 0; m < M; m++)
                    if (counts[m] > 0 && counts[m] < opt.MinUsage && forced.Add(pool[m].ID)) added = true;
                if (!added) break;
            }

            // Report the overall change (original assignment → final), however many rounds it took.
            var res = new StockAwareResult { Assignment = current, Level = level, MinUsage = opt.MinUsage };
            int[] before = new int[M], after = new int[M];
            for (int p = 0; p < N; p++) { before[assign[p]]++; after[current[p]]++; }
            var labCache = new Dictionary<int, (double L, double A, double B)>();
            var poolLab = new (double L, double A, double B)[M];
            for (int m = 0; m < M; m++) poolLab[m] = ColorMatcher.RgbToLab(pool[m].r, pool[m].g, pool[m].b);
            double Dist((double L, double A, double B) x, (double L, double A, double B) s)
            {
                double dl = (x.L - s.L) * opt.LightnessWeight, da = x.A - s.A, db = x.B - s.B;
                return Math.Sqrt(dl * dl + da * da + db * db);
            }
            var moves = new Dictionary<(int s, int t), int>();
            double shift = 0;
            for (int i = 0; i < R; i++)
                for (int j = 0; j < C; j++)
                {
                    int p = i * C + j, s = assign[p], t = current[p];
                    if (s == t) continue;
                    byte b = src[i, j, 0], g = src[i, j, 1], rr = src[i, j, 2];
                    int key = (rr << 16) | (g << 8) | b;
                    if (!labCache.TryGetValue(key, out var lab))
                    {
                        lab = ColorMatcher.RgbToLab(rr, g, b);
                        if (gamut != null) lab = gamut.Map(lab);
                        labCache[key] = lab;
                    }
                    shift += Dist(lab, poolLab[t]) - Dist(lab, poolLab[s]);
                    moves[(s, t)] = (moves.TryGetValue((s, t), out int n) ? n : 0) + 1;
                    res.MovedPixels++;
                }
            res.Changed = res.MovedPixels > 0;
            res.MeanShift = res.MovedPixels > 0 ? shift / res.MovedPixels : 0;
            foreach (var kv in moves)
                res.Moves.Add(new StockMove { FromId = pool[kv.Key.s].ID, ToId = pool[kv.Key.t].ID, Count = kv.Value });
            res.Moves.Sort((x, y) => x.FromId != y.FromId ? x.FromId.CompareTo(y.FromId) : y.Count.CompareTo(x.Count));
            for (int m = 0; m < M; m++)
            {
                int id = pool[m].ID;
                int? cap = capacityOfId(id);
                if (cap != null) res.Capacity[id] = Math.Max(0, cap.Value);
                if (before[m] > 0) res.CountBefore[id] = before[m];
                if (after[m] > 0) res.CountAfter[id] = after[m];
                if (after[m] > 0 && before[m] == 0) res.AddedIds.Add(id);
                if (cap != null && after[m] > Math.Max(0, cap.Value)) res.ShortIds.Add(id);
                if (cap == null && after[m] > 0) res.UnknownIds.Add(id);
                // Only stones the original mosaic used; a substitute that briefly appeared and was dropped again
                // is not worth reporting.
                if (forced.Contains(id) && before[m] > 0 && !(cap != null && after[m] > Math.Max(0, cap.Value)))
                    (after[m] == 0 ? res.SmallRemovedIds : res.SmallKeptIds).Add(id);
            }
            return res;
        }

        // capacityOfId: stones available (pieces) for a stone ID, or null when the stone has no stock data;
        // such stones keep their pixels and are never used as substitutes.
        // familyOfId: stone name shared by the finishes of the same stone, or null.
        public static StockAwareResult Solve(byte[,,] src, int R, int C, int[] assign, List<rgb> pool,
            Func<int, int?> capacityOfId, Func<int, string?> familyOfId, StockAwareOptions opt, GamutMapper? gamut)
        {
            int N = R * C, M = pool.Count;
            double W = opt.LightnessWeight;
            var res = new StockAwareResult();

            int[] count = new int[M];
            for (int p = 0; p < N; p++) count[assign[p]]++;
            int[] cap = new int[M];
            for (int m = 0; m < M; m++)
            {
                int? c = capacityOfId(pool[m].ID);
                if (c != null && c.Value < 0) c = 0; // a negative "Bizdeki" means nothing on hand
                cap[m] = c ?? Unknown;
                if (c != null) res.Capacity[pool[m].ID] = c.Value;
                if (count[m] > 0) res.CountBefore[pool[m].ID] = count[m];
            }

            var over = new List<int>();
            for (int m = 0; m < M; m++)
                if (cap[m] != Unknown && count[m] > cap[m]) over.Add(m);
            for (int m = 0; m < M; m++)
                if (count[m] > 0 && cap[m] == Unknown) res.UnknownIds.Add(pool[m].ID);
            if (over.Count == 0)
            {
                res.Assignment = assign;
                res.CountAfter = new Dictionary<int, int>(res.CountBefore);
                return res;
            }

            // Source colours in Lab (same conversion and gamut handling as Optimum), edge weights, 3×3 neighbourhood.
            var pL = new double[N]; var pA = new double[N]; var pB = new double[N];
            var keyOf = new int[N];
            var labCache = new Dictionary<int, (double L, double A, double B)>();
            for (int i = 0; i < R; i++)
                for (int j = 0; j < C; j++)
                {
                    byte b = src[i, j, 0], g = src[i, j, 1], r = src[i, j, 2];
                    int key = (r << 16) | (g << 8) | b;
                    if (!labCache.TryGetValue(key, out var lab))
                    {
                        lab = ColorMatcher.RgbToLab(r, g, b);
                        if (gamut != null) lab = gamut.Map(lab);
                        labCache[key] = lab;
                    }
                    int p = i * C + j;
                    pL[p] = lab.L; pA[p] = lab.A; pB[p] = lab.B; keyOf[p] = key;
                }
            double[] edge = OptimalPaletteService.SobelMagnitude(pL, R, C);
            double edgeRef = OptimalPaletteService.Percentile(edge, 0.98);
            if (edgeRef <= 1e-9) edgeRef = 1;
            var wt = new double[N];
            for (int p = 0; p < N; p++)
                wt[p] = 1.0 + OptimalPaletteService.EdgeWeight * Math.Min(1.0, edge[p] / edgeRef);

            double[] sL = new double[M], sA = new double[M], sB = new double[M];
            for (int m = 0; m < M; m++)
            {
                var lab = ColorMatcher.RgbToLab(pool[m].r, pool[m].g, pool[m].b);
                sL[m] = lab.L; sA[m] = lab.A; sB[m] = lab.B;
            }
            double D2(double l, double a, double b, int m)
            {
                double dl = (l - sL[m]) * W, da = a - sA[m], db = b - sB[m];
                return dl * dl + da * da + db * db;
            }
            double StoneDist(int s, int t) => Math.Sqrt(D2(sL[s], sA[s], sB[s], t));
            bool SameFamily(int s, int t)
            {
                string? fs = familyOfId(pool[s].ID), ft = familyOfId(pool[t].ID);
                return !string.IsNullOrWhiteSpace(fs) && string.Equals(fs.Trim(), ft?.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            // Groups: pixels of (nearly) one source colour on one over-used stone; the group colour is their mean.
            var groups = new List<Group>();
            var groupIndex = new Dictionary<(int s, int l, int a, int b), int>();
            var overSet = new HashSet<int>(over);
            double step = Math.Max(1e-6, opt.GroupStep);
            for (int p = 0; p < N; p++)
            {
                int s = assign[p];
                if (!overSet.Contains(s)) continue;
                var gk = (s, (int)Math.Floor(pL[p] / step), (int)Math.Floor(pA[p] / step), (int)Math.Floor(pB[p] / step));
                if (!groupIndex.TryGetValue(gk, out int gi))
                {
                    gi = groups.Count;
                    groupIndex[gk] = gi;
                    groups.Add(new Group { Stone = s });
                }
                var g = groups[gi];
                g.Pixels.Add(p);
                g.WeightSum += wt[p];
                g.L += pL[p]; g.A += pA[p]; g.B += pB[p];
            }
            foreach (var g in groups)
            {
                g.L /= g.Pixels.Count; g.A /= g.Pixels.Count; g.B /= g.Pixels.Count;
            }

            // Substitute candidates per over-used stone: similar, with spare stock, stock data known.
            var palette = new List<int>();
            var fresh = new List<int>();
            for (int m = 0; m < M; m++)
            {
                if (cap[m] == Unknown || cap[m] - count[m] <= 0) continue;
                if (count[m] > 0) palette.Add(m); else fresh.Add(m);
            }
            // The closest few stones within the tolerance; farther ones would only be used after these are full,
            // and keeping the network small keeps the solver fast on photos with many colours.
            List<int> Near(int s, List<int> from, double tol)
            {
                var list = new List<(double d, int t)>();
                foreach (int t in from)
                {
                    if (t == s) continue;
                    double d = StoneDist(s, t);
                    if (d <= tol) list.Add((d, t));
                }
                list.Sort((x, y) => x.d != y.d ? x.d.CompareTo(y.d) : x.t.CompareTo(y.t));
                var result = new List<int>();
                for (int q = 0; q < list.Count && q < opt.MaxCandidates; q++) result.Add(list[q].t);
                return result;
            }

            double GroupCost(Group g, int t)
            {
                double c = g.WeightSum / g.Pixels.Count * (D2(g.L, g.A, g.B, t) - D2(g.L, g.A, g.B, g.Stone));
                if (c > 0 && SameFamily(g.Stone, t)) c *= opt.SameFamilyFactor;
                return c;
            }

            Plan SolveWith(HashSet<int> allowed, double tol)
            {
                var targets = new Dictionary<int, List<int>>();
                foreach (int s in over)
                {
                    var t = new List<int>();
                    foreach (int x in Near(s, palette, tol)) if (allowed.Contains(x)) t.Add(x);
                    foreach (int x in Near(s, fresh, tol)) if (allowed.Contains(x)) t.Add(x);
                    targets[s] = t;
                }
                return MinCostMove(groups, over, targets, count, cap, GroupCost);
            }

            // Twins of an over-used stone count as part of the mosaic's own stones.
            var twins = new HashSet<int>();
            foreach (int s in over)
            {
                // Only the closest twin: splitting a small area over two look-alike stones is just one more
                // colour change for the robot.
                var near = Near(s, fresh, opt.TwinTolerance);
                if (near.Count > 0) twins.Add(near[0]);
            }

            // A: substitutes from the mosaic's own stones (and twins) only. B: also up to maxExtra new stone types.
            Plan Best(double tol, int maxExtra)
            {
                var allowedA = new HashSet<int>(palette);
                allowedA.UnionWith(twins);
                var planA = SolveWith(allowedA, tol);
                if (maxExtra <= 0 || fresh.Count == 0) return planA;
                var allowedB = new HashSet<int>(allowedA);
                foreach (int s in over) foreach (int t in Near(s, fresh, tol)) allowedB.Add(t);
                var planB = SolveWith(allowedB, tol);
                while (true)
                {
                    var usedNew = new Dictionary<int, long>();
                    foreach (var kv in planB.Flow)
                        if (count[kv.Key.t] == 0 && !twins.Contains(kv.Key.t))
                            usedNew[kv.Key.t] = (usedNew.TryGetValue(kv.Key.t, out long f) ? f : 0) + kv.Value;
                    if (usedNew.Count <= maxExtra) break;
                    int weakest = -1; long least = long.MaxValue;
                    foreach (var kv in usedNew)
                        if (kv.Value < least || (kv.Value == least && kv.Key > weakest)) { least = kv.Value; weakest = kv.Key; }
                    allowedB.Remove(weakest);
                    planB = SolveWith(allowedB, tol);
                }
                // "Clearly better" must also hold when costs are negative (a substitute closer than Optimum's
                // stone), so the margin is measured on the absolute cost.
                bool clearlyCheaper = planB.Cost < planA.Cost - (1 - opt.NewStoneGain) * Math.Abs(planA.Cost);
                return planB.Unmet < planA.Unmet || (planB.Unmet == planA.Unmet && clearlyCheaper) ? planB : planA;
            }

            // Stock is a hard limit: when similar stones cannot take all the excess, the search widens step by
            // step (wider similarity, then more new stone types) and the report says how far it had to go.
            double tau = opt.SimilarityTolerance;
            var levels = new (double tol, int extra)[]
            {
                (tau, opt.MaxExtraStones), (2 * tau, opt.MaxExtraStones), (4 * tau, opt.MaxExtraStones),
                (double.PositiveInfinity, opt.MaxExtraStones), (double.PositiveInfinity, int.MaxValue)
            };
            Plan chosen = null!;
            for (int lv = 0; lv < levels.Length; lv++)
            {
                var plan = Best(levels[lv].tol, levels[lv].extra);
                if (chosen == null || plan.Unmet < chosen.Unmet) { chosen = plan; res.Level = lv; }
                if (plan.Unmet == 0) break;
            }

            // Turn group flows into pixels: per target, the pixels whose neighbourhood is relatively closest to it.
            var newAssign = (int[])assign.Clone();
            var byGroup = new Dictionary<int, List<(int t, long f, double c)>>();
            foreach (var kv in chosen.Flow)
            {
                if (kv.Value <= 0) continue;
                if (!byGroup.TryGetValue(kv.Key.g, out var l)) byGroup[kv.Key.g] = l = new();
                l.Add((kv.Key.t, kv.Value, GroupCost(groups[kv.Key.g], kv.Key.t)));
            }
            var moveCount = new Dictionary<(int s, int t), int>();
            double shiftSum = 0; int moved = 0;
            var gKeys = new List<int>(byGroup.Keys); gKeys.Sort();
            foreach (int gi in gKeys)
            {
                var g = groups[gi];
                var flows = byGroup[gi];
                flows.Sort((x, y) => x.c != y.c ? x.c.CompareTo(y.c) : x.t.CompareTo(y.t));
                var remaining = new List<int>(g.Pixels);
                foreach (var (t, f, _) in flows)
                {
                    var scored = new List<(double score, int bayer, int p)>(remaining.Count);
                    foreach (int p in remaining)
                    {
                        NeighbourMean(pL, pA, pB, R, C, p, out double nl, out double na, out double nb);
                        double score = wt[p] * (D2(nl, na, nb, t) - D2(nl, na, nb, g.Stone));
                        scored.Add((score, Bayer8[(p / C) % 8, (p % C) % 8], p));
                    }
                    scored.Sort((x, y) => x.score != y.score ? x.score.CompareTo(y.score)
                        : x.bayer != y.bayer ? x.bayer.CompareTo(y.bayer) : x.p.CompareTo(y.p));
                    int take = (int)Math.Min(f, scored.Count);
                    var taken = new HashSet<int>();
                    for (int q = 0; q < take; q++)
                    {
                        int p = scored[q].p;
                        newAssign[p] = t;
                        taken.Add(p);
                        shiftSum += Math.Sqrt(D2(pL[p], pA[p], pB[p], t)) - Math.Sqrt(D2(pL[p], pA[p], pB[p], g.Stone));
                    }
                    moved += take;
                    var key = (g.Stone, t);
                    moveCount[key] = (moveCount.TryGetValue(key, out int mc) ? mc : 0) + take;
                    remaining.RemoveAll(taken.Contains);
                }
            }

            res.Changed = moved > 0;
            res.Assignment = res.Changed ? newAssign : assign;
            res.MovedPixels = moved;
            res.MeanShift = moved > 0 ? shiftSum / moved : 0;
            int[] after = new int[M];
            for (int p = 0; p < N; p++) after[res.Assignment[p]]++;
            for (int m = 0; m < M; m++)
            {
                if (after[m] > 0) res.CountAfter[pool[m].ID] = after[m];
                if (after[m] > 0 && count[m] == 0) res.AddedIds.Add(pool[m].ID);
                if (cap[m] != Unknown && after[m] > cap[m]) res.ShortIds.Add(pool[m].ID);
            }
            foreach (var kv in moveCount)
                res.Moves.Add(new StockMove { FromId = pool[kv.Key.s].ID, ToId = pool[kv.Key.t].ID, Count = kv.Value });
            res.Moves.Sort((x, y) => x.FromId != y.FromId ? x.FromId.CompareTo(y.FromId) : y.Count.CompareTo(x.Count));
            return res;
        }

        private sealed class Group
        {
            public int Stone;
            public double L, A, B, WeightSum;
            public readonly List<int> Pixels = new();
        }

        private sealed class Plan
        {
            public Dictionary<(int g, int t), long> Flow = new();
            public double Cost;
            public long Unmet;
        }

        private static void NeighbourMean(double[] L, double[] A, double[] B, int R, int C, int p,
            out double l, out double a, out double b)
        {
            int i = p / C, j = p % C, n = 0;
            l = a = b = 0;
            for (int y = Math.Max(0, i - 1); y <= Math.Min(R - 1, i + 1); y++)
                for (int x = Math.Max(0, j - 1); x <= Math.Min(C - 1, j + 1); x++)
                {
                    int q = y * C + x;
                    l += L[q]; a += A[q]; b += B[q]; n++;
                }
            l /= n; a /= n; b /= n;
        }

        // Ordered-dither matrix: spreads ties evenly over an area instead of filling it from one side.
        private static readonly int[,] Bayer8 =
        {
            { 0, 32, 8, 40, 2, 34, 10, 42 }, { 48, 16, 56, 24, 50, 18, 58, 26 },
            { 12, 44, 4, 36, 14, 46, 6, 38 }, { 60, 28, 52, 20, 62, 30, 54, 22 },
            { 3, 35, 11, 43, 1, 33, 9, 41 }, { 51, 19, 59, 27, 49, 17, 57, 25 },
            { 15, 47, 7, 39, 13, 45, 5, 37 }, { 63, 31, 55, 23, 61, 29, 53, 21 }
        };

        // Network: source → over-used stone (excess) → its colour groups (size) → substitute (per-pixel cost)
        // → sink (spare stock). Successive shortest paths give the cheapest way to move as much excess as fits.
        private static Plan MinCostMove(List<Group> groups, List<int> over, Dictionary<int, List<int>> targets,
            int[] count, int[] cap, Func<Group, int, double> costOf)
        {
            var plan = new Plan();
            var net = new Network();
            int source = net.AddNode(), sink = net.AddNode();
            var stoneNode = new Dictionary<int, int>();
            var targetNode = new Dictionary<int, int>();
            long need = 0;
            foreach (int s in over)
            {
                int node = net.AddNode();
                stoneNode[s] = node;
                long excess = count[s] - cap[s];
                need += excess;
                net.AddEdge(source, node, excess, 0);
            }
            var gtEdges = new List<(int g, int t, int edge, long capacity)>();
            for (int gi = 0; gi < groups.Count; gi++)
            {
                var g = groups[gi];
                var ts = targets[g.Stone];
                if (ts.Count == 0) continue;
                int gNode = net.AddNode();
                long size = g.Pixels.Count;
                net.AddEdge(stoneNode[g.Stone], gNode, size, 0);
                foreach (int t in ts)
                {
                    if (!targetNode.TryGetValue(t, out int tNode))
                    {
                        tNode = net.AddNode();
                        targetNode[t] = tNode;
                        net.AddEdge(tNode, sink, cap[t] - count[t], 0);
                    }
                    int e = net.AddEdge(gNode, tNode, size, costOf(g, t));
                    gtEdges.Add((gi, t, e, size));
                }
            }
            var (flow, cost) = net.Run(source, sink, need);
            foreach (var (g, t, e, capacity) in gtEdges)
            {
                long f = capacity - net.Residual(e);
                if (f > 0) plan.Flow[(g, t)] = f;
            }
            plan.Cost = cost;
            plan.Unmet = need - flow;
            return plan;
        }

        private sealed class Network
        {
            private readonly List<int> _to = new(), _next = new();
            private readonly List<long> _cap = new();
            private readonly List<double> _cost = new();
            private readonly List<int> _head = new();

            public int AddNode() { _head.Add(-1); return _head.Count - 1; }

            // Returns the index of the forward edge.
            public int AddEdge(int u, int v, long capacity, double cost)
            {
                int e = _to.Count;
                _to.Add(v); _cap.Add(capacity); _cost.Add(cost); _next.Add(_head[u]); _head[u] = e;
                _to.Add(u); _cap.Add(0); _cost.Add(-cost); _next.Add(_head[v]); _head[v] = e + 1;
                return e;
            }

            public long Residual(int e) => _cap[e];

            public (long flow, double cost) Run(int s, int t, long limit)
            {
                int n = _head.Count;
                long flow = 0; double total = 0;
                var dist = new double[n];
                var prevEdge = new int[n];

                // Potentials from one Bellman-Ford pass (costs may be negative: a substitute can be closer than the
                // stone Optimum picked; the network is acyclic at the start). After that every search is a
                // Dijkstra on non-negative reduced costs.
                var pot = new double[n];
                Array.Fill(pot, double.PositiveInfinity);
                pot[s] = 0;
                var inQueue = new bool[n];
                var queue = new Queue<int>();
                queue.Enqueue(s); inQueue[s] = true;
                while (queue.Count > 0)
                {
                    int u = queue.Dequeue(); inQueue[u] = false;
                    for (int e = _head[u]; e >= 0; e = _next[e])
                    {
                        if (_cap[e] <= 0) continue;
                        int v = _to[e];
                        double nd = pot[u] + _cost[e];
                        if (nd < pot[v] - 1e-12)
                        {
                            pot[v] = nd;
                            if (!inQueue[v]) { queue.Enqueue(v); inQueue[v] = true; }
                        }
                    }
                }
                for (int v = 0; v < n; v++) if (double.IsPositiveInfinity(pot[v])) pot[v] = 0;

                var heap = new PriorityQueue<int, double>();
                while (flow < limit)
                {
                    Array.Fill(dist, double.PositiveInfinity);
                    Array.Fill(prevEdge, -1);
                    dist[s] = 0;
                    heap.Clear();
                    heap.Enqueue(s, 0);
                    while (heap.TryDequeue(out int u, out double du))
                    {
                        if (du > dist[u]) continue;
                        for (int e = _head[u]; e >= 0; e = _next[e])
                        {
                            if (_cap[e] <= 0) continue;
                            int v = _to[e];
                            double rc = _cost[e] + pot[u] - pot[v];
                            if (rc < 0) rc = 0; // rounding noise only
                            double nd = du + rc;
                            if (nd < dist[v] - 1e-12)
                            {
                                dist[v] = nd; prevEdge[v] = e;
                                heap.Enqueue(v, nd);
                            }
                        }
                    }
                    if (double.IsPositiveInfinity(dist[t])) break;
                    for (int v = 0; v < n; v++) if (!double.IsPositiveInfinity(dist[v])) pot[v] += dist[v];

                    long push = limit - flow;
                    for (int v = t; v != s; v = _to[prevEdge[v] ^ 1]) push = Math.Min(push, _cap[prevEdge[v]]);
                    double pathCost = 0;
                    for (int v = t; v != s; v = _to[prevEdge[v] ^ 1])
                    {
                        _cap[prevEdge[v]] -= push;
                        _cap[prevEdge[v] ^ 1] += push;
                        pathCost += _cost[prevEdge[v]];
                    }
                    flow += push;
                    total += push * pathCost;
                }
                return (flow, total);
            }
        }
    }
}
