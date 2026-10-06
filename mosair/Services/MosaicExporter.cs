using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SkiaSharp;

namespace mosair.Services
{
    // Writes the whole stone image (RS) to a JPEG or PNG file at any size the user asks for.
    // - Images that fit one SKBitmap (≤ ImageService.MaxBitmapPixels) are encoded by Skia exactly as before.
    // - Larger PNGs are streamed: one stone row at a time is drawn and compressed straight into the file, so
    //   memory stays small whatever the size.
    // - Larger JPEGs (each side ≤ 65,535 px, the format's own limit) are drawn into one native buffer and
    //   encoded from there; they need about width × height × 4 bytes of RAM.
    public static class MosaicExporter
    {
        public const int JpegMaxSide = 65535;

        public sealed record Size(int Width, int Height)
        {
            public long Pixels => (long)Width * Height;
        }

        public static Size ImageSize(MosaicRenderSource src, int n) => new(src.Cols * n, src.Rows * n);

        public static bool JpegPossible(Size s) => s.Width <= JpegMaxSide && s.Height <= JpegMaxSide;

        // RAM a JPEG of this size needs (the whole image in memory while encoding).
        public static long JpegMemoryBytes(Size s) => s.Pixels * 4;

        // Memory free right now: installed (or allowed) memory minus what the whole system is using.
        public static long FreeMemoryBytes()
        {
            var info = GC.GetGCMemoryInfo();
            return Math.Max(0, info.TotalAvailableMemoryBytes - info.MemoryLoadBytes);
        }

        // JPEG for the quick export when it is possible and its buffer takes at most half of the free memory;
        // otherwise PNG, which is streamed and needs almost no memory.
        public static bool QuickExportUsesJpeg(Size s)
        {
            if (!JpegPossible(s)) return false;
            if (s.Pixels <= ImageService.MaxBitmapPixels) return true;
            return JpegMemoryBytes(s) <= FreeMemoryBytes() / 2;
        }

        // Estimated file size: a few parts of the mosaic are really encoded the same way and the bytes per pixel
        // are scaled to the whole image. JPEG compresses in small blocks, so 512 × 512 px squares are enough
        // (measured within ~5%). PNG gains a lot from stone images repeating along a row, so it is sampled with
        // full-width stone rows (a 512-px square overestimated it about 2×).
        public static long EstimateBytes(MosaicRenderSource src, int n, bool grid, int gw, SKColor gc, bool jpeg)
        {
            var size = ImageSize(src, n);
            bool streamed = !jpeg && size.Pixels > ImageService.MaxBitmapPixels;
            long bytes = 0, pixels = 0;
            if (jpeg)
            {
                int ts = Math.Max(1, 512 / n);
                int rows = Math.Min(ts, src.Rows), cols = Math.Min(ts, src.Cols);
                double[] fx = { 0.25, 0.75, 0.5, 0.25, 0.75 }, fy = { 0.25, 0.25, 0.5, 0.75, 0.75 };
                for (int k = 0; k < fx.Length; k++)
                {
                    int r0 = Math.Clamp((int)(src.Rows * fy[k]) - rows / 2, 0, src.Rows - rows);
                    int c0 = Math.Clamp((int)(src.Cols * fx[k]) - cols / 2, 0, src.Cols - cols);
                    using var bmp = src.RenderRegion(r0, c0, rows, cols, n, grid, gw, gc);
                    bytes += EncodedSize(bmp, jpeg: true);
                    pixels += (long)bmp.Width * bmp.Height;
                }
            }
            else
            {
                foreach (double fy in new[] { 0.25, 0.5, 0.75 })
                {
                    int r0 = Math.Clamp((int)(src.Rows * fy), 0, src.Rows - 1);
                    using var bmp = src.RenderRegion(r0, 0, 1, src.Cols, n, grid, gw, gc);
                    bytes += streamed ? StreamedPngSize(bmp) : EncodedSize(bmp, jpeg: false);
                    pixels += (long)bmp.Width * bmp.Height;
                }
            }
            return pixels == 0 ? 0 : (long)((double)bytes / pixels * size.Pixels);
        }

