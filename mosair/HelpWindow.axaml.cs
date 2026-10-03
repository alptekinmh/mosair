using Avalonia.Controls;
using Avalonia.Interactivity;
using mosair.Services;

namespace mosair;

public partial class HelpWindow : Window
{
    private readonly TextBlock[] _sectionsTr;
    private readonly TextBlock[] _sectionsEn;

    public HelpWindow()
    {
        InitializeComponent();
        _sectionsTr = new TextBlock[] { sec0, sec1, sec2, sec3, sec4, sec5, sec6, sec7, sec8 };
        _sectionsEn = new TextBlock[] { sec0en, sec1en, sec2en, sec3en, sec4en, sec5en, sec6en, sec7en, sec8en };
    }

    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not string tag) return;
        if (!int.TryParse(tag, out int idx)) return;

        var sections = Loc.Instance.Lang == "en" ? _sectionsEn : _sectionsTr;
        if (idx < 0 || idx >= sections.Length) return;
        sections[idx].BringIntoView();
    }
}
