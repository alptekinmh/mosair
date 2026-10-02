using System;
using System.Collections.Generic;

using SkiaSharp;
using mosair.Models;

namespace mosair.Services
{
    public class DimensionResult
    {
        public double WidthCm { get; set; }
        public double HeightCm { get; set; }
        public double AreaM2 { get; set; }
        public int StoneColumns { get; set; }
        public int StoneRows { get; set; }
        public int Stones { get; set; }
        public int MouldColumns { get; set; }
        public int MouldRows { get; set; }
        public int Moulds { get; set; }
        public int OriginalWidth { get; set; }
        public int OriginalHeight { get; set; }
    }

    public static class MosaicEngine
    {
        // im.cs static fields
        public static double width, height;
        public static int wi, he;
        public static double actualWidth, actualHeight;
        public static double rate;
        public static double excessiveW, excessiveH;
        public static int rgbM = 3;
        public static int RGBInc = 19;
        public static int minRGBInc = 2;
        public static int numOfMinRGB;
        public static bool boolLab;
        public static bool boolAv;
        public static InterpolationMethod interpolationMethod = InterpolationMethod.Area;
        public static int penW = 1;

        // --- Public API ---

        public static SKBitmap? LoadImage(string path)
        {
            var bmp = ImageService.LoadImage(path);
            if (bmp == null) return null;
            MosaicData.inputBitmap = bmp;
            return MosaicData.inputBitmap;
        }

        public static DimensionResult? CalculateDimensions(double widthCm)
        {
            if (MosaicData.inputBitmap == null) return null;

            int numOfStonesInRow = Convert.ToInt32((widthCm * 10.0) / 12.0);
            actualWidth = numOfStonesInRow * 12;
            width = numOfStonesInRow;

            double mouldNW = (double)numOfStonesInRow / 26.0;
            int w1 = Convert.ToInt32(Math.Floor(mouldNW));
            excessiveW = mouldNW - w1;
            wi = Convert.ToInt32(Math.Ceiling(mouldNW));

            rate = (double)MosaicData.inputBitmap.Height / (double)MosaicData.inputBitmap.Width;

            double mouldNH = (actualWidth * rate) / (12.0 * 26.0);
            int h1 = Convert.ToInt32(Math.Floor(mouldNH));
            excessiveH = mouldNH - h1;
            he = Convert.ToInt32(Math.Ceiling(mouldNH));

            double tem = width * rate;
            height = Convert.ToInt32(tem);
            actualHeight = height * 12;

            return new DimensionResult
            {
                WidthCm = actualWidth / 10.0,
                HeightCm = actualHeight / 10.0,
                AreaM2 = actualWidth * actualHeight / 1_000_000.0,
                StoneColumns = (int)width,
                StoneRows = (int)height,
                Stones = (int)width * (int)height,
                MouldColumns = wi,
                MouldRows = he,
                Moulds = he * wi,
                OriginalWidth = MosaicData.inputBitmap.Width,
                OriginalHeight = MosaicData.inputBitmap.Height
            };
        }

        public static OptimalPaletteResult? LastOptimalResult { get; private set; }

        // Comparison only: reproduce WPF's color removal inside RunM3.
        public static bool WpfStyleRemoval;

        public static byte[,,] GetSourceStoneData()
        {
            using var bmp = ImageService.Resize(MosaicData.inputBitmap!, (int)width, (int)height, interpolationMethod);
            return ImageService.ToByteArray(bmp);
        }

        public static GamutMapper? LastGamut { get; private set; }

        public static SKBitmap RunOptimal(InterpolationMethod interpMethod = InterpolationMethod.Area,
            Action<int>? onProgress = null, bool prepareTextures = true, bool useGamut = false)
        {
            interpolationMethod = interpMethod;
            MosaicData.reducedBitmap = ImageService.Resize(MosaicData.inputBitmap!, (int)width, (int)height, interpolationMethod);
            int R = MosaicData.reducedBitmap.Height;
            int C = MosaicData.reducedBitmap.Width;
            byte[,,] src = ImageService.ToByteArray(MosaicData.reducedBitmap);

            var candidates = new List<rgb>(MosaicData.arRGB);
            var gamut = useGamut ? GamutMapper.Build(src, R, C, candidates) : null;
            LastGamut = gamut;
            var result = OptimalPaletteService.Analyze(src, R, C, candidates, p => onProgress?.Invoke(p * 9 / 10), gamut);
            LastOptimalResult = result;
            _optSrc = src;
            _optCandidates = candidates;
            _optGamut = gamut;

            var bmp = ApplyOptimalK(result.KOptimal, prepareTextures);
            onProgress?.Invoke(100);
            return bmp;
        }

