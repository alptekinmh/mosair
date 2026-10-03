using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using mosair.Models;
using SkiaSharp;

namespace mosair.Services
{
    public class ProjectData
    {
        public byte[]? DataM1Flat { get; set; }
        public int[] DataM1Dims { get; set; } = Array.Empty<int>();
        public byte[]? DataM3Flat { get; set; }
        public int[] DataM3Dims { get; set; } = Array.Empty<int>();
        public byte[]? DataM3FFLat { get; set; }
        public int[] DataM3FDims { get; set; } = Array.Empty<int>();
        public byte[]? DataM3BackupFlat { get; set; }
        public int[] DataM3BackupDims { get; set; } = Array.Empty<int>();
        public int[]? DrlDatFlat { get; set; }
        public int[] DrlDatDims { get; set; } = Array.Empty<int>();
        public List<RgbData> ArRGBAll { get; set; } = new();
        public List<RgbData> ArRGB { get; set; } = new();
        public List<List<RgbData>> ArMA { get; set; } = new();
        public List<List<RgbData>> ArMB { get; set; } = new();
        public List<List<RgbData>> ArMBR { get; set; } = new();
        public List<EditedPixelData> EditedPixels { get; set; } = new();
        public List<RegionData> Regions { get; set; } = new();
        public double Width { get; set; }
        public double Height { get; set; }
        public int RgbM { get; set; }
        public int N { get; set; } = 20;
        public bool ShowGrid { get; set; }
        public bool ShowMouldLines { get; set; }
        public byte GridColorR { get; set; }
        public byte GridColorG { get; set; }
        public byte GridColorB { get; set; }
        public int InterpolationMethod { get; set; }
        public double ZoomLevel { get; set; } = 1;
        public double WidthCm { get; set; }
        public string? PictureFileName { get; set; }
        public int[]? Arn { get; set; }
        public string? Source { get; set; }
    }

    public class RgbData
    {
        public double R { get; set; }
        public double G { get; set; }
        public double B { get; set; }
        public int ID { get; set; }
        public string CodeName { get; set; } = "";
        public string Name { get; set; } = "";
        public bool BoolLeaveOut { get; set; }
        public int NumOfPixel { get; set; }
        public double Dis { get; set; }
        public int Uc { get; set; } = 1;
        // Same extra fields as WPF's JsonRgbData: WPF's middle palette column shows U on a Ri/Gi/Bi background.
        public int U { get; set; }
        public int Reg { get; set; }
        public double Ri { get; set; }
        public double Gi { get; set; }
        public double Bi { get; set; }

        public static RgbData FromRgb(rgb c) => new()
        {
            R = c.r, G = c.g, B = c.b, ID = c.ID,
            CodeName = c.codeName, Name = c.name,
            BoolLeaveOut = c.boolLeaveOut, NumOfPixel = c.numOfPixel,
            Dis = c.dis, Uc = c.uc,
            U = c.u, Reg = c.reg, Ri = c.ri, Gi = c.gi, Bi = c.bi
        };

        public rgb ToRgb() => new()
        {
            r = R, g = G, b = B, ID = ID,
            codeName = CodeName, name = Name,
            boolLeaveOut = BoolLeaveOut, numOfPixel = NumOfPixel,
            dis = Dis, uc = Uc,
            u = U, reg = Reg, ri = Ri, gi = Gi, bi = Bi
        };
    }

    public class EditedPixelData
    {
        public int Y { get; set; }
        public int X { get; set; }
        public RgbData Source { get; set; } = new();
        public RgbData Target { get; set; } = new();
        public RgbData Original { get; set; } = new();
    }

