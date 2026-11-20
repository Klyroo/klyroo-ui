using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme App { get; } = Themes.App();
    public ComponentTheme Header { get; } = Themes.Header();
    public ComponentTheme Footer { get; } = Themes.Footer();
    public ComponentTheme FooterColumns { get; } = Themes.FooterColumns();
    public ComponentTheme Main { get; } = Themes.Main();
    public ComponentTheme Sidebar { get; } = Themes.Sidebar();
    public ComponentTheme Error { get; } = Themes.Error();
    public ComponentTheme LocaleSelect { get; } = Themes.LocaleSelect();
}