        private static byte[,,]? _optSrc;
        private static List<rgb>? _optCandidates;
        private static GamutMapper? _optGamut;

        // Rebuilds the mosaic from the last RunOptimal analysis using the k best stones (nested subsets).
        public static SKBitmap ApplyOptimalK(int k, bool prepareTextures = true)
        {
            var result = LastOptimalResult!;
            var src = _optSrc!;
            var candidates = _optCandidates!;
            var gamut = _optGamut;
            int R = src.GetLength(0), C = src.GetLength(1);
            int M = result.CandidateCount;
            k = Math.Clamp(k, 1, M);

            PixelEditService.Reset();
            rgbM = k;
            CreateSingleRegion(k, R, C);

            var selected = new List<rgb>();
            for (int i = M - k; i < M; i++) selected.Add(candidates[result.RemovalOrder[i]]);
            int K = selected.Count;
            double[] sL = new double[K], sA = new double[K], sB = new double[K];
            for (int s = 0; s < K; s++)
            {
                var lab = ColorMatcher.RgbToLab(selected[s].r, selected[s].g, selected[s].b);
                sL[s] = lab.L; sA[s] = lab.A; sB[s] = lab.B;
            }

            var assignCache = new Dictionary<int, int>();
            int[] counts = new int[K];
            MosaicData.dataM3 = new byte[R, C, 3];
            for (int i = 0; i < R; i++)
                for (int j = 0; j < C; j++)
                {
                    byte b = src[i, j, 0], g = src[i, j, 1], r = src[i, j, 2];
                    int key = (r << 16) | (g << 8) | b;
                    if (!assignCache.TryGetValue(key, out int s))
                    {
                        var lab = ColorMatcher.RgbToLab(r, g, b);
                        if (gamut != null) lab = gamut.Map(lab);
                        double bestD = double.MaxValue;
                        s = 0;
                        for (int t = 0; t < K; t++)
                        {
                            double dl = (lab.L - sL[t]) * OptimalPaletteService.LightnessWeight;
                            double da = lab.A - sA[t], db = lab.B - sB[t];
                            double d = dl * dl + da * da + db * db;
                            if (d < bestD) { bestD = d; s = t; }
                        }
                        assignCache[key] = s;
                    }
                    counts[s]++;
                    MosaicData.dataM3[i, j, 0] = (byte)selected[s].b;
                    MosaicData.dataM3[i, j, 1] = (byte)selected[s].g;
                    MosaicData.dataM3[i, j, 2] = (byte)selected[s].r;
                    drl.dat[i, j, 3] = selected[s].ID;
                }

            var palette = new List<rgb>();
            for (int s = 0; s < K; s++)
            {
                if (counts[s] == 0) continue;
                var c = CloneRgb(selected[s]);
                c.numOfPixel = counts[s];
                c.reg = 1;
                c.boolLeaveOut = false;
                palette.Add(c);
            }
            palette.Sort((a, b) => a.ID.CompareTo(b.ID));
            for (int z = 0; z < palette.Count; z++)
            {
                palette[z].u = z + 1;
                palette[z].uc = z + 1;
                palette[z].ri = palette[z].r; palette[z].gi = palette[z].g; palette[z].bi = palette[z].b;
                palette[z].dis = (palette[z].r + palette[z].g + palette[z].b) / 3.0;
            }

            MosaicData.dataM1 = (byte[,,])MosaicData.dataM3.Clone();
            MosaicData.arMB = new List<List<rgb>> { palette };
            MosaicData.arMA = CloneNestedList(MosaicData.arMB);
            BackupM3(R, C);

            MosaicData.reducedBitmap = ImageService.FromByteArray(MosaicData.dataM3, R, C);
            MosaicData.exportBitmap = MosaicData.reducedBitmap.Copy();

            if (prepareTextures)
            {
                StoneTextureService.PopulateRandomIndices(R, C);
                StoneTextureService.LoadTextures();
                StoneTextureService.ResizeTextures(MosaicData.N);
            }

            return MosaicData.reducedBitmap;
        }

