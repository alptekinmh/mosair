using System;
using System.IO;
using SkiaSharp;

namespace mosair.Services
{
    public enum InterpolationMethod
    {
        Area,
        Nearest,
        Linear,
        Cubic,
        Lanczos4,
        LinearExact,
        NearestExact
    }

    public static class ImageService
    {
        // CvInvoke.Imread → SKBitmap.Decode
        public static SKBitmap? LoadImage(string path)
        {
            if (!File.Exists(path)) return null;
            return SKBitmap.Decode(path);
        }

        public static unsafe SKBitmap Resize(SKBitmap src, int dstW, int dstH,
            InterpolationMethod method = InterpolationMethod.Area)
        {
            if (method != InterpolationMethod.Area)
                return ResizeWithSampling(src, dstW, dstH, method);

            int srcW = src.Width;
            int srcH = src.Height;

            if (dstW >= srcW && dstH >= srcH)
            {
                var info = new SKImageInfo(dstW, dstH, src.ColorType, src.AlphaType);
                return src.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell));
            }

            bool isBgra = src.ColorType == SKColorType.Bgra8888;
            IntPtr srcPtr = src.GetPixels();
            byte* srcPx = (byte*)srcPtr;
            int srcStride = src.RowBytes;

            double scaleX = (double)srcW / dstW;
            double scaleY = (double)srcH / dstH;

            var result = new SKBitmap(dstW, dstH, SKColorType.Rgba8888, SKAlphaType.Opaque);
            IntPtr dstPtr = result.GetPixels();
            byte* dstPx = (byte*)dstPtr;

            System.Threading.Tasks.Parallel.For(0, dstH, dy =>
            {
                double srcY0 = dy * scaleY;
                double srcY1 = (dy + 1) * scaleY;
                int iy0 = (int)srcY0;
                int iy1 = (int)Math.Ceiling(srcY1);
                if (iy1 > srcH) iy1 = srcH;

                for (int dx = 0; dx < dstW; dx++)
                {
                    double srcX0 = dx * scaleX;
                    double srcX1 = (dx + 1) * scaleX;
                    int ix0 = (int)srcX0;
                    int ix1 = (int)Math.Ceiling(srcX1);
                    if (ix1 > srcW) ix1 = srcW;

                    double sumR = 0, sumG = 0, sumB = 0, totalW = 0;

                    for (int iy = iy0; iy < iy1; iy++)
                    {
                        double wy = Math.Min(iy + 1.0, srcY1) - Math.Max((double)iy, srcY0);
                        for (int ix = ix0; ix < ix1; ix++)
                        {
                            double wx = Math.Min(ix + 1.0, srcX1) - Math.Max((double)ix, srcX0);
                            double w = wx * wy;

                            int off = iy * srcStride + ix * 4;
                            double pr, pg, pb;
                            if (isBgra)
                            { pb = srcPx[off]; pg = srcPx[off + 1]; pr = srcPx[off + 2]; }
                            else
                            { pr = srcPx[off]; pg = srcPx[off + 1]; pb = srcPx[off + 2]; }

                            sumR += pr * w;
                            sumG += pg * w;
                            sumB += pb * w;
                            totalW += w;
                        }
                    }

                    int dstOff = (dy * dstW + dx) * 4;
                    dstPx[dstOff + 0] = (byte)(sumR / totalW + 0.5);
                    dstPx[dstOff + 1] = (byte)(sumG / totalW + 0.5);
                    dstPx[dstOff + 2] = (byte)(sumB / totalW + 0.5);
                    dstPx[dstOff + 3] = 255;
                }
            });

