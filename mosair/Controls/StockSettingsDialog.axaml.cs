using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using mosair.Services;

namespace mosair.Controls;

public partial class StockSettingsDialog : Window
{
    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.FromRgb(0x3a, 0x7b, 0xfd));

    public StockSettingsDialog()
    {
        InitializeComponent();
    }

    public StockSettingsDialog(StockSheetService.Config current) : this()
    {
        Title = Loc.Get("StockSettingsTitle");
        TitleText.Text = Loc.Get("StockSettingsTitle");
        SheetIdLabel.Text = Loc.Get("StockSheetIdLabel");
        SheetIdHint.Text = Loc.Get("StockSheetIdHint");
        SetExample(SheetIdExample, "docs.google.com/spreadsheets/d/", "1AbC…xYz", "/edit#gid=0");
        ScriptUrlLabel.Text = Loc.Get("StockScriptUrlLabel");
        ScriptUrlHint.Text = Loc.Get("StockScriptUrlHint");
        SetExample(ScriptUrlExample, "", "https://script.google.com/macros/s/AKfy…/exec", "");
        RequirementsTitle.Text = Loc.Get("StockRequirementsTitle");
        RequirementsText.Text = Loc.Get("StockRequirements");
        NoteText.Text = Loc.Get("StockSettingsNote");
        SaveButton.Content = Loc.Get("DlgSave");
        CancelButton.Content = Loc.Get("DlgCancel");
        SheetIdBox.Text = current.SheetId;
        ScriptUrlBox.Text = current.ScriptUrl;
    }

    // Example link with the part to enter highlighted.
    private static void SetExample(TextBlock target, string before, string part, string after)
    {
        target.Inlines = new InlineCollection
        {
            new Run(before),
            new Run(part) { Foreground = HighlightBrush, FontWeight = FontWeight.SemiBold },
            new Run(after)
        };
    }

    // Accepts the bare ID or the whole sheet link and keeps only the ID.
    private static string ExtractSheetId(string text)
    {
        var m = Regex.Match(text, @"/spreadsheets/d/([A-Za-z0-9_-]+)");
        return m.Success ? m.Groups[1].Value : text;
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e) =>
        Close(new StockSheetService.Config
        {
            SheetId = ExtractSheetId((SheetIdBox.Text ?? "").Trim()),
            ScriptUrl = (ScriptUrlBox.Text ?? "").Trim()
        });

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
