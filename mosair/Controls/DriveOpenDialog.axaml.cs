using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using mosair.Services;

namespace mosair.Controls;

// One project card: name, date and size, and the original image's preview once it has arrived.
public sealed class DriveFileRow : INotifyPropertyChanged
{
    public DriveFileRow(DriveService.DriveFile file, string name, string details)
    {
        File = file;
        Name = name;
        Details = details;
    }

    public DriveService.DriveFile File { get; }
    public string Name { get; }
    public string Details { get; }

    private Bitmap? _thumbnail;
    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        set { _thumbnail = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasThumbnail)); }
    }
    public bool HasThumbnail => _thumbnail != null;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// Drive project browser: the projects in the Drive folder as cards with the original image's preview, with
// search, sorting, refresh and a button that opens the folder in the browser. Returns the chosen file, or null.
public partial class DriveOpenDialog : Window
{
    private readonly DriveService.Config _config;
    private List<DriveFileRow> _rows = new();
    private int _loadVersion;

    public DriveOpenDialog()
    {
        InitializeComponent();
        _config = new DriveService.Config();
    }

    public DriveOpenDialog(DriveService.Config config) : this()
    {
        _config = config;
        Title = Loc.Get("DriveOpenTitle");
        TitleText.Text = Loc.Get("DriveOpenTitle");
        OpenButton.Content = Loc.Get("DriveOpenButton");
        CancelButton.Content = Loc.Get("DlgCancel");
        RefreshButton.Content = Loc.Get("DriveRefresh");
        BrowserButton.Content = Loc.Get("DriveShowInBrowser");
        SearchBox.PlaceholderText = Loc.Get("DriveSearch");
        SortBox.ItemsSource = new[] { Loc.Get("DriveSortNewest"), Loc.Get("DriveSortName") };
        SortBox.SelectedIndex = 0;
        Opened += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        int version = ++_loadVersion;
        RefreshButton.IsEnabled = false;
        _rows = new List<DriveFileRow>();
        FileList.ItemsSource = null;
        OpenButton.IsEnabled = false;
        MessageText.Text = Loc.Get("StatusDriveListing");
        FolderText.Text = "";
        try
        {
            string folder = await DriveService.PingAsync(_config);
            var files = await DriveService.ListAsync(_config);
            if (version != _loadVersion) return;
            _rows = files.Select(f => new DriveFileRow(f, f.Name, Details(f))).ToList();
            FolderText.Text = Loc.Fmt("DriveOpenFolder", folder, files.Count);
            MessageText.Text = files.Count == 0 ? Loc.Fmt("DriveOpenEmpty", folder) : "";
            ApplyView();
            await LoadThumbnailsAsync(_rows);
        }
        catch (Exception ex)
        {
            if (version == _loadVersion) MessageText.Text = Loc.Fmt("DriveFailed", ex.Message);
        }
        finally
        {
            if (version == _loadVersion) RefreshButton.IsEnabled = true;
        }
    }

    private static string Details(DriveService.DriveFile f)
    {
        string date = f.Modified == DateTime.MinValue ? "" : f.Modified.ToString("g");
        string size = f.Size >= 1 << 20
            ? Loc.Fmt("SizeMB", (f.Size / (double)(1 << 20)).ToString("0.0"))
            : Loc.Fmt("SizeKB", Math.Max(1, f.Size / 1024));
        return date.Length > 0 ? date + " · " + size : size;
    }

    // Previews arrive after the list, in one request; a card without one keeps its placeholder.
    private async Task LoadThumbnailsAsync(List<DriveFileRow> rows)
    {
        var ids = rows.Select(r => r.File.ImageId).Where(id => id.Length > 0).Distinct().ToList();
        if (ids.Count == 0) return;
        Dictionary<string, byte[]> thumbs;
        try { thumbs = await DriveService.ThumbnailsAsync(_config, ids); }
        catch (Exception) { return; }   // previews are optional
        foreach (var row in rows)
            if (thumbs.TryGetValue(row.File.ImageId, out var bytes))
            {
                try { row.Thumbnail = new Bitmap(new MemoryStream(bytes)); }
                catch (Exception) { }
            }
    }

    // Search (by project or folder name) and sort, applied to the loaded list.
    private void ApplyView()
    {
        string q = (SearchBox.Text ?? "").Trim();
        IEnumerable<DriveFileRow> view = _rows;
        if (q.Length > 0)
            view = view.Where(r => r.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
                                   r.File.Folder.Contains(q, StringComparison.CurrentCultureIgnoreCase));
        view = SortBox.SelectedIndex == 1
            ? view.OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
            : view.OrderByDescending(r => r.File.Modified);
        var list = view.ToList();
        FileList.ItemsSource = list;
        OpenButton.IsEnabled = FileList.SelectedItem != null;
        if (_rows.Count > 0)
            MessageText.Text = list.Count == 0 ? Loc.Get("DriveNoMatch") : "";
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e) => ApplyView();
    private void OnSortChanged(object? sender, SelectionChangedEventArgs e) { if (_rows.Count > 0) ApplyView(); }
    private async void OnRefresh(object? sender, RoutedEventArgs e) => await LoadAsync();

    private async void OnOpenInBrowser(object? sender, RoutedEventArgs e)
    {
        try
        {
            var launcher = TopLevel.GetTopLevel(this)?.Launcher;
            if (launcher != null) await launcher.LaunchUriAsync(new Uri(DriveService.FolderWebUrl(_config)));
        }
        catch (Exception ex)
        {
            MessageText.Text = Loc.Fmt("DriveFailed", ex.Message);
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        OpenButton.IsEnabled = FileList.SelectedItem != null;

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (FileList.SelectedItem is DriveFileRow row) Close(row.File);
    }

    private void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is DriveFileRow row) Close(row.File);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
