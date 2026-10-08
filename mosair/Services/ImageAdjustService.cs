using System;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SkiaSharp;

namespace mosair.Services
{
    // The Görsel Ayarları panel's settings, like Photoshop's Light and Hue/Saturation panels. Saved in the project
    // file (ProjectData.Adjust); every value 0 = unchanged.
    public sealed class ImageAdjustSettings
    {
        // Colour ranges of the Hue/Saturation part: 0 = all colours ("Ana"), then reds, yellows, greens, cyans,
        // blues, magentas (centred on 0°, 60°, 120°, 180°, 240°, 300°).
        public const int RangeCount = 7;

        // ---- Light (Işık) ----
        public int Exposure { get; set; }     // -200..200 = -2.00..+2.00 EV
        public int Brightness { get; set; }   // -100..100 (also each of the rest)
        public int Contrast { get; set; }
        public int Highlights { get; set; }
        public int Shadows { get; set; }
        public int Whites { get; set; }
        public int Blacks { get; set; }
        public int Gamma { get; set; }

        // ---- Hue/Saturation (Ton/Doygunluk), one value per range ----
        public int[] Hue { get; set; } = new int[RangeCount];          // -180..180 degrees
        public int[] Saturation { get; set; } = new int[RangeCount];   // -100..100
        public int[] Lightness { get; set; } = new int[RangeCount];    // -100..100

        // Colorize (Renklendir): the whole image in one hue.
        public bool Colorize { get; set; }
        public int ColorizeHue { get; set; }                 // 0..360
        public int ColorizeSaturation { get; set; } = 25;    // 0..100
        public int ColorizeLightness { get; set; }           // -100..100

        [JsonIgnore]
        public bool IsToneNeutral => Exposure == 0 && Brightness == 0 && Contrast == 0 && Highlights == 0 &&
                                     Shadows == 0 && Whites == 0 && Blacks == 0 && Gamma == 0;

        [JsonIgnore]
        public bool IsColorNeutral
        {
            get
            {
                if (Colorize) return false;
                for (int i = 0; i < RangeCount; i++)
                    if (At(Hue, i) != 0 || At(Saturation, i) != 0 || At(Lightness, i) != 0) return false;
                return true;
            }
        }

        [JsonIgnore]
        public bool IsNeutral => IsToneNeutral && IsColorNeutral;

        private static int At(int[]? a, int i) => a != null && i < a.Length ? a[i] : 0;

        public ImageAdjustSettings Clone()
        {
            var c = (ImageAdjustSettings)MemberwiseClone();
            c.Hue = Fixed(Hue); c.Saturation = Fixed(Saturation); c.Lightness = Fixed(Lightness);
            return c;
        }

        // Arrays always RangeCount long (a file may have shorter or missing ones).
        private static int[] Fixed(int[]? a)
        {
            var r = new int[RangeCount];
            if (a != null) Array.Copy(a, r, Math.Min(a.Length, RangeCount));
            return r;
        }

        // Projects saved before this panel had only [brightness, contrast, saturation, gamma].
        public static ImageAdjustSettings FromLegacy(int[]? a)
        {
            var s = new ImageAdjustSettings();
            if (a == null || a.Length < 4) return s;
            s.Brightness = Math.Clamp(a[0], -100, 100);
            s.Contrast = Math.Clamp(a[1], -100, 100);
            s.Saturation[0] = Math.Clamp(a[2], -100, 100);
            s.Gamma = Math.Clamp(a[3], -100, 100);
            return s;
        }
    }

    // Applies the settings to the loaded image; Mos then works from the adjusted copy, the file on disk is not
    // changed. Two steps: a tone curve (exposure, brightness, gamma, contrast, highlights, shadows, whites,
    // blacks) as one Skia colour table, then — only when used — the hue/saturation/lightness pass per pixel.
    public static class ImageAdjustService
    {
        public static SKBitmap Apply(SKBitmap source, ImageAdjustSettings a)
        {
            var result = new SKBitmap(source.Info);
            using (var canvas = new SKCanvas(result))
            {
                canvas.Clear(SKColors.Transparent);
                if (a.IsToneNeutral)
                    canvas.DrawBitmap(source, 0, 0);
                else
                {
                    var curve = ToneCurve(a);
                    var identity = new byte[256];
                    for (int v = 0; v < 256; v++) identity[v] = (byte)v;
                    using var filter = SKColorFilter.CreateTable(identity, curve, curve, curve);
                    using var paint = new SKPaint { ColorFilter = filter };
                    canvas.DrawBitmap(source, 0, 0, paint);
                }
                canvas.Flush();
            }
            if (!a.IsColorNeutral) HslPass(result, a);
            return result;
        }

        private static double Bell(double x, double centre, double width)
        {
            double t = (x - centre) / width;
            return Math.Exp(-t * t);
        }

        // One curve for R, G and B (0..255 → 0..255).
        public static byte[] ToneCurve(ImageAdjustSettings a)
        {
            double exposure = Math.Pow(2, a.Exposure / 100.0);
            double brightness = a.Brightness / 100.0 * 0.4;
            double gammaExp = Math.Pow(2, -a.Gamma / 100.0);
            double contrast = a.Contrast >= 0 ? 1 + a.Contrast / 50.0 : 1 + a.Contrast / 100.0;
            var curve = new byte[256];
            for (int v = 0; v < 256; v++)
            {
                double x = v / 255.0 * exposure + brightness;
                x = Math.Pow(Math.Clamp(x, 0, 1), gammaExp);
                x = (x - 0.5) * contrast + 0.5;
                // Each acts on its own part of the tonal range (smooth bell weights).
                x += a.Highlights / 100.0 * 0.30 * Bell(x, 0.75, 0.20);
                x += a.Shadows / 100.0 * 0.30 * Bell(x, 0.25, 0.20);
                x += a.Whites / 100.0 * 0.25 * Bell(x, 0.95, 0.12);
                x += a.Blacks / 100.0 * 0.25 * Bell(x, 0.05, 0.12);
                curve[v] = (byte)Math.Round(Math.Clamp(x, 0, 1) * 255);
            }
            return curve;
        }

