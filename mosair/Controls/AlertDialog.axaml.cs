using Avalonia.Controls;
using Avalonia.Interactivity;

namespace mosair.Controls;

public partial class AlertDialog : Window
{
    public AlertDialog()
    {
        InitializeComponent();
    }

    public AlertDialog(string title, string message) : this()
    {
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        OkButton.Content = Services.Loc.Get("WarnOk");
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
