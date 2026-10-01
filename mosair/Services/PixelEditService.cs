using System;
using System.Collections.Generic;
using mosair.Models;
using SkiaSharp;

namespace mosair.Services
{
    public class PixelEditRecord
    {
        public int Y { get; set; }
        public int X { get; set; }
        public rgb Source { get; set; } = new();
        public rgb Target { get; set; } = new();
        public rgb Original { get; set; } = new();
    }

    public static class PixelEditService
    {
        public static List<PixelEditRecord> EditedPixels { get; } = new();
        public static PixelEditRecord? Current { get; set; }
        public static bool IsSourcePixelMode { get; set; }
        public static bool IsTargetPixelMode { get; set; }
        public static bool IsPixelEditActive { get; set; }

        private static readonly Stack<PixelEditRecord> _undoStack = new();
        private static readonly Stack<PixelEditRecord> _redoStack = new();
        public static bool CanUndo => _undoStack.Count > 0;
        public static bool CanRedo => _redoStack.Count > 0;

        public static void Reset()
        {
            EditedPixels.Clear();
            _undoStack.Clear();
            _redoStack.Clear();
            Current = null;
            IsSourcePixelMode = false;
            IsTargetPixelMode = false;
            IsPixelEditActive = false;
        }

        public static void TogglePixelEditMode()
        {
            IsPixelEditActive = !IsPixelEditActive;
            if (IsPixelEditActive)
            {
                IsSourcePixelMode = true;
                IsTargetPixelMode = false;
                Current = new PixelEditRecord();
            }
            else
            {
                IsSourcePixelMode = false;
                IsTargetPixelMode = false;
            }
        }

        public static void SetSourcePixel(int y, int x, byte r, byte g, byte b, int id, string codeName = "")
        {
            if (Current == null) Current = new PixelEditRecord();
            Current.Source = new rgb { r = r, g = g, b = b, ID = id, codeName = codeName };
            IsSourcePixelMode = false;
            IsTargetPixelMode = true;
        }

        public static string EditPixel(int y, int x, byte r, byte g, byte b, int id, string codeName = "")
        {
            if (Current == null) return "no source selected";

            Current.Target = new rgb { r = r, g = g, b = b, ID = id, codeName = codeName };
            Current.Y = y;
            Current.X = x;

            if (Current.Source.ID == Current.Target.ID)
                return "same color, skipped";

            PixelEditRecord? existing = null;
            int existingIdx = -1;
            bool sourceIsOriginal = false;

            for (int i = 0; i < EditedPixels.Count; i++)
            {
                var p = EditedPixels[i];
                if (p.Y == y && p.X == x)
                {
                    if (p.Source.ID == Current.Source.ID)
                        return "already edited with same source";

                    if (p.Original.ID == Current.Source.ID)
                        sourceIsOriginal = true;

                    existing = p;
                    existingIdx = i;
                    break;
                }
            }

            if (existing != null && !sourceIsOriginal)
            {
                MosaicData.dataM3[y, x, 0] = (byte)Current.Source.b;
                MosaicData.dataM3[y, x, 1] = (byte)Current.Source.g;
                MosaicData.dataM3[y, x, 2] = (byte)Current.Source.r;
                drl.dat[y, x, 3] = Current.Source.ID;
                EditedPixels[existingIdx].Source = CloneRgb(Current.Source);
                EditedPixels[existingIdx].Target = CloneRgb(Current.Target);
                UpdateRSForPixel(EditedPixels[existingIdx]);
                _undoStack.Push(EditedPixels[existingIdx]);
                _redoStack.Clear();
                return $"replaced #{existingIdx + 1}";
            }
            else if (existing != null && sourceIsOriginal)
            {
                MosaicData.dataM3[y, x, 0] = (byte)existing.Original.b;
                MosaicData.dataM3[y, x, 1] = (byte)existing.Original.g;
                MosaicData.dataM3[y, x, 2] = (byte)existing.Original.r;
                drl.dat[y, x, 3] = existing.Original.ID;
                RestoreRSForPixel(existing);
                EditedPixels.RemoveAt(existingIdx);
                _redoStack.Clear();
                return $"restored to original";
            }
            else
            {
                Current.Original = new rgb
                {
                    r = Current.Target.r,
                    g = Current.Target.g,
                    b = Current.Target.b,
                    ID = Current.Target.ID
                };

                MosaicData.dataM3[Current.Y, Current.X, 0] = (byte)Current.Source.b;
                MosaicData.dataM3[Current.Y, Current.X, 1] = (byte)Current.Source.g;
                MosaicData.dataM3[Current.Y, Current.X, 2] = (byte)Current.Source.r;
                drl.dat[Current.Y, Current.X, 3] = Current.Source.ID;

                var record = new PixelEditRecord
                {
                    Y = Current.Y,
                    X = Current.X,
                    Source = CloneRgb(Current.Source),
                    Target = CloneRgb(Current.Target),
                    Original = CloneRgb(Current.Original)
                };
                EditedPixels.Add(record);
                UpdateRSForPixel(record);
                _undoStack.Push(record);
                _redoStack.Clear();
                return $"edited pixel ({EditedPixels.Count} total)";
            }
        }

