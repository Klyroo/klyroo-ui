using Blux.UI;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    private static readonly ComponentDoc[] Dashboard =
    [
        new("dashboard-group", "DashboardGroup", "Dashboard", "The root of a dashboard: a sidebar and resizable panels in a row, with their sizes and collapsed state remembered under a storage key.",
            [new("A complete dashboard", typeof(Examples.DashboardExample), "Sidebar, navbar, toolbar, panels and search. Ctrl+K opens the search; the sidebar and the panel sizes are remembered under the StorageKey."),
             new("Bound state", typeof(Examples.DashboardBoundExample), "@bind-Sizes and @bind-Collapsed report to .NET; your state wins over the stored one.")],
            [typeof(BxDashboardGroup)]),
        new("dashboard-sidebar", "DashboardSidebar", "Dashboard", "A resizable sidebar that collapses to icons and becomes an off-canvas panel below the lg breakpoint. It builds on Sidebar.",
            [new("Collapse and resize", typeof(Examples.DashboardSidebarExample), "The collapse button and the mobile toggle are separate components, placed in the navbar."),
             new("In a complete dashboard", typeof(Examples.DashboardExample))],
            [typeof(BxDashboardSidebar)]),
        new("dashboard-sidebar-collapse", "DashboardSidebarCollapse", "Dashboard", "A button that collapses the sidebar to icons from the lg breakpoint up.",
            [new("In a navbar", typeof(Examples.DashboardSidebarExample), "aria-expanded and aria-controls are kept up to date in the browser, so it works in static rendering.")],
            [typeof(BxDashboardSidebarCollapse)]),
        new("dashboard-sidebar-toggle", "DashboardSidebarToggle", "Dashboard", "A button that opens the sidebar as an off-canvas panel below the lg breakpoint.",
            [new("In a navbar", typeof(Examples.DashboardSidebarExample), "The navbar shows it by default. Narrow the window below the lg breakpoint to see it.")],
            [typeof(BxDashboardSidebarToggle)]),
        new("dashboard-panel", "DashboardPanel", "Dashboard", "A column with a header, a scrollable body and a footer, optionally resizable between limits.",
            [new("Resizable panels", typeof(Examples.DashboardPanelExample), "Sizes are percentages shared by the panels. Each limit applies to the panel and to its neighbour.")],
            [typeof(BxDashboardPanel)]),
        new("dashboard-resize-handle", "DashboardResizeHandle", "Dashboard", "A separator that resizes the panel before it with the mouse or the keyboard.",
            [new("Between two panels", typeof(Examples.DashboardResizeHandleExample), "Use Resizable on a panel for the common case; place a handle yourself to control where it goes.")],
            [typeof(BxDashboardResizeHandle)]),
        new("dashboard-navbar", "DashboardNavbar", "Dashboard", "The top bar of a panel: sidebar toggle, title, and left, center and right areas.",
            [new("Title and actions", typeof(Examples.DashboardNavbarExample))],
            [typeof(BxDashboardNavbar)]),
        new("dashboard-toolbar", "DashboardToolbar", "Dashboard", "A secondary bar under a navbar, for filters and bulk actions.",
            [new("Filters and a count", typeof(Examples.DashboardToolbarExample))],
            [typeof(BxDashboardToolbar)]),
        new("dashboard-search", "DashboardSearch", "Dashboard", "A command palette in a modal, opened by a shortcut (Ctrl or Cmd plus K by default).",
            [new("Links and actions", typeof(Examples.DashboardSearchExample), "Links navigate, other commands raise OnSelect. Everything else, including the shortcut, runs in the browser.")],
            [typeof(BxDashboardSearch)]),
        new("dashboard-search-button", "DashboardSearchButton", "Dashboard", "A button that opens the search and shows its shortcut.",
            [new("With the search", typeof(Examples.DashboardSearchExample), "Inside a collapsed sidebar only the icon is shown.")],
            [typeof(BxDashboardSearchButton)]),
    ];
}