        public static SKBitmap RunM3(int targetColors, int rgbIncrement, bool useLab, bool useAverage,
            InterpolationMethod interpMethod = InterpolationMethod.Area, Action<int>? onProgress = null,
            bool prepareTextures = true)
        {
            rgbM = targetColors;
            RGBInc = rgbIncrement;
            boolLab = useLab;
            boolAv = useAverage;
            interpolationMethod = interpMethod;


            // Resize input to stone dimensions
            MosaicData.reducedBitmap = ImageService.Resize(MosaicData.inputBitmap!, (int)width, (int)height, interpolationMethod);
            int R = MosaicData.reducedBitmap.Height;
            int C = MosaicData.reducedBitmap.Width;

            // Create single region covering entire image
            CreateSingleRegion(targetColors, R, C);

            // Init M3
            InitM3();

            // Generate initial palette (all RGB combos with step)
            GenerateInitialPalette();

            // Run M1 first pass
            RunM1(R, C);

            // Copy M1 results to M3
            CopyM1ToM3(R, C);

            // Iterative reduction per region
            for (int reg = 0; reg < drl.arar.Count; reg++)
            {
                drl dr = drl.arar[reg];
                numOfMinRGB = 0;
                List<rgb> ar3 = MosaicData.arMA[reg];


                var reducedForLoop = ImageService.Resize(MosaicData.inputBitmap!, (int)width, (int)height, interpolationMethod);
                MosaicData.reducedBitmap = reducedForLoop;

                while (ar3.Count > dr.rgbM)
                {
                    if (ar3.Count >= 2000) minRGBInc = 5;
                    else if (ar3.Count > 1000) minRGBInc = 2;
                    else minRGBInc = 1;

                    RemoveMinimalColors(reg);

                    var reducedForIter = ImageService.Resize(MosaicData.inputBitmap!, (int)width, (int)height, interpolationMethod);
                    MosaicData.reducedBitmap = reducedForIter;
                    ProcessM3(reg, R, C);

                    numOfMinRGB += minRGBInc;

                    onProgress?.Invoke(ar3.Count > 0 ? (int)((double)dr.rgbM / ar3.Count * 100.0) : 100);
                }

                // Assign color numbers for this region
                AssignColorNumbers(R, C, reg);
            }

            // Deep copy arMA → arMB
            MosaicData.arMB = CloneNestedList(MosaicData.arMA);

            // --- Section 2: Automatic catalog color assignment ---
            for (int i = 0; i < MosaicData.arMB.Count; i++)
            {
                List<rgb> ar = MosaicData.arMB[i];
                ColorMatcher.ResetUseOnce();
                int regId = i + 1;

                var colorUpdates = new Dictionary<(byte b, byte g, byte r, int reg, int u), (byte nb, byte ng, byte nr)>();
                for (int j = 0; j < ar.Count; j++)
                {
                    rgb ra = ar[j];
                    int n = ColorMatcher.SelectNearest(ColorMatcher.FindCatalogDistances(ra));
                    rgb r_ = MosaicData.arRGB[n];

                    if (regId == ra.reg)
                    {
                        colorUpdates[((byte)ra.b, (byte)ra.g, (byte)ra.r, ra.reg, ra.u)] = ((byte)r_.b, (byte)r_.g, (byte)r_.r);
                        ra.r = r_.r; ra.g = r_.g; ra.b = r_.b;
                        ra.ID = r_.ID; ra.codeName = r_.codeName; ra.name = r_.name;
                    }
                }

                for (int i_ = 0; i_ < R; i_++)
                    for (int j_ = 0; j_ < C; j_++)
                    {
                        var key = (MosaicData.dataM3[i_, j_, 0], MosaicData.dataM3[i_, j_, 1], MosaicData.dataM3[i_, j_, 2],
                                   drl.dat[i_, j_, 2], drl.dat[i_, j_, 3]);
                        if (colorUpdates.TryGetValue(key, out var upd))
                        {
                            MosaicData.dataM3[i_, j_, 0] = upd.nb;
                            MosaicData.dataM3[i_, j_, 1] = upd.ng;
                            MosaicData.dataM3[i_, j_, 2] = upd.nr;
                        }
                    }
            }


            // --- Section 3: Assign uc (combine identical colors) ---
            for (int i = 0; i < MosaicData.arMB.Count; i++)
            {
                List<rgb> ar = MosaicData.arMB[i];
                for (int j = 0; j < ar.Count; j++)
                {
                    rgb r = ar[j];
                    for (int k = r.u - 1; k < ar.Count; k++)
                    {
                        rgb r1 = ar[k];
                        if (!r1.boolUCDone && r.r == r1.r && r.g == r1.g && r.b == r1.b)
                        {
                            r1.uc = r.u;
                            r1.boolUCDone = true;
                        }
                    }
                }
            }

            // Update dat[,,3] with uc values — single pass
            {
                var ucMap = new Dictionary<(byte, byte, byte, int, int), int>();
                for (int i = 0; i < MosaicData.arMB.Count; i++)
                {
                    List<rgb> ar = MosaicData.arMB[i];
                    for (int j = 0; j < ar.Count; j++)
                    {
                        rgb r = ar[j];
                        ucMap[((byte)r.b, (byte)r.g, (byte)r.r, r.reg, r.u)] = r.uc;
                    }
                }

                for (int i_ = 0; i_ < R; i_++)
                    for (int j_ = 0; j_ < C; j_++)
                    {
                        var key = (MosaicData.dataM3[i_, j_, 0], MosaicData.dataM3[i_, j_, 1], MosaicData.dataM3[i_, j_, 2],
                                   drl.dat[i_, j_, 2], drl.dat[i_, j_, 3]);
                        if (ucMap.TryGetValue(key, out int uc))
                            drl.dat[i_, j_, 3] = uc;
                    }
            }

            // u = uc
            for (int i = 0; i < MosaicData.arMB.Count; i++)
            {
                List<rgb> ar = MosaicData.arMB[i];
                for (int j = 0; j < ar.Count; j++)
                    ar[j].u = ar[j].uc;
            }


            // --- Section 4: Combine identical catalog colors ---
            var arT = new List<rgb>();
            for (int j = 0; j < MosaicData.arRGB.Count; j++)
            {
                rgb r = MosaicData.arRGB[j];
                int total_ = 0, h_ = 0;
                bool boolSave = true;
                rgb? re = null;

                for (int k = 0; k < MosaicData.arMB[0].Count; k++)
                {
                    rgb r_ = MosaicData.arMB[0][k];
                    if (r.codeName == r_.codeName)
                    {
                        h_++;
                        total_ += r_.numOfPixel;
                        if (boolSave)
                        {
                            boolSave = false;
                            re = CloneRgb(r_);
                        }
                    }
                }

                if (h_ != 0 && re != null)
                {
                    re.numOfPixel = total_;
                    arT.Add(re);
                }
            }
            MosaicData.arMB[0] = CloneList(arT);
            MosaicData.arMB[0].RemoveAll(c => c.numOfPixel == 0);


            // --- Section 5: Update dat[,,3] with ID ---
            for (int i = 0; i < MosaicData.arMB.Count; i++)
            {
                List<rgb> ar = MosaicData.arMB[i];

                var idMap = new Dictionary<(byte, byte, byte, int, int), int>();
                for (int j = 0; j < ar.Count; j++)
                {
                    rgb r = ar[j];
                    idMap[((byte)r.b, (byte)r.g, (byte)r.r, r.reg, r.u)] = r.ID;
                }

                for (int i_ = 0; i_ < R; i_++)
                    for (int j_ = 0; j_ < C; j_++)
                    {
                        var key = (MosaicData.dataM3[i_, j_, 0], MosaicData.dataM3[i_, j_, 1], MosaicData.dataM3[i_, j_, 2],
                                   drl.dat[i_, j_, 2], drl.dat[i_, j_, 3]);
                        if (idMap.TryGetValue(key, out int id))
                            drl.dat[i_, j_, 3] = id;
                    }

                byte e = 0;
                for (int j = 0; j < ar.Count; j++)
                    ar[j].u = ++e;
            }


            // Backup
            MosaicData.arMA = CloneNestedList(MosaicData.arMB);
            BackupM3(R, C);

            // Final bitmap from dataM3
            MosaicData.reducedBitmap = ImageService.FromByteArray(MosaicData.dataM3, R, C);
            MosaicData.exportBitmap = MosaicData.reducedBitmap.Copy();

            // Prepare stone textures (RS bitmap created by ViewModel with grid params)
            if (prepareTextures)
            {
                StoneTextureService.PopulateRandomIndices(R, C);
                StoneTextureService.LoadTextures();
                StoneTextureService.ResizeTextures(MosaicData.N);
            }

            onProgress?.Invoke(100);
            return MosaicData.reducedBitmap;
        }

