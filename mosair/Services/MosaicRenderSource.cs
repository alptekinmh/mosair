using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SkiaSharp;
using mosair.Models;

namespace mosair.Services
{
    // Everything needed to draw the stone-texture image (RS) of one mosaic, captured once per mosaic version so
    // background tile rendering never races the engine (which clears and refills the static texture tables).
    // Stone colours (dataM3) and variants (arn) are referenced, not copied: pixel edits and variant changes made
    // on the UI thread show up on the next render of the affected tile.
    public sealed class MosaicRenderSource
    {
        public int Rows { get; }
        public int Cols { get; }
        public int Version { get; }

        private readonly byte[,,] _data;
        private readonly int[]? _arn;
        // Palette colour -> stone code (arMB first, then arMA), exactly as GenerateRSBitmap always mapped it.
        private readonly Dictionary<(byte b, byte g, byte r), string> _codeOf;
        // Codes whose textures were loaded for this mosaic (the palette); nearest-colour fallback picks among these.
        private readonly List<((byte b, byte g, byte r) key, string code)> _fallbackEntries;
        // Catalog colour -> code, for stones changed by pixel editing to a colour that is not in the palette.
        private readonly Dictionary<(byte b, byte g, byte r), (string code, rgb color)> _catalogOf;
        private readonly IReadOnlyDictionary<string, List<byte[,,]>> _paletteTextures;

        private readonly ConcurrentDictionary<(byte, byte, byte), string?> _fallbackCache = new();
        private readonly ConcurrentDictionary<string, List<byte[,,]>> _extraTextures = new();
        private readonly ConcurrentDictionary<(string code, int size), List<byte[,,]>> _resized = new();

        private static int _nextVersion;

        internal MosaicRenderSource(byte[,,] data, int[]? arn,
            IReadOnlyDictionary<string, List<byte[,,]>> paletteTextures)
        {
            _data = data;
            _arn = arn;
            Rows = data.GetLength(0);
            Cols = data.GetLength(1);
            Version = System.Threading.Interlocked.Increment(ref _nextVersion);
            _paletteTextures = paletteTextures;

            _codeOf = new Dictionary<(byte, byte, byte), string>();
            foreach (var list in MosaicData.arMB)
                foreach (var c in list)
                    if (!string.IsNullOrEmpty(c.codeName))
                        _codeOf.TryAdd(((byte)c.b, (byte)c.g, (byte)c.r), c.codeName);
            foreach (var list in MosaicData.arMA)
                foreach (var c in list)
                    if (!string.IsNullOrEmpty(c.codeName))
                        _codeOf.TryAdd(((byte)c.b, (byte)c.g, (byte)c.r), c.codeName);

            _fallbackEntries = new List<((byte, byte, byte), string)>();
            foreach (var kv in _codeOf)
                if (paletteTextures.ContainsKey(kv.Value))
                    _fallbackEntries.Add((kv.Key, kv.Value));

            _catalogOf = new Dictionary<(byte, byte, byte), (string, rgb)>();
            foreach (var c in MosaicData.arRGBAll)
                if (!string.IsNullOrEmpty(c.codeName))
                    _catalogOf.TryAdd(((byte)c.b, (byte)c.g, (byte)c.r), (c.codeName, c));
        }

        public bool HasTextures => _paletteTextures.Count > 0;

        // Same mosaic with its own copy of the stone colours and variants: export draws from this, so pixel
        // edits made while the file is being written do not reach it.
        public MosaicRenderSource WithStoneSnapshot() =>
            new MosaicRenderSource((byte[,,])_data.Clone(), (int[]?)_arn?.Clone(), _paletteTextures);