        private static long EncodedSize(SKBitmap bmp, bool jpeg)
        {
            using var image = SKImage.FromBitmap(bmp);
            using var data = image.Encode(jpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, 100);
            return data?.Size ?? 0;
        }

        private static long StreamedPngSize(SKBitmap bmp)
        {
            using var counter = new CountingStream();
            using (var png = new StreamingPngWriter(counter, bmp.Width, bmp.Height))
                png.WriteRows(bmp, 0, bmp.Height);
            return counter.Length;
        }

        // progress: 0..1, reported from the worker thread.
        // Without stone images every stone is drawn in its own colour at the same size, so the file always has
        // the size shown in the menu. A failed export removes its half-written file.
        public static void Export(MosaicRenderSource src, string path, bool jpeg, int n, bool grid, int gw,
            SKColor gc, Action<double>? progress = null)
        {
            var size = ImageSize(src, n);
            if (jpeg && !JpegPossible(size))
                throw new InvalidOperationException(Loc.Fmt("ExportJpegTooLarge",
                    size.Width.ToString("N0"), size.Height.ToString("N0"), JpegMaxSide.ToString("N0")));
            try
            {
                if (size.Pixels <= ImageService.MaxBitmapPixels)
                {
                    // Fits one bitmap: the same Skia encoding as always.
                    using var bmp = src.RenderRegion(0, 0, src.Rows, src.Cols, n, grid, gw, gc);
                    ImageService.ExportImage(bmp, path, jpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png);
                    progress?.Invoke(1);
                }
                else if (jpeg) ExportLargeJpeg(src, path, size, n, grid, gw, gc, progress);
                else ExportStreamedPng(src, path, size, n, grid, gw, gc, progress);
            }
            catch
            {
                try { File.Delete(path); } catch { }
                throw;
            }
        }

        private static void ExportStreamedPng(MosaicRenderSource src, string path, Size size, int n, bool grid,
            int gw, SKColor gc, Action<double>? progress)
        {
            using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20);
            using var png = new StreamingPngWriter(file, size.Width, size.Height);
            // Draw the next stone row while the current one is being compressed.
            Task<SKBitmap>? next = Task.Run(() => src.RenderRegion(0, 0, 1, src.Cols, n, grid, gw, gc));
            for (int row = 0; row < src.Rows; row++)
            {
                using var band = next!.GetAwaiter().GetResult();
                int nextRow = row + 1;
                next = nextRow < src.Rows
                    ? Task.Run(() => src.RenderRegion(nextRow, 0, 1, src.Cols, n, grid, gw, gc))
                    : null;
                png.WriteRows(band, 0, band.Height);
                progress?.Invoke((double)(row + 1) / src.Rows);
            }
        }

        private static unsafe void ExportLargeJpeg(MosaicRenderSource src, string path, Size size, int n,
            bool grid, int gw, SKColor gc, Action<double>? progress)
        {
            long rowBytes = (long)size.Width * 4;
            byte* buffer = (byte*)NativeMemory.Alloc((nuint)(rowBytes * size.Height));
            try
            {
                for (int row = 0; row < src.Rows; row++)
                {
                    using var band = src.RenderRegion(row, 0, 1, src.Cols, n, grid, gw, gc);
                    byte* from = (byte*)band.GetPixels();
                    for (int y = 0; y < band.Height; y++)
                        Buffer.MemoryCopy(from + (long)y * band.RowBytes, buffer + ((long)row * n + y) * rowBytes,
                            rowBytes, rowBytes);
                    progress?.Invoke(0.8 * (row + 1) / src.Rows);
                }
                var info = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
                using var pixmap = new SKPixmap(info, (IntPtr)buffer, (int)rowBytes);
                using var file = File.Create(path);
                using var stream = new SKManagedWStream(file);
                if (!pixmap.Encode(stream, new SKJpegEncoderOptions(100, SKJpegEncoderDownsample.Downsample420,
                        SKJpegEncoderAlphaOption.Ignore)))
                    throw new IOException(Loc.Get("ExportJpegFailed"));
                progress?.Invoke(1);
            }
            finally
            {
                NativeMemory.Free(buffer);
            }
        }