        // ----- Hue / saturation / lightness, per pixel, row by row in parallel -----
        private static readonly double[] RangeCentre = { 0, 0, 60, 120, 180, 240, 300 };

        private static void HslPass(SKBitmap bmp, ImageAdjustSettings a)
        {
            bool bgra = bmp.ColorType == SKColorType.Bgra8888;
            if (!bgra && bmp.ColorType != SKColorType.Rgba8888) return;   // other formats are not produced by the loader
            int ri = bgra ? 2 : 0, bi = bgra ? 0 : 2;
            int width = bmp.Width, rowBytes = bmp.RowBytes;
            IntPtr pixels = bmp.GetPixels();
            var s = a.Clone();
            Parallel.For(0, bmp.Height, () => new byte[rowBytes], (y, _, row) =>
            {
                IntPtr p = pixels + y * rowBytes;
                Marshal.Copy(p, row, 0, rowBytes);
                for (int x = 0, o = 0; x < width; x++, o += 4)
                {
                    double r = row[o + ri] / 255.0, g = row[o + 1] / 255.0, b = row[o + bi] / 255.0;
                    Adjust(ref r, ref g, ref b, s);
                    row[o + ri] = (byte)Math.Round(r * 255);
                    row[o + 1] = (byte)Math.Round(g * 255);
                    row[o + bi] = (byte)Math.Round(b * 255);
                }
                Marshal.Copy(row, 0, p, rowBytes);
                return row;
            }, _ => { });
        }

        public static void Adjust(ref double r, ref double g, ref double b, ImageAdjustSettings a)
        {
            RgbToHsl(r, g, b, out double h, out double s, out double l);
            if (a.Colorize)
            {
                // One hue for the whole image; lightness from the pixel's luminance.
                h = a.ColorizeHue;
                s = a.ColorizeSaturation / 100.0;
                l = 0.299 * r + 0.587 * g + 0.114 * b;
                l = LightnessShift(l, a.ColorizeLightness / 100.0);
                HslToRgb(h, s, l, out r, out g, out b);
                return;
            }

            double dh = a.Hue[0], ds = a.Saturation[0], dl = a.Lightness[0];
            // The colour ranges act on coloured pixels only (greys have no hue), fading out over ±15°..±45°.
            double colourful = Math.Min(1, s * 4);
            for (int i = 1; i < ImageAdjustSettings.RangeCount; i++)
            {
                if (a.Hue[i] == 0 && a.Saturation[i] == 0 && a.Lightness[i] == 0) continue;
                double d = Math.Abs(h - RangeCentre[i]);
                if (d > 180) d = 360 - d;
                double w = d <= 15 ? 1 : d >= 45 ? 0 : (45 - d) / 30;
                w *= colourful;
                dh += w * a.Hue[i]; ds += w * a.Saturation[i]; dl += w * a.Lightness[i];
            }
            if (dh == 0 && ds == 0 && dl == 0) return;
            h = (h + dh) % 360;
            if (h < 0) h += 360;
            ds /= 100;
            s = ds < 0 ? s * (1 + Math.Max(-1, ds)) : s + (1 - s) * Math.Min(1, ds) * Math.Min(1, s * 2);
            l = LightnessShift(l, dl / 100);
            HslToRgb(h, Math.Clamp(s, 0, 1), Math.Clamp(l, 0, 1), out r, out g, out b);
        }

        private static double LightnessShift(double l, double d) =>
            d < 0 ? l * (1 + Math.Max(-1, d)) : l + (1 - l) * Math.Min(1, d);

        private static void RgbToHsl(double r, double g, double b, out double h, out double s, out double l)
        {
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            l = (max + min) / 2;
            double d = max - min;
            if (d < 1e-9) { h = 0; s = 0; return; }
            s = d / (1 - Math.Abs(2 * l - 1));
            if (max == r) h = 60 * (((g - b) / d) % 6);
            else if (max == g) h = 60 * ((b - r) / d + 2);
            else h = 60 * ((r - g) / d + 4);
            if (h < 0) h += 360;
        }

        private static void HslToRgb(double h, double s, double l, out double r, out double g, out double b)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double hp = h / 60;
            double x = c * (1 - Math.Abs(hp % 2 - 1));
            double r1 = 0, g1 = 0, b1 = 0;
            if (hp < 1) { r1 = c; g1 = x; }
            else if (hp < 2) { r1 = x; g1 = c; }
            else if (hp < 3) { g1 = c; b1 = x; }
            else if (hp < 4) { g1 = x; b1 = c; }
            else if (hp < 5) { r1 = x; b1 = c; }
            else { r1 = c; b1 = x; }
            double m = l - c / 2;
            r = Math.Clamp(r1 + m, 0, 1); g = Math.Clamp(g1 + m, 0, 1); b = Math.Clamp(b1 + m, 0, 1);
        }
    }
}
