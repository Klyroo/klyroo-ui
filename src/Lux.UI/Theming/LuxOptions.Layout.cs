using Lux.UI.Theming;

namespace Lux.UI;

public sealed partial class LuxOptions
{
    public ComponentTheme App { get; } = Themes.App();
    public ComponentTheme Header { get; } = Themes.Header();
    public ComponentTheme Footer { get; } = Themes.Footer();
    public ComponentTheme FooterColumns { get; } = Themes.FooterColumns();
    public ComponentTheme Main { get; } = Themes.Main();
    public ComponentTheme SkipLink { get; } = Themes.SkipLink();
    public ComponentTheme Sidebar { get; } = Themes.Sidebar();
    public ComponentTheme Error { get; } = Themes.Error();
    public ComponentTheme LocaleSelect { get; } = Themes.LocaleSelect();
}