        public static void Reset()
        {
            MosaicData.arMA.Clear();
            MosaicData.arMB.Clear();
            MosaicData.arMBR.Clear();
            drl.arar.Clear();
            MosaicData.dataM1 = new byte[3, 3, 3];
            MosaicData.dataM3 = new byte[3, 3, 3];
            MosaicData.dataM3F = new byte[3, 3, 3];
            MosaicData.dataM3Backup = new byte[3, 3, 3];
            drl.dat = new int[1, 1, 4];
            MosaicData.reducedBitmap?.Dispose();
            MosaicData.reducedBitmap = null;
            MosaicData.exportBitmap?.Dispose();
            MosaicData.exportBitmap = null;
            MosaicData.rsBitmap?.Dispose();
            MosaicData.rsBitmap = null;
            StoneTextureService.Reset();
            PixelEditService.Reset();
            LastOptimalResult = null;
            _optSrc = null;
            _optCandidates = null;
            _optGamut = null;
        }

        // --- Private pipeline methods ---

        private static void CreateSingleRegion(int targetColors, int R, int C)
        {
            drl.arar.Clear();
            drl.arar.Add(new drl());
            drl.arar[0].x1 = 0;
            drl.arar[0].y1 = 0;
            drl.arar[0].x2 = C;
            drl.arar[0].y2 = R;
            drl.arar[0].rgbM = targetColors;

            drl dr = drl.arar[0];
            for (int i = 0; i < R; i++)
            {
                for (int j = 0; j < C; j++)
                {
                    var d = new dr { x = j, y = i };
                    dr.ar.Add(d);
                }
            }

            drl.dat = new int[R, C, 4];
            for (int j = 0; j < R; j++)
            {
                for (int k = 0; k < C; k++)
                {
                    drl.dat[j, k, 0] = 1;
                    drl.dat[j, k, 1] = 1;
                    drl.dat[j, k, 2] = 1;
                }
            }
        }

