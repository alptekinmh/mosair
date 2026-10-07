using System;
using SkiaSharp;

namespace mosair.Services
{
    // Basic image adjustments (Görsel Ayarları panel): each value runs from -100 to 100, 0 = unchanged.
    public readonly record struct ImageAdjustments(int Brightness, int Contrast, int Saturation, int Gamma)
    {
        public bool IsNeutral => Brightness == 0 && Contrast == 0 && Saturation == 0 && Gamma == 0;

        // Stored in the project file as [brightness, contrast, saturation, gamma]; null when unchanged.
        public int[]? ToArray() => IsNeutral ? null : new[] { Brightness, Contrast, Saturation, Gamma };

        public static ImageAdjustments FromArray(int[]? a) =>
            a == null || a.Length < 4 ? default : new ImageAdjustments(Clamp(a[0]), Clamp(a[1]), Clamp(a[2]), Clamp(a[3]));

        private static int Clamp(int v) => Math.Clamp(v, -100, 100);
    }

    // Applies the adjustments to the loaded image; Mos then works from the adjusted copy, the file on disk is not
    // changed. One Skia colour filter: a tone curve (gamma, contrast, brightness) per channel, then saturation.
    public static class ImageAdjustService
    {
        public static SKBitmap Apply(SKBitmap source, ImageAdjustments a)
        {
            var result = new SKBitmap(source.Info);
            using var canvas = new SKCanvas(result);
            using var filter = CreateFilter(a);
            using var paint = new SKPaint { ColorFilter = filter };
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(source, 0, 0, paint);
            canvas.Flush();
            return result;
        }

        private static SKColorFilter CreateFilter(ImageAdjustments a)
        {
            // Gamma: +100 lifts the mid-tones (exponent 0.5), -100 darkens them (exponent 2).
            double exponent = Math.Pow(2, -a.Gamma / 100.0);
            // Contrast around mid-grey: -100 → flat grey, +100 → three times steeper.
            double contrast = a.Contrast >= 0 ? 1 + a.Contrast / 50.0 : 1 + a.Contrast / 100.0;
            // Brightness: up to ±40 % of the full range.
            double brightness = a.Brightness / 100.0 * 0.4;

            var curve = new byte[256];
            var alpha = new byte[256];
            for (int v = 0; v < 256; v++)
            {
                double x = Math.Pow(v / 255.0, exponent);
                x = (x - 0.5) * contrast + 0.5 + brightness;
                curve[v] = (byte)Math.Round(Math.Clamp(x, 0, 1) * 255);
                alpha[v] = (byte)v;
            }
            var tone = SKColorFilter.CreateTable(alpha, curve, curve, curve);

            // Saturation: mixes each pixel with its luminance (Rec. 709 weights); -100 → grey, +100 → twice as vivid.
            float s = 1 + a.Saturation / 100f;
            const float lr = 0.2126f, lg = 0.7152f, lb = 0.0722f;
            float[] m =
            {
                lr + (1 - lr) * s, lg - lg * s,       lb - lb * s,       0, 0,
                lr - lr * s,       lg + (1 - lg) * s, lb - lb * s,       0, 0,
                lr - lr * s,       lg - lg * s,       lb + (1 - lb) * s, 0, 0,
                0,                 0,                 0,                 1, 0
            };
            using var saturation = SKColorFilter.CreateColorMatrix(m);
            // The tone curve first, then saturation.
            var filter = SKColorFilter.CreateCompose(saturation, tone);
            tone.Dispose();
            return filter;
        }
    }
}
