namespace Blux.UI.Theming;

/// <summary>Themes of the layout family: app, header, footer, footer columns, main, sidebar, error, locale select.</summary>
internal static partial class Themes
{
    private const string LayoutFocus =
        "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--blux-primary-text)";

    private const string LayoutIconButton =
        "inline-flex size-8 shrink-0 items-center justify-center rounded-(--blux-radius) text-(--blux-text-muted) transition-colors " +
        "hover:bg-(--blux-bg-elevated) hover:text-(--blux-text) " + LayoutFocus;

    public static ComponentTheme App() =>
        new ComponentTheme("app", "root")
            .Slot("root", "isolate");

    /// <summary>Header height comes from <c>--bx-header-height</c> or <c>--blux-header-height</c> (4rem by default), read at the element, which <see cref="Main"/> subtracts from the viewport.</summary>
    public static ComponentTheme Header() =>
        new ComponentTheme("header", "root", "container", "left", "title", "center", "right", "menu")
            .Slot("root", "z-40 h-[var(--bx-header-height,var(--blux-header-height))] w-full border-b border-(--blux-border) bg-(--blux-bg) text-(--blux-text)")
            .Slot("container", "mx-auto flex h-full w-full max-w-(--blux-container) items-center gap-4 px-4 sm:px-6 lg:px-8")
            .Slot("left", "flex min-w-0 items-center gap-2 lg:flex-1")
            .Slot("title", "inline-flex items-center gap-2 rounded-sm text-lg font-semibold text-(--blux-text-highlighted) " + LayoutFocus)
            .Slot("center", "hidden lg:flex")
            .Slot("right", "ms-auto flex items-center gap-1.5 lg:flex-1 lg:justify-end")
            .Slot("menu", "lg:hidden")
            .Variant("sticky", "true", "root", "sticky top-0")
            .Default("sticky", "true");

    public static ComponentTheme Footer() =>
        new ComponentTheme("footer", "root", "top", "container", "left", "center", "right", "bottom")
            .Slot("root", "w-full border-t border-(--blux-border) bg-(--blux-bg) text-(--blux-text-muted)")
            .Slot("top", "mx-auto w-full max-w-(--blux-container) border-b border-(--blux-border) px-4 py-8 sm:px-6 lg:px-8 lg:py-12")
            .Slot("container", "mx-auto flex w-full max-w-(--blux-container) flex-col items-center gap-3 px-4 py-8 text-sm sm:px-6 lg:flex-row lg:px-8")
            .Slot("left", "flex items-center justify-center gap-1.5 lg:order-1 lg:flex-1 lg:justify-start")
            .Slot("center", "flex items-center justify-center gap-1.5 lg:order-2")
            .Slot("right", "flex items-center justify-center gap-1.5 lg:order-3 lg:flex-1 lg:justify-end")
            .Slot("bottom", "mx-auto w-full max-w-(--blux-container) border-t border-(--blux-border) px-4 py-8 sm:px-6 lg:px-8");

    public static ComponentTheme FooterColumns() =>
        new ComponentTheme("footerColumns", "root", "left", "nav", "column", "label", "list", "item", "link", "icon", "right")
            .Slot("root", "xl:grid xl:grid-cols-3 xl:gap-8")
            .Slot("left", "mb-10 xl:mb-0")
            .Slot("nav", "grid grid-cols-2 gap-8 md:grid-cols-3 xl:col-span-2")
            .Slot("column", "min-w-0")
            .Slot("label", "text-sm font-semibold text-(--blux-text-highlighted)")
            .Slot("list", "mt-6 space-y-4")
            .Slot("item", "relative")
            .Slot("link", "inline-flex items-center gap-1.5 rounded-sm text-sm text-(--blux-text-muted) transition-colors hover:text-(--blux-text) " +
                          "aria-[current=page]:font-medium aria-[current=page]:text-(--blux-text) " + LayoutFocus)
            .Slot("icon", "size-4 shrink-0")
            .Slot("right", "mt-10 xl:mt-0");

    public static ComponentTheme Main() =>
        new ComponentTheme("main", "base")
            .Slot("base", "min-h-[calc(100dvh_-_var(--bx-header-height,var(--blux-header-height)))] focus:outline-none");

    /// <summary>
    /// The aside carries <c>group/sidebar</c> and <c>data-state</c>, so anything inside it can react to the collapsed
    /// state with <c>group-data-[state=collapsed]/sidebar:</c> variants. The mobile copy lives outside the aside and
    /// is never collapsed.
    /// </summary>
    public static ComponentTheme Sidebar() =>
        new ComponentTheme("sidebar", "root", "host", "header", "headerContent", "toggle", "toggleIcon", "content", "footer", "handle", "mobile", "mobileBody")
            .Slot("root", "group/sidebar relative hidden w-(--bx-sidebar-width) shrink-0 flex-col border-e border-(--blux-border) bg-(--blux-bg) text-(--blux-text) " +
                          "data-ready:transition-[width] data-ready:duration-200 data-ready:motion-reduce:transition-none lg:flex " +
                          "data-[state=collapsed]:w-16 data-ready:data-dragging:transition-none")
            .Slot("host", "contents")
            .Slot("header", "flex h-16 shrink-0 items-center gap-2 border-b border-(--blux-border) px-4 " +
                            "group-data-[state=collapsed]/sidebar:justify-center group-data-[state=collapsed]/sidebar:px-2")
            .Slot("headerContent", "min-w-0 flex-1 group-data-[state=collapsed]/sidebar:hidden")
            .Slot("toggle", LayoutIconButton)
            .Slot("toggleIcon", "size-5 rtl:-scale-x-100 group-data-[state=collapsed]/sidebar:rotate-180")
            .Slot("content", "flex min-h-0 flex-1 flex-col gap-2 overflow-y-auto p-4 group-data-[state=collapsed]/sidebar:px-2")
            .Slot("footer", "flex shrink-0 items-center gap-1.5 border-t border-(--blux-border) p-4 " +
                            "group-data-[state=collapsed]/sidebar:justify-center group-data-[state=collapsed]/sidebar:px-2")
            .Slot("handle", "absolute inset-y-0 end-0 z-10 w-1.5 translate-x-1/2 cursor-col-resize touch-none rounded-full transition-colors rtl:-translate-x-1/2 " +
                            "hover:bg-(--blux-border-accented) data-dragging:bg-(--blux-border-accented) " + LayoutFocus + " " +
                            "group-data-[state=collapsed]/sidebar:hidden")
            .Slot("mobile", "p-2 lg:hidden")
            .Slot("mobileBody", "flex min-h-full flex-col");

    public static ComponentTheme Error() =>
        new ComponentTheme("error", "root", "statusCode", "statusMessage", "message", "actions")
            .Slot("root", "flex flex-col items-center justify-center gap-3 px-4 py-16 text-center")
            .Slot("statusCode", "text-base font-semibold text-(--blux-primary-text)")
            .Slot("statusMessage", "text-4xl font-bold tracking-tight text-balance text-(--blux-text-highlighted) sm:text-5xl")
            .Slot("message", "max-w-prose text-base text-(--blux-text-muted)")
            .Slot("actions", "mt-6 flex flex-wrap items-center justify-center gap-3");

    public static ComponentTheme LocaleSelect() =>
        new ComponentTheme("localeSelect", "root")
            .Slot("root", "inline-block min-w-40");
}