        private static void InitM3()
        {
            MosaicData.arMA.Clear();
            MosaicData.arMB.Clear();
        }

        private static void GenerateInitialPalette()
        {
            for (int z = 0; z < drl.arar.Count; z++)
            {
                var ar = new List<rgb>();
                MosaicData.arMA.Add(ar);
                for (int i = 0; i < 255; i += RGBInc)
                {
                    for (int j = 0; j < 255; j += RGBInc)
                    {
                        for (int k = 0; k < 255; k += RGBInc)
                            ar.Add(new rgb(i, j, k));
                    }
                }
            }
        }

        private static void RunM1(int R, int C)
        {
            MosaicData.reducedBitmap = ImageService.Resize(MosaicData.inputBitmap!, (int)width, (int)height, interpolationMethod);
            byte[,,] data = ImageService.ToByteArray(MosaicData.reducedBitmap);

            int arCount = MosaicData.arRGB.Count;
            for (int i = 0; i < arCount; i++)
                MosaicData.arRGB[i].numOfPixel = 0;

            double[] catL = new double[arCount];
            double[] catA = new double[arCount];
            double[] catBl = new double[arCount];
            double[] catR = new double[arCount];
            double[] catG = new double[arCount];
            double[] catBv = new double[arCount];

            for (int i = 0; i < arCount; i++)
            {
                rgb r = MosaicData.arRGB[i];
                if (boolLab)
                {
                    var lab = ColorMatcher.RgbToLab(r.r, r.g, r.b);
                    r.L = lab.L; r.A = lab.A; r.B = lab.B;
                    catL[i] = lab.L; catA[i] = lab.A; catBl[i] = lab.B;
                }
                catR[i] = r.r; catG[i] = r.g; catBv[i] = r.b;
            }

            MosaicData.dataM1 = new byte[R, C, 3];
            int[][] rowBestIdx = new int[R][];

            System.Threading.Tasks.Parallel.For(0, R, i =>
            {
                rowBestIdx[i] = new int[C];
                for (int j = 0; j < C; j++)
                {
                    double min = double.MaxValue;
                    int n = 0;
                    double pb = data[i, j, 0], pg = data[i, j, 1], pr = data[i, j, 2];

                    double pixL = 0, pixA = 0, pixB = 0;
                    if (boolLab)
                    {
                        var lab = ColorMatcher.RgbToLab(pr, pg, pb);
                        pixL = lab.L; pixA = lab.A; pixB = lab.B;
                    }

                    for (int k = 0; k < arCount; k++)
                    {
                        double db, dg, dr, av;
                        if (boolLab)
                        {
                            db = pixL - catL[k]; dg = pixA - catA[k]; dr = pixB - catBl[k];
                        }
                        else
                        {
                            db = pb - catBv[k]; dg = pg - catG[k]; dr = pr - catR[k];
                        }

                        if (boolAv) av = (Math.Abs(db) + Math.Abs(dg) + Math.Abs(dr)) / 3.0;
                        else av = db * db + dg * dg + dr * dr;

                        if (av < min) { min = av; n = k; }
                    }

                    MosaicData.dataM1[i, j, 0] = (byte)MosaicData.arRGB[n].b;
                    MosaicData.dataM1[i, j, 1] = (byte)MosaicData.arRGB[n].g;
                    MosaicData.dataM1[i, j, 2] = (byte)MosaicData.arRGB[n].r;
                    rowBestIdx[i][j] = n;
                }
            });

            for (int i = 0; i < R; i++)
                for (int j = 0; j < C; j++)
                {
                    int n = rowBestIdx[i][j];
                    MosaicData.arRGB[n].numOfPixel++;
                    MosaicData.arRGB[n].n = n;
                }

            MosaicData.reducedBitmap = ImageService.FromByteArray(MosaicData.dataM1, R, C);
        }

