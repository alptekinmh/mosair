using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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

        private bool _stockShort;
        // Set by "stok kontrol" when the sheet's estimated remaining stock for this stone went negative.
        public bool StockShort
        {
            get => _stockShort;
            set { _stockShort = value; OnPropertyChanged(); }
        }

        // Tooltip stock line, as in WPF: "on hand → remaining kg". On hand ("Bizdeki (kg)") comes from Fetch Stock
        // and Check Stock, remaining ("Tahmini Kalan") from Check Stock. Each number is red at 0 or below, else green.
        private double? _stockKg;
        private double? _remainingKg;
        private static readonly IBrush KgOkBrush = new SolidColorBrush(Color.FromRgb(0x4e, 0xcb, 0x71));
        private static readonly IBrush KgShortBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));

        public double? StockKg
        {
            get => _stockKg;
            set { _stockKg = value; NotifyStockText(); }
        }

        public double? RemainingKg
        {
            get => _remainingKg;
            set { _remainingKg = value; NotifyStockText(); }
        }

        public bool StockKgVisible => _stockKg.HasValue;
        public bool RemainingVisible => _remainingKg.HasValue;
        public bool StockArrowVisible => _stockKg.HasValue && _remainingKg.HasValue;
        public string StockKgText => _stockKg.HasValue
            ? FormatKg(_stockKg.Value) + (_remainingKg.HasValue ? "" : " kg")
            : "";
        public string RemainingText => _remainingKg.HasValue
            ? FormatKg(_remainingKg.Value) + " kg" + (_stockKg.HasValue ? "" : " " + Loc.Get("StockKgLeft"))
            : "";
        public IBrush StockKgBrush => _stockKg <= 0 ? KgShortBrush : KgOkBrush;
        public IBrush RemainingBrush => _remainingKg <= 0 ? KgShortBrush : KgOkBrush;

        private static string FormatKg(double v) => v.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture);

        private void NotifyStockText()
        {
            OnPropertyChanged(nameof(StockKg));
            OnPropertyChanged(nameof(RemainingKg));
            OnPropertyChanged(nameof(StockKgVisible));
            OnPropertyChanged(nameof(RemainingVisible));
            OnPropertyChanged(nameof(StockArrowVisible));
            OnPropertyChanged(nameof(StockKgText));
            OnPropertyChanged(nameof(RemainingText));
            OnPropertyChanged(nameof(StockKgBrush));
            OnPropertyChanged(nameof(RemainingBrush));
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
        public Func<string, string, Task<bool>>? ShowConfirm { get; set; }
        public Func<StockSheetService.Config, Task<StockSheetService.Config?>>? ShowStockSettings { get; set; }
        public Func<string, Task>? OpenUrl { get; set; }

        private void Alert(string title, string message)
        {
            if (ShowAlert != null)
                Avalonia.Threading.Dispatcher.UIThread.Post(async () => await ShowAlert(title, message));
        }

        // ===== Google Sheet stock (ported from WPF: 📊, stok çek, stok kontrol, stok temizle, stok ekle) =====

        private bool _isStockBusy;
        public bool IsStockBusy
        {
            get => _isStockBusy;
            private set { _isStockBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanUseStock)); OnPropertyChanged(nameof(IsBusy)); }
        }
        public bool CanUseStock => !_isStockBusy;

        // Same column header the sheet uses for this mosaic: picture name first, then project file name.
        private static string StockProjectName()
        {
            if (!string.IsNullOrEmpty(ProjectService.CurrentPictureFileName))
                return System.IO.Path.GetFileNameWithoutExtension(ProjectService.CurrentPictureFileName);
            if (!string.IsNullOrEmpty(ProjectService.CurrentFileName))
                return System.IO.Path.GetFileNameWithoutExtension(ProjectService.CurrentFileName);
            return "";
        }

        private bool TryGetStockConfig(bool needScript, out StockSheetService.Config config)
        {
            config = StockSheetService.LoadConfig();
            if (string.IsNullOrEmpty(config.SheetId) || (needScript && string.IsNullOrEmpty(config.ScriptUrl)))
            {
                Alert(Loc.Get("StockTitle"), Loc.Get("StockNotConfigured"));
                return false;
            }
            return true;
        }

        private async Task<bool> Confirm(string title, string message) =>
            ShowConfirm != null && await ShowConfirm(title, message);

        private async Task RunStockAction(Func<Task> action, string doneMessage)
        {
            IsStockBusy = true;
            try
            {
                await action();
                StatusText = doneMessage;
            }
            catch (Exception ex)
            {
                StatusText = Loc.Fmt("StatusError", ex.Message);
                Alert(Loc.Get("StockTitle"), Loc.Fmt("StockErrFmt", ex.Message));
            }
            finally
            {
                IsStockBusy = false;
            }
        }

        public async Task OpenStockSheetAsync()
        {
            if (!TryGetStockConfig(false, out var config)) return;
            if (OpenUrl != null) await OpenUrl(StockSheetService.SheetUrl(config.SheetId));
        }

        public async Task ConfigureStockAsync()
        {
            if (ShowStockSettings == null) return;
            var updated = await ShowStockSettings(StockSheetService.LoadConfig());
            if (updated == null) return;
            StockSheetService.SaveConfig(updated);
            StatusText = Loc.Get("StockSettingsSaved");
        }

        // Stock last read from the configured sheet (at start-up and whenever an image or project is loaded).
        // "Stoğa göre" Mos uses it; this mosaic's own sheet column is left out of the other mosaics' share.
        private Dictionary<int, StockSheetService.StoneStock>? _loadedStock;

        public Task LoadStockOnStartupAsync() => RefreshStockAsync();

        // Reads "Bizdeki (kg)" (minus the other mosaics' share) so the catalog tooltips show stock and Mos can
        // keep to it. Only information here: the selection and the red dots are not touched; a failure is a note.
        public async Task RefreshStockAsync()
        {
            var config = StockSheetService.LoadConfig();
            if (string.IsNullOrEmpty(config.SheetId)) return;
            string project = StockProjectName();
            try
            {
                var stock = await StockSheetService.FetchOnHandAsync(config.SheetId, project.Length > 0 ? project : null);
                // A newer image may have been loaded while this was reading; keep only the stock for the current one.
                if (project != StockProjectName()) return;
                _loadedStock = stock;
                int matched = 0;
                foreach (var item in CatalogColors)
                    if (stock.TryGetValue(item.ID, out var s)) { item.StockKg = s.OnHandKg; matched++; }
                AppendStartupStatus(Loc.Fmt("StockLoadedOnStart", matched));
            }
            catch (Exception ex)
            {
                AppendStartupStatus(Loc.Fmt("StockLoadOnStartFailed", ex.Message));
            }
        }

        // Keeps a start-up warning that is already in the status bar (e.g. skipped catalog lines).
        private void AppendStartupStatus(string message) =>
            StatusText = StatusText == Loc.Get("StatusReady") ? message : StatusText + " · " + message;

        // markOnly = false: stones listed with no stock are unchecked, the others checked (as in WPF).
        // markOnly = true: the selection is left alone; stones with no stock only get a red dot.
        public async Task FetchStockAsync(bool markOnly = false)
        {
            if (!TryGetStockConfig(false, out var config)) return;
            Dictionary<int, double>? stock = null;
            await RunStockAction(async () =>
            {
                stock = await StockSheetService.FetchStockAsync(config.SheetId);
                foreach (var item in CatalogColors)
                    if (stock.TryGetValue(item.ID, out double onHand)) item.StockKg = onHand;
                if (markOnly)
                {
                    foreach (var c in MosaicData.arRGBAll)
                        c.stokYetersiz = stock.TryGetValue(c.ID, out double kg) && kg <= 0;
                    foreach (var item in CatalogColors)
                        item.StockShort = stock.TryGetValue(item.ID, out double kg) && kg <= 0;
                }
                else
                {
                    // Stones listed in the sheet are enabled when stock > 0 and disabled otherwise; others keep their state.
                    ApplyStockSelection(id => stock.TryGetValue(id, out double kg) ? kg <= 0 : null);
                }
            }, "");
            if (stock != null)
            {
                if (markOnly)
                {
                    int empty = stock.Count(kv => kv.Value <= 0 && MosaicData.arRGBAll.Any(c => c.ID == kv.Key));
                    StatusText = Loc.Fmt("StockFetchedMarked", stock.Count, empty);
                }
                else
                {
                    int off = 0;
                    foreach (var c in MosaicData.arRGBAll) if (c.boolLeaveOut) off++;
                    StatusText = Loc.Fmt("StockFetched", stock.Count, off);
                }
            }
        }

        public async Task CheckStockAsync()
        {
            if (!TryGetStockConfig(true, out var config)) return;
            string projectName = StockProjectName();
            if (projectName.Length == 0)
            {
                Alert(Loc.Get("StockTitle"), Loc.Get("StockNoProject"));
                return;
            }
            if (!MosaicDone || MosaicData.arMA.Count == 0)
            {
                Alert(Loc.Get("StockTitle"), Loc.Get("StockNoMosaic"));
                return;
            }

            // "Stoğa göre": fix the mosaic to stock first, so the sheet gets the final counts in one write.
            // When the fix cannot run (reason already shown) Stok Kontrol still does its normal check below.
            Dictionary<int, StockSheetService.StoneStock>? stock = null;
            if (UseStockAware)
                stock = await FixToStockAsync(config, projectName);

            var counts = new Dictionary<int, int>();
            foreach (var c in MosaicData.arMA[0])
                if (c.numOfPixel > 0)
                    counts[c.ID] = counts.TryGetValue(c.ID, out int n) ? n + c.numOfPixel : c.numOfPixel;
            if (counts.Count == 0)
            {
                Alert(Loc.Get("StockTitle"), Loc.Get("StockNoMosaic"));
                return;
            }
            var stones = new List<(int Id, int Count)>();
            foreach (var kv in counts) stones.Add((kv.Key, kv.Value));

            string fixReport = StatusText;
            HashSet<int>? shortIds = null;
            await RunStockAction(async () =>
            {
                var check = await StockSheetService.CheckStockAsync(config.ScriptUrl, config.SheetId, projectName, stones);
                shortIds = check.ShortIds;
                // Like WPF, short stones are only marked (red dot, red remaining kg); the selection is left as is.
                foreach (var c in MosaicData.arRGBAll) c.stokYetersiz = shortIds.Contains(c.ID);
                foreach (var item in CatalogColors)
                {
                    item.StockShort = shortIds.Contains(item.ID);
                    item.RemainingKg = check.Remaining.TryGetValue(item.ID, out double left) ? left : null;
                    if (check.OnHand.TryGetValue(item.ID, out double onHand)) item.StockKg = onHand;
                }
            }, "");
            if (shortIds == null) return; // write failed; the error was shown

            if (stock != null)
            {
                // The sheet's "Tahmini Kalan" can be read back before Google has recalculated it, so red dots
                // and remaining kg come from the stock read for the fix instead.
                ShowStockMarks(stock, counts);
                bool changed = fixReport != Loc.Get("StockAwareOk");
                StatusText = fixReport + " · " + Loc.Get(changed ? "StockAwareWritten" : "StockCountsWritten");
            }
            else
                StatusText = shortIds.Count == 0
                    ? Loc.Fmt("StockCheckOk", projectName)
                    : Loc.Fmt("StockCheckShort", projectName, shortIds.Count);
        }

        // "Stoğa göre" before Stok Kontrol writes: stones short of stock are used only as far as stock goes; the
        // rest of their pixels go to similar stones. Returns the stock that was used,
        // or null when the fix could not run (the reason is shown in a dialog, nothing is written then).
        private async Task<Dictionary<int, StockSheetService.StoneStock>?> FixToStockAsync(
            StockSheetService.Config config, string projectName)
        {
            void Fail(string message)
            {
                SetStockAwareReport(message);
                StatusText = message;
                Alert(Loc.Get("StockAwareTitle"), message);
            }

            // Works on a mosaic made in this session (Optimum or classic Mos); an opened project has no source
            // image data at stone resolution to judge substitutes by.
            if (!_mosaicMadeThisSession || MosaicEngine.LastRunPool == null)
            {
                Fail(Loc.Get("StockAwareNeedsMos"));
                return null;
            }

            // Available = Bizdeki minus the other mosaic columns; this mosaic's own column is left out.
            Dictionary<int, StockSheetService.StoneStock> stock;
            try
            {
                stock = await StockSheetService.FetchOnHandAsync(config.SheetId, projectName);
            }
            catch (Exception ex)
            {
                Fail(Loc.Fmt("StockAwareReadFailed", ex.Message));
                return null;
            }

            bool needsFix = false;
            foreach (var c in MosaicData.arMA[0])
            {
                if (c.numOfPixel <= 0) continue;
                if (stock.TryGetValue(c.ID, out var s) && c.numOfPixel > s.Capacity) needsFix = true;
            }
            _stockOnHand = stock;
            if (!needsFix)
            {
                SetStockAwareReport(Loc.Get("StockAwareOk"));
                StatusText = Loc.Get("StockAwareOk");
                return stock;
            }
            if (EditedPixelCount > 0 &&
                !await Confirm(Loc.Get("StockAwareTitle"), Loc.Fmt("StockAwareEditsConfirm", EditedPixelCount)))
                return null;

            int version = StartNewContent();
            IsProcessing = true;
            var sw = Stopwatch.StartNew();
            try
            {
                SKBitmap? result = null;
                var oldExport = MosaicData.exportBitmap;
                int k = OptimalK;
                bool optimum = OptimalAvailable;
                bool fixedOk = true;
                await Task.Run(() =>
                {
                    if (optimum)
                        result = ApplyOptimalKFor(k, stock);
                    else
                    {
                        fixedOk = FixClassicMosaicToStock(stock);
                        result = MosaicData.reducedBitmap;
                    }
                });
                sw.Stop();
                if (version != _contentVersion) return null; // a new image or project was opened meanwhile
                if (!fixedOk)
                {
                    Fail(Loc.Get("StockAwareCannotFix"));
                    return null;
                }
                FinishMosaic(result, sw.Elapsed);
                DisposeIfReplaced(oldExport, MosaicData.exportBitmap);
                ShowStockAwareResult(stock, windowIfLong: true);
                return stock;
            }
            catch (Exception ex)
            {
                StatusText = Loc.Fmt("StatusError", ex.Message);
                Alert(Loc.Get("AlertErrorTitle"), Loc.Fmt("AlertErrorBody", ex.Message));
                return null;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        // Red dots and "remaining" kg from the stock that was read: short = used beyond what is available.
        private void ShowStockMarks(Dictionary<int, StockSheetService.StoneStock> stock, Dictionary<int, int> counts)
        {
            bool Short(int id) => stock.TryGetValue(id, out var s) && counts.TryGetValue(id, out int n) && n > s.Capacity;
            foreach (var c in MosaicData.arRGBAll) c.stokYetersiz = Short(c.ID);
            foreach (var item in CatalogColors)
            {
                item.StockShort = Short(item.ID);
                if (stock.TryGetValue(item.ID, out var s))
                {
                    counts.TryGetValue(item.ID, out int used);
                    item.StockKg = s.OnHandKg;
                    item.RemainingKg = s.AvailableKg - used * StockSheetService.StoneWeightKg;
                }
            }
        }

        public async Task ClearStockOneAsync()
        {
            if (!TryGetStockConfig(true, out var config)) return;
            string projectName = StockProjectName();
            if (projectName.Length == 0)
            {
                Alert(Loc.Get("StockTitle"), Loc.Get("StockNoProject"));
                return;
            }
            if (!await Confirm(Loc.Get("StockClearTitle"), Loc.Fmt("StockClearOneConfirm", projectName))) return;
            await RunStockAction(() => StockSheetService.ClearOneAsync(config.ScriptUrl, config.SheetId, projectName),
                Loc.Fmt("StockClearedOne", projectName));
        }

        public async Task ClearStockAllAsync()
        {
            if (!TryGetStockConfig(true, out var config)) return;
            if (!await Confirm(Loc.Get("StockClearAllTitle"), Loc.Get("StockClearAllConfirm"))) return;
            await RunStockAction(() => StockSheetService.ClearAllAsync(config.ScriptUrl, config.SheetId),
                Loc.Get("StockClearedAll"));
        }

        public async Task AddStockAsync()
        {
            if (!TryGetStockConfig(true, out var config)) return;
            if (!await Confirm(Loc.Get("StockAddTitle"), Loc.Get("StockAddConfirm"))) return;
            await RunStockAction(() => StockSheetService.AddStockAsync(config.ScriptUrl, config.SheetId),
                Loc.Get("StockAdded"));
        }

        // Applies a stock-driven exclusion (true = exclude, false = include, null = leave as is) to the catalog.
        // The same change goes into the remembered Optimum base selection, so stock filters narrow the full pool
        // instead of the "used stones only" view shown after a Mos.
        private void ApplyStockSelection(Func<int, bool?> leaveOutForId)
        {
            var before = CaptureCatalogSelection();
            bool untouched = _optimumAutoSelection != null && before.AsSpan().SequenceEqual(_optimumAutoSelection);

            var current = (bool[])before.Clone();
            for (int i = 0; i < MosaicData.arRGBAll.Count; i++)
            {
                var decision = leaveOutForId(MosaicData.arRGBAll[i].ID);
                if (decision == null) continue;
                current[i] = decision.Value;
                if (_optimumUserSelection != null && i < _optimumUserSelection.Length)
                    _optimumUserSelection[i] = decision.Value;
            }
            ApplyCatalogSelection(current);
            ColorCatalogService.SetActiveColors();
            if (untouched) _optimumAutoSelection = current;
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
            set { _displayBitmap = value; OnPropertyChanged(); OnPropertyChanged(nameof(NavBitmap)); }
        }

        // What MosaicView draws: a snapshot of the current mosaic (stone colours, variants, textures).
        private MosaicRenderSource? _renderSource;
        public MosaicRenderSource? RenderSource
        {
            get => _renderSource;
            private set { _renderSource = value; OnPropertyChanged(); }
        }

        // One pixel per stone: the zoomed-out view, the placeholder while tiles render, and the navigator.
        private Bitmap? _overviewBitmap;
        public Bitmap? OverviewBitmap
        {
            get => _overviewBitmap;
            private set { _overviewBitmap = value; OnPropertyChanged(); OnPropertyChanged(nameof(NavBitmap)); }
        }

        public Bitmap? NavBitmap => _mosaicDone && _overviewBitmap != null ? _overviewBitmap : _displayBitmap;

        // A single stone changed (pixel edit, variant choice, undo/redo); MainWindow forwards it to MosaicView.
        public event Action<int, int>? StoneInvalidated;

        private void RefreshMosaicView()
        {
            if (!_mosaicDone) return;
            var src = StoneTextureService.CreateRenderSource();
            RenderSource = src;
            using var overview = src.RenderOverview();
            OverviewBitmap = ImageService.ToAvaloniaBitmap(overview);
        }

        private void RefreshOverview()
        {
            if (_renderSource == null) return;
            using var overview = _renderSource.RenderOverview();
            OverviewBitmap = ImageService.ToAvaloniaBitmap(overview);
        }

        private void InvalidateStone(int row, int col)
        {
            RefreshOverview();
            StoneInvalidated?.Invoke(row, col);
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
            set { _gridColor = value; OnPropertyChanged(); }
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
            set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRunMosaic)); OnPropertyChanged(nameof(CanExport)); OnPropertyChanged(nameof(IsBusy)); }
        }

        public bool IsExporting
        {
            get => _isExporting;
            set { _isExporting = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRunMosaic)); OnPropertyChanged(nameof(CanExport)); OnPropertyChanged(nameof(IsBusy)); }
        }

        // Any work in progress (Mos, stock fit, stone-texture rebuild, stock sheet action, export): drives the
        // wave animation in the status bar.
        public bool IsBusy => _isProcessing || _isStockBusy || _isExporting;

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
            set
            {
                _mosaicDone = value;
                OnPropertyChanged(); OnPropertyChanged(nameof(CanExport)); OnPropertyChanged(nameof(OptimalAvailable));
                OnPropertyChanged(nameof(NavBitmap));
            }
        }

        private bool _useOptimal;        // off at start-up; Mos uses the classic algorithm until it is ticked
        private bool _lastRunOptimal;
        private int _optimalK;
        private int _optimalKMax = 1;
        private int _optimalKSuggested;
        private string _optimalInfo = "";
        private bool _suppressOptimalApply;
        private System.Threading.CancellationTokenSource? _optimalApplyCts;
        // Catalog selection the user had before the last Optimum run, and the "used stones only" selection
        // the app applied afterwards. If the user has not touched the checkboxes since, the next Optimum run
        // starts again from the user's selection instead of the narrowed one.
        private bool[]? _optimumUserSelection;
        private bool[]? _optimumAutoSelection;

        private bool[] CaptureCatalogSelection()
        {
            var sel = new bool[MosaicData.arRGBAll.Count];
            for (int i = 0; i < sel.Length; i++) sel[i] = MosaicData.arRGBAll[i].boolLeaveOut;
            return sel;
        }

        private void ApplyCatalogSelection(bool[] leaveOut)
        {
            if (leaveOut.Length != MosaicData.arRGBAll.Count) return;
            for (int i = 0; i < leaveOut.Length; i++)
            {
                MosaicData.arRGBAll[i].boolLeaveOut = leaveOut[i];
                if (i < MosaicData.arcs.Count) MosaicData.arcs[i] = leaveOut[i];
            }
            foreach (var item in CatalogColors)
                if (item.Index >= 0 && item.Index < leaveOut.Length)
                    item.IsExcluded = leaveOut[item.Index];
        }

        private void RestoreOptimumUserSelectionIfUntouched()
        {
            if (_optimumUserSelection == null || _optimumAutoSelection == null) return;
            var current = CaptureCatalogSelection();
            if (current.AsSpan().SequenceEqual(_optimumAutoSelection))
                ApplyCatalogSelection(_optimumUserSelection);
        }

        public bool UseOptimal
        {
            get => _useOptimal;
            set { _useOptimal = value; OnPropertyChanged(); }
        }

        // ===== Stock-aware Optimum ("Stoğa göre"): runs after Stok Kontrol =====

        private bool _useStockAware;     // off at start-up
        // Stock read by the last Stok Kontrol fix; the stone-count slider reuses it. Cleared by a new Mos,
        // a new image or an opened project.
        private Dictionary<int, StockSheetService.StoneStock>? _stockOnHand;
        private string _stockAwareReport = "";
        // True once Mos (Optimum or classic) has produced the mosaic on screen in this session; an opened
        // project or a newly loaded image clears it.
        private bool _mosaicMadeThisSession;

        public bool UseStockAware
        {
            get => _useStockAware;
            set { _useStockAware = value; OnPropertyChanged(); OnPropertyChanged(nameof(StockAwareTip)); }
        }

        public string StockAwareTip => _stockAwareReport.Length == 0
            ? Loc.Get("TipStockAware")
            : Loc.Get("TipStockAware") + "\n\n" + _stockAwareReport;

        private void SetStockAwareReport(string text)
        {
            _stockAwareReport = text;
            OnPropertyChanged(nameof(StockAwareTip));
        }

        // The minimum-usage rule (dropping stones used only a few times) is switched off: MinUsage stays 0.
        private static StockAwareOptions StockOptions() => new();

        // Runs on the worker thread.
        private static SKBitmap ApplyOptimalKFor(int k, Dictionary<int, StockSheetService.StoneStock>? stock) =>
            stock == null
                ? MosaicEngine.ApplyOptimalK(k)
                : MosaicEngine.ApplyOptimalKWithStock(k,
                    id => stock.TryGetValue(id, out var s) ? s.Capacity : null,
                    id => stock.TryGetValue(id, out var s) ? s.Name : null,
                    StockOptions());

        // Runs on the worker thread: fixes a classic Mos result in place.
        private static bool FixClassicMosaicToStock(Dictionary<int, StockSheetService.StoneStock> stock) =>
            MosaicEngine.FixCurrentMosaicToStock(
                id => stock.TryGetValue(id, out var s) ? s.Capacity : null,
                id => stock.TryGetValue(id, out var s) ? s.Name : null,
                StockOptions());

        // After a stock-aware run: kg values and red dots in the catalog, a short status note and the full report.
        // About how many characters fit in the status bar's centre at its font size.
        private const int StatusBarFitChars = 110;

        // windowIfLong: when the report does not fit the status bar, also open it in a window (Mos and Stok
        // Kontrol); the stone slider only points to the tooltip so moving it does not keep opening windows.
        private void ShowStockAwareResult(Dictionary<int, StockSheetService.StoneStock> stock, bool windowIfLong = false)
        {
            var res = MosaicEngine.LastStockResult;
            if (res == null) return;
            string Label(int id) => stock.TryGetValue(id, out var s)
                ? string.Join(" ", new[] { $"#{id}", s.Code, s.Name.Trim() }.Where(x => x.Length > 0))
                : $"#{id}";

            foreach (var c in MosaicData.arRGBAll) c.stokYetersiz = res.ShortIds.Contains(c.ID);
            foreach (var item in CatalogColors)
            {
                item.StockShort = res.ShortIds.Contains(item.ID);
                if (stock.TryGetValue(item.ID, out var s))
                {
                    res.CountAfter.TryGetValue(item.ID, out int used);
                    item.StockKg = s.OnHandKg;
                    // What stays after the other mosaics and this one (the sheet's "Tahmini Kalan").
                    item.RemainingKg = s.AvailableKg - used * StockSheetService.StoneWeightKg;
                }
            }

            var sb = new System.Text.StringBuilder();
            string summary;
            if (!res.Changed && res.ShortIds.Count == 0)
            {
                summary = Loc.Get("StockAwareOk");
                sb.Append(summary);
            }
            else
            {
                summary = Loc.Fmt("StockAwareChanged", res.Moves.Select(m => m.FromId).Distinct().Count(), res.MovedPixels);
                sb.AppendLine(summary);
                foreach (var m in res.Moves)
                    sb.AppendLine(Loc.Fmt("StockAwareMove", Label(m.FromId), Label(m.ToId), m.Count,
                        (m.Count * StockSheetService.StoneWeightKg).ToString("0.00")));
                if (res.AddedIds.Count > 0)
                    sb.AppendLine(Loc.Fmt("StockAwareAdded", string.Join(", ", res.AddedIds.Select(Label))));
                if (res.SmallRemovedIds.Count > 0)
                    sb.AppendLine(Loc.Fmt("StockAwareSmall", res.MinUsage,
                        string.Join(", ", res.SmallRemovedIds.Select(id =>
                            $"{Label(id)} ({(res.CountBefore.TryGetValue(id, out int n) ? n : 0)})"))));
                if (res.Level > 0)
                    sb.AppendLine(Loc.Get("StockAwareLevel" + Math.Min(res.Level, 4)));
                if (res.ShortIds.Count > 0)
                {
                    string shortList = string.Join(", ", res.ShortIds.Select(Label));
                    sb.AppendLine(Loc.Fmt("StockAwareShort", shortList));
                    summary += " · " + Loc.Fmt("StockAwareShort", shortList);
                }
            }
            if (res.UnknownIds.Count > 0)
            {
                // Without a sheet row these stones could not be checked against stock.
                string unknown = Loc.Fmt("StockAwareUnknown", string.Join(", ", res.UnknownIds.Select(Label)));
                sb.AppendLine().Append(unknown);
                summary += " · " + unknown;
            }
            string report = sb.ToString().TrimEnd();
            SetStockAwareReport(report);
            string line = StatusText + " · " + summary;
            if (line.Length <= StatusBarFitChars)
            {
                StatusText = line;
                return;
            }

            // Too long for the status bar (a large mosaic can move many stones and leave long stone lists):
            // only the counts stay there, the full report goes to a window.
            string brief = res.Changed
                ? Loc.Fmt("StockAwareChanged", res.Moves.Select(m => m.FromId).Distinct().Count(), res.MovedPixels)
                : Loc.Get("StockAwareOk");
            if (res.ShortIds.Count > 0) brief += " · " + Loc.Fmt("StockAwareShortCount", res.ShortIds.Count);
            if (res.UnknownIds.Count > 0) brief += " · " + Loc.Fmt("StockAwareUnknownCount", res.UnknownIds.Count);
            StatusText = StatusText + " · " + brief + " · " +
                         Loc.Get(windowIfLong ? "StockAwareSeeWindow" : "StockAwareSeeTooltip");
            if (windowIfLong)
                Alert(Loc.Get("StockAwareTitle"), report);
        }

        public bool OptimalAvailable => MosaicDone && _lastRunOptimal;

        public int OptimalKMax
        {
            get => _optimalKMax;
            private set { _optimalKMax = value; OnPropertyChanged(); }
        }

        public int OptimalKSuggested
        {
            get => _optimalKSuggested;
            private set { _optimalKSuggested = value; OnPropertyChanged(); }
        }

        public string OptimalInfo
        {
            get => _optimalInfo;
            private set { _optimalInfo = value; OnPropertyChanged(); }
        }

        public int OptimalK
        {
            get => _optimalK;
            set
            {
                if (_optimalK == value) return;
                _optimalK = value;
                OnPropertyChanged();
                UpdateOptimalInfo();
                if (!_suppressOptimalApply) ScheduleOptimalApply();
            }
        }

        private void UpdateOptimalInfo()
        {
            var res = MosaicEngine.LastOptimalResult;
            if (res == null || _optimalK < 1 || _optimalK > res.CandidateCount) { OptimalInfo = ""; return; }
            OptimalInfo = Loc.Fmt("OptimumInfoFmt", res.KOptimal);
        }

        // Debounced so dragging the slider rebuilds the mosaic only once it settles.
        private async void ScheduleOptimalApply()
        {
            _optimalApplyCts?.Cancel();
            var cts = new System.Threading.CancellationTokenSource();
            _optimalApplyCts = cts;
            try { await Task.Delay(350, cts.Token); }
            catch (TaskCanceledException) { return; }
            if (cts.IsCancellationRequested || !OptimalAvailable || IsProcessing) return;
            await ApplyOptimalKAsync(_optimalK);
        }

        private async Task ApplyOptimalKAsync(int k)
        {
            int version = _contentVersion;
            IsProcessing = true;
            var sw = Stopwatch.StartNew();
            try
            {
                SKBitmap? result = null;
                var oldExport = MosaicData.exportBitmap;
                var stock = UseStockAware ? _stockOnHand : null;
                await Task.Run(() =>
                {
                    result = ApplyOptimalKFor(k, stock);
                });
                sw.Stop();
                if (version != _contentVersion) return; // another image or Mos took over
                FinishMosaic(result, sw.Elapsed);
                DisposeIfReplaced(oldExport, MosaicData.exportBitmap);
                if (stock != null)
                {
                    ShowStockAwareResult(stock);
                    // The sheet column still holds the counts from the last Stok Kontrol.
                    StatusText += " · " + Loc.Get("StockAwareRecheck");
                }
            }
            catch (Exception ex)
            {
                StatusText = Loc.Fmt("StatusError", ex.Message);
                Alert(Loc.Get("AlertErrorTitle"), Loc.Fmt("AlertErrorBody", ex.Message));
            }
            finally
            {
                IsProcessing = false;
            }
        }

        // The engine replaces exportBitmap on its worker thread; the old one is freed here, on the UI thread,
        // once nothing on screen can still be drawing from it.
        private static void DisposeIfReplaced(SKBitmap? old, SKBitmap? current)
        {
            if (old != null && !ReferenceEquals(old, current)) old.Dispose();
        }

        private void FinishMosaic(SKBitmap? result, TimeSpan elapsed)
        {
            ElapsedTime = elapsed.ToString(@"m\:ss\.ff");

            if (result != null)
            {
                // The stone image is no longer built as one bitmap: MosaicView draws the visible part from tiles.
                // Its virtual size stays C·N × R·N so zoom, fit, navigator and clicks keep their meaning.
                _bitmapPixelWidth = result.Width * _stonePixelSize;
                _bitmapPixelHeight = result.Height * _stonePixelSize;
            }

            if (result == null) return;

            // The mosaic was rebuilt (new Mos or new Optimum stone count): earlier stone-variant undo entries
            // point into the old mosaic, and the engine has already cleared the pixel edits.
            _stoneUndoStack.Clear();
            _stoneRedoStack.Clear();
            EditedPixelCount = PixelEditService.EditedPixels.Count;

            OnPropertyChanged(nameof(BitmapPixelWidth));
            OnPropertyChanged(nameof(BitmapPixelHeight));
            OnPropertyChanged(nameof(StoneColumns));
            OnPropertyChanged(nameof(StoneRows));
            MosaicDone = true;
            FilterCatalogByUsedColors();
            if (_lastRunOptimal)
                _optimumAutoSelection = CaptureCatalogSelection();
            RefreshMosaicView();
            int totalColors = MosaicData.arRGBAll.Count;
            var uniqueCodes = new HashSet<string>();
            foreach (var arList in MosaicData.arMB)
                foreach (var c in arList)
                    if (!string.IsNullOrEmpty(c.codeName))
                        uniqueCodes.Add(c.codeName);
            int usedColors = uniqueCodes.Count;
            UsedColorInfo = Loc.Fmt("StatusUsedColors", totalColors, usedColors);
            StatusText = Loc.Fmt("StatusCompleted", usedColors, elapsed.TotalSeconds.ToString("F1"));
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
        public string ZoomInfo => $"N={(_zoomLevel < 0.1 ? _zoomLevel.ToString("0.###") : _zoomLevel.ToString("F1"))}  {_bitmapPixelWidth * _zoomLevel:F0}x{_bitmapPixelHeight * _zoomLevel:F0}";


        public double NavViewLeft { get => _navViewLeft; set { _navViewLeft = value; OnPropertyChanged(); } }
        public double NavViewTop { get => _navViewTop; set { _navViewTop = value; OnPropertyChanged(); } }
        public double NavViewWidth { get => _navViewWidth; set { _navViewWidth = value; OnPropertyChanged(); } }
        public double NavViewHeight { get => _navViewHeight; set { _navViewHeight = value; OnPropertyChanged(); } }

        // Margin of the image panel inside the canvas ScrollViewer (MainWindow.axaml).
        private const double CanvasMargin = 4;

        // Where the thumbnail sits in the navigator: Stretch="Uniform" centres it in the navSize × navSize area.
        private (double x0, double y0, double w, double h) NavThumb(double navSize)
        {
            double imgW = ImageDisplayWidth, imgH = ImageDisplayHeight;
            double scale = Math.Min(navSize / imgW, navSize / imgH);
            double w = imgW * scale, h = imgH * scale;
            return ((navSize - w) / 2, (navSize - h) / 2, w, h);
        }

        // navSize: the navigator's inner size (inside its border), the area the thumbnail is centred in.
        public void UpdateNavigator(double viewportW, double viewportH, double offsetX, double offsetY, double navSize)
        {
            double imgW = ImageDisplayWidth;
            double imgH = ImageDisplayHeight;
            if (imgW <= 0 || imgH <= 0 || navSize <= 0) return;

            var t = NavThumb(navSize);
            // The part of the image that is on screen, in image pixels (the panel has a small margin).
            double left = Math.Clamp(offsetX - CanvasMargin, 0, imgW);
            double top = Math.Clamp(offsetY - CanvasMargin, 0, imgH);
            double right = Math.Clamp(offsetX - CanvasMargin + viewportW, 0, imgW);
            double bottom = Math.Clamp(offsetY - CanvasMargin + viewportH, 0, imgH);

            NavViewLeft = t.x0 + left / imgW * t.w;
            NavViewTop = t.y0 + top / imgH * t.h;
            NavViewWidth = (right - left) / imgW * t.w;
            NavViewHeight = (bottom - top) / imgH * t.h;
        }

        // Scroll offset that centres the view on the navigator point (x, y), or null outside the thumbnail.
        public (double x, double y)? NavigatorTarget(double x, double y, double navSize, double viewportW, double viewportH)
        {
            double imgW = ImageDisplayWidth;
            double imgH = ImageDisplayHeight;
            if (imgW <= 0 || imgH <= 0 || navSize <= 0) return null;

            var t = NavThumb(navSize);
            double rx = Math.Clamp((x - t.x0) / t.w, 0, 1);
            double ry = Math.Clamp((y - t.y0) / t.h, 0, 1);
            return (Math.Max(0, rx * imgW + CanvasMargin - viewportW / 2),
                    Math.Max(0, ry * imgH + CanvasMargin - viewportH / 2));
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
                if (ColorCatalogService.SkippedLines.Count > 0)
                    StatusText = Loc.Fmt("StatusCatalogSkipped", string.Join(", ", ColorCatalogService.SkippedLines));
            }
            catch (Exception ex)
            {
                StatusText = Loc.Fmt("StatusError", ex.Message);
            }

            Loc.Instance.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Loc.Lang)) UpdateOptimalInfo();
            };
        }

        public void LoadImage(string path)
        {
            StartNewContent();
            ProjectService.CurrentPictureFileName = path;
            ProjectService.CurrentFileName = "";
            ProjectService.ForgetWpfState();
            _stockOnHand = null;
            _mosaicMadeThisSession = false;
            SetStockAwareReport("");
            MosaicEngine.Reset();
            MosaicDone = false;
            RenderSource = null;
            OverviewBitmap = null;

            // Clear old mosaic state
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
                OnPropertyChanged(nameof(DocumentTitle));
                _loadedStock = null;
                _ = RefreshStockAsync();
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
            int version = StartNewContent();

            IsProcessing = true;
            Progress = 0;
            MosaicDone = false;
            StatusText = Loc.Get("StatusStarting");
            ElapsedTime = "";
            ProjectService.ForgetWpfState();

            foreach (var c in MosaicData.arRGBAll) c.stokYetersiz = false;
            // A new mosaic drops the red dots and the "remaining" kg, which belonged to the old stone counts.
            // The kg on hand (loaded at start-up or by Stok Çek) does not depend on the mosaic and stays.
            foreach (var item in CatalogColors) { item.StockShort = false; item.RemainingKg = null; }

            if (UseOptimal)
            {
                RestoreOptimumUserSelectionIfUntouched();
                _optimumUserSelection = CaptureCatalogSelection();
                _optimumAutoSelection = null;
            }
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
            int steps = (int)Math.Ceiling(256.0 / RgbIncrement);
            int paletteSize = steps * steps * steps;
            TargetColors = Math.Clamp(Math.Max(2, ew * 10), 2, Math.Max(2, totalPixels));
            if (TargetColors >= paletteSize)
                TargetColors = paletteSize - 1;
            StatusText = Loc.Fmt("StatusCalculated", TargetColors);

            var sw = Stopwatch.StartNew();

            try
            {
                SKBitmap? result = null;
                bool optimal = UseOptimal;
                _lastRunOptimal = false;
                var oldExport = MosaicData.exportBitmap;
                // "Stoğa göre": the mosaic is built and then fitted to the stock read from the sheet (loaded with
                // the image/project; read here if it is not there yet). Without stock the Mos is plain.
                _stockOnHand = null;
                SetStockAwareReport("");
                Dictionary<int, StockSheetService.StoneStock>? stock = null;
                // Without a stock sheet set up there is nothing to fit to, and no note is shown.
                bool stockConfigured = !string.IsNullOrEmpty(StockSheetService.LoadConfig().SheetId);
                if (UseStockAware && stockConfigured)
                {
                    if (_loadedStock == null) await RefreshStockAsync();
                    stock = _loadedStock;
                    if (stock == null) SetStockAwareReport(Loc.Get("StockAwareNoStock"));
                }
                bool stockFixOk = true;
                await Task.Run(() =>
                {
                    if (optimal)
                    {
                        result = MosaicEngine.RunOptimal(SelectedInterpolation,
                            progress => Dispatcher.UIThread.Post(() => Progress = progress),
                            prepareTextures: stock == null);
                        if (stock != null)
                            result = ApplyOptimalKFor(MosaicEngine.LastOptimalResult!.KOptimal, stock);
                    }
                    else
                    {
                        result = MosaicEngine.RunM3(
                            TargetColors,
                            RgbIncrement,
                            UseLab,
                            UseAverage,
                            SelectedInterpolation,
                            progress => Dispatcher.UIThread.Post(() => Progress = progress)
                        );
                        if (stock != null)
                        {
                            stockFixOk = FixClassicMosaicToStock(stock);
                            result = MosaicData.reducedBitmap;
                        }
                    }
                });

                sw.Stop();
                if (version != _contentVersion) return; // a new image or project was opened meanwhile
                var res = MosaicEngine.LastOptimalResult;
                if (optimal && res != null)
                {
                    _lastRunOptimal = true;
                    _suppressOptimalApply = true;
                    OptimalKMax = res.CandidateCount;
                    OptimalKSuggested = res.KOptimal;
                    OptimalK = res.KOptimal;
                    _suppressOptimalApply = false;
                    UpdateOptimalInfo();
                }
                FinishMosaic(result, sw.Elapsed);
                DisposeIfReplaced(oldExport, MosaicData.exportBitmap);
                _mosaicMadeThisSession = result != null;
                if (UseStockAware && stockConfigured && stock == null && result != null)
                    StatusText += " · " + Loc.Get("StockAwareNoStock");
                if (stock != null && result != null)
                {
                    if (stockFixOk)
                    {
                        _stockOnHand = stock; // the stone-count slider keeps to the same stock
                        ShowStockAwareResult(stock, windowIfLong: true);
                    }
                    else
                    {
                        SetStockAwareReport(Loc.Get("StockAwareCannotFix"));
                        Alert(Loc.Get("StockAwareTitle"), Loc.Get("StockAwareCannotFix"));
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                StatusText = Loc.Get("AlertMemoryBody");
                Alert(Loc.Get("AlertMemoryTitle"), Loc.Get("AlertMemoryBody"));
            }
            catch (Exception ex)
            {
                StatusText = Loc.Fmt("StatusError", ex.Message);
                Alert(Loc.Get("AlertErrorTitle"), Loc.Fmt("AlertErrorBody", ex.Message));
            }
            finally
            {
                IsProcessing = false;
                Progress = 100;
            }
        }

        // Returns false (after telling the user) when the file could not be written, e.g. disk full or no access.
        public bool SaveProject(string filePath)
        {
            try
            {
                ProjectService.Save(filePath, WidthCm, ZoomLevel,
                    ShowGrid, false,
                    _gridColor.R, _gridColor.G, _gridColor.B,
                    (int)SelectedInterpolation);
            }
            catch (Exception ex)
            {
                ReportSaveFailed(ex);
                return false;
            }
            StatusText = Loc.Fmt("StatusSaved", System.IO.Path.GetFileName(filePath));
            OnPropertyChanged(nameof(DocumentTitle));
            return true;
        }

        // Shown in the title bar: the project file once saved or opened, otherwise the loaded image.
        public string DocumentTitle
        {
            get
            {
                string path = !string.IsNullOrEmpty(ProjectService.CurrentFileName)
                    ? ProjectService.CurrentFileName
                    : ProjectService.CurrentPictureFileName;
                return string.IsNullOrEmpty(path) ? "" : System.IO.Path.GetFileName(path);
            }
        }

        public void ReportSaveFailed(Exception ex)
        {
            StatusText = Loc.Fmt("StatusError", ex.Message);
            Alert(Loc.Get("AlertProjectTitle"), Loc.Fmt("AlertSaveFailed", ex.Message));
        }

        public async void OpenProject(string filePath)
        {
            int version = StartNewContent();
            var data = ProjectService.Open(filePath);
            if (data == null)
            {
                StatusText = Loc.Get("StatusOpenFailed");
                Alert(Loc.Get("AlertProjectTitle"), Loc.Get("AlertProjectOpenFailed"));
                return;
            }
            OnPropertyChanged(nameof(DocumentTitle));

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
            _bitmapPixelWidth = C * data.N;
            _bitmapPixelHeight = R * data.N;
            OnPropertyChanged(nameof(BitmapPixelWidth));
            OnPropertyChanged(nameof(BitmapPixelHeight));
            OnPropertyChanged(nameof(StoneColumns));
            OnPropertyChanged(nameof(StoneRows));

            // An opened project has no Optimum analysis; hide the stone slider left from an earlier Optimum Mos.
            _lastRunOptimal = false;
            _stockOnHand = null;
            _mosaicMadeThisSession = false;
            SetStockAwareReport("");
            MosaicDone = true;
            ImageLoaded = true;
            _stoneUndoStack.Clear();
            _stoneRedoStack.Clear();
            EditedPixelCount = PixelEditService.EditedPixels.Count;
            // ProjectService.Open left pixel-edit mode; keep the toolbar in step.
            IsPixelEditActive = PixelEditService.IsPixelEditActive;
            IsSourcePixelMode = PixelEditService.IsSourcePixelMode;
            IsTargetPixelMode = PixelEditService.IsTargetPixelMode;

            RefreshCatalogList();
            FilterCatalogByUsedColors();
            UpdateDimensions();

            // Show the stones' colours at once (no textures from an earlier mosaic); MosaicView adds the
            // textures as soon as they are loaded.
            StoneTextureService.Reset();
            RefreshMosaicView();
            StatusText = Loc.Get("StatusGeneratingRs");
            IsProcessing = true;
            try
            {
                await Task.Run(StoneTextureService.LoadTextures);
            }
            catch (Exception)
            {
                // No stone images: the view keeps showing stone colours.
            }
            IsProcessing = false;
            if (version != _contentVersion) return; // another image or project was opened meanwhile
            RefreshMosaicView();

            FitToWindow(_lastViewportWidth, _lastViewportHeight);
            StatusText = Loc.Fmt("StatusOpened", System.IO.Path.GetFileName(filePath));
            _loadedStock = null;
            _ = RefreshStockAsync();
        }

        // Export quality choices shown to the user as image sizes; internally the pixels per stone (N).
        public static readonly int[] ExportQualities = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

        // Estimated file sizes per (pixels per stone, JPEG?), kept until the mosaic or the grid changes.
        private readonly Dictionary<(int n, bool jpeg), long> _exportEstimates = new();
        private (int version, bool grid, Color color) _exportEstimatesKey;
        private bool _estimatingExport;

        // Raised (on the UI thread) whenever a new estimate is ready, so the menu texts can be refreshed.
        public event Action? ExportEstimatesChanged;

        // Quick export: JPEG when possible and affordable at this quality, otherwise PNG.
        public string QuickExportExtension(int n)
        {
            var src = _renderSource;
            if (src == null) return "jpeg";
            return MosaicExporter.QuickExportUsesJpeg(MosaicExporter.ImageSize(src, n)) ? "jpeg" : "png";
        }

        // The text of one quality choice: image size in pixels and the estimated file size(s); no "N".
        public string ExportChoiceLabel(int n, bool saveAs)
        {
            var src = _renderSource;
            if (src == null) return "";
            var size = MosaicExporter.ImageSize(src, n);
            string dims = Loc.Fmt("ExportDimsPx", size.Width.ToString("N0"), size.Height.ToString("N0"));
            string current = n == _stonePixelSize ? " " + Loc.Get("ExportCurrentQuality") : "";
            string Est(bool jpeg) => _exportEstimates.TryGetValue((n, jpeg), out long b) ? FormatBytes(b) : Loc.Get("ExportEstimating");
            if (!saveAs)
            {
                bool jpeg = MosaicExporter.QuickExportUsesJpeg(size);
                return Loc.Fmt("ExportChoiceQuick", dims, jpeg ? "JPEG" : "PNG", Est(jpeg), current);
            }
            return MosaicExporter.JpegPossible(size)
                ? Loc.Fmt("ExportChoiceAs", dims, Est(true), Est(false), current)
                : Loc.Fmt("ExportChoiceAsPngOnly", dims, Est(false), current);
        }

        // Fills in the missing estimates, smallest quality first, telling the menu after each one.
        public async Task RefreshExportEstimatesAsync()
        {
            var src = _renderSource;
            if (!MosaicDone || src == null) return;
            var key = (src.Version, _showGrid, _gridColor);
            if (key != _exportEstimatesKey)
            {
                _exportEstimates.Clear();
                _exportEstimatesKey = key;
            }
            if (_estimatingExport)
            {
                // A run is going on; it starts over once more when it ends (the mosaic or grid may have changed).
                _estimateExportAgain = true;
                return;
            }
            _estimatingExport = true;
            bool changed = false;
            try
            {
                bool grid = _showGrid;
                var gc = new SKColor(_gridColor.R, _gridColor.G, _gridColor.B);
                foreach (int n in ExportQualities)
                {
                    foreach (bool jpeg in new[] { true, false })
                    {
                        if (_exportEstimates.ContainsKey((n, jpeg))) continue;
                        if (jpeg && !MosaicExporter.JpegPossible(MosaicExporter.ImageSize(src, n))) continue;
                        int gw = grid ? Math.Max(1, n / 11) : 0;
                        long bytes = await Task.Run(() => MosaicExporter.EstimateBytes(src, n, grid, gw, gc, jpeg));
                        if (!ReferenceEquals(src, _renderSource) || key != _exportEstimatesKey) { changed = true; return; }
                        _exportEstimates[(n, jpeg)] = bytes;
                        ExportEstimatesChanged?.Invoke();
                    }
                }
            }
            catch (Exception)
            {
                // An estimate is only a hint; the menu keeps showing "estimating".
            }
            finally
            {
                _estimatingExport = false;
                if (changed || _estimateExportAgain)
                {
                    _estimateExportAgain = false;
                    _ = RefreshExportEstimatesAsync();
                }
            }
        }

        private bool _estimateExportAgain;

        private static string FormatBytes(long bytes) =>
            bytes >= 1L << 30 ? Loc.Fmt("SizeGB", (bytes / (double)(1L << 30)).ToString("0.0"))
                              : Loc.Fmt("SizeMB", Math.Max(1, (int)Math.Round(bytes / (double)(1 << 20))));

        // A screenshot of the image area: premultiplied BGRA from the screen, put on the canvas colour and saved as PNG.
        public async Task SaveScreenshotAsync(byte[] bgra, int width, int height, byte bgR, byte bgG, byte bgB, string path)
        {
            string name = System.IO.Path.GetFileName(path);
            try
            {
                await Task.Run(() =>
                {
                    using var bmp = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
                    var dst = bmp.GetPixelSpan();
                    for (int i = 0; i < width * height; i++)
                    {
                        int o = i * 4;
                        int a = bgra[o + 3], ia = 255 - a;
                        dst[o] = (byte)(bgra[o + 2] + bgR * ia / 255);
                        dst[o + 1] = (byte)(bgra[o + 1] + bgG * ia / 255);
                        dst[o + 2] = (byte)(bgra[o] + bgB * ia / 255);
                        dst[o + 3] = 255;
                    }
                    ImageService.ExportImage(bmp, path, SKEncodedImageFormat.Png);
                });
                StatusText = Loc.Fmt("StatusScreenshotSaved", name);
            }
            catch (Exception ex)
            {
                StatusText = Loc.Fmt("StatusError", ex.Message);
                Alert(Loc.Get("AlertExportTitle"), Loc.Fmt("AlertErrorBody", ex.Message));
            }
        }

        // n: the chosen image quality (pixels per stone); by default the current Detail setting.
        public async Task ExportImageAsync(string path, int? quality = null)
        {
            if (IsExporting) return;

            if (!MosaicDone || _renderSource == null)
            {
                Alert(Loc.Get("AlertExportTitle"), Loc.Get("AlertExportNoMosaic"));
                return;
            }

            bool jpeg = path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
            int n = quality ?? _stonePixelSize;
            var size = MosaicExporter.ImageSize(_renderSource, n);
            if (jpeg && !MosaicExporter.JpegPossible(size))
            {
                Alert(Loc.Get("AlertExportTitle"),
                    Loc.Fmt("ExportJpegTooLarge", size.Width.ToString("N0"), size.Height.ToString("N0"),
                        MosaicExporter.JpegMaxSide.ToString("N0")));
                return;
            }

            // A JPEG too big for one bitmap is built in memory (≈ width × height × 4 bytes): ask when that is more
            // than half of what the computer has free.
            if (jpeg && size.Pixels > ImageService.MaxBitmapPixels)
            {
                long need = MosaicExporter.JpegMemoryBytes(size);
                long free = MosaicExporter.FreeMemoryBytes();
                if (need > free / 2 &&
                    !await Confirm(Loc.Get("AlertExportTitle"),
                        Loc.Fmt("ExportJpegMemoryConfirm", FormatBytes(need), FormatBytes(free))))
                    return;
            }

            // The whole stone image at the chosen quality, with no size limit; a copy of the stones, so pixel
            // edits made during the export cannot reach the file.
            var src = _renderSource.WithStoneSnapshot();
            bool grid = _showGrid;
            int gw = grid ? Math.Max(1, n / 11) : 0;   // as on screen at full detail
            var gc = new SKColor(_gridColor.R, _gridColor.G, _gridColor.B);

            IsExporting = true;
            string name = System.IO.Path.GetFileName(path);
            StatusText = Loc.Fmt("StatusExporting", name);
            int lastPercent = -1;
            void Report(double fraction)
            {
                int pct = (int)(fraction * 100);
                if (pct == lastPercent) return;
                lastPercent = pct;
                Dispatcher.UIThread.Post(() =>
                {
                    if (IsExporting) StatusText = Loc.Fmt("StatusExportingPct", name, pct);
                });
            }
            try
            {
                await Task.Run(() => MosaicExporter.Export(src, path, jpeg, n, grid, gw, gc,
                    size.Pixels > ImageService.MaxBitmapPixels ? Report : null));
                StatusText = Loc.Fmt("StatusSaved", name);
            }
            catch (Exception ex)
            {
                StatusText = Loc.Fmt("StatusError", ex.Message);
                Alert(Loc.Get("AlertExportTitle"), Loc.Fmt("AlertErrorBody", ex.Message));
            }
            finally
            {
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
            _minZoomLevel = Math.Max(0.001, fit);
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
            _minZoomLevel = Math.Max(0.001, fit);
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
                if (!mbByCode.TryGetValue(catItem.CodeName, out var c) || c.numOfPixel == 0)
                    continue;

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

                    InvalidateStone(y, x);
                }
            }
        }

        // Bumped whenever the mosaic on screen is replaced (new image, opened project, new Mos). A background job
        // that finishes under an older number belongs to a mosaic that is gone and drops its result.
        private int _contentVersion;

        // Cancels the pending stone-slider rebuild.
        private int StartNewContent()
        {
            _optimalApplyCts?.Cancel();
            return ++_contentVersion;
        }

        // N or the grid changed. Nothing is rebuilt here: MosaicView redraws from its bindings. Only the virtual
        // size of the stone image (C·N × R·N) follows N, keeping the on-screen size the same.
        private void RegenerateRS()
        {
            if (!MosaicDone) return;
            int R = MosaicData.dataM3.GetLength(0);
            int C = MosaicData.dataM3.GetLength(1);
            int oldW = _bitmapPixelWidth, oldH = _bitmapPixelHeight;
            _bitmapPixelWidth = C * _stonePixelSize;
            _bitmapPixelHeight = R * _stonePixelSize;
            if (oldW == _bitmapPixelWidth && oldH == _bitmapPixelHeight) return;
            OnPropertyChanged(nameof(BitmapPixelWidth));
            OnPropertyChanged(nameof(BitmapPixelHeight));
            AdjustZoomForBitmapChange(oldW, oldH);
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

            StoneInvalidated?.Invoke(_selectedPixelY, _selectedPixelX);
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
                StoneInvalidated?.Invoke(entry.pixelY, entry.pixelX);
                return;
            }

            if (!PixelEditService.CanUndo) return;
            string result = PixelEditService.UndoLastEdit();
            EditedPixelCount = PixelEditService.EditedPixels.Count;
            StatusText = Loc.Fmt("StatusPixelEdit", result);
            InvalidateStone(PixelEditService.LastChanged.Y, PixelEditService.LastChanged.X);
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
                StoneInvalidated?.Invoke(entry.pixelY, entry.pixelX);
                return;
            }

            if (!PixelEditService.CanRedo) return;
            string result = PixelEditService.RedoLastEdit();
            EditedPixelCount = PixelEditService.EditedPixels.Count;
            StatusText = Loc.Fmt("StatusPixelEdit", result);
            InvalidateStone(PixelEditService.LastChanged.Y, PixelEditService.LastChanged.X);
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