        private static unsafe void PatchRSRegion(PixelEditRecord p, rgb color)
        {
            if (MosaicData.rsBitmap == null) return;

            int N = MosaicData.N;
            int baseY = p.Y * N;
            int baseX = p.X * N;
            int rsW = MosaicData.rsBitmap.Width;
            int rsH = MosaicData.rsBitmap.Height;
            if (baseY + N > rsH || baseX + N > rsW) return;

            var codeName = FindCodeNameForColor(color);
            if (codeName == null && !string.IsNullOrEmpty(color.codeName))
                codeName = color.codeName;
            SKBitmap? texBmp = null;

            if (codeName != null)
            {
                var folder = StoneTextureService.FindFolderForCode(codeName);
                if (folder != null)
                {
                    int texIdx = new Random().Next(1, 16);
                    string imgPath = System.IO.Path.Combine(folder, $"{texIdx}.jpg");
                    if (System.IO.File.Exists(imgPath))
                    {
                        var src = SKBitmap.Decode(imgPath);
                        if (src != null)
                        {
                            var info = new SKImageInfo(N, N);
                            texBmp = src.Resize(info, new SKSamplingOptions(SKFilterMode.Linear));
                            src.Dispose();
                        }
                    }
                }
            }

            bool isRgba = MosaicData.rsBitmap.ColorType == SKColorType.Rgba8888;
            byte* rsPtr = (byte*)MosaicData.rsBitmap.GetPixels();
            int rsStride = MosaicData.rsBitmap.RowBytes;

            if (texBmp != null)
            {
                bool texRgba = texBmp.ColorType == SKColorType.Rgba8888;
                byte* texPtr = (byte*)texBmp.GetPixels();
                int texStride = texBmp.RowBytes;

                for (int dy = 0; dy < N; dy++)
                {
                    byte* rsRow = rsPtr + (baseY + dy) * rsStride + baseX * 4;
                    byte* texRow = texPtr + dy * texStride;
                    for (int dx = 0; dx < N; dx++)
                    {
                        byte tr, tg, tb;
                        if (texRgba) { tr = texRow[0]; tg = texRow[1]; tb = texRow[2]; }
                        else { tb = texRow[0]; tg = texRow[1]; tr = texRow[2]; }

                        if (isRgba) { rsRow[0] = tr; rsRow[1] = tg; rsRow[2] = tb; rsRow[3] = 255; }
                        else { rsRow[0] = tb; rsRow[1] = tg; rsRow[2] = tr; rsRow[3] = 255; }

                        rsRow += 4;
                        texRow += 4;
                    }
                }
                texBmp.Dispose();
            }
            else
            {
                byte cr = (byte)color.r, cg = (byte)color.g, cb = (byte)color.b;
                for (int dy = 0; dy < N; dy++)
                {
                    byte* rsRow = rsPtr + (baseY + dy) * rsStride + baseX * 4;
                    for (int dx = 0; dx < N; dx++)
                    {
                        if (isRgba) { rsRow[0] = cr; rsRow[1] = cg; rsRow[2] = cb; rsRow[3] = 255; }
                        else { rsRow[0] = cb; rsRow[1] = cg; rsRow[2] = cr; rsRow[3] = 255; }
                        rsRow += 4;
                    }
                }
            }
        }

        private static void UpdateRSForPixel(PixelEditRecord p)
        {
            PatchRSRegion(p, p.Source);
        }

        private static void RestoreRSForPixel(PixelEditRecord p)
        {
            PatchRSRegion(p, p.Original);
        }

        private static string? FindCodeNameForColor(rgb color)
        {
            foreach (var arList in MosaicData.arMA)
            {
                foreach (var c in arList)
                {
                    if (c.r == color.r && c.g == color.g && c.b == color.b)
                        return c.codeName;
                }
            }
            return null;
        }

        private static rgb CloneRgb(rgb src)
        {
            return new rgb { r = src.r, g = src.g, b = src.b, ID = src.ID, codeName = src.codeName };
        }

        public static string UndoLastEdit()
        {
            if (_undoStack.Count == 0) return "nothing to undo";

            var record = _undoStack.Pop();

            MosaicData.dataM3[record.Y, record.X, 0] = (byte)record.Original.b;
            MosaicData.dataM3[record.Y, record.X, 1] = (byte)record.Original.g;
            MosaicData.dataM3[record.Y, record.X, 2] = (byte)record.Original.r;
            drl.dat[record.Y, record.X, 3] = record.Original.ID;
            RestoreRSForPixel(record);

            for (int i = EditedPixels.Count - 1; i >= 0; i--)
            {
                if (EditedPixels[i].Y == record.Y && EditedPixels[i].X == record.X)
                {
                    EditedPixels.RemoveAt(i);
                    break;
                }
            }

            _redoStack.Push(record);
            return $"undo ({_undoStack.Count} remaining)";
        }

        public static string RedoLastEdit()
        {
            if (_redoStack.Count == 0) return "nothing to redo";

            var record = _redoStack.Pop();

            MosaicData.dataM3[record.Y, record.X, 0] = (byte)record.Source.b;
            MosaicData.dataM3[record.Y, record.X, 1] = (byte)record.Source.g;
            MosaicData.dataM3[record.Y, record.X, 2] = (byte)record.Source.r;
            drl.dat[record.Y, record.X, 3] = record.Source.ID;
            UpdateRSForPixel(record);

            EditedPixels.Add(new PixelEditRecord
            {
                Y = record.Y,
                X = record.X,
                Source = CloneRgb(record.Source),
                Target = CloneRgb(record.Target),
                Original = CloneRgb(record.Original)
            });

            _undoStack.Push(record);
            return $"redo ({_redoStack.Count} remaining)";
        }

    }
}