        // Draws stones [row0, row0+rows) × [col0, col0+cols) at N pixels per stone, with the same pixels the
        // whole-mosaic RS has at that place (grid baked in when showGrid).
        public unsafe SKBitmap RenderRegion(int row0, int col0, int rows, int cols, int N,
            bool showGrid, int gridWidth, SKColor gridColor)
        {
            int bmpW = cols * N;
            int bmpH = rows * N;
            var bitmap = new SKBitmap(bmpW, bmpH, SKColorType.Rgba8888, SKAlphaType.Opaque);
            byte* dest = (byte*)bitmap.GetPixels();
            long stride = (long)bmpW * 4;
            bool grid = showGrid && gridWidth > 0;

            if (grid)
            {
                byte gr = gridColor.Red, gg = gridColor.Green, gb = gridColor.Blue;
                for (int y = 0; y < bmpH; y++)
                {
                    long rowOff = y * stride;
                    for (int x = 0; x < bmpW; x++)
                    {
                        long off = rowOff + x * 4;
                        dest[off + 0] = gr;
                        dest[off + 1] = gg;
                        dest[off + 2] = gb;
                        dest[off + 3] = 255;
                    }
                }
            }

            int half = gridWidth / 2;
            int stoneN = grid ? N - gridWidth : N;
            if (stoneN <= 0) return bitmap;

            for (int i = 0; i < rows; i++)
            {
                int si = row0 + i;
                for (int j = 0; j < cols; j++)
                {
                    int sj = col0 + j;
                    byte pb = _data[si, sj, 0], pg = _data[si, sj, 1], pr = _data[si, sj, 2];
                    int n = _arn != null && si * Cols + sj < _arn.Length ? _arn[si * Cols + sj] : 0;
                    var textures = TexturesFor(pb, pg, pr, stoneN);
                    int baseY = i * N + (grid ? half : 0);
                    int baseX = j * N + (grid ? half : 0);

                    if (textures != null && n < textures.Count)
                    {
                        byte[,,] tex = textures[n];
                        int blitH = Math.Min(stoneN, tex.GetLength(0));
                        int blitW = Math.Min(stoneN, tex.GetLength(1));
                        for (int ty = 0; ty < blitH; ty++)
                        {
                            long rowOff = (baseY + ty) * stride + baseX * 4L;
                            for (int tx = 0; tx < blitW; tx++)
                            {
                                long off = rowOff + tx * 4;
                                dest[off + 0] = tex[ty, tx, 2];
                                dest[off + 1] = tex[ty, tx, 1];
                                dest[off + 2] = tex[ty, tx, 0];
                                dest[off + 3] = 255;
                            }
                        }
                    }
                    else if (!HasTextures)
                    {
                        // No stone images installed: draw the stone in its own colour.
                        for (int ty = 0; ty < stoneN; ty++)
                        {
                            long rowOff = (baseY + ty) * stride + baseX * 4L;
                            for (int tx = 0; tx < stoneN; tx++)
                            {
                                long off = rowOff + tx * 4;
                                dest[off + 0] = pr;
                                dest[off + 1] = pg;
                                dest[off + 2] = pb;
                                dest[off + 3] = 255;
                            }
                        }
                    }
                }
            }
            return bitmap;
        }

        // One stone per pixel (RGBA), the colour of each stone: the zoomed-out view and the navigator.
        public unsafe SKBitmap RenderOverview()
        {
            var bitmap = new SKBitmap(Cols, Rows, SKColorType.Rgba8888, SKAlphaType.Opaque);
            byte* dest = (byte*)bitmap.GetPixels();
            for (int i = 0; i < Rows; i++)
            {
                long rowOff = (long)i * Cols * 4;
                for (int j = 0; j < Cols; j++)
                {
                    long off = rowOff + j * 4L;
                    dest[off + 0] = _data[i, j, 2];
                    dest[off + 1] = _data[i, j, 1];
                    dest[off + 2] = _data[i, j, 0];
                    dest[off + 3] = 255;
                }
            }
            return bitmap;
        }

        private List<byte[,,]>? TexturesFor(byte b, byte g, byte r, int size)
        {
            string? code = CodeFor(b, g, r, out bool palette);
            if (code == null) return null;
            if (!_resized.TryGetValue((code, size), out var list))
            {
                List<byte[,,]>? originals = palette
                    ? (_paletteTextures.TryGetValue(code, out var p) ? p : null)
                    : _extraTextures.GetOrAdd(code, c => StoneTextureService.LoadTextureSet(c, _catalogOf[(b, g, r)].color));
                if (originals == null) return null;
                list = _resized.GetOrAdd((code, size), _ => StoneTextureService.ResizeSet(originals, size));
            }
            return list;
        }

        private string? CodeFor(byte b, byte g, byte r, out bool palette)
        {
            palette = true;
            var key = (b, g, r);
            if (_codeOf.TryGetValue(key, out var code) && _paletteTextures.ContainsKey(code))
                return code;
            if (!_codeOf.ContainsKey(key) && HasTextures && _catalogOf.TryGetValue(key, out var cat))
            {
                // A colour that only pixel editing can introduce: use that stone's own images.
                if (_paletteTextures.ContainsKey(cat.code)) return cat.code;
                palette = false;
                return cat.code;
            }
            return _fallbackCache.GetOrAdd(key, k =>
            {
                double minDist = double.MaxValue;
                string? best = null;
                foreach (var entry in _fallbackEntries)
                {
                    double db = k.Item1 - entry.key.b;
                    double dg = k.Item2 - entry.key.g;
                    double dr = k.Item3 - entry.key.r;
                    double dist = db * db + dg * dg + dr * dr;
                    if (dist < minDist) { minDist = dist; best = entry.code; }
                }
                return best;
            });
        }
    }
}
