namespace Blux.UI.Theming;

internal static partial class Themes
{
    public static ComponentTheme Tabs() =>
        new ComponentTheme("tabs", "root", "list", "trigger", "icon", "label", "content")
            .Slot("root", "flex flex-col gap-2")
            .Slot("list", "relative flex")
            .Slot("trigger", "inline-flex min-w-0 flex-1 items-center justify-center gap-1.5 whitespace-nowrap font-medium transition-colors " +
                             "disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-(--bx-color-text)")
            .Slot("icon", "size-4 shrink-0")
            .Slot("label", "truncate")
            .Slot("content", "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--bx-color-text)")
            .Variant("variant", "pill", "list", "gap-1 rounded-[calc(var(--blux-radius)_+_2px)] bg-(--blux-bg-elevated) p-1")
            .Variant("variant", "pill", "trigger", "rounded-(--blux-radius) px-3 py-1.5 text-sm text-(--blux-text-muted) hover:text-(--blux-text) " +
                                                   "aria-selected:bg-(--blux-bg) aria-selected:text-(--blux-text) aria-selected:shadow-xs")
            .Variant("variant", "link", "list", "gap-1 border-b border-(--blux-border)")
            .Variant("variant", "link", "trigger", "-mb-px border-b-2 border-transparent px-3 py-2 text-sm text-(--blux-text-muted) hover:text-(--blux-text) " +
                                                   "aria-selected:border-(--bx-color) aria-selected:text-(--bx-color-text)")
            .Variant("orientation", "vertical", "root", "flex-row")
            .Variant("orientation", "vertical", "list", "flex-col self-start")
            .Default("variant", "pill")
            .Default("orientation", "horizontal");

    public static ComponentTheme Accordion() =>
        new ComponentTheme("accordion", "root", "item", "header", "trigger", "leadingIcon", "label", "trailingIcon", "content", "body")
            .Slot("root", "w-full")
            .Slot("item", "group/item border-b border-(--blux-border) last:border-b-0")
            .Slot("header", "flex")
            .Slot("trigger", "flex min-w-0 flex-1 items-center gap-1.5 py-3.5 text-start text-sm font-medium text-(--blux-text) " +
                             "disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-(--blux-primary-text)")
            .Slot("leadingIcon", "size-5 shrink-0")
            .Slot("label", "text-start break-words")
            .Slot("trailingIcon", "ms-auto size-5 shrink-0 text-(--blux-text-muted) transition-transform duration-200 group-data-[state=open]/item:rotate-180")
            .Slot("content", "")
            .Slot("body", "pb-3.5 text-sm text-(--blux-text-muted)");

    public static ComponentTheme Table() =>
        new ComponentTheme("table", "root", "base", "caption", "thead", "tr", "th", "sortButton", "sortIcon", "tbody", "td", "empty")
            .Slot("root", "relative overflow-auto")
            .Slot("base", "min-w-full text-sm")
            .Slot("caption", "sr-only")
            .Slot("thead", "")
            .Slot("tr", "border-b border-(--blux-border) last:border-b-0")
            .Slot("th", "whitespace-nowrap px-4 py-3.5 text-start font-semibold text-(--blux-text)")
            .Slot("sortButton", "-mx-1 inline-flex items-center gap-1 rounded-(--blux-radius) px-1 font-semibold hover:text-(--blux-text-muted) " +
                                "disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-(--blux-primary-text)")
            .Slot("sortIcon", "size-4 shrink-0 text-(--blux-text-dimmed)")
            .Slot("tbody", "")
            .Slot("td", "whitespace-nowrap px-4 py-4 text-(--blux-text-muted)")
            .Slot("empty", "py-6 text-center text-(--blux-text-muted)")
            .Variant("sticky", "true", "thead", "sticky top-0 z-[1] bg-(--blux-bg)/75 backdrop-blur")
            .Default("sticky", "false");
}
