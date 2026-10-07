using System;
using System.Collections.Generic;

namespace mosair.Services
{
    // Exact nearest-colour search for the classic Mos, with the same answer as the plain loop it replaces:
    //
    //     for k in 0..P-1: d = dist(q, p[k]); if (d < min) { min = d; n = k; }
    //
    // i.e. the smallest distance, and on a tie the lowest index. Distances are computed with the very same
    // expressions (same operand order), so the floating-point values compared are identical.
    // The palette points are put in a uniform 3-D grid; a query looks at the cells around its own, ring by ring,
    // and stops once no point further out can be as close as the best one found (with a small safety margin, so
    // rounding can never make it skip a point that the plain loop would have picked).
    public sealed class NearestColorIndex
    {
        private readonly double[] _c0, _c1, _c2;
        private readonly bool _average;
        private readonly double _min0, _min1, _min2, _cell;
        private readonly int _n0, _n1, _n2;
        private readonly int[] _cellStart;   // CSR layout: points of cell c are _points[_cellStart[c] .. _cellStart[c+1])
        private readonly int[] _points;      // ascending point indices within each cell

        // c0/c1/c2: the palette's three coordinates in the order the distance uses them (classic Mos: b,g,r or L,A,B).
        // average: the "average" distance (|d0|+|d1|+|d2|)/3; otherwise the squared distance d0²+d1²+d2².
        public NearestColorIndex(double[] c0, double[] c1, double[] c2, bool average)
        {
            _c0 = c0; _c1 = c1; _c2 = c2; _average = average;
            int count = c0.Length;
            if (count == 0) { _cell = 1; _n0 = _n1 = _n2 = 1; _cellStart = new int[2]; _points = Array.Empty<int>(); return; }

            double max0 = double.MinValue, max1 = double.MinValue, max2 = double.MinValue;
            _min0 = _min1 = _min2 = double.MaxValue;
            for (int k = 0; k < count; k++)
            {
                _min0 = Math.Min(_min0, c0[k]); max0 = Math.Max(max0, c0[k]);
                _min1 = Math.Min(_min1, c1[k]); max1 = Math.Max(max1, c1[k]);
                _min2 = Math.Min(_min2, c2[k]); max2 = Math.Max(max2, c2[k]);
            }
            double span = Math.Max(max0 - _min0, Math.Max(max1 - _min1, max2 - _min2));
            // About two points per cell along the widest side's scale.
            int perSide = Math.Max(1, (int)Math.Ceiling(Math.Cbrt(count / 2.0)));
            _cell = span > 0 ? span / perSide : 1;
            _n0 = Cells(max0 - _min0); _n1 = Cells(max1 - _min1); _n2 = Cells(max2 - _min2);

            var cellOf = new int[count];
            var counts = new int[_n0 * _n1 * _n2 + 1];
            for (int k = 0; k < count; k++)
            {
                int c = CellIndex(Clamp(Coord(c0[k], _min0), _n0), Clamp(Coord(c1[k], _min1), _n1), Clamp(Coord(c2[k], _min2), _n2));
                cellOf[k] = c;
                counts[c + 1]++;
            }
            for (int c = 0; c < _n0 * _n1 * _n2; c++) counts[c + 1] += counts[c];
            _cellStart = (int[])counts.Clone();
            _points = new int[count];
            var fill = (int[])counts.Clone();
            for (int k = 0; k < count; k++) _points[fill[cellOf[k]]++] = k;   // k ascending, so each cell is sorted
        }

        private int Cells(double extent) => Math.Max(1, (int)Math.Floor(extent / _cell) + 1);
        private int Coord(double v, double min) => (int)Math.Floor((v - min) / _cell);
        private static int Clamp(int i, int n) => i < 0 ? 0 : i >= n ? n - 1 : i;
        private int CellIndex(int i0, int i1, int i2) => (i0 * _n1 + i1) * _n2 + i2;

