using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using mosair.Models;

namespace mosair.Services
{
    public static class StoneTextureService
    {
        private static Dictionary<string, List<byte[,,]>> _textures = new();
        private static Dictionary<string, List<byte[,,]>> _resizedTextures = new();
        private static string[]? _rsDirNames;
        private static string? _rsBasePath;

        public static string? FindRSPath()
        {
            string exeDir = AppContext.BaseDirectory;

            string path1 = Path.Combine(exeDir, "Assets", "02_RS");
            if (Directory.Exists(path1)) return path1;

            string path2 = Path.Combine(exeDir, "mosaicFiles", "02_RS");
            if (Directory.Exists(path2)) return path2;

            string path3 = Path.Combine(exeDir, "02_RS");
            if (Directory.Exists(path3)) return path3;

            string path4 = @"D:\dev\mosairWPF\ourRobotWpf\bin\x64\Debug\mosaicFiles\02_RS";
            if (Directory.Exists(path4)) return path4;

            return null;
        }

        public static void PopulateRandomIndices(int R, int C)
        {
            var rand = new Random();
            MosaicData.arn = new int[R * C];
            for (int i = 0; i < R * C; i++)
                MosaicData.arn[i] = rand.Next(1, 16);
        }

        public static void LoadTextures()
        {
            _textures.Clear();
            _resizedTextures.Clear();

            _rsBasePath = FindRSPath();
            if (_rsBasePath == null) return;

            _rsDirNames = Directory.GetDirectories(_rsBasePath);

            var codeNames = new HashSet<string>();
            foreach (var arList in MosaicData.arMA)
                foreach (var color in arList)
                    if (!string.IsNullOrEmpty(color.codeName))
                        codeNames.Add(color.codeName);

            var colorList = new List<(string codeName, rgb color)>();
            foreach (var arList in MosaicData.arMA)
                foreach (var color in arList)
                    if (!string.IsNullOrEmpty(color.codeName) && codeNames.Remove(color.codeName))
                        colorList.Add((color.codeName, color));

            var results = new System.Collections.Concurrent.ConcurrentDictionary<string, List<byte[,,]>>();

            System.Threading.Tasks.Parallel.ForEach(colorList, item =>
            {
                string? folderPath = FindFolder(item.codeName);
                var texList = new List<byte[,,]>();

                for (int i = 1; i <= 16; i++)
                {
                    byte[,,] data;
                    if (folderPath != null)
                    {
                        string imgPath = Path.Combine(folderPath, $"{i}.jpg");
                        if (File.Exists(imgPath))
                        {
                            using var bmp = SKBitmap.Decode(imgPath);
                            if (bmp != null)
                            {
                                data = ImageService.ToByteArray(bmp);
                                texList.Add(data);
                                continue;
                            }
                        }
                    }
                    data = CreateSolidTexture(item.color, 3);
                    texList.Add(data);
                }
                results[item.codeName] = texList;
            });

            foreach (var kvp in results)
                _textures[kvp.Key] = kvp.Value;
        }

        private static string? FindFolder(string codeName)
        {
            if (_rsDirNames == null) return null;
            for (int j = 0; j < _rsDirNames.Length; j++)
            {
                string dirName = Path.GetFileName(_rsDirNames[j]);
                int pos = dirName.IndexOf(codeName);
                if (pos > 0) return _rsDirNames[j];
            }
            return null;
        }

        public static string? FindFolderForCode(string codeName)
        {
            if (_rsBasePath == null)
                _rsBasePath = FindRSPath();
            if (_rsBasePath == null) return null;
            if (_rsDirNames == null)
                _rsDirNames = Directory.GetDirectories(_rsBasePath);
            return FindFolder(codeName);
        }