        private static void ProcessM1(int R, int C)
        {
            // Same as RunM1 but without resize (already done)
            byte[,,] data = ImageService.ToByteArray(MosaicData.reducedBitmap!);

            for (int i = 0; i < MosaicData.arRGB.Count; i++)
                MosaicData.arRGB[i].numOfPixel = 0;

            if (boolLab)
            {
                for (int i = 0; i < MosaicData.arRGB.Count; i++)
                {
                    rgb r = MosaicData.arRGB[i];
                    var lab = ColorMatcher.RgbToLab(r.r, r.g, r.b);
                    r.L = lab.L; r.A = lab.A; r.B = lab.B;
                }
            }

            MosaicData.dataM1 = new byte[R, C, 3];

            for (int i = 0; i < R; i++)
            {
                for (int j = 0; j < C; j++)
                {
                    double min = 1000, db, dg, dr, av;
                    int n = 0;

                    for (int k = 0; k < MosaicData.arRGB.Count; k++)
                    {
                        if (boolLab)
                        {
                            var lab = ColorMatcher.RgbToLab(data[i, j, 2], data[i, j, 1], data[i, j, 0]);
                            db = Math.Abs(lab.L - MosaicData.arRGB[k].L);
                            dg = Math.Abs(lab.A - MosaicData.arRGB[k].A);
                            dr = Math.Abs(lab.B - MosaicData.arRGB[k].B);
                        }
                        else
                        {
                            db = Math.Abs(data[i, j, 0] - MosaicData.arRGB[k].b);
                            dg = Math.Abs(data[i, j, 1] - MosaicData.arRGB[k].g);
                            dr = Math.Abs(data[i, j, 2] - MosaicData.arRGB[k].r);
                        }

                        if (boolAv) av = (db + dg + dr) / 3.0;
                        else av = Math.Sqrt(db * db + dg * dg + dr * dr);

                        if (av < min) { min = av; n = k; }
                    }

                    MosaicData.dataM1[i, j, 0] = (byte)MosaicData.arRGB[n].b;
                    MosaicData.dataM1[i, j, 1] = (byte)MosaicData.arRGB[n].g;
                    MosaicData.dataM1[i, j, 2] = (byte)MosaicData.arRGB[n].r;
                    MosaicData.arRGB[n].numOfPixel++;
                    MosaicData.arRGB[n].n = n;
                }
            }

            MosaicData.reducedBitmap = ImageService.FromByteArray(MosaicData.dataM1, R, C);
        }

