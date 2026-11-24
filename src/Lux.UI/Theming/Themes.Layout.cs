namespace Lux.UI.Theming;

/// <summary>Themes of the layout family: app, header, footer, footer columns, main, skip link, sidebar, error, locale select.</summary>
internal static partial class Themes
{
    private const string LayoutFocus =
        "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--lux-primary-text)";

    private const string LayoutIconButton =
        "inline-flex size-8 shrink-0 items-center justify-center rounded-(--lux-radius) text-(--lux-text-muted) transition-colors " +
        "hover:bg-(--lux-bg-elevated) hover:text-(--lux-text) " + LayoutFocus;

    public static ComponentTheme App() =>
        new ComponentTheme("app", "root")
            .Slot("root", "isolate");

    /// <summary>Header height comes from <c>--lx-header-height</c> or <c>--lux-header-height</c> (4rem by default), read at the element, which <see cref="Main"/> subtracts from the viewport.</summary>
    public static ComponentTheme Header() =>
        new ComponentTheme("header", "root", "container", "left", "title", "center", "right", "menu")
            .Slot("root", "z-40 h-[calc(var(--lx-header-height,var(--lux-header-height))_+_var(--lx-safe-top))] w-full pt-(--lx-safe-top) ps-(--lx-safe-left) pe-(--lx-safe-right) border-b border-(--lux-border) bg-(--lux-bg) text-(--lux-text)")
            .Slot("container", "mx-auto flex h-full w-full max-w-(--lux-container) items-center gap-4 px-4 sm:px-6 lg:px-8")
            .Slot("left", "flex min-w-0 items-center gap-2 lg:flex-1")
            .Slot("title", "inline-flex items-center gap-2 rounded-sm text-lg font-semibold text-(--lux-text-highlighted) " + LayoutFocus)
            .Slot("center", "hidden lg:flex")
            .Slot("right", "ms-auto flex items-center gap-1.5 lg:flex-1 lg:justify-end")
            .Slot("menu", "lg:hidden")
            .Variant("sticky", "true", "root", "sticky top-0")
            .Default("sticky", "true");

    public static ComponentTheme Footer() =>
        new ComponentTheme("footer", "root", "top", "container", "left", "center", "right", "bottom")
            .Slot("root", "w-full border-t border-(--lux-border) bg-(--lux-bg) pb-(--lx-safe-bottom) ps-(--lx-safe-left) pe-(--lx-safe-right) text-(--lux-text-muted)")
            .Slot("top", "mx-auto w-full max-w-(--lux-container) border-b border-(--lux-border) px-4 py-8 sm:px-6 lg:px-8 lg:py-12")
            .Slot("container", "mx-auto flex w-full max-w-(--lux-container) flex-col items-center gap-3 px-4 py-8 text-sm sm:px-6 lg:flex-row lg:px-8")
            .Slot("left", "flex items-center justify-center gap-1.5 lg:order-1 lg:flex-1 lg:justify-start")
            .Slot("center", "flex items-center justify-center gap-1.5 lg:order-2")
            .Slot("right", "flex items-center justify-center gap-1.5 lg:order-3 lg:flex-1 lg:justify-end")
            .Slot("bottom", "mx-auto w-full max-w-(--lux-container) border-t border-(--lux-border) px-4 py-8 sm:px-6 lg:px-8");

    public static ComponentTheme FooterColumns() =>
        new ComponentTheme("footerColumns", "root", "left", "nav", "column", "label", "list", "item", "link", "icon", "right")
            .Slot("root", "xl:grid xl:grid-cols-3 xl:gap-8")
            .Slot("left", "mb-10 xl:mb-0")
            .Slot("nav", "grid grid-cols-2 gap-8 md:grid-cols-3 xl:col-span-2")
            .Slot("column", "min-w-0")
            .Slot("label", "text-sm font-semibold text-(--lux-text-highlighted)")
            .Slot("list", "mt-6 space-y-4")
            .Slot("item", "relative")
            .Slot("link", "inline-flex items-center gap-1.5 rounded-sm text-sm text-(--lux-text-muted) transition-colors hover:text-(--lux-text) " +
                          "aria-[current=page]:font-medium aria-[current=page]:text-(--lux-text) " + LayoutFocus)
            .Slot("icon", "size-4 shrink-0")
            .Slot("right", "mt-10 xl:mt-0");

    public static ComponentTheme Main() =>
        new ComponentTheme("main", "base")
            // The bottom inset is applied once, at the outermost bottom edge: here, unless a footer follows (it takes it). The TV
            // overscan margins (--lx-tv-inset, set on html in TV mode) are taken from the minimum height so the page does not always scroll.
            .Slot("base", "min-h-[calc(100dvh_-_var(--lx-header-height,var(--lux-header-height))_-_var(--lx-safe-top)_-_var(--lx-tv-inset,0px))] pb-(--lx-safe-bottom) has-[~footer]:pb-0 focus:outline-none");

    /// <summary>Hidden (not removed from the tab order) until it has the focus, then pinned to the top start corner of the screen above the page.</summary>
    public static ComponentTheme SkipLink() =>
        new ComponentTheme("skipLink", "root")
            .Slot("root", "sr-only focus:not-sr-only focus:fixed focus:start-4 focus:top-4 focus:z-50 focus:rounded-(--lux-radius) focus:bg-(--lux-bg) focus:px-3 focus:py-2 " +
                          "focus:text-sm focus:font-medium focus:text-(--lux-text-highlighted) focus:shadow-md focus:ring-2 focus:ring-(--lux-primary-text) focus:outline-none");

