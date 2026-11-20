using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme Breadcrumb { get; } = Themes.Breadcrumb();
    public ComponentTheme Pagination { get; } = Themes.Pagination();
    public ComponentTheme Stepper { get; } = Themes.Stepper();
    public ComponentTheme Timeline { get; } = Themes.Timeline();
    public ComponentTheme NavigationMenu { get; } = Themes.NavigationMenu();
    public ComponentTheme Collapsible { get; } = Themes.Collapsible();
    public ComponentTheme Tree { get; } = Themes.Tree();
    public ComponentTheme Marquee { get; } = Themes.Marquee();
    public ComponentTheme ScrollArea { get; } = Themes.ScrollArea();
    public ComponentTheme Splitter { get; } = Themes.Splitter();
}
