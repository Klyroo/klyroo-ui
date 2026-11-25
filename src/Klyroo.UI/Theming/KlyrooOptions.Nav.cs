using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
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
