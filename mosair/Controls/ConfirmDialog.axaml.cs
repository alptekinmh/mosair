using Avalonia.Controls;
using Avalonia.Interactivity;

namespace mosair.Controls;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string title, string message) : this()
    {
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        YesButton.Content = Services.Loc.Get("DlgYes");
        NoButton.Content = Services.Loc.Get("DlgNo");
    }

    private void OnYesClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnNoClick(object? sender, RoutedEventArgs e) => Close(false);
}
