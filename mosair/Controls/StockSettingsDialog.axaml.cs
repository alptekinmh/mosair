using Avalonia.Controls;
using Avalonia.Interactivity;
using mosair.Services;

namespace mosair.Controls;

public partial class StockSettingsDialog : Window
{
    public StockSettingsDialog()
    {
        InitializeComponent();
    }

    public StockSettingsDialog(StockSheetService.Config current) : this()
    {
        Title = Loc.Get("StockSettingsTitle");
        TitleText.Text = Loc.Get("StockSettingsTitle");
        SheetIdLabel.Text = Loc.Get("StockSheetIdLabel");
        ScriptUrlLabel.Text = Loc.Get("StockScriptUrlLabel");
        NoteText.Text = Loc.Get("StockSettingsNote");
        SaveButton.Content = Loc.Get("DlgSave");
        CancelButton.Content = Loc.Get("DlgCancel");
        SheetIdBox.Text = current.SheetId;
        ScriptUrlBox.Text = current.ScriptUrl;
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e) =>
        Close(new StockSheetService.Config
        {
            SheetId = (SheetIdBox.Text ?? "").Trim(),
            ScriptUrl = (ScriptUrlBox.Text ?? "").Trim()
        });

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
