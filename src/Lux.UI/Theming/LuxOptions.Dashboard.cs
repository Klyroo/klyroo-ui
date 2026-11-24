using Lux.UI.Theming;

namespace Lux.UI;

public sealed partial class LuxOptions
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