        // Index of the nearest palette point to (q0,q1,q2); 0 for an empty palette (as the plain loop).
        public int Find(double q0, double q1, double q2)
        {
            if (_points.Length == 0) return 0;
            int a0 = Clamp(Coord(q0, _min0), _n0), a1 = Clamp(Coord(q1, _min1), _n1), a2 = Clamp(Coord(q2, _min2), _n2);
            double min = double.MaxValue;
            int best = int.MaxValue;
            int maxRing = Math.Max(_n0, Math.Max(_n1, _n2));
            for (int ring = 0; ring <= maxRing; ring++)
            {
                // Every point in this ring or further out differs from q by at least (ring-1)·cell in one
                // coordinate (q may sit anywhere inside its own cell, or outside the grid when clamped).
                if (ring >= 2 && best != int.MaxValue)
                {
                    double gap = (ring - 1) * _cell;
                    double bound = _average ? gap / 3.0 : gap * gap;
                    if (bound * (1 - 1e-9) > min) break;
                }
                bool any = false;
                for (int i0 = a0 - ring; i0 <= a0 + ring; i0++)
                {
                    if (i0 < 0 || i0 >= _n0) continue;
                    bool edge0 = i0 == a0 - ring || i0 == a0 + ring;
                    for (int i1 = a1 - ring; i1 <= a1 + ring; i1++)
                    {
                        if (i1 < 0 || i1 >= _n1) continue;
                        bool edge1 = edge0 || i1 == a1 - ring || i1 == a1 + ring;
                        if (edge1)
                        {
                            for (int i2 = a2 - ring; i2 <= a2 + ring; i2++)
                                if (i2 >= 0 && i2 < _n2) { any = true; Scan(CellIndex(i0, i1, i2), q0, q1, q2, ref min, ref best); }
                        }
                        else
                        {
                            // Inner rows of the shell: only the two end cells along the last axis.
                            int lo = a2 - ring, hi = a2 + ring;
                            if (lo >= 0 && lo < _n2) { any = true; Scan(CellIndex(i0, i1, lo), q0, q1, q2, ref min, ref best); }
                            if (hi != lo && hi >= 0 && hi < _n2) { any = true; Scan(CellIndex(i0, i1, hi), q0, q1, q2, ref min, ref best); }
                        }
                    }
                }
                if (!any && ring > 0 && a0 - ring < 0 && a0 + ring >= _n0 && a1 - ring < 0 && a1 + ring >= _n1 &&
                    a2 - ring < 0 && a2 + ring >= _n2) break;
            }
            return best;
        }

        private void Scan(int cell, double q0, double q1, double q2, ref double min, ref int best)
        {
            for (int p = _cellStart[cell]; p < _cellStart[cell + 1]; p++)
            {
                int k = _points[p];
                double d0 = q0 - _c0[k], d1 = q1 - _c1[k], d2 = q2 - _c2[k];
                double av = _average ? (Math.Abs(d0) + Math.Abs(d1) + Math.Abs(d2)) / 3.0 : d0 * d0 + d1 * d1 + d2 * d2;
                // Same pick as the plain loop: smaller distance wins; equal distance → lower index.
                if (av < min || (av == min && k < best)) { min = av; best = k; }
            }
        }
    }

    // The distinct colours of a stone image (BGR bytes, as ImageService.ToByteArray gives them), so the nearest
    // palette colour is looked up once per colour instead of once per stone.
    public sealed class DistinctColors
    {
        public readonly int[] PixelColor;          // per stone (row-major), index into B/G/R
        public readonly byte[] B, G, R;

        public DistinctColors(byte[,,] data)
        {
            int rows = data.GetLength(0), cols = data.GetLength(1);
            PixelColor = new int[rows * cols];
            var map = new Dictionary<int, int>();
            var b = new List<byte>(); var g = new List<byte>(); var r = new List<byte>();
            for (int i = 0, p = 0; i < rows; i++)
                for (int j = 0; j < cols; j++, p++)
                {
                    int key = data[i, j, 0] << 16 | data[i, j, 1] << 8 | data[i, j, 2];
                    if (!map.TryGetValue(key, out int id))
                    {
                        id = b.Count;
                        map[key] = id;
                        b.Add(data[i, j, 0]); g.Add(data[i, j, 1]); r.Add(data[i, j, 2]);
                    }
                    PixelColor[p] = id;
                }
            B = b.ToArray(); G = g.ToArray(); R = r.ToArray();
        }

        public int Count => B.Length;
    }
}