            return result;
        }

        private static SKBitmap ResizeWithSampling(SKBitmap src, int dstW, int dstH, InterpolationMethod method)
        {
            var info = new SKImageInfo(dstW, dstH, src.ColorType, src.AlphaType);
            SKSamplingOptions sampling = method switch
            {
                InterpolationMethod.Nearest or InterpolationMethod.NearestExact
                    => new SKSamplingOptions(SKFilterMode.Nearest),
                InterpolationMethod.Linear or InterpolationMethod.LinearExact
                    => new SKSamplingOptions(SKFilterMode.Linear),
                InterpolationMethod.Cubic
                    => new SKSamplingOptions(SKCubicResampler.Mitchell),
                InterpolationMethod.Lanczos4
                    => new SKSamplingOptions(SKCubicResampler.CatmullRom),
                _ => new SKSamplingOptions(SKFilterMode.Nearest),
            };
            return src.Resize(info, sampling);
        }

        // byte[,,] (BGR) → SKBitmap (RGBA) — unsafe yuksek performans
        public static unsafe SKBitmap FromByteArray(byte[,,] data, int rows, int cols)
        {
            var bitmap = new SKBitmap(cols, rows, SKColorType.Rgba8888, SKAlphaType.Opaque);
            IntPtr ptr = bitmap.GetPixels();
            byte* dest = (byte*)ptr;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    int offset = (i * cols + j) * 4;
                    dest[offset + 0] = data[i, j, 2]; // B → R
                    dest[offset + 1] = data[i, j, 1]; // G → G
                    dest[offset + 2] = data[i, j, 0]; // R → B
                    dest[offset + 3] = 255;            // A
                }
            }

            return bitmap;
        }

        // SKBitmap → byte[,,] (BGR) — unsafe yuksek performans
        // Bgra8888 (B,G,R,A) ve Rgba8888 (R,G,B,A) formatlarini destekler
        public static unsafe byte[,,] ToByteArray(SKBitmap bitmap)
        {
            int rows = bitmap.Height;
            int cols = bitmap.Width;
            var data = new byte[rows, cols, 3];

            bool isBgra = bitmap.ColorType == SKColorType.Bgra8888;

            IntPtr ptr = bitmap.GetPixels();
            byte* src = (byte*)ptr;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    int offset = (i * cols + j) * 4;
                    if (isBgra)
                    {
                        data[i, j, 0] = src[offset + 0]; // B
                        data[i, j, 1] = src[offset + 1]; // G
                        data[i, j, 2] = src[offset + 2]; // R
                    }
                    else
                    {
                        data[i, j, 0] = src[offset + 2]; // B
                        data[i, j, 1] = src[offset + 1]; // G
                        data[i, j, 2] = src[offset + 0]; // R
                    }
                }
            }

            return data;
        }

        // CvInvoke.Line + PutText → SKCanvas.DrawLine/DrawText
        public static SKBitmap DrawOverlay(SKBitmap src, int stoneSize,
            bool showGrid, bool showMouldLines, bool showRowColNum,
            bool showMouldId, int penWidth, SKColor? gridColor = null)
        {
            var result = src.Copy();
            using var canvas = new SKCanvas(result);

            double smallInterval = stoneSize;
            double largeInterval = smallInterval * 26;

            float gridStroke = Math.Max(1, stoneSize / 6f);
            var grayPaint = new SKPaint
            {
                Color = gridColor ?? SKColors.Gray, StrokeWidth = gridStroke,
                Style = SKPaintStyle.Stroke, IsAntialias = false
            };
            var blackPaint = new SKPaint
            {
                Color = SKColors.Black, StrokeWidth = penWidth,
                Style = SKPaintStyle.Stroke, IsAntialias = false
            };
            var textPaint = new SKPaint
            {
                Color = SKColors.Black, IsAntialias = true
            };
            var textFont = new SKFont(SKTypeface.Default, 10);
            var textPaintLarge = new SKPaint
            {
                Color = SKColors.White, IsAntialias = true
            };
            var textFontLarge = new SKFont(SKTypeface.Default, (float)(largeInterval * 0.4))
            {
                Embolden = true
            };

            if (showGrid)
            {
                // Yatay cizgiler
                for (double y = 0; y < result.Height; y += smallInterval)
                    canvas.DrawLine(0, (float)y, result.Width, (float)y, grayPaint);
                // Dikey cizgiler
                for (double x = 0; x < result.Width; x += smallInterval)
                    canvas.DrawLine((float)x, 0, (float)x, result.Height, grayPaint);
            }

            if (showMouldLines)
            {
                // Yatay kalip cizgileri
                for (double y = 0; y < result.Height * 2; y += largeInterval)
                    canvas.DrawLine(0, (float)y, result.Width, (float)y, blackPaint);
                // Dikey kalip cizgileri
                for (double x = 0; x < result.Width * 2; x += largeInterval)
                    canvas.DrawLine((float)x, 0, (float)x, result.Height, blackPaint);
            }

            if (showMouldId)
            {
                int nu = 0;
                for (double y = 0; y < result.Height; y += largeInterval)
                {
                    for (double x = 0; x < result.Width; x += largeInterval)
                    {
                        float tx = (float)(x + 0.9 * largeInterval / 2);
                        float ty = (float)(y + 1.15 * largeInterval / 2);
                        // Siyah gölge + beyaz metin
                        using var shadowPaint = new SKPaint
                        {
                            Color = SKColors.Black, IsAntialias = true
                        };
                        canvas.DrawText(nu.ToString(), tx + 2, ty + 2, SKTextAlign.Left, textFontLarge, shadowPaint);
                        canvas.DrawText(nu.ToString(), tx, ty, SKTextAlign.Left, textFontLarge, textPaintLarge);
                        nu++;
                    }
                }
            }

            if (showRowColNum)
            {
                textFont.Size = (float)(smallInterval * 0.45);
                int nu = 0;
                for (double y = 0; y <= result.Height; y += largeInterval)
                {
                    for (double x = 0; x < result.Width; x += smallInterval)
                    {
                        float tx = (float)(x + smallInterval / 4.0);
                        float ty = (float)(y + smallInterval / 1.4);
                        canvas.DrawText(nu.ToString(), tx, ty, SKTextAlign.Left, textFont, textPaint);
                        nu++;
                        if (nu > 12) nu = 0;
                    }
                }
            }

            grayPaint.Dispose();
            blackPaint.Dispose();
            textPaint.Dispose();
            textPaintLarge.Dispose();
            textFont.Dispose();
            textFontLarge.Dispose();

            return result;
        }

        public static Avalonia.Media.Imaging.Bitmap ToAvaloniaBitmap(SKBitmap bmp)
        {
            long totalPixels = (long)bmp.Width * bmp.Height;
            if (totalPixels > 800_000_000L)
                throw new OutOfMemoryException(Loc.Fmt("StatusRsBitmapTooLarge", bmp.Width, bmp.Height));

            var wb = new Avalonia.Media.Imaging.WriteableBitmap(
                new Avalonia.PixelSize(bmp.Width, bmp.Height),
                new Avalonia.Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888,
                Avalonia.Platform.AlphaFormat.Premul);

            using var converted = bmp.Copy(SKColorType.Bgra8888);
            if (converted == null) return wb;

            using (var fb = wb.Lock())
            {
                long totalBytes = (long)bmp.Height * fb.RowBytes;
                if (converted.RowBytes == fb.RowBytes)
                {
                    unsafe
                    {
                        Buffer.MemoryCopy((void*)converted.GetPixels(), (void*)fb.Address, totalBytes, totalBytes);
                    }
                }
                else
                {
                    int copyLen = Math.Min(converted.RowBytes, fb.RowBytes);
                    unsafe
                    {
                        byte* src = (byte*)converted.GetPixels();
                        byte* dst = (byte*)fb.Address;
                        for (int y = 0; y < bmp.Height; y++)
                            Buffer.MemoryCopy(src + y * converted.RowBytes, dst + y * fb.RowBytes, fb.RowBytes, copyLen);
                    }
                }
            }
            return wb;
        }

        // Dosyaya kaydet (PNG/JPEG disa aktarma)
        public static void ExportImage(SKBitmap bmp, string path, SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 100)
        {
            using var image = SKImage.FromBitmap(bmp);
            using var data = image.Encode(format, quality);
            using var stream = File.Create(path);
            data.SaveTo(stream);
        }
    }
}
