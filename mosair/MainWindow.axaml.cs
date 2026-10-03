using System;
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
        DataContext = _vm;
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
                     or nameof(MainViewModel.StonePixelSize) or nameof(MainViewModel.GridColor))
            {
                RefreshToolsMenuChecks();
            }
        };

        BuildToolsMenu();
    }

    // ===== Tools menu: mirrors the toolbar controls through the same ViewModel properties =====

    private const string CheckGeometry = "M9,16.17 L4.83,12 L3.41,13.41 L9,19 L21,7 L19.59,5.59 Z";
    private readonly System.Collections.Generic.List<(MenuItem Item, Services.InterpolationMethod Value)> _interpMenuItems = new();
    private readonly System.Collections.Generic.List<(MenuItem Item, int Value)> _detailMenuItems = new();
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

        // Same range and step as the toolbar detail slider.
        for (int v = 10; v <= 100; v += 10)
        {
            int value = v;
            var item = new MenuItem { Header = value.ToString(), Icon = NewMenuCheck() };
            item.Click += (_, _) => _vm.StonePixelSize = value;
            menuDetail.Items.Add(item);
            _detailMenuItems.Add((item, value));
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
        foreach (var (item, value) in _detailMenuItems)
            SetMenuCheck(item, value == _vm.StonePixelSize);
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

    private void OnToggleOptimum(object? sender, RoutedEventArgs e)
    {
        _vm.UseOptimal = !_vm.UseOptimal;
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

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        bool mod = e.KeyModifiers == KeyModifiers.Control ||
                   e.KeyModifiers == KeyModifiers.Meta;
        if (mod)
        {
            if (e.Key == Key.Z)
            {
                _vm.UndoPixelEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Y)
            {
                _vm.RedoPixelEdit();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.F1)
        {
            ShowHelp();
            e.Handled = true;
        }
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

    private async void OnExportImage(object? sender, RoutedEventArgs e)
    {
        string exportDir = GetExportDir();

        string baseName = "mosair";
        if (!string.IsNullOrEmpty(Services.ProjectService.CurrentPictureFileName))
            baseName = System.IO.Path.GetFileNameWithoutExtension(Services.ProjectService.CurrentPictureFileName);

        string date = DateTime.Now.ToString("M.dd.yyyy");
        string time = DateTime.Now.ToString("HH.mm.ss");
        int w = (int)Math.Round(_vm.WidthCm);
        int h = (int)Math.Round(_vm.HeightCm);
        string path = System.IO.Path.Combine(exportDir, $"{date}_{time}__{baseName}__{w}x{h}.jpeg");

        await _vm.ExportImageAsync(path);
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

    private async void OnExportAsImage(object? sender, RoutedEventArgs e)
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
                await _vm.ExportImageAsync(path);
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


    private void OnImageWheel(object? sender, PointerWheelEventArgs e)
    {
        var mousePos = e.GetPosition(imageScroller);
        double oldZoom = _vm.ZoomLevel;
        double factor = e.Delta.Y > 0 ? 1.25 : 0.8;
        double newZoom = Math.Max(_vm.MinZoomLevel, Math.Min(20, oldZoom * factor));

        double pointX = imageScroller.Offset.X + mousePos.X;
        double pointY = imageScroller.Offset.Y + mousePos.Y;

        double ratio = newZoom / oldZoom;
        _vm.ZoomLevel = newZoom;

        imageScroller.Offset = new Avalonia.Vector(
            Math.Max(0, pointX * ratio - mousePos.X),
            Math.Max(0, pointY * ratio - mousePos.Y));

        e.Handled = true;
        UpdateNav();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateNav();
    }

    private void UpdateNav()
    {
        _vm.UpdateNavigator(
            imageScroller.Viewport.Width,
            imageScroller.Viewport.Height,
            imageScroller.Offset.X,
            imageScroller.Offset.Y,
            navPanel.Width);
    }

    private void OnNavPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border nav) return;
        NavigateFromNav(e.GetPosition(nav));
        e.Handled = true;
    }

    private void OnNavPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Border nav) return;
        var props = e.GetCurrentPoint(nav).Properties;
        if (!props.IsLeftButtonPressed) return;
        NavigateFromNav(e.GetPosition(nav));
        e.Handled = true;
    }

    private void NavigateFromNav(Avalonia.Point pos)
    {
        double imgW = _vm.ImageDisplayWidth;
        double imgH = _vm.ImageDisplayHeight;
        if (imgW <= 0 || imgH <= 0) return;

        double navSize = navPanel.Width;
        double scale = Math.Min(navSize / imgW, navSize / imgH);
        double thumbW = imgW * scale;
        double thumbH = imgH * scale;

        double ratioX = pos.X / thumbW;
        double ratioY = pos.Y / thumbH;

        double targetX = ratioX * imgW - imageScroller.Viewport.Width / 2;
        double targetY = ratioY * imgH - imageScroller.Viewport.Height / 2;

        imageScroller.Offset = new Avalonia.Vector(
            Math.Max(0, targetX),
            Math.Max(0, targetY));
    }

    private async void OnSaveProject(object? sender, RoutedEventArgs e)
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
        _vm.SaveProject(mosPath);

        if (!string.IsNullOrEmpty(srcImagePath) && System.IO.File.Exists(srcImagePath))
        {
            string ext = System.IO.Path.GetExtension(srcImagePath);
            string imgDest = System.IO.Path.Combine(folder, baseName + ext);
            if (!System.IO.File.Exists(imgDest))
                System.IO.File.Copy(srcImagePath, imgDest);
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
                string dir = System.IO.Path.GetDirectoryName(path)!;
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                string folder = System.IO.Path.Combine(dir, name);
                System.IO.Directory.CreateDirectory(folder);
                string fullPath = System.IO.Path.Combine(folder, name + ".mos");
                _vm.SaveProject(fullPath);
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
        if (sender is not Image img) return;
        var pos = e.GetPosition(img);
        _vm.OnImagePointerMoved(pos.X, pos.Y, img.Bounds.Width, img.Bounds.Height);
    }

    private void OnImagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Image img) return;
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

    private void OnDismissNWarning(object? sender, RoutedEventArgs e)
    {
        _vm.DismissNWarning();
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