        // Minimal PNG writer (8-bit RGB, no interlace) that takes rows as they come and compresses them straight
        // into the output, so the whole image never has to be in memory.
        private sealed class StreamingPngWriter : IDisposable
        {
            private readonly Stream _out;
            private readonly IdatStream _idat;
            private readonly ZLibStream _zlib;
            private readonly byte[] _row;

            public StreamingPngWriter(Stream output, int width, int height)
            {
                _out = output;
                _out.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
                var ihdr = new byte[13];
                WriteBE(ihdr, 0, (uint)width);
                WriteBE(ihdr, 4, (uint)height);
                ihdr[8] = 8;   // bit depth
                ihdr[9] = 2;   // colour type: RGB
                WriteChunk(_out, "IHDR", ihdr, ihdr.Length);
                _idat = new IdatStream(_out);
                _zlib = new ZLibStream(_idat, CompressionLevel.Fastest, leaveOpen: true);
                _row = new byte[1 + width * 3];
            }

            public unsafe void WriteRows(SKBitmap rgba, int y0, int count)
            {
                byte* basePtr = (byte*)rgba.GetPixels();
                int w = rgba.Width;
                for (int y = y0; y < y0 + count; y++)
                {
                    byte* p = basePtr + (long)y * rgba.RowBytes;
                    _row[0] = 0;   // filter: none
                    for (int x = 0, o = 1; x < w; x++, o += 3)
                    {
                        _row[o] = p[x * 4];
                        _row[o + 1] = p[x * 4 + 1];
                        _row[o + 2] = p[x * 4 + 2];
                    }
                    _zlib.Write(_row, 0, _row.Length);
                }
            }

            public void Dispose()
            {
                _zlib.Dispose();
                _idat.Flush();
                WriteChunk(_out, "IEND", Array.Empty<byte>(), 0);
                _out.Flush();
            }
        }

        // Cuts the compressed stream into IDAT chunks of up to 1 MB.
        private sealed class IdatStream : Stream
        {
            private readonly Stream _out;
            private readonly byte[] _buf = new byte[1 << 20];
            private int _len;

            public IdatStream(Stream output) { _out = output; }

            public override void Write(byte[] buffer, int offset, int count)
            {
                while (count > 0)
                {
                    int n = Math.Min(count, _buf.Length - _len);
                    Buffer.BlockCopy(buffer, offset, _buf, _len, n);
                    _len += n; offset += n; count -= n;
                    if (_len == _buf.Length) Flush();
                }
            }

            public override void Flush()
            {
                if (_len == 0) return;
                WriteChunk(_out, "IDAT", _buf, _len);
                _len = 0;
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
        }

        // Counts bytes instead of storing them (for the size estimate).
        private sealed class CountingStream : Stream
        {
            private long _length;
            public override void Write(byte[] buffer, int offset, int count) => _length += count;
            public override void Write(ReadOnlySpan<byte> buffer) => _length += buffer.Length;
            public override void Flush() { }
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => _length;
            public override long Position { get => _length; set => throw new NotSupportedException(); }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
        }

        private static void WriteChunk(Stream s, string type, byte[] data, int length)
        {
            var head = new byte[8];
            WriteBE(head, 0, (uint)length);
            for (int i = 0; i < 4; i++) head[4 + i] = (byte)type[i];
            s.Write(head, 0, 8);
            s.Write(data, 0, length);
            uint crc = Crc32(head, 4, 4, 0xFFFFFFFFu);
            crc = Crc32(data, 0, length, crc) ^ 0xFFFFFFFFu;
            var tail = new byte[4];
            WriteBE(tail, 0, crc);
            s.Write(tail, 0, 4);
        }

        private static void WriteBE(byte[] b, int at, uint v)
        {
            b[at] = (byte)(v >> 24); b[at + 1] = (byte)(v >> 16); b[at + 2] = (byte)(v >> 8); b[at + 3] = (byte)v;
        }

        private static readonly uint[] CrcTable = BuildCrcTable();

        private static uint[] BuildCrcTable()
        {
            var t = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                t[n] = c;
            }
            return t;
        }

        private static uint Crc32(byte[] data, int offset, int length, uint crc)
        {
            for (int i = offset; i < offset + length; i++) crc = CrcTable[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
            return crc;
        }
    }
}
