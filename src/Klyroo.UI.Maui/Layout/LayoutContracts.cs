namespace Klyroo.UI.Maui;

/// <summary>A view inside a <see cref="KlySidebar"/> that adapts when the sidebar collapses or turns into a panel.</summary>
internal interface ILayoutSidebarPart
{
    void SidebarChanged(KlySidebar sidebar);
}

/// <summary>A view inside a <see cref="KlyDashboardGroup"/> that adapts when the dashboard changes between narrow and wide.</summary>
internal interface ILayoutDashboardPart
{
    void DashboardChanged(KlyDashboardGroup group);
}
