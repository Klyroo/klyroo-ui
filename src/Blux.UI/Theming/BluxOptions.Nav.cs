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

public sealed partial class BluxStrings
{
    public string Breadcrumb { get; set; } = "Breadcrumb";
    public string Pagination { get; set; } = "Pagination";
    public string Page { get; set; } = "Page";
    public string FirstPage { get; set; } = "First page";
    public string PreviousPage { get; set; } = "Previous page";
    public string NextPage { get; set; } = "Next page";
    public string LastPage { get; set; } = "Last page";
    public string StepCompleted { get; set; } = "completed";
    public string ResizePanels { get; set; } = "Resize panels";
}
