using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// Theme of <see cref="KlyDashboardGroup"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme DashboardGroup { get; } = Themes.DashboardGroup();
    /// <summary>
    /// Theme of <see cref="KlyDashboardPanel"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme DashboardPanel { get; } = Themes.DashboardPanel();
    /// <summary>
    /// Theme of <see cref="KlyDashboardResizeHandle"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme DashboardResizeHandle { get; } = Themes.DashboardResizeHandle();
    /// <summary>
    /// Theme of <see cref="KlyDashboardNavbar"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme DashboardNavbar { get; } = Themes.DashboardNavbar();
    /// <summary>
    /// Theme of <see cref="KlyDashboardToolbar"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme DashboardToolbar { get; } = Themes.DashboardToolbar();
    /// <summary>
    /// Theme of <see cref="KlyDashboardSidebarCollapse"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme DashboardSidebarCollapse { get; } = Themes.DashboardSidebarCollapse();
    /// <summary>
    /// Theme of <see cref="KlyDashboardSidebarToggle"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme DashboardSidebarToggle { get; } = Themes.DashboardSidebarToggle();
    /// <summary>
    /// Theme of <see cref="KlyDashboardSearchButton"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme DashboardSearchButton { get; } = Themes.DashboardSearchButton();
}