    /// <summary>
    /// The aside carries <c>group/sidebar</c> and <c>data-state</c>, so anything inside it can react to the collapsed
    /// state with <c>group-data-[state=collapsed]/sidebar:</c> variants; below lg the browser keeps it "expanded". The aside is a popover:
    /// from lg up it is shown as a column whatever the popover says (the UA rule that hides a closed popover is overridden), below lg it is
    /// hidden until it is open and then a fixed panel on the start edge, with a backdrop and a slide-in.
    /// </summary>
    public static ComponentTheme Sidebar() =>
        new ComponentTheme("sidebar", "root", "host", "panel", "header", "headerContent", "toggle", "toggleIcon", "content", "footer", "handle", "mobile", "mobileHeader", "mobileTitle", "mobileClose")
            .Slot("root", "group/sidebar relative m-0 flex-col border-0 border-e border-(--lux-border) bg-(--lux-bg) p-0 text-(--lux-text) " +
                          // From lg: the column.
                          "lg:flex lg:h-auto lg:w-(--lx-sidebar-width) lg:shrink-0 lg:overflow-visible lg:data-[state=collapsed]:w-16 " +
                          "lg:data-ready:transition-[width] lg:data-ready:duration-200 lg:data-ready:motion-reduce:transition-none lg:data-ready:data-dragging:transition-none " +
                          // Below lg: the panel, once it is open.
                          "max-lg:open:fixed max-lg:open:inset-y-0 max-lg:open:start-0 max-lg:open:end-auto max-lg:open:flex max-lg:open:h-dvh max-lg:open:w-80 " +
                          "max-lg:open:max-w-[85vw] max-lg:open:overflow-hidden max-lg:open:shadow-xl max-lg:open:translate-x-0 " +
                          "max-lg:open:starting:-translate-x-full max-lg:open:starting:rtl:translate-x-full max-lg:open:transition-[translate] " +
                          "max-lg:open:duration-200 motion-reduce:max-lg:open:transition-none backdrop:bg-zinc-950/60")
            .Slot("host", "contents")
            .Slot("panel", "flex min-h-0 flex-1 flex-col")
            .Slot("header", "flex h-16 shrink-0 items-center gap-2 border-b border-(--lux-border) px-4 " +
                            "lg:group-data-[state=collapsed]/sidebar:justify-center lg:group-data-[state=collapsed]/sidebar:px-2")
            .Slot("headerContent", "min-w-0 flex-1 lg:group-data-[state=collapsed]/sidebar:hidden")
            .Slot("toggle", LayoutIconButton + " max-lg:hidden")
            .Slot("toggleIcon", "size-5 rtl:-scale-x-100 lg:group-data-[state=collapsed]/sidebar:rotate-180")
            .Slot("content", "flex min-h-0 flex-1 flex-col gap-2 overflow-y-auto p-4 lg:group-data-[state=collapsed]/sidebar:px-2")
            .Slot("footer", "flex shrink-0 items-center gap-1.5 border-t border-(--lux-border) p-4 " +
                            "lg:group-data-[state=collapsed]/sidebar:justify-center lg:group-data-[state=collapsed]/sidebar:px-2")
            .Slot("handle", "absolute inset-y-0 end-0 z-10 w-1.5 translate-x-1/2 cursor-col-resize touch-none rounded-full transition-colors rtl:-translate-x-1/2 " +
                            "hover:bg-(--lux-border-accented) data-dragging:bg-(--lux-border-accented) " + LayoutFocus + " " +
                            "max-lg:hidden lg:group-data-[state=collapsed]/sidebar:hidden")
            .Slot("mobile", "p-2 lg:hidden")
            .Slot("mobileHeader", "flex h-16 shrink-0 items-center justify-between gap-2 border-b border-(--lux-border) px-4 lg:hidden")
            .Slot("mobileTitle", "min-w-0 flex-1 truncate text-base font-semibold text-(--lux-text-highlighted)")
            .Slot("mobileClose", "rounded-(--lux-radius) p-1 text-(--lux-text-muted) hover:bg-(--lux-bg-elevated) hover:text-(--lux-text) " +
                                 "focus-visible:outline-2 focus-visible:outline-(--lux-primary-text)");

    public static ComponentTheme Error() =>
        new ComponentTheme("error", "root", "statusCode", "statusMessage", "message", "actions")
            .Slot("root", "flex flex-col items-center justify-center gap-3 px-4 py-16 text-center")
            .Slot("statusCode", "text-base font-semibold text-(--lux-primary-text)")
            .Slot("statusMessage", "text-4xl font-bold tracking-tight text-balance text-(--lux-text-highlighted) sm:text-5xl")
            .Slot("message", "max-w-prose text-base text-(--lux-text-muted)")
            .Slot("actions", "mt-6 flex flex-wrap items-center justify-center gap-3");

    public static ComponentTheme LocaleSelect() =>
        new ComponentTheme("localeSelect", "root")
            .Slot("root", "inline-block min-w-40");
}
