using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SkiaSharp;
using mosair.Models;
using mosair.Services;

namespace mosair.ViewModels
{
    public class ColorItem : INotifyPropertyChanged
    {
        private bool _isExcluded;
        private Bitmap? _tooltipBitmap;
        private bool _tooltipLoaded;
        private Bitmap? _thumbnailBitmap;
        private bool _thumbnailLoaded;
        public int Index { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public string CodeName { get; set; } = "";
        public string Name { get; set; } = "";
        public int ID { get; set; }

        public bool IsExcluded
        {
            get => _isExcluded;
            set { _isExcluded = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsSelected)); }
        }

        public bool IsSelected
        {
            get => !_isExcluded;
            set { IsExcluded = !value; }
        }


        public string TooltipHeader => $"{CodeName}  {Name}  {R} {G} {B}";
        public IBrush ColorBrush => new SolidColorBrush(Color.FromRgb(R, G, B));

        public Bitmap? ThumbnailBitmap
        {
            get
            {
                if (!_thumbnailLoaded)
                {
                    _thumbnailLoaded = true;
                    var skBmp = StoneTextureService.LoadSingleThumbnail(CodeName, 16, 14);
                    if (skBmp != null)
                    {
                        using var img = SKImage.FromBitmap(skBmp);
                        using var data = img.Encode(SKEncodedImageFormat.Png, 90);
                        using var stream = new System.IO.MemoryStream(data.ToArray());
                        _thumbnailBitmap = new Bitmap(stream);
                        skBmp.Dispose();
                    }
                }
                return _thumbnailBitmap;
            }
        }

        public Bitmap? TooltipBitmap
        {
            get
            {
                if (!_tooltipLoaded)
                {
                    _tooltipLoaded = true;
                    _tooltipBitmap = BuildTooltipBitmap();
                }
                return _tooltipBitmap;
            }
        }

        private Bitmap? BuildTooltipBitmap()
        {
            var images = StoneTextureService.LoadTooltipImages(CodeName, 60);
            if (images.Count == 0) return null;

            int cols = 4;
            int rows = (images.Count + cols - 1) / cols;
            int cellSize = 62;
            int w = cols * cellSize;
            int h = rows * cellSize;

            using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(w, h));
            var canvas = surface.Canvas;
            canvas.Clear(SkiaSharp.SKColors.Transparent);

            for (int i = 0; i < images.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                canvas.DrawBitmap(images[i], col * cellSize + 1, row * cellSize + 1);
                images[i].Dispose();
            }

            using var img = surface.Snapshot();
            using var data = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            using var stream = new System.IO.MemoryStream(data.ToArray());
            return new Bitmap(stream);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class PaletteItem
    {
        public int Index { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public IBrush ColorBrush => new SolidColorBrush(Color.FromRgb(R, G, B));
    }

    public class AssignedItem
    {
        public int Num { get; set; }
        public int ID { get; set; }
        public string CodeName { get; set; } = "";
        public int PixelCount { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public IBrush RowBrush => new SolidColorBrush(Color.FromRgb(R, G, B));
        public IBrush TextBrush
        {
            get
            {
                double lum = R * 0.299 + G * 0.587 + B * 0.114;
                return lum > 140 ? Brushes.Black : Brushes.White;
            }
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        public Func<string, string, Task>? ShowAlert { get; set; }

        private void Alert(string title, string message)
        {
            if (ShowAlert != null)
                Avalonia.Threading.Dispatcher.UIThread.Post(async () => await ShowAlert(title, message));
        }

        private Bitmap? _displayBitmap;
        private double _widthCm = 93.6;
        private double _heightCm;
        private double _initialZoomLevel = 2;
        private double _minZoomLevel = 1.0;
        private double _lastViewportWidth;
        private double _lastViewportHeight;
        public double MinZoomLevel => _minZoomLevel;
        private int _targetColors = 15;
        private int _rgbIncrement = 10;

        private bool _useLab;
        private bool _useAverage;
        private InterpolationMethod _interpolationMethod = InterpolationMethod.Area;
        private bool _showGrid = true;
        private int _stonePixelSize = 40;
        private bool _nWarningVisible;
        private bool _nWarningShown;
        private Color _gridColor = Color.FromRgb(128, 128, 128);

        private int _progress;
        private bool _isProcessing;
        private bool _isExporting;
        private string _dimensionInfo = "";
        private string _dimensionSize = "";
        private string _dimensionArea = "";
        private string _stoneInfo = "";
        private string _mouldInfo = "";
        private string _originalInfo = "";

        private string _elapsedTime = "";
        private string _statusText = Loc.Get("StatusReady");
        private bool _imageLoaded;
        private bool _mosaicDone;
        private double _zoomLevel = 1;
        private int _bitmapPixelWidth = 1;
        private int _bitmapPixelHeight = 1;
        private string _pixelCoordInfo = "";
        private string _pixelDetailInfo = "";
        private string _pixelColorInfo = "";
        private string _pixelScaleInfo = "";
        private string _usedColorInfo = "";
        private bool _isPixelEditActive;

        private double _navViewLeft;
        private double _navViewTop;
        private double _navViewWidth;
        private double _navViewHeight;
        private bool _isSourcePixelMode;
        private bool _isTargetPixelMode;
        private int _editedPixelCount;

        private string _propStoneName = "";
        private string _propStoneId = "";
        private string _propPixelCoord = "";
        private string _propMouldCoord = "";
        private string _propRgbInfo = "";
        private IBrush _propColorBrush = Brushes.Transparent;
        private bool _hasSelection;
        private Bitmap? _propTextureBitmap;
        private int _selectedStoneIndex;
        private int _selectedPixelY, _selectedPixelX;
        private string _selectedCodeName = "";
        private ObservableCollection<StoneThumbItem> _propStoneThumbs = new();

        private readonly Stack<(int arnIndex, int oldStoneIndex, int newStoneIndex, string codeName, int pixelY, int pixelX)> _stoneUndoStack = new();
        private readonly Stack<(int arnIndex, int oldStoneIndex, int newStoneIndex, string codeName, int pixelY, int pixelX)> _stoneRedoStack = new();

        public Bitmap? DisplayBitmap
        {
            get => _displayBitmap;
            set { _displayBitmap = value; OnPropertyChanged(); }
        }

        public double WidthCm
        {
            get => _widthCm;
            set
            {
                if (Math.Abs(_widthCm - value) < 0.01) return;
                _widthCm = value;
                OnPropertyChanged();
            }
        }

        public double HeightCm => _heightCm;

        public int TargetColors
        {
            get => _targetColors;
            set { _targetColors = value; OnPropertyChanged(); }
        }

        public int RgbIncrement
        {
            get => _rgbIncrement;
            set { _rgbIncrement = value; OnPropertyChanged(); }
        }

public bool UseLab
        {
            get => _useLab;
            set { _useLab = value; OnPropertyChanged(); }
        }

        public bool UseAverage
        {
            get => _useAverage;
            set { _useAverage = value; OnPropertyChanged(); }
        }

        public InterpolationMethod SelectedInterpolation
        {
            get => _interpolationMethod;
            set { _interpolationMethod = value; OnPropertyChanged(); }
        }

        public static InterpolationMethod[] InterpolationMethods { get; } =
            (InterpolationMethod[])Enum.GetValues(typeof(InterpolationMethod));

        public bool ShowGrid
        {
            get => _showGrid;
            set { _showGrid = value; OnPropertyChanged(); RegenerateRS(); }
        }

        public int StonePixelSize
        {
            get => _stonePixelSize;
            set
            {
                if (_stonePixelSize == value) return;
                _stonePixelSize = value;
                MosaicData.N = value;
                OnPropertyChanged();
                RegenerateRS();
                if (value > 40 && !_nWarningShown)
                {
                    _nWarningShown = true;
                    NWarningVisible = true;
                }
            }
        }

        public bool NWarningVisible
        {
            get => _nWarningVisible;
            set { _nWarningVisible = value; OnPropertyChanged(); }
        }

        public void DismissNWarning() => NWarningVisible = false;

        public Color GridColor
        {
            get => _gridColor;
            set { _gridColor = value; OnPropertyChanged(); if (_showGrid) RegenerateRS(); else RedrawOverlay(); }
        }

        public static Color[] GridColorPresets { get; } = new[]
        {
            Colors.Black, Colors.Gray, Colors.White,
            Colors.Red, Colors.Green, Colors.Blue,
            Colors.Yellow, Colors.Orange, Colors.Purple,
            Colors.Cyan, Colors.Magenta, Colors.Brown
        };

        private Color[] _gridColorShades = Array.Empty<Color>();
        public Color[] GridColorShades
        {
            get => _gridColorShades;
            set { _gridColorShades = value; OnPropertyChanged(); }
        }

        public void SelectMainColor(Color c)
        {
            var shades = new Color[7];
            for (int i = 0; i < 7; i++)
            {
                double t = i / 6.0;
                if (t < 0.5)
                {
                    double f = t / 0.5;
                    shades[i] = Color.FromRgb(
                        (byte)(c.R * f),
                        (byte)(c.G * f),
                        (byte)(c.B * f));
                }
                else
                {
                    double f = (t - 0.5) / 0.5;
                    shades[i] = Color.FromRgb(
                        (byte)(c.R + (255 - c.R) * f),
                        (byte)(c.G + (255 - c.G) * f),
                        (byte)(c.B + (255 - c.B) * f));
                }
            }
            GridColorShades = shades;
        }

        public int Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRunMosaic)); OnPropertyChanged(nameof(CanExport)); }
        }

