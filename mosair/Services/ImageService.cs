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
        public static SKBitmap? LoadImage(string path)
        {
            if (!File.Exists(path)) return null;
            using var stream = File.OpenRead(path);
            using var codec = SKCodec.Create(stream);
            if (codec == null) return null;

            var bitmap = SKBitmap.Decode(codec);
            if (bitmap == null) return null;

            var origin = codec.EncodedOrigin;
            if (origin == SKEncodedOrigin.Default || origin == SKEncodedOrigin.TopLeft)
                return bitmap;

            var rotated = ApplyOrientation(bitmap, origin);
            bitmap.Dispose();
            return rotated;
        }

        private static SKBitmap ApplyOrientation(SKBitmap src, SKEncodedOrigin origin)
        {
            bool swap = origin >= SKEncodedOrigin.LeftTop;
            int w = swap ? src.Height : src.Width;
            int h = swap ? src.Width : src.Height;

            var result = new SKBitmap(w, h, src.ColorType, src.AlphaType);
            using var canvas = new SKCanvas(result);

            switch (origin)
            {
                case SKEncodedOrigin.TopRight:
                    canvas.Scale(-1, 1, w / 2f, 0);
                    break;
                case SKEncodedOrigin.BottomRight:
                    canvas.RotateDegrees(180, w / 2f, h / 2f);
                    break;
                case SKEncodedOrigin.BottomLeft:
                    canvas.Scale(1, -1, 0, h / 2f);
                    break;
                case SKEncodedOrigin.LeftTop:
                    canvas.Translate(0, 0);
                    canvas.RotateDegrees(90, 0, 0);
                    canvas.Translate(0, -h);
                    canvas.Scale(1, -1, 0, h / 2f);
                    break;
                case SKEncodedOrigin.RightTop:
                    canvas.Translate(w, 0);
                    canvas.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.RightBottom:
                    canvas.Translate(w, 0);
                    canvas.RotateDegrees(90);
                    canvas.Scale(1, -1, 0, h / 2f);
                    break;
                case SKEncodedOrigin.LeftBottom:
                    canvas.Translate(0, h);
                    canvas.RotateDegrees(-90);
                    break;
            }

            canvas.DrawBitmap(src, 0, 0);
            return result;
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

        // Largest RGBA bitmap SkiaSharp can allocate: its byte size must fit in an int (2 GB), i.e. ~536.9M pixels.
        // Above this SKBitmap fails with "Unable to allocate pixels", so callers check against this first.
        public const long MaxBitmapPixels = int.MaxValue / 4;

        public static Avalonia.Media.Imaging.Bitmap ToAvaloniaBitmap(SKBitmap bmp)
        {
            long totalPixels = (long)bmp.Width * bmp.Height;
            if (totalPixels > MaxBitmapPixels)
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

        // ----- Size tag: an image exported before Mos remembers its mosaic size, so opening it again brings back the
        // same width (e.g. 156 cm with its Kalıp Dolgu padding) and the same stone grid. A JPEG gets it as a comment
        // segment (COM) right after SOI, a PNG as a tEXt chunk right after IHDR; both read "mosairSize=W;C;R;". -----
        private const string SizeTagKey = "mosairSize=";

        public sealed record SizeTag(double WidthCm, int Columns, int Rows);

        // Returns the encoded file with the tag added (unchanged when the format is not recognised).
        public static byte[] AddSizeTag(byte[] file, SizeTag tag)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            byte[] text = System.Text.Encoding.ASCII.GetBytes(
                $"{SizeTagKey}{tag.WidthCm.ToString("0.0", inv)};{tag.Columns};{tag.Rows};");
            if (file.Length > 2 && file[0] == 0xFF && file[1] == 0xD8)
            {
                int len = text.Length + 2;
                var seg = new byte[4 + text.Length];
                seg[0] = 0xFF; seg[1] = 0xFE; seg[2] = (byte)(len >> 8); seg[3] = (byte)len;
                Buffer.BlockCopy(text, 0, seg, 4, text.Length);
                return Splice(file, 2, seg);
            }
            if (file.Length > 33 && file[0] == 0x89 && file[1] == (byte)'P' && file[12] == (byte)'I' && file[15] == (byte)'R')
            {
                byte[] keyword = System.Text.Encoding.ASCII.GetBytes("mosair\0");
                keyword[^1] = 0;
                var data = new byte[keyword.Length + text.Length];
                Buffer.BlockCopy(keyword, 0, data, 0, keyword.Length);
                Buffer.BlockCopy(text, 0, data, keyword.Length, text.Length);
                var chunk = new byte[12 + data.Length];
                WriteBE(chunk, 0, (uint)data.Length);
                chunk[4] = (byte)'t'; chunk[5] = (byte)'E'; chunk[6] = (byte)'X'; chunk[7] = (byte)'t';
                Buffer.BlockCopy(data, 0, chunk, 8, data.Length);
                WriteBE(chunk, 8 + data.Length, Crc32(chunk, 4, 4 + data.Length));
                return Splice(file, 33, chunk);   // 8-byte signature + 25-byte IHDR chunk
            }
            return file;
        }

        // The tag of an image file, or null (no tag, unreadable file).
        public static SizeTag? ReadSizeTag(string path)
        {
            try
            {
                using var fs = File.OpenRead(path);
                var buf = new byte[(int)Math.Min(fs.Length, 256 * 1024)];
                int n = fs.Read(buf, 0, buf.Length);
                string head = System.Text.Encoding.ASCII.GetString(buf, 0, n);
                int i = head.IndexOf(SizeTagKey, StringComparison.Ordinal);
                if (i < 0) return null;
                var parts = head.Substring(i + SizeTagKey.Length, Math.Min(64, head.Length - i - SizeTagKey.Length)).Split(';');
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                if (parts.Length < 3 || !double.TryParse(parts[0], System.Globalization.NumberStyles.Float, inv, out double w)
                    || !int.TryParse(parts[1], out int c) || !int.TryParse(parts[2], out int r)
                    || w <= 0 || c < 2 || r < 1) return null;
                return new SizeTag(w, c, r);
            }
            catch { return null; }
        }

        private static byte[] Splice(byte[] file, int at, byte[] insert)
        {
            var result = new byte[file.Length + insert.Length];
            Buffer.BlockCopy(file, 0, result, 0, at);
            Buffer.BlockCopy(insert, 0, result, at, insert.Length);
            Buffer.BlockCopy(file, at, result, at + insert.Length, file.Length - at);
            return result;
        }

        private static void WriteBE(byte[] b, int at, uint v)
        {
            b[at] = (byte)(v >> 24); b[at + 1] = (byte)(v >> 16); b[at + 2] = (byte)(v >> 8); b[at + 3] = (byte)v;
        }

        private static uint Crc32(byte[] b, int start, int count)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = start; i < start + count; i++)
            {
                crc ^= b[i];
                for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
            return ~crc;
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
