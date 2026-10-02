using System;
using System.Collections.Generic;
using mosair.Models;

namespace mosair.Services
{
    // Compresses the image's Lab lightness range and chroma into the range the stone catalog can reproduce,
    // keeping hue, so distinct source colors stay distinct instead of collapsing onto the same boundary stone.
    // Only ever compresses; an image already inside the catalog gamut is left unchanged.
    public sealed class GamutMapper
    {
        private readonly double _srcL0, _srcL1, _dstL0, _dstL1, _chromaScale;

        private GamutMapper(double srcL0, double srcL1, double dstL0, double dstL1, double chromaScale)
        {
            _srcL0 = srcL0; _srcL1 = srcL1; _dstL0 = dstL0; _dstL1 = dstL1; _chromaScale = chromaScale;
        }

        public static GamutMapper Build(byte[,,] src, int R, int C, List<rgb> catalog)
        {
            var imgL = new List<double>(R * C);
            var imgC = new List<double>(R * C);
            for (int i = 0; i < R; i++)
                for (int j = 0; j < C; j++)
                {
                    var lab = ColorMatcher.RgbToLab(src[i, j, 2], src[i, j, 1], src[i, j, 0]);
                    imgL.Add(lab.L);
                    imgC.Add(Math.Sqrt(lab.A * lab.A + lab.B * lab.B));
                }
            var catL = new List<double>();
            var catC = new List<double>();
            foreach (var c in catalog)
            {
                var lab = ColorMatcher.RgbToLab(c.r, c.g, c.b);
                catL.Add(lab.L);
                catC.Add(Math.Sqrt(lab.A * lab.A + lab.B * lab.B));
            }

            double sL0 = Pct(imgL, 0.01), sL1 = Pct(imgL, 0.99);
            double dL0 = Pct(catL, 0.0), dL1 = Pct(catL, 1.0);
            // Compress lightness only where the image extends beyond the catalog.
            double tL0 = Math.Max(sL0, dL0), tL1 = Math.Min(sL1, dL1);
            if (tL1 - tL0 < 1) { tL0 = sL0; tL1 = sL1; }

            double imgChroma = Pct(imgC, 0.95);
            double catChroma = Pct(catC, 0.95);
            double scale = imgChroma > catChroma && imgChroma > 1e-6 ? catChroma / imgChroma : 1.0;

            return new GamutMapper(sL0, sL1, tL0, tL1, scale);
        }

        public (double L, double A, double B) Map((double L, double A, double B) lab)
        {
            double L = lab.L;
            if (_srcL1 - _srcL0 > 1e-6)
            {
                double t = Math.Clamp((L - _srcL0) / (_srcL1 - _srcL0), 0, 1);
                L = _dstL0 + t * (_dstL1 - _dstL0);
            }
            return (L, lab.A * _chromaScale, lab.B * _chromaScale);
        }

        public override string ToString() =>
            $"L[{_srcL0:F0}..{_srcL1:F0}]->[{_dstL0:F0}..{_dstL1:F0}] chroma×{_chromaScale:F2}";

        private static double Pct(List<double> v, double q)
        {
            var a = v.ToArray();
            Array.Sort(a);
            return a[(int)Math.Clamp(Math.Round(q * (a.Length - 1)), 0, a.Length - 1)];
        }
    }
}
