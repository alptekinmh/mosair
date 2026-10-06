using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using mosair.Services;
using mosair.ViewModels;

namespace mosair;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private bool _isPanning;
    private Avalonia.Point _panStart;
    private bool _syncingScroll;
    private Avalonia.Vector _scrollStart;
    private DispatcherTimer? _mosAnimTimer;
    private int _mosAnimFrame;
    private DispatcherTimer? _exportAnimTimer;
    private int _exportAnimFrame;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        _vm.ShowAlert = async (title, message) =>
        {
            var dlg = new Controls.AlertDialog(title, message);
            await dlg.ShowDialog(this);
        };
        _vm.ShowConfirm = (title, message) =>
            new Controls.ConfirmDialog(title, message).ShowDialog<bool>(this);
        _vm.ShowStockSettings = current =>
            new Controls.StockSettingsDialog(current).ShowDialog<StockSheetService.Config?>(this);
        _vm.ShowDriveSettings = current =>
            new Controls.DriveSettingsDialog(current).ShowDialog<DriveService.Config?>(this);
        _vm.ShowDriveOpen = config =>
            new Controls.DriveOpenDialog(config).ShowDialog<DriveService.DriveFile?>(this);
        _vm.OpenUrl = async url =>
        {
            var launcher = TopLevel.GetTopLevel(this)?.Launcher;
            if (launcher != null) await launcher.LaunchUriAsync(new Uri(url));
        };
        DataContext = _vm;
        BuildExportMenus();
        // A pixel edit or variant choice redraws only the tile with that stone.
        _vm.StoneInvalidated += (row, col) => mosaicView.InvalidateStone(row, col);
        // Stock on hand from the configured sheet, once the window is up (does not block start-up).
        Opened += async (_, _) => await _vm.LoadStockOnStartupAsync();
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        KeyDown += OnKeyDown;

        var palScroll = this.FindControl<ScrollViewer>("paletteScroll");
        var assScroll = this.FindControl<ScrollViewer>("assignedScroll");
        if (palScroll != null && assScroll != null)
        {
            palScroll.ScrollChanged += (_, _) =>
            {
                if (_syncingScroll) return;
                _syncingScroll = true;
                assScroll.Offset = new Vector(assScroll.Offset.X, palScroll.Offset.Y);
                _syncingScroll = false;
            };
            assScroll.ScrollChanged += (_, _) =>
            {
                if (_syncingScroll) return;
                _syncingScroll = true;
                palScroll.Offset = new Vector(palScroll.Offset.X, assScroll.Offset.Y);
                _syncingScroll = false;
            };
        }

        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsProcessing))
            {
                if (_vm.IsProcessing) StartMosAnim();
                else StopMosAnim();
            }
            else if (e.PropertyName == nameof(MainViewModel.IsExporting))
            {
                if (_vm.IsExporting) StartExportAnim();
                else StopExportAnim();
            }
            else if (e.PropertyName == nameof(MainViewModel.GridColorShades))
            {
                RebuildGridShadeItems();
                RefreshToolsMenuChecks();
            }
            else if (e.PropertyName is nameof(MainViewModel.SelectedInterpolation)
                     or nameof(MainViewModel.GridColor))
            {
                RefreshToolsMenuChecks();
            }
        };

        BuildToolsMenu();
        SetMenuShortcutTexts();
    }

    // ===== Tools menu: mirrors the toolbar controls through the same ViewModel properties =====

    private const string CheckGeometry = "M9,16.17 L4.83,12 L3.41,13.41 L9,19 L21,7 L19.59,5.59 Z";
    private readonly System.Collections.Generic.List<(MenuItem Item, Services.InterpolationMethod Value)> _interpMenuItems = new();
    private readonly System.Collections.Generic.List<(MenuItem Item, Avalonia.Media.Color Value)> _gridColorMenuItems = new();
    private readonly System.Collections.Generic.List<MenuItem> _gridShadeMenuItems = new();
    private Separator? _gridShadeSeparator;

    private static PathIcon NewMenuCheck() => new()
    {
        Width = 12, Height = 12, IsVisible = false,
        Data = Avalonia.Media.StreamGeometry.Parse(CheckGeometry)
    };

    private static Border ColorSwatch(Avalonia.Media.Color c) => new()
    {
        Width = 40, Height = 14, CornerRadius = new CornerRadius(3),
        BorderThickness = new Thickness(1),
        BorderBrush = Avalonia.Media.Brushes.Gray,
        Background = new Avalonia.Media.SolidColorBrush(c)
    };

    private void BuildToolsMenu()
    {
        foreach (var method in MainViewModel.InterpolationMethods)
        {
            var item = new MenuItem { Header = method.ToString(), Icon = NewMenuCheck() };
            item.Click += (_, _) => _vm.SelectedInterpolation = method;
            menuInterp.Items.Add(item);
            _interpMenuItems.Add((item, method));
        }

        foreach (var color in MainViewModel.GridColorPresets)
        {
            var c = color;
            var item = new MenuItem { Header = ColorSwatch(c), Icon = NewMenuCheck() };
            item.Click += (_, _) =>
            {
                _vm.GridColor = c;
                _vm.SelectMainColor(c);
            };
            menuGridColor.Items.Add(item);
            _gridColorMenuItems.Add((item, c));
        }
        _gridShadeSeparator = new Separator();
        menuGridColor.Items.Add(_gridShadeSeparator);

        RebuildGridShadeItems();
        RefreshToolsMenuChecks();
    }

    private void RebuildGridShadeItems()
    {
        foreach (var old in _gridShadeMenuItems)
        {
            menuGridColor.Items.Remove(old);
            _gridColorMenuItems.RemoveAll(x => ReferenceEquals(x.Item, old));
        }
        _gridShadeMenuItems.Clear();

        var shades = _vm.GridColorShades ?? Array.Empty<Avalonia.Media.Color>();
        foreach (var shade in shades)
        {
            var c = shade;
            var item = new MenuItem { Header = ColorSwatch(c), Icon = NewMenuCheck() };
            item.Click += (_, _) => _vm.GridColor = c;
            menuGridColor.Items.Add(item);
            _gridShadeMenuItems.Add(item);
            _gridColorMenuItems.Add((item, c));
        }
        if (_gridShadeSeparator != null)
            _gridShadeSeparator.IsVisible = shades.Length > 0;
    }

    private void RefreshToolsMenuChecks()
    {
        foreach (var (item, value) in _interpMenuItems)
            SetMenuCheck(item, value == _vm.SelectedInterpolation);
        bool marked = false;
        foreach (var (item, value) in _gridColorMenuItems)
        {
            // Mark only the first match so a preset and an identical shade are not both ticked.
            bool on = !marked && value == _vm.GridColor;
            SetMenuCheck(item, on);
            marked |= on;
        }
    }

    private static void SetMenuCheck(MenuItem item, bool on)
    {
        if (item.Icon is Control icon) icon.IsVisible = on;
    }

    private async void OnStockSheet(object? sender, RoutedEventArgs e) => await _vm.OpenStockSheetAsync();

    private async void OnStockSettings(object? sender, RoutedEventArgs e) => await _vm.ConfigureStockAsync();

    private async void OnStockFetch(object? sender, RoutedEventArgs e) => await _vm.FetchStockAsync();

    private async void OnStockFetchMark(object? sender, RoutedEventArgs e) => await _vm.FetchStockAsync(markOnly: true);

    private async void OnStockCheck(object? sender, RoutedEventArgs e) => await _vm.CheckStockAsync();

    private async void OnStockClearOne(object? sender, RoutedEventArgs e) => await _vm.ClearStockOneAsync();

    private async void OnStockClearAll(object? sender, RoutedEventArgs e) => await _vm.ClearStockAllAsync();

    private async void OnStockAdd(object? sender, RoutedEventArgs e) => await _vm.AddStockAsync();

    private void OnToggleOptimum(object? sender, RoutedEventArgs e)
    {
        _vm.UseOptimal = !_vm.UseOptimal;
    }

    private void OnToggleStockAware(object? sender, RoutedEventArgs e)
    {
        _vm.UseStockAware = !_vm.UseStockAware;
    }

    private void OnStonesSuggested(object? sender, RoutedEventArgs e)
    {
        if (_vm.IsProcessing) return;
        _vm.OptimalK = _vm.OptimalKSuggested;
    }

    private void OnStonesMore(object? sender, RoutedEventArgs e)
    {
        if (_vm.IsProcessing) return;
        _vm.OptimalK = Math.Min(_vm.OptimalK + 1, _vm.OptimalKMax);
    }

    private void OnStonesLess(object? sender, RoutedEventArgs e)
    {
        if (_vm.IsProcessing) return;
        _vm.OptimalK = Math.Max(_vm.OptimalK - 1, 1);
    }

    // ⌘ on macOS, Ctrl elsewhere — the same modifier the user guide shows (Loc.KeyMod).
    private static readonly KeyModifiers CmdKey = Loc.IsMac ? KeyModifiers.Meta : KeyModifiers.Control;

    private void SetMenuShortcutTexts()
    {
        menuLoadImage.InputGesture = new KeyGesture(Key.I, CmdKey);
        menuOpenProject.InputGesture = new KeyGesture(Key.O, CmdKey);
        menuSave.InputGesture = new KeyGesture(Key.S, CmdKey);
        menuSaveAs.InputGesture = new KeyGesture(Key.S, CmdKey | KeyModifiers.Shift);
        menuExport.InputGesture = new KeyGesture(Key.E, CmdKey);
        menuFitToScreen.InputGesture = new KeyGesture(Key.D0, CmdKey);
        menuMosaicize.InputGesture = new KeyGesture(Key.M, CmdKey);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Esc cancels the running job (same as the status-bar cancel button).
        if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None && _vm.CanCancel)
        {
            _vm.CancelWork();
            e.Handled = true;
            return;
        }
        var mods = e.KeyModifiers;
        bool cmd = mods == CmdKey;
        bool cmdShift = mods == (CmdKey | KeyModifiers.Shift);
        // Undo/redo keep accepting either Ctrl or ⌘, as before.
        bool anyMod = mods == KeyModifiers.Control || mods == KeyModifiers.Meta;
        bool anyModShift = mods == (KeyModifiers.Control | KeyModifiers.Shift) ||
                           mods == (KeyModifiers.Meta | KeyModifiers.Shift);

        if (anyMod && e.Key == Key.Z) { _vm.UndoPixelEdit(); e.Handled = true; }
        else if ((anyMod && e.Key == Key.Y) || (anyModShift && e.Key == Key.Z)) { _vm.RedoPixelEdit(); e.Handled = true; }
        else if (cmd && e.Key == Key.I) { OnLoadImage(this, new RoutedEventArgs()); e.Handled = true; }
        else if (cmd && e.Key == Key.O) { OnOpenProject(this, new RoutedEventArgs()); e.Handled = true; }
        else if (cmd && e.Key == Key.S)
        {
            if (_vm.MosaicDone) OnSaveProject(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (cmdShift && e.Key == Key.S)
        {
            if (_vm.MosaicDone) OnSaveAsProject(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (cmd && e.Key == Key.E)
        {
            if (_vm.CanExport) OnExportImage(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (cmd && (e.Key == Key.D0 || e.Key == Key.NumPad0)) { OnResetSize(this, new RoutedEventArgs()); e.Handled = true; }
        else if (cmd && e.Key == Key.M) { OnRunMosaic(this, new RoutedEventArgs()); e.Handled = true; }
        else if (mods == KeyModifiers.None && e.Key == Key.F1) { ShowHelp(); e.Handled = true; }
    }

    private void OnShowHelp(object? sender, RoutedEventArgs e)
    {
        ShowHelp();
    }

    private void ShowHelp()
    {
        var help = new HelpWindow();
        help.ShowDialog(this);
    }

    private async void OnLoadImage(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Loc.Get("DlgSelectImage"),
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(Loc.Get("DlgImages")) { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.tiff" } },
                FilePickerFileTypes.All
            }
        });

        if (files.Count > 0)
        {
            var path = files[0].TryGetLocalPath();
            if (path != null)
                _vm.LoadImage(path);
        }
    }

    private async void OnRunMosaic(object? sender, RoutedEventArgs e)
    {
        await _vm.RunMosaicAsync();
        _vm.FitToWindow(imageScroller.Bounds.Width, imageScroller.Bounds.Height);
    }

    private static string GetExportDir()
    {
        string desktop = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
        string exportDir = System.IO.Path.Combine(desktop, "mosairEXPORT");
        System.IO.Directory.CreateDirectory(exportDir);
        return exportDir;
    }

    // Screenshot of the image area exactly as it is on screen (visible part, zoom, grid), without the
    // navigator and the scroll bars; saved as PNG in the mosairEXPORT folder.
    private async void OnScreenshot(object? sender, RoutedEventArgs e)
    {
        var viewport = imageScroller.Viewport;
        if (viewport.Width <= 0 || viewport.Height <= 0) return;
        double scale = RenderScaling;
        var whole = new PixelSize(
            Math.Max(1, (int)Math.Ceiling(imageScroller.Bounds.Width * scale)),
            Math.Max(1, (int)Math.Ceiling(imageScroller.Bounds.Height * scale)));
        int w = Math.Min(whole.Width, (int)Math.Round(viewport.Width * scale));
        int h = Math.Min(whole.Height, (int)Math.Round(viewport.Height * scale));
        if (w <= 0 || h <= 0) return;

        byte[] pixels = new byte[w * h * 4];
        using (var rtb = new Avalonia.Media.Imaging.RenderTargetBitmap(whole, new Vector(96 * scale, 96 * scale)))
        {
            rtb.Render(imageScroller);
            unsafe
            {
                fixed (byte* ptr = pixels)
                    rtb.CopyPixels(new PixelRect(0, 0, w, h), (IntPtr)ptr, pixels.Length, w * 4);
            }
        }
        // Areas around a small image are transparent in the render; give them the canvas colour.
        var bg = this.TryFindResource("BgCanvas", ActualThemeVariant, out var res) && res is Avalonia.Media.Color c
            ? c : Avalonia.Media.Color.FromRgb(0x2e, 0x2e, 0x34);

        string path;
        try { path = System.IO.Path.Combine(GetExportDir(), ScreenshotFileName()); }
        catch (Exception ex) { await ShowExportFolderError(ex); return; }
        await _vm.SaveScreenshotAsync(pixels, w, h, bg.R, bg.G, bg.B, path);
    }

    // The mosairEXPORT folder could not be created (e.g. no permission on the desktop).
    private async Task ShowExportFolderError(Exception ex)
    {
        if (_vm.ShowAlert != null)
            await _vm.ShowAlert(Loc.Get("AlertExportTitle"), Loc.Fmt("AlertErrorBody", ex.Message));
    }

    private static string ScreenshotFileName()
    {
        string baseName = "mosair";
        if (!string.IsNullOrEmpty(Services.ProjectService.CurrentPictureFileName))
            baseName = System.IO.Path.GetFileNameWithoutExtension(Services.ProjectService.CurrentPictureFileName);
        return $"{DateTime.Now:M.dd.yyyy}_{DateTime.Now:HH.mm.ss}__{baseName}__ekran.png";
    }

    // ===== Google Drive (toolbar button + arrow, File > Google Drive) =====
    private async void OnDriveSave(object? sender, RoutedEventArgs e) => await _vm.SaveToDriveAsync();

    private async void OnDriveOpen(object? sender, RoutedEventArgs e)
    {
        if (await _vm.OpenFromDriveAsync())
            _vm.FitToWindow(imageScroller.Bounds.Width, imageScroller.Bounds.Height);
    }

    private async void OnDriveSettings(object? sender, RoutedEventArgs e) => await _vm.ConfigureDriveAsync();

    // Status-bar cancel button.
    private void OnCancelWork(object? sender, RoutedEventArgs e) => _vm.CancelWork();

    // Ctrl/⌘+E: quick export at the default quality.
    private void OnExportImage(object? sender, RoutedEventArgs e) => _ = ExportQuickAsync(MainViewModel.DefaultExportQuality);

    private async Task ExportQuickAsync(int quality)
    {
        string exportDir;
        try { exportDir = GetExportDir(); }
        catch (Exception ex) { await ShowExportFolderError(ex); return; }

        string baseName = "mosair";
        if (!string.IsNullOrEmpty(Services.ProjectService.CurrentPictureFileName))
            baseName = System.IO.Path.GetFileNameWithoutExtension(Services.ProjectService.CurrentPictureFileName);

        string date = DateTime.Now.ToString("M.dd.yyyy");
        string time = DateTime.Now.ToString("HH.mm.ss");
        int w = (int)Math.Round(_vm.WidthCm);
        int h = (int)Math.Round(_vm.HeightCm);
        string ext = _vm.QuickExportExtension(quality);
        string path = System.IO.Path.Combine(exportDir, $"{date}_{time}__{baseName}__{w}x{h}.{ext}");

        await _vm.ExportImageAsync(path, quality);
    }

    // ===== Export lists: mosairEXPORT ▸ and mosairEXPORT As ▸, each "Görüntü kalitesi seçiniz" + quality choices.
    // The toolbar button and the File menu share the same structure; the choice texts come from the view model.
    private readonly List<(MenuItem item, int quality, bool saveAs)> _exportChoices = new();

    private void BuildExportMenus()
    {
        var flyout = new MenuFlyout { Placement = Avalonia.Controls.PlacementMode.BottomEdgeAlignedRight };
        var quick = new MenuItem();
        quick.Bind(MenuItem.HeaderProperty, new Avalonia.Data.Binding("[MenuExport]") { Source = Loc.Instance });
        var saveAs = new MenuItem();
        saveAs.Bind(MenuItem.HeaderProperty, new Avalonia.Data.Binding("[MenuExportAs]") { Source = Loc.Instance });
        FillExportChoices(quick, false);
        FillExportChoices(saveAs, true);
        flyout.Items.Add(quick);
        flyout.Items.Add(saveAs);
        flyout.Opening += (_, _) => RefreshExportChoices();
        exportBtn.Flyout = flyout;

        FillExportChoices(menuExport, false);
        FillExportChoices(menuExportAs, true);
        menuExport.SubmenuOpened += (_, _) => RefreshExportChoices();
        menuExportAs.SubmenuOpened += (_, _) => RefreshExportChoices();

        _vm.ExportEstimatesChanged += UpdateExportChoiceTexts;
    }

    private void FillExportChoices(MenuItem parent, bool saveAs)
    {
        parent.Items.Clear();
        var title = new MenuItem { IsEnabled = false };
        title.Bind(MenuItem.HeaderProperty, new Avalonia.Data.Binding("[ExportChooseQuality]") { Source = Loc.Instance });
        parent.Items.Add(title);
        parent.Items.Add(new Separator());
        foreach (int q in MainViewModel.ExportQualities)
        {
            var item = new MenuItem();
            int quality = q;
            item.Click += async (_, _) =>
            {
                exportBtn.Flyout?.Hide();
                if (saveAs) await ExportAsAsync(quality);
                else await ExportQuickAsync(quality);
            };
            parent.Items.Add(item);
            _exportChoices.Add((item, quality, saveAs));
        }
    }

    private void RefreshExportChoices()
    {
        // The view model drops outdated estimates synchronously first, so the texts never show old sizes.
        _ = _vm.RefreshExportEstimatesAsync();
        UpdateExportChoiceTexts();
    }

    private void UpdateExportChoiceTexts()
    {
        foreach (var (item, quality, saveAs) in _exportChoices)
            item.Header = _vm.ExportChoiceLabel(quality, saveAs);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var src = e.Source as Avalonia.Visual;
        while (src != null && src != sender as Avalonia.Visual)
        {
            if (src is Avalonia.Controls.Button || src is Avalonia.Controls.MenuItem || src is Avalonia.Controls.Menu)
                return;
            src = src.GetVisualParent() as Avalonia.Visual;
        }
        if (e.ClickCount == 2)
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else
            BeginMoveDrag(e);
    }

    private void OnExportPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(sender as Avalonia.Visual).Properties;
        if (props.IsRightButtonPressed)
        {
            string exportDir = GetExportDir();
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exportDir,
                    UseShellExecute = true
                });
            }
            catch { }
            e.Handled = true;
        }
    }

    private async Task ExportAsAsync(int quality)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Loc.Get("DlgExportImage"),
            DefaultExtension = "jpg",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JPEG") { Patterns = new[] { "*.jpg", "*.jpeg" } },
                new FilePickerFileType("PNG") { Patterns = new[] { "*.png" } }
            }
        });
        if (file != null)
        {
            var path = file.TryGetLocalPath();
            if (path != null)
                await _vm.ExportImageAsync(path, quality);
        }
    }

    private void OnWidthGotFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox tb)
            Dispatcher.UIThread.Post(() => tb.SelectAll());
    }

    private void OnWidthTextInput(object? sender, TextInputEventArgs e)
    {
        if (e.Text == ",")
        {
            e.Text = ".";
        }
    }

    private void OnWidthChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
            tb.Text = tb.Text?.Replace(',', '.');
        _vm.UpdateDimensions();
    }

    private void OnWidthKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (sender is TextBox tb)
                tb.Text = tb.Text?.Replace(',', '.');
            _vm.UpdateDimensions();
            e.Handled = true;
        }
    }

    private void OnResetSize(object? sender, RoutedEventArgs e)
    {
        _vm.FitToWindow(imageScroller.Bounds.Width, imageScroller.Bounds.Height);
    }

    private void OnColorCheckChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is ColorItem item)
        {
            item.IsExcluded = cb.IsChecked != true;
            _vm.SyncColorExclusion(item);
        }
    }


    // Zoom about the mouse: the image point under the cursor stays under the cursor.
    private void OnImageWheel(object? sender, PointerWheelEventArgs e)
    {
        e.Handled = true;
        if (sender is not Control img) return;
        var mouse = e.GetPosition(imageScroller);
        var under = e.GetPosition(img);   // point under the cursor, in the image control's own coordinates
        double oldZoom = _vm.ZoomLevel;
        double factor = e.Delta.Y > 0 ? 1.25 : 0.8;
        _vm.ZoomLevel = oldZoom * factor;   // the view model clamps it
        double ratio = _vm.ZoomLevel / oldZoom;
        if (ratio == 1) return;

        // Let the new size reach the layout first; setting the offset before that clamps it to the old size.
        imageScroller.UpdateLayout();
        var moved = img.TranslatePoint(new Avalonia.Point(under.X * ratio, under.Y * ratio), imageScroller);
        if (moved == null) return;
        imageScroller.Offset = new Avalonia.Vector(
            Math.Max(0, imageScroller.Offset.X + moved.Value.X - mouse.X),
            Math.Max(0, imageScroller.Offset.Y + moved.Value.Y - mouse.Y));
        UpdateNav();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateNav();
    }

    // The navigator's content area: inside its border, where the thumbnail is centred and the view box is drawn.
    private Control NavContent => (Control)navPanel.Child!;
    private double NavInnerSize => Math.Min(NavContent.Bounds.Width, NavContent.Bounds.Height) is > 0 and var s
        ? s : navPanel.Width - navPanel.BorderThickness.Left - navPanel.BorderThickness.Right;

    private void UpdateNav()
    {
        _vm.UpdateNavigator(
            imageScroller.Viewport.Width,
            imageScroller.Viewport.Height,
            imageScroller.Offset.X,
            imageScroller.Offset.Y,
            NavInnerSize);
    }

    private void OnNavPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        NavigateFromNav(e.GetPosition(NavContent));
        e.Handled = true;
    }

    private void OnNavPointerMoved(object? sender, PointerEventArgs e)
    {
        var props = e.GetCurrentPoint(NavContent).Properties;
        if (!props.IsLeftButtonPressed) return;
        NavigateFromNav(e.GetPosition(NavContent));
        e.Handled = true;
    }

    private void NavigateFromNav(Avalonia.Point pos)
    {
        var target = _vm.NavigatorTarget(pos.X, pos.Y, NavInnerSize,
            imageScroller.Viewport.Width, imageScroller.Viewport.Height);
        if (target == null) return;
        imageScroller.Offset = new Avalonia.Vector(target.Value.x, target.Value.y);
    }

    private async void OnSaveProject(object? sender, RoutedEventArgs e)
    {
        try
        {
            string desktop = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
            string projectDir = System.IO.Path.Combine(desktop, "mosairPROJECT");
            System.IO.Directory.CreateDirectory(projectDir);

            string baseName = "mosair_project";
            string? srcImagePath = Services.ProjectService.CurrentPictureFileName;
            if (!string.IsNullOrEmpty(srcImagePath))
                baseName = System.IO.Path.GetFileNameWithoutExtension(srcImagePath);

            string folder = System.IO.Path.Combine(projectDir, baseName);
            System.IO.Directory.CreateDirectory(folder);

            string mosPath = System.IO.Path.Combine(folder, baseName + ".mos");
            if (!_vm.SaveProject(mosPath)) return;

            if (!string.IsNullOrEmpty(srcImagePath) && System.IO.File.Exists(srcImagePath))
            {
                string ext = System.IO.Path.GetExtension(srcImagePath);
                string imgDest = System.IO.Path.Combine(folder, baseName + ext);
                if (!System.IO.File.Exists(imgDest))
                    System.IO.File.Copy(srcImagePath, imgDest);
            }
        }
        catch (Exception ex)
        {
            // Folder could not be created or the image could not be copied (disk full, no access, ...).
            _vm.ReportSaveFailed(ex);
            return;
        }

        saveIcon.IsVisible = false;
        saveCheckIcon.IsVisible = true;
        await Task.Delay(1200);
        saveCheckIcon.IsVisible = false;
        saveIcon.IsVisible = true;
    }

    private async void OnSaveAsProject(object? sender, RoutedEventArgs e)
    {
        await SaveAsDialog();
    }

    private async Task SaveAsDialog()
    {
        string? suggested = null;
        if (!string.IsNullOrEmpty(Services.ProjectService.CurrentPictureFileName))
            suggested = System.IO.Path.GetFileNameWithoutExtension(Services.ProjectService.CurrentPictureFileName);

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Loc.Get("DlgSaveProject"),
            DefaultExtension = "mos",
            SuggestedFileName = suggested,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Mosaic Project") { Patterns = new[] { "*.mos" } }
            }
        });
        if (file != null)
        {
            var path = file.TryGetLocalPath();
            if (path != null)
            {
                try
                {
                    string dir = System.IO.Path.GetDirectoryName(path)!;
                    string name = System.IO.Path.GetFileNameWithoutExtension(path);
                    string folder = System.IO.Path.Combine(dir, name);
                    System.IO.Directory.CreateDirectory(folder);
                    string fullPath = System.IO.Path.Combine(folder, name + ".mos");
                    _vm.SaveProject(fullPath);
                }
                catch (Exception ex)
                {
                    _vm.ReportSaveFailed(ex);
                }
            }
        }
    }

    private async void OnOpenProject(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Loc.Get("DlgOpenProject"),
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Mosaic Project") { Patterns = new[] { "*.mos" } },
                FilePickerFileTypes.All
            }
        });
        if (files.Count > 0)
        {
            var path = files[0].TryGetLocalPath();
            if (path != null)
            {
                _vm.OpenProject(path);
                _vm.FitToWindow(imageScroller.Bounds.Width, imageScroller.Bounds.Height);
            }
        }
    }

    private void OnSelectAll(object? sender, RoutedEventArgs e)
    {
        _vm.SetAllColors(false);
    }

    private void OnDeselectAll(object? sender, RoutedEventArgs e)
    {
        _vm.SetAllColors(true);
    }

    private void OnImagePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning)
        {
            var current = e.GetPosition(imageScroller);
            var dx = current.X - _panStart.X;
            var dy = current.Y - _panStart.Y;
            imageScroller.Offset = new Avalonia.Vector(
                Math.Max(0, _scrollStart.X - dx),
                Math.Max(0, _scrollStart.Y - dy));
            e.Handled = true;
            return;
        }
        // The loaded image before Mos, MosaicView after it; both cover the whole canvas.
        if (sender is not Control img) return;
        var pos = e.GetPosition(img);
        _vm.OnImagePointerMoved(pos.X, pos.Y, img.Bounds.Width, img.Bounds.Height);
    }

    private void OnImagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control img) return;
        var props = e.GetCurrentPoint(img).Properties;

        if (props.IsRightButtonPressed)
        {
            _isPanning = true;
            _panStart = e.GetPosition(imageScroller);
            _scrollStart = imageScroller.Offset;
            e.Pointer.Capture(img);
            e.Handled = true;
            return;
        }

        var pos = e.GetPosition(img);
        bool isLeft = props.IsLeftButtonPressed;
        bool isMiddle = props.IsMiddleButtonPressed;
        _vm.OnImagePressed(pos.X, pos.Y, img.Bounds.Width, img.Bounds.Height, isLeft, isMiddle);
        if (isMiddle && !PixelEditService.IsPixelEditActive)
            catalogListBox.SelectedIndex = -1;
        e.Handled = true;
    }

    private void OnImagePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void OnTogglePixelEdit(object? sender, RoutedEventArgs e)
    {
        _vm.TogglePixelEditMode();
        if (!PixelEditService.IsPixelEditActive)
            catalogListBox.SelectedIndex = -1;
    }

    private void OnCatalogSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!PixelEditService.IsPixelEditActive) return;
        if (sender is not ListBox lb || lb.SelectedItem is not ViewModels.ColorItem item) return;
        _vm.SetSourceFromCatalog(item);
    }

    private void OnSelectStone(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int index)
            _vm.SelectStone(index);
    }

    private void OnGridMainColorPick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Avalonia.Media.Color color)
        {
            _vm.GridColor = color;
            _vm.SelectMainColor(color);
        }
    }

    private void OnGridColorPick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Avalonia.Media.Color color)
            _vm.GridColor = color;
    }

    private void OnSetLanguageTr(object? sender, RoutedEventArgs e)
    {
        Loc.Instance.Lang = "tr";
        _vm.RefreshLocalized();
    }

    private void OnSetLanguageEn(object? sender, RoutedEventArgs e)
    {
        Loc.Instance.Lang = "en";
        _vm.RefreshLocalized();
    }

    private bool _isLightTheme;
    private void OnToggleTheme(object? sender, RoutedEventArgs e)
    {
        _isLightTheme = !_isLightTheme;
        Application.Current!.RequestedThemeVariant =
            _isLightTheme ? Avalonia.Styling.ThemeVariant.Light
                          : Avalonia.Styling.ThemeVariant.Dark;
        iconDark.IsVisible = !_isLightTheme;
        iconLight.IsVisible = _isLightTheme;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Contains(DataFormat.File)) return;
        var files = e.DataTransfer.TryGetFiles();
        if (files == null) return;

        foreach (var item in files)
        {
            var path = item.TryGetLocalPath();
            if (path == null) continue;
            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tiff")
            {
                _vm.LoadImage(path);
                break;
            }
        }
    }

    private void OnToggleGrid(object? sender, RoutedEventArgs e)
    {
        _vm.ShowGrid = !_vm.ShowGrid;
    }

    private void OnSelectInterpolation(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Services.InterpolationMethod method)
            _vm.SelectedInterpolation = method;
    }

    private void StartMosAnim()
    {
        _mosAnimFrame = 0;
        _mosAnimTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _mosAnimTimer.Tick += OnMosAnimTick;
        OnMosAnimTick(null, EventArgs.Empty);
        _mosAnimTimer.Start();
    }

    private void StopMosAnim()
    {
        _mosAnimTimer?.Stop();
        _mosAnimTimer = null;
        mosQ0.Opacity = 0.15;
        mosQ1.Opacity = 0.15;
        mosQ2.Opacity = 0.15;
        mosQ3.Opacity = 0.15;
    }

    private void OnMosAnimTick(object? sender, EventArgs e)
    {
        var borders = new[] { mosQ0, mosQ1, mosQ2, mosQ3 };
        for (int i = 0; i < 4; i++)
            borders[i].Opacity = i == _mosAnimFrame ? 1.0 : 0.15;
        _mosAnimFrame = (_mosAnimFrame + 1) % 4;
    }

    // Arrow offsets per frame: drops in from above the icon, then rests on the tray
    private static readonly double[] ExportAnimOffsets = { -14, -11, -8, -5, -2, 0, 0, 0 };

    private void StartExportAnim()
    {
        exportBtn.Classes.Add("exporting");
        _exportAnimFrame = 0;
        _exportAnimTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
        _exportAnimTimer.Tick += OnExportAnimTick;
        OnExportAnimTick(null, EventArgs.Empty);
        _exportAnimTimer.Start();
    }

    private void StopExportAnim()
    {
        _exportAnimTimer?.Stop();
        _exportAnimTimer = null;
        exportBtn.Classes.Remove("exporting");
        if (exportArrow.RenderTransform is Avalonia.Media.TranslateTransform t)
            t.Y = 0;
    }

    private void OnExportAnimTick(object? sender, EventArgs e)
    {
        if (exportArrow.RenderTransform is Avalonia.Media.TranslateTransform t)
            t.Y = ExportAnimOffsets[_exportAnimFrame];
        _exportAnimFrame = (_exportAnimFrame + 1) % ExportAnimOffsets.Length;
    }
}