        public bool IsExporting
        {
            get => _isExporting;
            set { _isExporting = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRunMosaic)); OnPropertyChanged(nameof(CanExport)); }
        }

        public string DimensionInfo
        {
            get => _dimensionInfo;
            set { _dimensionInfo = value; OnPropertyChanged(); }
        }

        public string DimensionSize
        {
            get => _dimensionSize;
            set { _dimensionSize = value; OnPropertyChanged(); }
        }

        public string DimensionArea
        {
            get => _dimensionArea;
            set { _dimensionArea = value; OnPropertyChanged(); }
        }

        public string StoneInfo
        {
            get => _stoneInfo;
            set { _stoneInfo = value; OnPropertyChanged(); }
        }

        public string MouldInfo
        {
            get => _mouldInfo;
            set { _mouldInfo = value; OnPropertyChanged(); }
        }

        public string OriginalInfo
        {
            get => _originalInfo;
            set { _originalInfo = value; OnPropertyChanged(); }
        }

        public string ElapsedTime
        {
            get => _elapsedTime;
            set { _elapsedTime = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public bool ImageLoaded
        {
            get => _imageLoaded;
            set { _imageLoaded = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRunMosaic)); }
        }

        public bool MosaicDone
        {
            get => _mosaicDone;
            set { _mosaicDone = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExport)); }
        }

        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                _zoomLevel = Math.Max(_minZoomLevel, Math.Min(20, value));
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImageDisplayWidth));
                OnPropertyChanged(nameof(ImageDisplayHeight));
                OnPropertyChanged(nameof(ZoomInfo));
            }
        }

        public double ImageDisplayWidth => _bitmapPixelWidth * _zoomLevel;
        public double ImageDisplayHeight => _bitmapPixelHeight * _zoomLevel;
        public int BitmapPixelWidth => _bitmapPixelWidth;
        public int BitmapPixelHeight => _bitmapPixelHeight;
        public int StoneColumns => (int)MosaicEngine.width;
        public int StoneRows => (int)MosaicEngine.height;
        public string ZoomInfo => $"N={_zoomLevel:F1}  {_bitmapPixelWidth * _zoomLevel:F0}x{_bitmapPixelHeight * _zoomLevel:F0}";


        public double NavViewLeft { get => _navViewLeft; set { _navViewLeft = value; OnPropertyChanged(); } }
        public double NavViewTop { get => _navViewTop; set { _navViewTop = value; OnPropertyChanged(); } }
        public double NavViewWidth { get => _navViewWidth; set { _navViewWidth = value; OnPropertyChanged(); } }
        public double NavViewHeight { get => _navViewHeight; set { _navViewHeight = value; OnPropertyChanged(); } }

        public void UpdateNavigator(double viewportW, double viewportH, double offsetX, double offsetY, double navSize)
        {
            double imgW = ImageDisplayWidth;
            double imgH = ImageDisplayHeight;
            if (imgW <= 0 || imgH <= 0) return;

            double scale = Math.Min(navSize / imgW, navSize / imgH);
            double thumbW = imgW * scale;
            double thumbH = imgH * scale;

            NavViewLeft = (offsetX / imgW) * thumbW;
            NavViewTop = (offsetY / imgH) * thumbH;
            NavViewWidth = Math.Min(viewportW / imgW, 1.0) * thumbW;
            NavViewHeight = Math.Min(viewportH / imgH, 1.0) * thumbH;
        }

        public string PixelCoordInfo
        {
            get => _pixelCoordInfo;
            set { _pixelCoordInfo = value; OnPropertyChanged(); }
        }

        public string PixelDetailInfo
        {
            get => _pixelDetailInfo;
            set { _pixelDetailInfo = value; OnPropertyChanged(); }
        }

        public string PixelColorInfo
        {
            get => _pixelColorInfo;
            set { _pixelColorInfo = value; OnPropertyChanged(); }
        }

        public string PixelScaleInfo
        {
            get => _pixelScaleInfo;
            set { _pixelScaleInfo = value; OnPropertyChanged(); }
        }

        public string UsedColorInfo
        {
            get => _usedColorInfo;
            set { _usedColorInfo = value; OnPropertyChanged(); }
        }

        public bool IsPixelEditActive
        {
            get => _isPixelEditActive;
            set { _isPixelEditActive = value; OnPropertyChanged(); }
        }

        public bool IsSourcePixelMode
        {
            get => _isSourcePixelMode;
            set { _isSourcePixelMode = value; OnPropertyChanged(); }
        }

        public bool IsTargetPixelMode
        {
            get => _isTargetPixelMode;
            set { _isTargetPixelMode = value; OnPropertyChanged(); }
        }


        public int EditedPixelCount
        {
            get => _editedPixelCount;
            set { _editedPixelCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(EditedPixelCountText)); }
        }

        public string SelectedStoneText => Loc.Fmt("PropStoneFmt", SelectedStoneIndex);
        public string EditedPixelCountText => Loc.Fmt("PropEditedFmt", EditedPixelCount);

        public bool HasSelection
        {
            get => _hasSelection;
            set { _hasSelection = value; OnPropertyChanged(); }
        }

        public string PropStoneName
        {
            get => _propStoneName;
            set { _propStoneName = value; OnPropertyChanged(); }
        }

        public string PropStoneId
        {
            get => _propStoneId;
            set { _propStoneId = value; OnPropertyChanged(); }
        }

        public string PropPixelCoord
        {
            get => _propPixelCoord;
            set { _propPixelCoord = value; OnPropertyChanged(); }
        }

        public string PropMouldCoord
        {
            get => _propMouldCoord;
            set { _propMouldCoord = value; OnPropertyChanged(); }
        }

        public string PropRgbInfo
        {
            get => _propRgbInfo;
            set { _propRgbInfo = value; OnPropertyChanged(); }
        }

        public IBrush PropColorBrush
        {
            get => _propColorBrush;
            set { _propColorBrush = value; OnPropertyChanged(); }
        }

        public Bitmap? PropTextureBitmap
        {
            get => _propTextureBitmap;
            set { _propTextureBitmap = value; OnPropertyChanged(); }
        }

        public int SelectedStoneIndex
        {
            get => _selectedStoneIndex;
            set { _selectedStoneIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedStoneText)); }
        }

        public ObservableCollection<StoneThumbItem> PropStoneThumbs
        {
            get => _propStoneThumbs;
            set { _propStoneThumbs = value; OnPropertyChanged(); }
        }

        public bool CanRunMosaic => ImageLoaded && !IsProcessing && !IsExporting;
        public bool CanExport => MosaicDone && !IsProcessing && !IsExporting;

        public ObservableCollection<ColorItem> CatalogColors { get; } = new();
        public ObservableCollection<PaletteItem> PaletteColors { get; } = new();
        public ObservableCollection<AssignedItem> AssignedColors { get; } = new();

        public MainViewModel()
        {
            try
            {
                ColorCatalogService.LoadDefaultCatalog();
                RefreshCatalogList();
            }
            catch (Exception ex)
            {
                StatusText = Loc.Fmt("StatusError", ex.Message);
            }
        }

        public void LoadImage(string path)
        {
            ProjectService.CurrentPictureFileName = path;
            ProjectService.CurrentFileName = "";
            MosaicEngine.Reset();
            MosaicDone = false;

            // Clear old mosaic state
            MosaicData.rsBitmap?.Dispose();
            MosaicData.rsBitmap = null;
            MosaicData.exportBitmap?.Dispose();
            MosaicData.exportBitmap = null;
            MosaicData.arn = null;
            MosaicData.arMA.Clear();
            MosaicData.arMB.Clear();
            MosaicData.arMBR.Clear();
            PixelEditService.EditedPixels.Clear();
            EditedPixelCount = 0;
            IsPixelEditActive = false;
            UsedColorInfo = "";
            ElapsedTime = "";
            HasSelection = false;
            _stoneUndoStack.Clear();
            _stoneRedoStack.Clear();
            StoneTextureService.Reset();

            var bmp = MosaicEngine.LoadImage(path);
            if (bmp != null)
            {
                _bitmapPixelWidth = bmp.Width;
                _bitmapPixelHeight = bmp.Height;
                OnPropertyChanged(nameof(BitmapPixelWidth));
                OnPropertyChanged(nameof(BitmapPixelHeight));
                _initialZoomLevel = 2;
                ZoomLevel = 2;
                DisplayBitmap = ImageService.ToAvaloniaBitmap(bmp);
                ImageLoaded = true;
                UpdateDimensions();
                AutoSelectGridColor(bmp);
                StatusText = Loc.Get("StatusImageLoaded");
            }
            else
            {
                Alert(Loc.Get("AlertImageTitle"), Loc.Get("AlertImageFailed"));
            }
        }

        private void AutoSelectGridColor(SKBitmap bmp)
        {
            int step = Math.Max(1, Math.Min(bmp.Width, bmp.Height) / 40);
            long total = 0;
            int count = 0;
            for (int y = 0; y < bmp.Height; y += step)
            {
                for (int x = 0; x < bmp.Width; x += step)
                {
                    var px = bmp.GetPixel(x, y);
                    total += (px.Red * 299 + px.Green * 587 + px.Blue * 114) / 1000;
                    count++;
                }
            }
            int avg = count > 0 ? (int)(total / count) : 128;
            SelectMainColor(Colors.Gray);
            int shadeIdx = avg < 100 ? 2 : avg > 155 ? 4 : 3;
            GridColor = GridColorShades[shadeIdx];
        }

        public void UpdateDimensions()
        {
            int numOfStones = Convert.ToInt32((WidthCm * 10.0) / 12.0);
            if (numOfStones < 2) numOfStones = 2;
            double roundedCm = numOfStones * 12 / 10.0;
            WidthCm = roundedCm;

            if (!ImageLoaded) return;

            if (MosaicData.inputBitmap != null)
            {
                int maxStones = MosaicData.inputBitmap.Width;
                double maxCm = maxStones * 12 / 10.0;
                if (WidthCm > maxCm)
                {
                    WidthCm = maxCm;
                    numOfStones = maxStones;
                    Alert(Loc.Get("AlertResolutionTitle"), Loc.Get("AlertResolutionBody"));
                }
            }

            var dim = MosaicEngine.CalculateDimensions(WidthCm);
            if (dim == null) return;

            _heightCm = dim.HeightCm;
            DimensionInfo = $"{dim.WidthCm:F1} cm x {dim.HeightCm:F1} cm = {dim.AreaM2:F2} m²";
            DimensionSize = $"{dim.WidthCm:F1} x {dim.HeightCm:F1} cm";
            DimensionArea = $"{dim.AreaM2:F2} m²";
            StoneInfo = Loc.Fmt("InfoStones", dim.StoneColumns, dim.StoneRows, dim.Stones);
            MouldInfo = Loc.Fmt("InfoMoulds", dim.MouldColumns, dim.MouldRows, dim.Moulds);
            OriginalInfo = Loc.Fmt("InfoOriginal", dim.OriginalWidth, dim.OriginalHeight);
        }

        public async Task RunMosaicAsync()
        {
            if (!ImageLoaded)
            {
                Alert(Loc.Get("AlertMosaicTitle"), Loc.Get("AlertMosaicNoImage"));
                return;
            }
            if (!CanRunMosaic) return;

            IsProcessing = true;
            Progress = 0;
            MosaicDone = false;
            StatusText = Loc.Get("StatusStarting");
            ElapsedTime = "";

            ColorCatalogService.SetActiveColors();
            int activeCount = MosaicData.arRGB.Count;

            if (activeCount == 0)
            {
                StatusText = Loc.Get("StatusNoColors");
                IsProcessing = false;
                Alert(Loc.Get("AlertMosaicTitle"), Loc.Get("AlertMosaicNoColors"));
                return;
            }

            StatusText = Loc.Fmt("StatusActiveColors", activeCount, MosaicData.arRGBAll.Count);

            // pix3: Mos basıldığında rgbM hesapla
            int totalPixels = (int)(MosaicEngine.width * MosaicEngine.height);
            int size = (int)(0.1 * totalPixels);
            int ew = Math.DivRem(size, 10, out _);
            TargetColors = Math.Clamp(Math.Max(2, ew * 10), 2, Math.Max(2, totalPixels));
            System.Diagnostics.Debug.WriteLine($"[MOS-DIAG] width={MosaicEngine.width} height={MosaicEngine.height} totalPixels={totalPixels} TargetColors={TargetColors} RgbInc={RgbIncrement} activeColors={activeCount} inputBitmap={MosaicData.inputBitmap?.Width}x{MosaicData.inputBitmap?.Height}");
            Console.WriteLine($"[MOS-DIAG] width={MosaicEngine.width} height={MosaicEngine.height} totalPixels={totalPixels} TargetColors={TargetColors} RgbInc={RgbIncrement} activeColors={activeCount} inputBitmap={MosaicData.inputBitmap?.Width}x{MosaicData.inputBitmap?.Height}");
            StatusText = Loc.Fmt("StatusCalculated", TargetColors);

            var sw = Stopwatch.StartNew();

            try
            {
                SKBitmap? result = null;
                SKBitmap? rsBmp = null;
                await Task.Run(() =>
                {
                    Console.WriteLine($"[MOS-DIAG] RunM3 starting...");
                    result = MosaicEngine.RunM3(
                        TargetColors,
                        RgbIncrement,
                        UseLab,
                        UseAverage,
                        SelectedInterpolation,
                        progress => Dispatcher.UIThread.Post(() => Progress = progress)
                    );

                    int R = MosaicData.dataM3.GetLength(0);
                    int C = MosaicData.dataM3.GetLength(1);
                    int N = MosaicData.N;
                    int gw = _showGrid ? Math.Max(1, N / 11) : 0;
                    if (gw > 0)
                        StoneTextureService.ResizeTextures(N - gw);
                    var gc = new SKColor(_gridColor.R, _gridColor.G, _gridColor.B);
                    rsBmp = StoneTextureService.GenerateRSBitmap(R, C, N, _showGrid, gw, gc);
                    Console.WriteLine($"[MOS-DIAG] RunM3 done. result={result?.Width}x{result?.Height} rsBmp={rsBmp?.Width}x{rsBmp?.Height} arMB0={MosaicData.arMB.Count}>{(MosaicData.arMB.Count > 0 ? MosaicData.arMB[0].Count : 0)}");
                });

                sw.Stop();
                ElapsedTime = sw.Elapsed.ToString(@"m\:ss\.ff");

                if (rsBmp != null)
                {
                    MosaicData.rsBitmap?.Dispose();
                    MosaicData.rsBitmap = rsBmp;
                    _bitmapPixelWidth = rsBmp.Width;
                    _bitmapPixelHeight = rsBmp.Height;
                }
                else if (result != null)
                {
                    _bitmapPixelWidth = result.Width;
                    _bitmapPixelHeight = result.Height;
                }

                if (result != null)
                {
                    OnPropertyChanged(nameof(BitmapPixelWidth));
                    OnPropertyChanged(nameof(BitmapPixelHeight));
                    OnPropertyChanged(nameof(StoneColumns));
                    OnPropertyChanged(nameof(StoneRows));
                    MosaicDone = true;
                    FilterCatalogByUsedColors();
                    RedrawOverlay();
                    int totalColors = MosaicData.arRGBAll.Count;
                    var uniqueCodes = new HashSet<string>();
                    foreach (var arList in MosaicData.arMB)
                        foreach (var c in arList)
                            if (!string.IsNullOrEmpty(c.codeName))
                                uniqueCodes.Add(c.codeName);
                    int usedColors = uniqueCodes.Count;
                    UsedColorInfo = Loc.Fmt("StatusUsedColors", totalColors, usedColors);
                    StatusText = Loc.Fmt("StatusCompleted", usedColors, sw.Elapsed.TotalSeconds.ToString("F1"));
                }
            }
            catch (OutOfMemoryException)
            {
                StatusText = Loc.Get("AlertMemoryBody");
                Alert(Loc.Get("AlertMemoryTitle"), Loc.Get("AlertMemoryBody"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MOS-DIAG] EXCEPTION: {ex}");
                StatusText = Loc.Fmt("StatusError", ex.Message);
                Alert(Loc.Get("AlertErrorTitle"), Loc.Fmt("AlertErrorBody", ex.Message));
            }
            finally
            {
                IsProcessing = false;
                Progress = 100;
            }
        }

        public void SaveProject(string filePath)
        {
            ProjectService.Save(filePath, WidthCm, ZoomLevel,
                ShowGrid, false,
                _gridColor.R, _gridColor.G, _gridColor.B,
                (int)SelectedInterpolation);
            StatusText = Loc.Fmt("StatusSaved", System.IO.Path.GetFileName(filePath));
        }

        public async void OpenProject(string filePath)
        {
            var data = ProjectService.Open(filePath);
            if (data == null)
            {
                StatusText = Loc.Get("StatusOpenFailed");
                Alert(Loc.Get("AlertProjectTitle"), Loc.Get("AlertProjectOpenFailed"));
                return;
            }

            if (!string.IsNullOrEmpty(ProjectService.CurrentPictureFileName) &&
                System.IO.File.Exists(ProjectService.CurrentPictureFileName))
            {
                var bmp = ImageService.LoadImage(ProjectService.CurrentPictureFileName);
                if (bmp != null) MosaicData.inputBitmap = bmp;
            }

            WidthCm = data.WidthCm;
            _stonePixelSize = data.N;
            OnPropertyChanged(nameof(StonePixelSize));
            GridColor = Color.FromRgb(data.GridColorR, data.GridColorG, data.GridColorB);
            SelectedInterpolation = (InterpolationMethod)data.InterpolationMethod;
            ShowGrid = data.ShowGrid;

            int R = MosaicData.dataM3.GetLength(0);
            int C = MosaicData.dataM3.GetLength(1);

            MosaicData.exportBitmap?.Dispose();
            MosaicData.exportBitmap = ImageService.FromByteArray(MosaicData.dataM3, R, C);
            _bitmapPixelWidth = MosaicData.exportBitmap.Width;
            _bitmapPixelHeight = MosaicData.exportBitmap.Height;
            OnPropertyChanged(nameof(BitmapPixelWidth));
            OnPropertyChanged(nameof(BitmapPixelHeight));
            OnPropertyChanged(nameof(StoneColumns));
            OnPropertyChanged(nameof(StoneRows));

            MosaicDone = true;
            ImageLoaded = true;
            _stoneUndoStack.Clear();
            _stoneRedoStack.Clear();
            EditedPixelCount = PixelEditService.EditedPixels.Count;

            RefreshCatalogList();
            FilterCatalogByUsedColors();
            UpdateDimensions();

            // Show exportBitmap immediately while RS generates
            DisplayBitmap = ImageService.ToAvaloniaBitmap(MosaicData.exportBitmap);
            StatusText = Loc.Get("StatusGeneratingRs");

            // Generate RS bitmap with stone textures
            int N = data.N;
            if (MosaicData.arn != null && N > 1)
            {
                IsProcessing = true;
                SKBitmap? rsBmp = null;
                try
                {
                    await Task.Run(() =>
                    {
                        StoneTextureService.LoadTextures();
                        int gw = _showGrid ? Math.Max(1, N / 11) : 0;
                        int texSize = gw > 0 ? N - gw : N;
                        StoneTextureService.ResizeTextures(texSize);
                        var gc = new SKColor(_gridColor.R, _gridColor.G, _gridColor.B);
                        rsBmp = StoneTextureService.GenerateRSBitmap(R, C, N, _showGrid, gw, gc);
                    });

                    if (rsBmp != null)
                    {
                        MosaicData.rsBitmap?.Dispose();
                        MosaicData.rsBitmap = rsBmp;
                        _bitmapPixelWidth = rsBmp.Width;
                        _bitmapPixelHeight = rsBmp.Height;
                        OnPropertyChanged(nameof(BitmapPixelWidth));
                        OnPropertyChanged(nameof(BitmapPixelHeight));
                        RedrawOverlay();
                    }
                }
                catch (Exception)
                {
                    rsBmp?.Dispose();
                }
                IsProcessing = false;
            }

            FitToWindow(_lastViewportWidth, _lastViewportHeight);
            StatusText = Loc.Fmt("StatusOpened", System.IO.Path.GetFileName(filePath));
        }

        public async Task ExportImageAsync(string path)
        {
            if (IsExporting) return;

            SKBitmap? bmp = MosaicData.rsBitmap ?? MosaicData.exportBitmap;
            if (bmp == null)
            {
                Alert(Loc.Get("AlertExportTitle"), Loc.Get("AlertExportNoMosaic"));
                return;
            }

            var fmt = path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                      path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                ? SKEncodedImageFormat.Jpeg
                : SKEncodedImageFormat.Png;

            // Encode a snapshot so pixel edits during export can't touch the bitmap being written
            var snapshot = bmp.Copy();
            IsExporting = true;
            StatusText = Loc.Fmt("StatusExporting", System.IO.Path.GetFileName(path));
            try
            {
                await Task.Run(() => ImageService.ExportImage(snapshot, path, fmt));
                StatusText = Loc.Fmt("StatusSaved", System.IO.Path.GetFileName(path));
            }
            catch (Exception ex)
            {
                StatusText = Loc.Fmt("StatusError", ex.Message);
            }
            finally
            {
                snapshot.Dispose();
                IsExporting = false;
            }
        }

        public void RefreshLocalized()
        {
            OnPropertyChanged(nameof(SelectedStoneText));
            OnPropertyChanged(nameof(EditedPixelCountText));
            if (ImageLoaded)
                UpdateDimensions();
            if (MosaicDone)
                RecalcUsedColorInfo();
            StatusText = Loc.Get("StatusReady");
        }

        private void RecalcUsedColorInfo()
        {
            int totalColors = MosaicData.arRGBAll.Count;
            var uniqueCodes = new HashSet<string>();
            foreach (var arList in MosaicData.arMB)
                foreach (var c in arList)
                    if (!string.IsNullOrEmpty(c.codeName))
                        uniqueCodes.Add(c.codeName);
            int usedColors = uniqueCodes.Count;
            UsedColorInfo = Loc.Fmt("StatusUsedColors", totalColors, usedColors);
        }

        public void FitToWindow(double viewportWidth, double viewportHeight)
        {
            if (_bitmapPixelWidth <= 0 || _bitmapPixelHeight <= 0) return;
            _lastViewportWidth = viewportWidth;
            _lastViewportHeight = viewportHeight;
            double fit = CalcFitZoom();
            _minZoomLevel = Math.Max(0.01, fit);
            _initialZoomLevel = Math.Max(_minZoomLevel, Math.Min(10, fit));
            ZoomLevel = _initialZoomLevel;
        }

        private double CalcFitZoom()
        {
            if (_lastViewportWidth <= 0 || _lastViewportHeight <= 0 ||
                _bitmapPixelWidth <= 0 || _bitmapPixelHeight <= 0) return 1.0;
            double zx = (_lastViewportWidth - 16) / _bitmapPixelWidth;
            double zy = (_lastViewportHeight - 16) / _bitmapPixelHeight;
            return Math.Min(zx, zy);
        }

        private void AdjustZoomForBitmapChange(int oldWidth, int oldHeight)
        {
            if (oldWidth <= 0 || oldHeight <= 0 || _lastViewportWidth <= 0) return;
            double scale = (double)oldWidth / _bitmapPixelWidth;
            double fit = CalcFitZoom();
            _minZoomLevel = Math.Max(0.01, fit);
            ZoomLevel = Math.Max(_minZoomLevel, _zoomLevel * scale);
        }

        public void SyncColorExclusion(ColorItem item)
        {
            ColorCatalogService.SetLeaveOut(item.Index, item.IsExcluded);
            ReorderCatalogList();
        }

        public void SetAllColors(bool excluded)
        {
            var items = new List<ColorItem>(CatalogColors);
            foreach (var item in items)
            {
                item.IsExcluded = excluded;
                if (item.Index >= 0 && item.Index < MosaicData.arRGBAll.Count)
                {
                    MosaicData.arRGBAll[item.Index].boolLeaveOut = excluded;
                    if (item.Index < MosaicData.arcs.Count)
                        MosaicData.arcs[item.Index] = excluded;
                }
            }
            ColorCatalogService.SetActiveColors();
            ReorderCatalogList();
        }

        private void ReorderCatalogList()
        {
        }

        private void RefreshCatalogList()
        {
            CatalogColors.Clear();
            var list = ColorCatalogService.GetCatalogList();

            foreach (var c in list)
            {
                CatalogColors.Add(new ColorItem
                {
                    Index = c.Index,
                    R = c.R, G = c.G, B = c.B,
                    CodeName = c.CodeName,
                    Name = c.Name,
                    ID = c.ID,
                    IsExcluded = c.IsExcluded
                });
            }
        }

        private void FilterCatalogByUsedColors()
        {
            if (MosaicData.arMB.Count == 0 || MosaicData.arMB[0].Count == 0) return;

            var usedCodes = new HashSet<string>();
            foreach (var r in MosaicData.arMB[0])
            {
                if (!string.IsNullOrEmpty(r.codeName))
                    usedCodes.Add(r.codeName);
            }

            foreach (var item in CatalogColors)
            {
                bool excluded = !usedCodes.Contains(item.CodeName);
                item.IsExcluded = excluded;
                if (item.Index >= 0 && item.Index < MosaicData.arRGBAll.Count)
                {
                    MosaicData.arRGBAll[item.Index].boolLeaveOut = excluded;
                    if (item.Index < MosaicData.arcs.Count)
                        MosaicData.arcs[item.Index] = excluded;
                }
            }

            PopulatePaletteAndAssigned();
        }

        private void PopulatePaletteAndAssigned()
        {
            PaletteColors.Clear();
            AssignedColors.Clear();

            if (MosaicData.arMB.Count == 0 || MosaicData.arMB[0].Count == 0) return;

            var mbByCode = new Dictionary<string, rgb>();
            foreach (var c in MosaicData.arMB[0])
            {
                if (!string.IsNullOrEmpty(c.codeName) && !mbByCode.ContainsKey(c.codeName))
                    mbByCode[c.codeName] = c;
            }

            int idx = 0;
            foreach (var catItem in CatalogColors)
            {
                if (catItem.IsExcluded) continue;

                idx++;
                if (mbByCode.TryGetValue(catItem.CodeName, out var c))
                {
                    PaletteColors.Add(new PaletteItem
                    {
                        Index = idx,
                        R = (byte)c.r, G = (byte)c.g, B = (byte)c.b
                    });
                    AssignedColors.Add(new AssignedItem
                    {
                        Num = idx,
                        ID = c.ID,
                        CodeName = c.codeName,
                        PixelCount = c.numOfPixel,
                        R = (byte)c.r, G = (byte)c.g, B = (byte)c.b
                    });
                }
                else
                {
                    PaletteColors.Add(new PaletteItem
                    {
                        Index = idx,
                        R = catItem.R, G = catItem.G, B = catItem.B
                    });
                    AssignedColors.Add(new AssignedItem
                    {
                        Num = idx,
                        ID = catItem.ID,
                        CodeName = catItem.CodeName,
                        PixelCount = 0,
                        R = catItem.R, G = catItem.G, B = catItem.B
                    });
                }
            }
        }

        public void OnImagePointerMoved(double pointerX, double pointerY, double imageControlWidth, double imageControlHeight)
        {
        }

        public void OnImagePressed(double pointerX, double pointerY, double imageControlWidth, double imageControlHeight, bool isLeftButton, bool isMiddleButton)
        {
            if (!MosaicDone) return;

            double sw = imageControlWidth / MosaicEngine.width;
            int x = (int)(pointerX / sw);
            int y = (int)(pointerY / sw);
            int w = (int)MosaicEngine.width;
            int h = (int)MosaicEngine.height;

            if (x < 0 || x >= w || y < 0 || y >= h) return;

            if (isMiddleButton)
            {
                TogglePixelEditMode();
                return;
            }

            if (!isLeftButton) return;

            byte b = MosaicData.dataM3[y, x, 0];
            byte g = MosaicData.dataM3[y, x, 1];
            byte r = MosaicData.dataM3[y, x, 2];

            int colorId = drl.dat[y, x, 3];
            string codeName = "";
            if (colorId > 0)
            {
                foreach (var c in MosaicData.arRGB)
                {
                    if (c.ID == colorId)
                    {
                        codeName = c.codeName;
                        break;
                    }
                }
            }
            if (colorId == 0 && MosaicData.arMA.Count > 0)
            {
                foreach (var c in MosaicData.arMA[0])
                {
                    if (c.r == r && c.g == g && c.b == b)
                    {
                        colorId = c.ID;
                        codeName = c.codeName;
                        break;
                    }
                }
            }
            if (colorId == 0)
            {
                foreach (var c in MosaicData.arRGB)
                {
                    if (c.r == r && c.g == g && c.b == b)
                    {
                        colorId = c.ID;
                        codeName = c.codeName;
                        break;
                    }
                }
            }

            int yef = 0, xef = 0;
            Math.DivRem(y, 26, out yef);
            int xee = Math.DivRem(x, 13, out xef);
            if (xee % 2 != 0) yef = 25 - yef;

            PixelDetailInfo = $"yi={yef}  x={xef}    [{colorId}]";
            PixelColorInfo = $"{r} {g} {b}";

            PropStoneName = codeName;
            PropStoneId = colorId > 0 ? $"#{colorId}" : "";
            PropPixelCoord = $"Y: {y}  X: {x}";
            PropMouldCoord = $"yi: {yef}  xi: {xef}";
            PropRgbInfo = $"{r}, {g}, {b}";
            PropColorBrush = new SolidColorBrush(Color.FromRgb(r, g, b));
            HasSelection = true;
            UpdatePropTexture(codeName, y, x);

            if (PixelEditService.IsPixelEditActive)
            {
                if (PixelEditService.IsSourcePixelMode)
                {
                    PixelEditService.SetSourcePixel(y, x, r, g, b, colorId, codeName);
                    IsSourcePixelMode = false;
                    IsTargetPixelMode = true;
                }
                else if (PixelEditService.IsTargetPixelMode)
                {
                    string result = PixelEditService.EditPixel(y, x, r, g, b, colorId, codeName);
                    EditedPixelCount = PixelEditService.EditedPixels.Count;
                    StatusText = Loc.Fmt("StatusPixelEdit", result);

                    RedrawOverlay();
                }
            }
        }

        private System.Threading.CancellationTokenSource? _rsRegenerateCts;
        private readonly System.Threading.SemaphoreSlim _rsLock = new(1, 1);

        private async void RegenerateRS()
        {
            if (!MosaicDone) return;
            if (MosaicData.arMA.Count == 0) return;

            _rsRegenerateCts?.Cancel();
            var cts = new System.Threading.CancellationTokenSource();
            _rsRegenerateCts = cts;
            var ct = cts.Token;

            try { await Task.Delay(300, ct); }
            catch (TaskCanceledException) { return; }

            try { await _rsLock.WaitAsync(ct); }
            catch (OperationCanceledException) { return; }

            SKBitmap? rsBmp = null;
            try
            {
                if (ct.IsCancellationRequested) return;

                int R = MosaicData.dataM3.GetLength(0);
                int C = MosaicData.dataM3.GetLength(1);
                int N = _stonePixelSize;

                long totalPixels = (long)R * N * C * N;
                if (totalPixels > 800_000_000L)
                {
                    long mb = totalPixels * 4 / 1_000_000;
                    StatusText = Loc.Fmt("StatusNTooLarge", N, mb);
                    Alert(Loc.Get("AlertNTooLargeTitle"), Loc.Fmt("AlertNTooLargeBody", N, mb));
                    return;
                }

                StatusText = Loc.Fmt("StatusRegenRs", N);
                IsProcessing = true;

                MosaicData.rsBitmap?.Dispose();
                MosaicData.rsBitmap = null;

                await Task.Run(() =>
                {
                    if (ct.IsCancellationRequested) return;
                    int gw = _showGrid ? Math.Max(1, N / 11) : 0;
                    int texSize = gw > 0 ? N - gw : N;
                    StoneTextureService.ResizeTextures(texSize);
                    if (ct.IsCancellationRequested) return;
                    var gc = new SKColor(_gridColor.R, _gridColor.G, _gridColor.B);
                    rsBmp = StoneTextureService.GenerateRSBitmap(R, C, N, _showGrid, gw, gc);
                });

                if (!ct.IsCancellationRequested && rsBmp != null)
                {
                    MosaicData.rsBitmap?.Dispose();
                    MosaicData.rsBitmap = rsBmp;
                    rsBmp = null;
                    int oldW = _bitmapPixelWidth, oldH = _bitmapPixelHeight;
                    _bitmapPixelWidth = MosaicData.rsBitmap.Width;
                    _bitmapPixelHeight = MosaicData.rsBitmap.Height;
                    OnPropertyChanged(nameof(BitmapPixelWidth));
                    OnPropertyChanged(nameof(BitmapPixelHeight));
                    AdjustZoomForBitmapChange(oldW, oldH);
                    RedrawOverlay();
                    StatusText = Loc.Get("StatusReady");
                }
            }
            catch (OutOfMemoryException)
            {
                rsBmp?.Dispose();
                StatusText = Loc.Fmt("StatusErrorTooLarge", _stonePixelSize,
                    MosaicData.dataM3.GetLength(0), MosaicData.dataM3.GetLength(1));
                Alert(Loc.Get("AlertMemoryTitle"), Loc.Get("AlertMemoryBody"));
            }
            catch (Exception ex)
            {
                rsBmp?.Dispose();
                StatusText = Loc.Fmt("StatusErrorTooLarge", _stonePixelSize,
                    MosaicData.dataM3.GetLength(0), MosaicData.dataM3.GetLength(1));
                Alert(Loc.Get("AlertErrorTitle"), Loc.Fmt("AlertErrorBody", ex.Message));
            }
            finally
            {
                IsProcessing = false;
                _rsLock.Release();
            }
        }

        private void UpdatePropTexture(string codeName, int pixelY, int pixelX)
        {
            PropTextureBitmap = null;
            PropStoneThumbs.Clear();
            _selectedCodeName = codeName;
            _selectedPixelY = pixelY;
            _selectedPixelX = pixelX;

            if (string.IsNullOrEmpty(codeName)) return;

            int w = (int)MosaicEngine.width;
            int arnIndex = pixelY * w + pixelX;
            int stoneNum = (MosaicData.arn != null && arnIndex < MosaicData.arn.Length)
                ? MosaicData.arn[arnIndex] : 0;
            SelectedStoneIndex = stoneNum;

            var folder = StoneTextureService.FindFolderForCode(codeName);
            if (folder == null) return;

            string selectedPath = System.IO.Path.Combine(folder, $"{stoneNum + 1}.jpg");
            if (System.IO.File.Exists(selectedPath))
            {
                using var src = SKBitmap.Decode(selectedPath);
                if (src != null)
                {
                    var resized = src.Resize(new SKImageInfo(80, 80), new SKSamplingOptions(SKFilterMode.Linear));
                    PropTextureBitmap = ImageService.ToAvaloniaBitmap(resized);
                    resized.Dispose();
                }
            }

            for (int i = 1; i <= 16; i++)
            {
                string path = System.IO.Path.Combine(folder, $"{i}.jpg");
                if (!System.IO.File.Exists(path)) continue;
                using var bmp = SKBitmap.Decode(path);
                if (bmp == null) continue;
                var thumb = bmp.Resize(new SKImageInfo(44, 44), new SKSamplingOptions(SKFilterMode.Linear));
                PropStoneThumbs.Add(new StoneThumbItem
                {
                    Index = i - 1,
                    DisplayIndex = i,
                    Thumbnail = ImageService.ToAvaloniaBitmap(thumb),
                    IsSelected = (i - 1) == stoneNum
                });
                thumb.Dispose();
            }
        }

        public void SelectStone(int stoneIndex)
        {
            if (string.IsNullOrEmpty(_selectedCodeName)) return;
            if (MosaicData.arn == null) return;

            int w = (int)MosaicEngine.width;
            int arnIndex = _selectedPixelY * w + _selectedPixelX;
            if (arnIndex >= MosaicData.arn.Length) return;

            int oldIndex = MosaicData.arn[arnIndex];
            _stoneUndoStack.Push((arnIndex, oldIndex, stoneIndex, _selectedCodeName, _selectedPixelY, _selectedPixelX));
            _stoneRedoStack.Clear();

            MosaicData.arn[arnIndex] = stoneIndex;
            SelectedStoneIndex = stoneIndex;

            foreach (var item in PropStoneThumbs)
                item.IsSelected = item.Index == stoneIndex;

            var folder = StoneTextureService.FindFolderForCode(_selectedCodeName);
            if (folder != null)
            {
                string path = System.IO.Path.Combine(folder, $"{stoneIndex + 1}.jpg");
                if (System.IO.File.Exists(path))
                {
                    using var src = SKBitmap.Decode(path);
                    if (src != null)
                    {
                        var resized = src.Resize(new SKImageInfo(80, 80), new SKSamplingOptions(SKFilterMode.Linear));
                        PropTextureBitmap = ImageService.ToAvaloniaBitmap(resized);
                        resized.Dispose();
                    }
                }
            }

            RegenerateRS();
        }

        public void TogglePixelEditMode()
        {
            if (!MosaicDone) return;
            PixelEditService.TogglePixelEditMode();
            IsPixelEditActive = PixelEditService.IsPixelEditActive;
            IsSourcePixelMode = PixelEditService.IsSourcePixelMode;
            IsTargetPixelMode = PixelEditService.IsTargetPixelMode;
        }

        public void SetSourceFromCatalog(ColorItem item)
        {
            if (PixelEditService.Current == null)
                PixelEditService.Current = new PixelEditRecord();
            PixelEditService.Current.Source = new Models.rgb
            {
                r = item.R, g = item.G, b = item.B,
                ID = item.ID, codeName = item.CodeName
            };
            PixelEditService.IsSourcePixelMode = false;
            PixelEditService.IsTargetPixelMode = true;
            IsSourcePixelMode = false;
            IsTargetPixelMode = true;
            StatusText = Loc.Fmt("StatusPixelEdit", $"source: {item.CodeName}");
        }

        public void UndoPixelEdit()
        {
            if (!MosaicDone) return;

            if (_stoneUndoStack.Count > 0)
            {
                var entry = _stoneUndoStack.Pop();
                _stoneRedoStack.Push(entry);
                MosaicData.arn[entry.arnIndex] = entry.oldStoneIndex;
                StatusText = Loc.Get("StatusStoneUndo");
                UpdatePropTexture(entry.codeName, entry.pixelY, entry.pixelX);
                RegenerateRS();
                return;
            }

            if (!PixelEditService.CanUndo) return;
            string result = PixelEditService.UndoLastEdit();
            EditedPixelCount = PixelEditService.EditedPixels.Count;
            StatusText = Loc.Fmt("StatusPixelEdit", result);
            RedrawOverlay();
        }

        public void RedoPixelEdit()
        {
            if (!MosaicDone) return;

            if (_stoneRedoStack.Count > 0)
            {
                var entry = _stoneRedoStack.Pop();
                _stoneUndoStack.Push(entry);
                MosaicData.arn[entry.arnIndex] = entry.newStoneIndex;
                StatusText = Loc.Get("StatusStoneRedo");
                UpdatePropTexture(entry.codeName, entry.pixelY, entry.pixelX);
                RegenerateRS();
                return;
            }

            if (!PixelEditService.CanRedo) return;
            string result = PixelEditService.RedoLastEdit();
            EditedPixelCount = PixelEditService.EditedPixels.Count;
            StatusText = Loc.Fmt("StatusPixelEdit", result);
            RedrawOverlay();
        }

        private void RedrawOverlay()
        {
            if (!MosaicDone) return;

            SKBitmap? baseBmp = MosaicData.rsBitmap ?? MosaicData.exportBitmap;
            if (baseBmp == null) return;

            try
            {
                DisplayBitmap = ImageService.ToAvaloniaBitmap(baseBmp);
            }
            catch (OutOfMemoryException)
            {
                StatusText = "Bitmap too large for display";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class StoneThumbItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public int Index { get; set; }
        public int DisplayIndex { get; set; }
        public Bitmap? Thumbnail { get; set; }
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