        public static SKBitmap? LoadSingleThumbnail(string codeName, int width, int height)
        {
            string? folder = FindFolderForCode(codeName);
            if (folder == null) return null;
            string path = Path.Combine(folder, "1.jpg");
            if (!File.Exists(path)) return null;
            using var src = SKBitmap.Decode(path);
            if (src == null) return null;
            return src.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKFilterMode.Linear));
        }

        public static List<SKBitmap> LoadTooltipImages(string codeName, int thumbSize = 60)
        {
            var images = new List<SKBitmap>();
            string? folder = FindFolderForCode(codeName);
            if (folder == null) return images;

            for (int i = 1; i <= 16; i++)
            {
                string path = Path.Combine(folder, $"{i}.jpg");
                if (File.Exists(path))
                {
                    using var src = SKBitmap.Decode(path);
                    if (src != null)
                    {
                        var info = new SKImageInfo(thumbSize, thumbSize);
                        var resized = src.Resize(info, new SKSamplingOptions(SKFilterMode.Linear));
                        images.Add(resized);
                    }
                }
            }
            return images;
        }

        private static byte[,,] CreateSolidTexture(rgb color, int size)
        {
            var data = new byte[size, size, 3];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    data[y, x, 0] = (byte)color.b;
                    data[y, x, 1] = (byte)color.g;
                    data[y, x, 2] = (byte)color.r;
                }
            return data;
        }

        public static void ResizeTextures(int N)
        {
            _resizedTextures.Clear();
            var keys = new List<string>(_textures.Keys);
            var results = new System.Collections.Concurrent.ConcurrentDictionary<string, List<byte[,,]>>();

            System.Threading.Tasks.Parallel.ForEach(keys, key =>
            {
                var resizedList = new List<byte[,,]>();
                foreach (var tex in _textures[key])
                {
                    int srcH = tex.GetLength(0);
                    int srcW = tex.GetLength(1);
                    if (srcH == N && srcW == N)
                    {
                        resizedList.Add(tex);
                        continue;
                    }
                    using var srcBmp = ImageService.FromByteArray(tex, srcH, srcW);
                    using var resized = ImageService.Resize(srcBmp, N, N);
                    resizedList.Add(ImageService.ToByteArray(resized));
                }
                results[key] = resizedList;
            });

            foreach (var kvp in results)
                _resizedTextures[kvp.Key] = kvp.Value;
        }

        public static unsafe SKBitmap? GenerateRSBitmap(int R, int C, int N,
            bool showGrid = false, int gridWidth = 0, SKColor gridColor = default)
        {
            if (_resizedTextures.Count == 0) return null;

            var colorToCodeName = new Dictionary<(byte b, byte g, byte r), string>();
            foreach (var arList in MosaicData.arMB)
            {
                foreach (var color in arList)
                {
                    if (string.IsNullOrEmpty(color.codeName)) continue;
                    var key = ((byte)color.b, (byte)color.g, (byte)color.r);
                    colorToCodeName.TryAdd(key, color.codeName);
                }
            }
            foreach (var arList in MosaicData.arMA)
            {
                foreach (var color in arList)
                {
                    if (string.IsNullOrEmpty(color.codeName)) continue;
                    var key = ((byte)color.b, (byte)color.g, (byte)color.r);
                    colorToCodeName.TryAdd(key, color.codeName);
                }
            }

            // Pre-build fallback lookup: cache nearest codeName per unique color
            var fallbackCache = new Dictionary<(byte, byte, byte), string>();
            var validEntries = new List<((byte b, byte g, byte r) key, string code)>();
            foreach (var kvp in colorToCodeName)
            {
                if (_resizedTextures.ContainsKey(kvp.Value))
                    validEntries.Add((kvp.Key, kvp.Value));
            }

            int bmpW = C * N;
            int bmpH = R * N;
            long totalPixels = (long)bmpW * bmpH;
            if (totalPixels > 800_000_000L)
                throw new OutOfMemoryException(Loc.Fmt("StatusRsBitmapTooLarge", bmpW, bmpH));
            var bitmap = new SKBitmap(bmpW, bmpH, SKColorType.Rgba8888, SKAlphaType.Opaque);
            byte* dest = (byte*)bitmap.GetPixels();
            int stride = bmpW * 4;

            if (showGrid && gridWidth > 0)
            {
                byte gr = gridColor.Red, gg = gridColor.Green, gb = gridColor.Blue;
                System.Threading.Tasks.Parallel.For(0, bmpH, y =>
                {
                    int rowOff = y * stride;
                    for (int x = 0; x < bmpW; x++)
                    {
                        int off = rowOff + x * 4;
                        dest[off + 0] = gr;
                        dest[off + 1] = gg;
                        dest[off + 2] = gb;
                        dest[off + 3] = 255;
                    }
                });
            }

            int half = gridWidth / 2;
            int stoneN = showGrid && gridWidth > 0 ? N - gridWidth : N;

            // Pre-map each cell to its codeName (single-threaded, fast)
            string?[] cellCodes = new string[R * C];
            for (int i = 0; i < R; i++)
            {
                for (int j = 0; j < C; j++)
                {
                    byte pb = MosaicData.dataM3[i, j, 0];
                    byte pg = MosaicData.dataM3[i, j, 1];
                    byte pr = MosaicData.dataM3[i, j, 2];
                    var key = (pb, pg, pr);

                    if (colorToCodeName.TryGetValue(key, out var code) && _resizedTextures.ContainsKey(code))
                    {
                        cellCodes[i * C + j] = code;
                    }
                    else
                    {
                        if (!fallbackCache.TryGetValue(key, out var fb))
                        {
                            double minDist = double.MaxValue;
                            string? best = null;
                            foreach (var entry in validEntries)
                            {
                                double db = pb - entry.key.b;
                                double dg = pg - entry.key.g;
                                double dr = pr - entry.key.r;
                                double dist = db * db + dg * dg + dr * dr;
                                if (dist < minDist) { minDist = dist; best = entry.code; }
                            }
                            fb = best;
                            fallbackCache[key] = fb!;
                        }
                        cellCodes[i * C + j] = fb;
                    }
                }
            }

            // Parallel texture blitting by row
            System.Threading.Tasks.Parallel.For(0, R, i =>
            {
                for (int j = 0; j < C; j++)
                {
                    int idx = i * C + j;
                    string? codeName = cellCodes[idx];
                    int n = MosaicData.arn[idx];

                    if (codeName != null &&
                        _resizedTextures.TryGetValue(codeName, out var textures) &&
                        n < textures.Count)
                    {
                        byte[,,] tex = textures[n];
                        int texH = tex.GetLength(0);
                        int texW = tex.GetLength(1);
                        int baseY = i * N;
                        int baseX = j * N;

                        if (showGrid && gridWidth > 0 && stoneN > 0)
                        {
                            int blitH = Math.Min(stoneN, texH);
                            int blitW = Math.Min(stoneN, texW);
                            for (int ty = 0; ty < blitH; ty++)
                            {
                                int rowOff = (baseY + half + ty) * stride + (baseX + half) * 4;
                                for (int tx = 0; tx < blitW; tx++)
                                {
                                    int off = rowOff + tx * 4;
                                    dest[off + 0] = tex[ty, tx, 2];
                                    dest[off + 1] = tex[ty, tx, 1];
                                    dest[off + 2] = tex[ty, tx, 0];
                                    dest[off + 3] = 255;
                                }
                            }
                        }
                        else
                        {
                            int blitH = Math.Min(N, texH);
                            int blitW = Math.Min(N, texW);
                            for (int ty = 0; ty < blitH; ty++)
                            {
                                int rowOff = (baseY + ty) * stride + baseX * 4;
                                for (int tx = 0; tx < blitW; tx++)
                                {
                                    int off = rowOff + tx * 4;
                                    dest[off + 0] = tex[ty, tx, 2];
                                    dest[off + 1] = tex[ty, tx, 1];
                                    dest[off + 2] = tex[ty, tx, 0];
                                    dest[off + 3] = 255;
                                }
                            }
                        }
                    }
                }
            });

            return bitmap;
        }

        public static void Reset()
        {
            _textures.Clear();
            _resizedTextures.Clear();
            _rsDirNames = null;
        }
    }
}