        private static void CopyM1ToM3(int R, int C)
        {
            MosaicData.dataM3 = new byte[R, C, 3];
            for (int j = 0; j < R; j++)
            {
                for (int i = 0; i < C; i++)
                {
                    MosaicData.dataM3[j, i, 0] = MosaicData.dataM1[j, i, 0];
                    MosaicData.dataM3[j, i, 1] = MosaicData.dataM1[j, i, 1];
                    MosaicData.dataM3[j, i, 2] = MosaicData.dataM1[j, i, 2];
                }
            }
        }

        private static void ProcessM3(int reg, int R, int C)
        {
            List<rgb> ar = MosaicData.arMA[reg];
            int arCount = ar.Count;

            // Pre-compute Lab values for all palette colors once
            double[] arL = new double[arCount];
            double[] arA = new double[arCount];
            double[] arB_ = new double[arCount];
            double[] arR = new double[arCount];
            double[] arG = new double[arCount];
            double[] arBv = new double[arCount];

            for (int i = 0; i < arCount; i++)
            {
                rgb r = ar[i];
                r.numOfPixel = 0;
                if (boolLab)
                {
                    var lab = ColorMatcher.RgbToLab(r.r, r.g, r.b);
                    r.L = lab.L; r.A = lab.A; r.B = lab.B;
                    arL[i] = lab.L; arA[i] = lab.A; arB_[i] = lab.B;
                }
                arR[i] = r.r; arG[i] = r.g; arBv[i] = r.b;
            }

            byte[,,] data = ImageService.ToByteArray(MosaicData.reducedBitmap!);
            int regId = reg + 1;

            // Per-row results for thread-safe numOfPixel accumulation
            int[][] rowBestIdx = new int[R][];

            System.Threading.Tasks.Parallel.For(0, R, i =>
            {
                rowBestIdx[i] = new int[C];
                for (int j = 0; j < C; j++)
                {
                    rowBestIdx[i][j] = -1;
                    if ((drl.dat[i, j, 0] == 1 || drl.dat[i, j, 1] == 1) && drl.dat[i, j, 2] == regId)
                    {
                        double min = double.MaxValue;
                        int n = 0;
                        double pb = data[i, j, 0], pg = data[i, j, 1], pr = data[i, j, 2];

                        double pixL = 0, pixA = 0, pixB = 0;
                        if (boolLab)
                        {
                            var lab = ColorMatcher.RgbToLab(pr, pg, pb);
                            pixL = lab.L; pixA = lab.A; pixB = lab.B;
                        }

                        for (int k = 0; k < arCount; k++)
                        {
                            double db, dg, dr, av;
                            if (boolLab)
                            {
                                db = pixL - arL[k]; dg = pixA - arA[k]; dr = pixB - arB_[k];
                            }
                            else
                            {
                                db = pb - arBv[k]; dg = pg - arG[k]; dr = pr - arR[k];
                            }

                            if (boolAv) av = (Math.Abs(db) + Math.Abs(dg) + Math.Abs(dr)) / 3.0;
                            else av = db * db + dg * dg + dr * dr;

                            if (av < min) { min = av; n = k; }
                        }

                        MosaicData.dataM3[i, j, 0] = (byte)ar[n].b;
                        MosaicData.dataM3[i, j, 1] = (byte)ar[n].g;
                        MosaicData.dataM3[i, j, 2] = (byte)ar[n].r;
                        rowBestIdx[i][j] = n;
                    }
                }
            });

            // Accumulate numOfPixel on main thread (thread-safe)
            for (int i = 0; i < R; i++)
            {
                if (rowBestIdx[i] == null) continue;
                for (int j = 0; j < C; j++)
                {
                    int n = rowBestIdx[i][j];
                    if (n >= 0)
                    {
                        ar[n].numOfPixel++;
                        ar[n].n = n;
                    }
                }
            }

            MosaicData.reducedBitmap = ImageService.FromByteArray(MosaicData.dataM3, R, C);
        }

