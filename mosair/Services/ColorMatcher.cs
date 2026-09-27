using System;
using System.Collections.Generic;
using mosair.Models;

namespace mosair.Services
{
    public static class ColorMatcher
    {
        public static (double L, double A, double B) RgbToLab(double r, double g, double b)
        {
            double[] xyz = new double[3];
            double[] rgb = new double[3];

            rgb[0] = r / 255.0;
            rgb[1] = g / 255.0;
            rgb[2] = b / 255.0;

            if (rgb[0] > .04045)
                rgb[0] = Math.Pow((rgb[0] + .055) / 1.055, 2.4);
            else
                rgb[0] = rgb[0] / 12.92;

            if (rgb[1] > .04045)
                rgb[1] = Math.Pow((rgb[1] + .055) / 1.055, 2.4);
            else
                rgb[1] = rgb[1] / 12.92;

            if (rgb[2] > .04045)
                rgb[2] = Math.Pow((rgb[2] + .055) / 1.055, 2.4);
            else
                rgb[2] = rgb[2] / 12.92;

            rgb[0] *= 100.0;
            rgb[1] *= 100.0;
            rgb[2] *= 100.0;

            xyz[0] = (rgb[0] * .412453) + (rgb[1] * .357580) + (rgb[2] * .180423);
            xyz[1] = (rgb[0] * .212671) + (rgb[1] * .715160) + (rgb[2] * .072169);
            xyz[2] = (rgb[0] * .019334) + (rgb[1] * .119193) + (rgb[2] * .950227);

            xyz[0] = xyz[0] / 95.047;
            xyz[1] = xyz[1] / 100.0;
            xyz[2] = xyz[2] / 108.883;

            if (xyz[0] > .008856)
                xyz[0] = Math.Pow(xyz[0], 1.0 / 3.0);
            else
                xyz[0] = (xyz[0] * 7.787) + (16.0 / 116.0);

            if (xyz[1] > .008856)
                xyz[1] = Math.Pow(xyz[1], 1.0 / 3.0);
            else
                xyz[1] = (xyz[1] * 7.787) + (16.0 / 116.0);

            if (xyz[2] > .008856)
                xyz[2] = Math.Pow(xyz[2], 1.0 / 3.0);
            else
                xyz[2] = (xyz[2] * 7.787) + (16.0 / 116.0);

            double L = (116.0 * xyz[1]) - 16.0;
            double A = 500.0 * (xyz[0] - xyz[1]);
            double B = 200.0 * (xyz[1] - xyz[2]);

            return (L, A, B);
        }

        public static cooo CalculateDistance(rgb ra, rgb raa, int k)
        {
            double db = Math.Abs(raa.b - ra.b);
            double dg = Math.Abs(raa.g - ra.g);
            double dr = Math.Abs(raa.r - ra.r);
            double av = db * db + dg * dg + dr * dr;
            return new cooo(av, k);
        }

        public static List<cooo> FindCatalogDistances(rgb ra)
        {
            var aro = new List<cooo>();
            for (int k = 0; k < MosaicData.arRGB.Count; k++)
            {
                rgb raa = MosaicData.arRGB[k];
                if (!raa.boolUseOnce)
                    aro.Add(CalculateDistance(ra, raa, k));
            }
            return aro;
        }

        public static int SelectNearest(List<cooo> aro)
        {
            double min = 10000;
            int n = 0;
            for (int z = 0; z < aro.Count; z++)
            {
                if (aro[z].av < min)
                {
                    min = aro[z].av;
                    n = aro[z].n;
                }
            }
            return n;
        }

        public static void ResetUseOnce()
        {
            for (int k = 0; k < MosaicData.arRGB.Count; k++)
                MosaicData.arRGB[k].boolUseOnce = false;
        }
    }
}
