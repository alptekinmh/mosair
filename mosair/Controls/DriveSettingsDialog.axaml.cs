using System;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using mosair.Services;

namespace mosair.Controls;

// Google Drive settings: the project folder link and the Apps Script URL, plus the setup steps, a button that
// copies the script code and a connection test. Returns the new Config, or null when cancelled.
public partial class DriveSettingsDialog : Window
{
    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.FromRgb(0x3a, 0x7b, 0xfd));

    public DriveSettingsDialog()
    {
        InitializeComponent();
    }

    public DriveSettingsDialog(DriveService.Config current) : this()
    {
        Title = Loc.Get("DriveSettingsTitle");
        TitleText.Text = Loc.Get("DriveSettingsTitle");
        FolderLabel.Text = Loc.Get("DriveFolderLabel");
        FolderHint.Text = Loc.Get("DriveFolderHint");
        SetExample(FolderExample, "drive.google.com/drive/folders/", "1AbC…xYz", "");
        ScriptUrlLabel.Text = Loc.Get("DriveScriptUrlLabel");
        ScriptUrlHint.Text = Loc.Get("DriveScriptUrlHint");
        SetExample(ScriptUrlExample, "", "https://script.google.com/macros/s/AKfy…/exec", "");
        StepsTitle.Text = Loc.Get("DriveStepsTitle");
        StepsText.Text = Loc.Get("DriveSteps");
        CopyScriptButton.Content = Loc.Get("DriveCopyScript");
        TestButton.Content = Loc.Get("DriveTest");
        NoteText.Text = Loc.Get("DriveSettingsNote");
        SaveButton.Content = Loc.Get("DlgSave");
        CancelButton.Content = Loc.Get("DlgCancel");
        FolderBox.Text = current.FolderUrl;
        ScriptUrlBox.Text = current.ScriptUrl;
    }

    private static void SetExample(TextBlock target, string before, string part, string after)
    {
        target.Inlines = new InlineCollection
        {
            new Run(before),
            new Run(part) { Foreground = HighlightBrush, FontWeight = FontWeight.SemiBold },
            new Run(after)
        };
    }

    private DriveService.Config Current() => new()
    {
        FolderUrl = (FolderBox.Text ?? "").Trim(),
        ScriptUrl = (ScriptUrlBox.Text ?? "").Trim()
    };

    private async void OnCopyScript(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;
        await clipboard.SetTextAsync(DriveService.ScriptCode());
        ResultText.Text = Loc.Get("DriveScriptCopied");
    }

    // Tries the folder and the script as they are typed now (before saving).
    private async void OnTest(object? sender, RoutedEventArgs e)
    {
        var config = Current();
        if (!DriveService.IsConfigured(config))
        {
            ResultText.Text = Loc.Get("DriveNotConfigured");
            return;
        }
        TestButton.IsEnabled = false;
        ResultText.Text = Loc.Get("DriveTesting");
        try
        {
            string folder = await DriveService.PingAsync(config);
            ResultText.Text = Loc.Fmt("DriveTestOk", folder);
        }
        catch (Exception ex)
        {
            ResultText.Text = Loc.Fmt("DriveTestFailed", ex.Message);
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e) => Close(Current());

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