        private static void RemoveMinimalColors(int reg)
        {
            var ar = MosaicData.arMA[reg];
            int target = drl.arar[reg].rgbM;
            int wouldRemain = 0;
            for (int i = 0; i < ar.Count; i++)
                if (ar[i].numOfPixel >= numOfMinRGB) wouldRemain++;
            // WPF's removeMinimalM3 drops every color under the threshold with no lower bound;
            // the fallback below is kept only to avoid an empty palette.
            if (WpfStyleRemoval ? wouldRemain > 0 : wouldRemain >= target)
                ar.RemoveAll(c => c.numOfPixel < numOfMinRGB);
            else
            {
                ar.Sort((a, b) => b.numOfPixel.CompareTo(a.numOfPixel));
                if (ar.Count > target)
                    ar.RemoveRange(target, ar.Count - target);
            }
        }

        private static void AssignColorNumbers(int R, int C, int reg)
        {
            List<rgb> ar = MosaicData.arMA[reg];
            int regId = reg + 1;

            var colorToU = new Dictionary<(byte, byte, byte), int>();
            int u = 1;
            for (int z = 0; z < ar.Count; z++)
            {
                rgb r = ar[z];
                r.ri = r.r; r.gi = r.g; r.bi = r.b;
                r.u = u++;
                r.reg = regId;
                r.dis = (r.r + r.g + r.b) / 3.0;
                var key = ((byte)r.b, (byte)r.g, (byte)r.r);
                colorToU.TryAdd(key, r.u);
            }

            for (int i = 0; i < R; i++)
                for (int j = 0; j < C; j++)
                {
                    if (drl.dat[i, j, 2] != regId) continue;
                    var key = (MosaicData.dataM3[i, j, 0], MosaicData.dataM3[i, j, 1], MosaicData.dataM3[i, j, 2]);
                    if (colorToU.TryGetValue(key, out int uVal))
                        drl.dat[i, j, 3] = uVal;
                }
        }

        private static void BackupM3(int R, int C)
        {
            MosaicData.arMBR = CloneNestedList(MosaicData.arMB);
            MosaicData.dataM3Backup = new byte[R, C, 3];
            for (int i = 0; i < R; i++)
            {
                for (int j = 0; j < C; j++)
                {
                    MosaicData.dataM3Backup[i, j, 0] = MosaicData.dataM3[i, j, 0];
                    MosaicData.dataM3Backup[i, j, 1] = MosaicData.dataM3[i, j, 1];
                    MosaicData.dataM3Backup[i, j, 2] = MosaicData.dataM3[i, j, 2];
                }
            }
        }

        // --- Clone helpers (replace gc<T>.DeepCopy) ---

        public static rgb CloneRgb(rgb src)
        {
            return new rgb
            {
                r = src.r, g = src.g, b = src.b,
                ri = src.ri, gi = src.gi, bi = src.bi,
                dis = src.dis, L = src.L, A = src.A, B = src.B,
                unitPrice = src.unitPrice, price = src.price, area = src.area,
                boolUseOnce = src.boolUseOnce, boolUCDone = src.boolUCDone,
                boolLeaveOut = src.boolLeaveOut, stokYetersiz = src.stokYetersiz,
                n = src.n, numOfPixel = src.numOfPixel, reg = src.reg,
                u = src.u, uc = src.uc, ID = src.ID,
                codeName = src.codeName, name = src.name
            };
        }

        public static List<rgb> CloneList(List<rgb> src)
        {
            var dst = new List<rgb>(src.Count);
            for (int i = 0; i < src.Count; i++)
                dst.Add(CloneRgb(src[i]));
            return dst;
        }

        public static List<List<rgb>> CloneNestedList(List<List<rgb>> src)
        {
            var dst = new List<List<rgb>>(src.Count);
            for (int i = 0; i < src.Count; i++)
                dst.Add(CloneList(src[i]));
            return dst;
        }
    }
}