    public class RegionData
    {
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }
        public int RgbM { get; set; }
    }

    public static class ProjectService
    {
        public static string CurrentFileName { get; set; } = "";
        public static string CurrentPictureFileName { get; set; } = "";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static void Save(string filePath, double widthCm, double zoomLevel,
            bool showGrid, bool showMouldLines, byte gcR, byte gcG, byte gcB,
            int interpMethod)
        {
            var data = new ProjectData
            {
                Width = MosaicEngine.width,
                Height = MosaicEngine.height,
                RgbM = MosaicEngine.rgbM,
                N = MosaicData.N,
                ShowGrid = showGrid,
                ShowMouldLines = showMouldLines,
                GridColorR = gcR,
                GridColorG = gcG,
                GridColorB = gcB,
                InterpolationMethod = interpMethod,
                ZoomLevel = zoomLevel,
                WidthCm = widthCm,
            };

            data.DataM1Flat = Flatten3D(MosaicData.dataM1, out var d1);
            data.DataM1Dims = d1;
            data.DataM3Flat = Flatten3D(MosaicData.dataM3, out var d3);
            data.DataM3Dims = d3;
            data.DataM3FFLat = Flatten3D(MosaicData.dataM3F, out var d3f);
            data.DataM3FDims = d3f;
            data.DataM3BackupFlat = Flatten3D(MosaicData.dataM3Backup, out var d3b);
            data.DataM3BackupDims = d3b;
            data.DrlDatFlat = FlattenInt3D(drl.dat, out var dd);
            data.DrlDatDims = dd;

            foreach (var c in MosaicData.arRGBAll) data.ArRGBAll.Add(RgbData.FromRgb(c));
            foreach (var c in MosaicData.arRGB) data.ArRGB.Add(RgbData.FromRgb(c));
            foreach (var list in MosaicData.arMA)
            {
                var l = new List<RgbData>();
                foreach (var c in list) l.Add(RgbData.FromRgb(c));
                data.ArMA.Add(l);
            }
            foreach (var list in MosaicData.arMB)
            {
                var l = new List<RgbData>();
                foreach (var c in list) l.Add(RgbData.FromRgb(c));
                data.ArMB.Add(l);
            }
            foreach (var list in MosaicData.arMBR)
            {
                var l = new List<RgbData>();
                foreach (var c in list) l.Add(RgbData.FromRgb(c));
                data.ArMBR.Add(l);
            }

            foreach (var ep in PixelEditService.EditedPixels)
            {
                data.EditedPixels.Add(new EditedPixelData
                {
                    Y = ep.Y, X = ep.X,
                    Source = RgbData.FromRgb(ep.Source),
                    Target = RgbData.FromRgb(ep.Target),
                    Original = RgbData.FromRgb(ep.Original)
                });
            }

            if (data.EditedPixels.Count > 0 && data.ArMA.Count > 0)
            {
                var armaIds = new HashSet<int>();
                foreach (var c in data.ArMA[0]) armaIds.Add(c.ID);

                foreach (var ep in data.EditedPixels)
                {
                    if (ep.Source.ID > 0 && !armaIds.Contains(ep.Source.ID))
                    {
                        foreach (var c in MosaicData.arRGBAll)
                        {
                            if (c.ID == ep.Source.ID)
                            {
                                var entry = RgbData.FromRgb(c);
                                entry.BoolLeaveOut = false;
                                if (entry.NumOfPixel < 1) entry.NumOfPixel = 1;
                                MarkAsPaletteEntry(entry, data.ArMA[0].Count + 1);
                                data.ArMA[0].Add(entry);
                                if (data.ArMB.Count > 0)
                                {
                                    var mbEntry = RgbData.FromRgb(c);
                                    mbEntry.BoolLeaveOut = false;
                                    if (mbEntry.NumOfPixel < 1) mbEntry.NumOfPixel = 1;
                                    MarkAsPaletteEntry(mbEntry, data.ArMB[0].Count + 1);
                                    data.ArMB[0].Add(mbEntry);
                                }
                                armaIds.Add(ep.Source.ID);
                                break;
                            }
                        }
                    }
                }
            }

            foreach (var reg in drl.arar)
            {
                data.Regions.Add(new RegionData
                {
                    X1 = reg.x1, Y1 = reg.y1, X2 = reg.x2, Y2 = reg.y2, RgbM = reg.rgbM
                });
            }

            data.Arn = MosaicData.arn;
            data.Source = "mosair";

            string dir = Path.GetDirectoryName(filePath)!;
            Directory.CreateDirectory(dir);

            if (MosaicData.inputBitmap != null && !string.IsNullOrEmpty(CurrentPictureFileName))
            {
                string picDest = Path.Combine(dir, Path.GetFileName(CurrentPictureFileName));
                if (!File.Exists(picDest))
                {
                    string? srcPath = FindOriginalImagePath();
                    if (srcPath != null && File.Exists(srcPath))
                        File.Copy(srcPath, picDest, true);
                }
                data.PictureFileName = Path.GetFileName(picDest);
            }

            byte[] json = JsonSerializer.SerializeToUtf8Bytes(data, JsonOpts);
            File.WriteAllBytes(filePath, json);

            CurrentFileName = filePath;
        }

        public static ProjectData? Open(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            byte[] json = File.ReadAllBytes(filePath);
            var data = JsonSerializer.Deserialize<ProjectData>(json, JsonOpts);
            if (data == null) return null;

            MosaicData.dataM1 = Unflatten3D(data.DataM1Flat, data.DataM1Dims);
            MosaicData.dataM3 = Unflatten3D(data.DataM3Flat, data.DataM3Dims);
            MosaicData.dataM3F = Unflatten3D(data.DataM3FFLat, data.DataM3FDims);
            MosaicData.dataM3Backup = Unflatten3D(data.DataM3BackupFlat, data.DataM3BackupDims);
            drl.dat = UnflattenInt3D(data.DrlDatFlat, data.DrlDatDims);

            MosaicData.arRGBAll.Clear();
            foreach (var c in data.ArRGBAll) MosaicData.arRGBAll.Add(c.ToRgb());
            MosaicData.arRGB.Clear();
            foreach (var c in data.ArRGB) MosaicData.arRGB.Add(c.ToRgb());

            MosaicData.arMA.Clear();
            foreach (var list in data.ArMA)
            {
                var l = new List<rgb>();
                foreach (var c in list) l.Add(c.ToRgb());
                MosaicData.arMA.Add(l);
            }
            MosaicData.arMB.Clear();
            foreach (var list in data.ArMB)
            {
                var l = new List<rgb>();
                foreach (var c in list) l.Add(c.ToRgb());
                MosaicData.arMB.Add(l);
            }
            MosaicData.arMBR.Clear();
            foreach (var list in data.ArMBR)
            {
                var l = new List<rgb>();
                foreach (var c in list) l.Add(c.ToRgb());
                MosaicData.arMBR.Add(l);
            }

            MosaicData.arcs.Clear();
            foreach (var c in MosaicData.arRGBAll)
                MosaicData.arcs.Add(c.boolLeaveOut);

            PixelEditService.EditedPixels.Clear();
            foreach (var ep in data.EditedPixels)
            {
                PixelEditService.EditedPixels.Add(new PixelEditRecord
                {
                    Y = ep.Y, X = ep.X,
                    Source = ep.Source.ToRgb(),
                    Target = ep.Target.ToRgb(),
                    Original = ep.Original.ToRgb()
                });
            }

            drl.arar.Clear();
            foreach (var reg in data.Regions)
            {
                drl.arar.Add(new drl(reg.X1, reg.Y1, reg.X2, reg.Y2) { rgbM = reg.RgbM });
            }

            MosaicEngine.width = data.Width;
            MosaicEngine.height = data.Height;
            MosaicEngine.rgbM = data.RgbM;
            MosaicData.N = data.N;

            if (data.Arn != null)
                MosaicData.arn = data.Arn;

            if (data.Source != "mosair")
            {
                FlipHorizontalInPlace(MosaicData.dataM1);
                FlipHorizontalInPlace(MosaicData.dataM3);
                FlipHorizontalInPlace(MosaicData.dataM3F);
                FlipHorizontalInPlace(MosaicData.dataM3Backup);
                FlipHorizontalIntInPlace(drl.dat);
            }

            CurrentFileName = filePath;

            if (data.PictureFileName != null)
            {
                string dir = Path.GetDirectoryName(filePath)!;
                CurrentPictureFileName = Path.Combine(dir, data.PictureFileName);
            }
            else
            {
                // Don't carry over the previous image's name: the stock sheet column is keyed by it.
                CurrentPictureFileName = "";
            }

            return data;
        }

        // A catalog stone added to the palette on save has no palette number or pre-catalog color of its own.
        private static void MarkAsPaletteEntry(RgbData entry, int u)
        {
            entry.U = u;
            entry.Reg = 1;
            entry.Ri = entry.R; entry.Gi = entry.G; entry.Bi = entry.B;
        }

        private static string? FindOriginalImagePath()
        {
            if (!string.IsNullOrEmpty(CurrentPictureFileName) && File.Exists(CurrentPictureFileName))
                return CurrentPictureFileName;
            return null;
        }

        private static byte[] Flatten3D(byte[,,] arr, out int[] dims)
        {
            int d0 = arr.GetLength(0), d1 = arr.GetLength(1), d2 = arr.GetLength(2);
            dims = new[] { d0, d1, d2 };
            var flat = new byte[d0 * d1 * d2];
            Buffer.BlockCopy(arr, 0, flat, 0, flat.Length);
            return flat;
        }

        private static byte[,,] Unflatten3D(byte[]? flat, int[] dims)
        {
            if (flat == null || dims.Length < 3)
                return new byte[3, 3, 3];
            var arr = new byte[dims[0], dims[1], dims[2]];
            Buffer.BlockCopy(flat, 0, arr, 0, Math.Min(flat.Length, dims[0] * dims[1] * dims[2]));
            return arr;
        }

        private static int[] FlattenInt3D(int[,,] arr, out int[] dims)
        {
            int d0 = arr.GetLength(0), d1 = arr.GetLength(1), d2 = arr.GetLength(2);
            dims = new[] { d0, d1, d2 };
            var flat = new int[d0 * d1 * d2];
            Buffer.BlockCopy(arr, 0, flat, 0, flat.Length * sizeof(int));
            return flat;
        }

        private static int[,,] UnflattenInt3D(int[]? flat, int[] dims)
        {
            if (flat == null || dims.Length < 3)
                return new int[1, 1, 4];
            var arr = new int[dims[0], dims[1], dims[2]];
            Buffer.BlockCopy(flat, 0, arr, 0, Math.Min(flat.Length * sizeof(int), dims[0] * dims[1] * dims[2] * sizeof(int)));
            return arr;
        }

        private static void FlipHorizontalInPlace(byte[,,] arr)
        {
            int rows = arr.GetLength(0), cols = arr.GetLength(1), ch = arr.GetLength(2);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols / 2; c++)
                {
                    int mc = cols - 1 - c;
                    for (int k = 0; k < ch; k++)
                        (arr[r, c, k], arr[r, mc, k]) = (arr[r, mc, k], arr[r, c, k]);
                }
        }

        private static void FlipHorizontalIntInPlace(int[,,] arr)
        {
            int rows = arr.GetLength(0), cols = arr.GetLength(1), ch = arr.GetLength(2);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols / 2; c++)
                {
                    int mc = cols - 1 - c;
                    for (int k = 0; k < ch; k++)
                        (arr[r, c, k], arr[r, mc, k]) = (arr[r, mc, k], arr[r, c, k]);
                }
        }
    }
}
