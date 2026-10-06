using System;
using System.Collections.Generic;
using mosair.Models;

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

        // The stone the last edit, undo or redo changed; the screen redraws only that stone's tile.
        public static (int Y, int X) LastChanged { get; private set; } = (-1, -1);

        public static void Reset()
        {
            EditedPixels.Clear();
            _undoStack.Clear();
            _redoStack.Clear();
            Current = null;
            LastChanged = (-1, -1);
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
                LastChanged = (y, x);
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
                LastChanged = (y, x);
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
                LastChanged = (record.Y, record.X);
                _undoStack.Push(record);
                _redoStack.Clear();
                return $"edited pixel ({EditedPixels.Count} total)";
            }
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
            LastChanged = (record.Y, record.X);

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
            LastChanged = (record.Y, record.X);

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
