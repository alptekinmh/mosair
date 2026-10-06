using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using mosair.Models;

namespace mosair.Services
{
    public static class StoneTextureService
    {
        // Replaced, never cleared, so a MosaicRenderSource that captured it stays valid while the engine
        // prepares the next mosaic on another thread. Resized copies live in each MosaicRenderSource.
        private static Dictionary<string, List<byte[,,]>> _textures = new();
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
            _textures = new Dictionary<string, List<byte[,,]>>();

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
                results[item.codeName] = LoadTextureSet(item.codeName, item.color);
            });

            var textures = new Dictionary<string, List<byte[,,]>>();
            foreach (var kvp in results)
                textures[kvp.Key] = kvp.Value;
            _textures = textures;
        }

        // The 16 variant images of one stone (1.jpg .. 16.jpg); a missing image becomes a small solid tile.
        internal static List<byte[,,]> LoadTextureSet(string codeName, rgb color)
        {
            _rsBasePath ??= FindRSPath();
            if (_rsBasePath != null && _rsDirNames == null)
                _rsDirNames = Directory.GetDirectories(_rsBasePath);
            string? folderPath = FindFolder(codeName);
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
                data = CreateSolidTexture(color, 3);
                texList.Add(data);
            }
            return texList;
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

        // The variant images of one stone resized to N×N (images already N×N are reused as they are).
        internal static List<byte[,,]> ResizeSet(List<byte[,,]> originals, int N)
        {
            var resizedList = new List<byte[,,]>(originals.Count);
            foreach (var tex in originals)
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
            return resizedList;
        }

        // Snapshot of the current mosaic for drawing: stone colours, variants, palette codes and loaded textures.
        public static MosaicRenderSource CreateRenderSource() =>
            new MosaicRenderSource(MosaicData.dataM3, MosaicData.arn, _textures);

        // The whole stone-texture image (RS): every stone drawn with its texture variant at N px, grid baked in.
        // Used for export; the screen draws only the visible part through MosaicView with the same renderer.
        public static SKBitmap? GenerateRSBitmap(int R, int C, int N,
            bool showGrid = false, int gridWidth = 0, SKColor gridColor = default)
        {
            if (_textures.Count == 0) return null;
            long totalPixels = (long)C * N * R * N;
            if (totalPixels > ImageService.MaxBitmapPixels)
                throw new OutOfMemoryException(Loc.Fmt("StatusRsBitmapTooLarge", C * N, R * N));
            return CreateRenderSource().RenderRegion(0, 0, R, C, N, showGrid, gridWidth, gridColor);
        }

        public static void Reset()
        {
            _textures = new Dictionary<string, List<byte[,,]>>();
            _rsDirNames = null;
        }
    }
}
