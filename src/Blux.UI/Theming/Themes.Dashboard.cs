namespace Blux.UI.Theming;

/// <summary>Themes of the dashboard family. The sidebar reuses <see cref="Sidebar"/>.</summary>
internal static partial class Themes
{
    private const string DashboardFocus =
        "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--blux-primary-text)";

    public static ComponentTheme DashboardGroup() =>
        new ComponentTheme("dashboardGroup", "root")
            .Slot("root", "relative flex size-full min-h-0 min-w-0 overflow-hidden bg-(--blux-bg) text-(--blux-text)");

    public static ComponentTheme DashboardPanel() =>
        new ComponentTheme("dashboardPanel", "root", "header", "body", "footer")
            .Slot("root", "relative flex min-h-0 min-w-0 flex-col not-last:border-e not-last:border-(--blux-border)")
            .Slot("header", "shrink-0")
            .Slot("body", "flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto p-4 sm:p-6")
            .Slot("footer", "shrink-0 border-t border-(--blux-border)");

    /// <summary>A flex item that overlaps its neighbours by its own half width; inside a panel the panel makes it absolute.</summary>
    public static ComponentTheme DashboardResizeHandle() =>
        new ComponentTheme("dashboardResizeHandle", "root")
            .Slot("root", "relative z-10 -mx-[3px] hidden w-1.5 shrink-0 cursor-col-resize touch-none rounded-full transition-colors lg:block " +
                          "hover:bg-(--blux-border-accented) data-dragging:bg-(--blux-border-accented) " + DashboardFocus);

    public static ComponentTheme DashboardNavbar() =>
        new ComponentTheme("dashboardNavbar", "root", "left", "title", "center", "right")
            .Slot("root", "flex h-16 shrink-0 items-center justify-between gap-4 border-b border-(--blux-border) px-4 sm:px-6")
            .Slot("left", "flex min-w-0 items-center gap-1.5")
            .Slot("title", "truncate text-base font-semibold text-(--blux-text-highlighted)")
            .Slot("center", "flex min-w-0 flex-1 items-center gap-1.5")
            .Slot("right", "flex shrink-0 items-center gap-1.5");

    public static ComponentTheme DashboardToolbar() =>
        new ComponentTheme("dashboardToolbar", "root", "left", "center", "right")
            .Slot("root", "flex h-12 shrink-0 items-center justify-between gap-4 border-b border-(--blux-border) px-4 sm:px-6")
            .Slot("left", "flex min-w-0 items-center gap-1.5")
            .Slot("center", "flex min-w-0 flex-1 items-center gap-1.5")
            .Slot("right", "flex shrink-0 items-center gap-1.5");

    public static ComponentTheme DashboardSidebarCollapse() =>
        new ComponentTheme("dashboardSidebarCollapse", "root")
            .Slot("root", "hidden lg:inline-flex");

    public static ComponentTheme DashboardSidebarToggle() =>
        new ComponentTheme("dashboardSidebarToggle", "root")
            .Slot("root", "lg:hidden");

    /// <summary>Inside a collapsed sidebar only the icon is left, centered.</summary>
    public static ComponentTheme DashboardSearchButton() =>
        new ComponentTheme("dashboardSearchButton", "root", "icon", "label", "kbds")
            .Slot("root", "inline-flex w-full min-w-0 items-center gap-1.5 rounded-(--blux-radius) bg-(--blux-bg) px-2.5 py-1.5 text-sm text-(--blux-text-muted) " +
                          "ring ring-inset ring-(--blux-border-accented) transition-colors hover:bg-(--blux-bg-elevated) hover:text-(--blux-text) " + DashboardFocus + " " +
                          "group-data-[state=collapsed]/sidebar:justify-center group-data-[state=collapsed]/sidebar:px-0")
            .Slot("icon", "size-5 shrink-0")
            .Slot("label", "min-w-0 flex-1 truncate text-start group-data-[state=collapsed]/sidebar:sr-only")
            .Slot("kbds", "ms-auto flex shrink-0 items-center gap-0.5 group-data-[state=collapsed]/sidebar:hidden");
}
