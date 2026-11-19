using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme DashboardGroup { get; } = Themes.DashboardGroup();
    public ComponentTheme DashboardPanel { get; } = Themes.DashboardPanel();
    public ComponentTheme DashboardResizeHandle { get; } = Themes.DashboardResizeHandle();
    public ComponentTheme DashboardNavbar { get; } = Themes.DashboardNavbar();
    public ComponentTheme DashboardToolbar { get; } = Themes.DashboardToolbar();
    public ComponentTheme DashboardSidebarCollapse { get; } = Themes.DashboardSidebarCollapse();
    public ComponentTheme DashboardSidebarToggle { get; } = Themes.DashboardSidebarToggle();
    public ComponentTheme DashboardSearchButton { get; } = Themes.DashboardSearchButton();
}

public sealed partial class BluxStrings
{
    /// <summary>Accessible name of the dashboard search dialog and its search field.</summary>
    public string DashboardSearch { get; set; } = "Search";

    /// <summary>Placeholder of the dashboard search field.</summary>
    public string DashboardSearchPlaceholder { get; set; } = "Type a command or search...";

    /// <summary>Text of the button that opens the dashboard search.</summary>
    public string DashboardSearchButton { get; set; } = "Search";
}
