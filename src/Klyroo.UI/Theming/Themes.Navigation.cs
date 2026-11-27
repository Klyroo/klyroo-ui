namespace Klyroo.UI.Theming;

internal static partial class Themes
{
    public static ComponentTheme Tabs() =>
        new ComponentTheme("tabs", "root", "list", "trigger", "icon", "label", "content")
            .Slot("root", "flex flex-col gap-2")
            .Slot("list", "relative flex overflow-x-auto")
            .Slot("trigger", "inline-flex flex-1 items-center justify-center gap-1.5 whitespace-nowrap font-medium transition-colors " +
                             "disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-(--kly-color-text)")
            .Slot("icon", "size-4 shrink-0")
            .Slot("label", "")
            .Slot("content", "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--kly-color-text)")
            .Variant("variant", "pill", "list", "gap-1 rounded-[calc(var(--klyroo-radius)_+_2px)] bg-(--klyroo-bg-elevated) p-1")
            .Variant("variant", "pill", "trigger", "rounded-(--klyroo-radius) px-3 py-1.5 text-sm text-(--klyroo-text-muted) hover:text-(--klyroo-text) " +
                                                   "aria-selected:bg-(--klyroo-bg) aria-selected:text-(--klyroo-text) aria-selected:shadow-xs aria-selected:ring aria-selected:ring-inset aria-selected:ring-(--klyroo-text-muted)")
            .Variant("variant", "link", "list", "gap-1 border-b border-(--klyroo-border)")
            .Variant("variant", "link", "trigger", "-mb-px border-b-2 border-transparent px-3 py-2 text-sm text-(--klyroo-text-muted) hover:text-(--klyroo-text) " +
                                                   "aria-selected:border-(--kly-color-text) aria-selected:text-(--kly-color-text)")
            .Variant("orientation", "vertical", "root", "flex-row")
            .Variant("orientation", "vertical", "list", "flex-col self-start")
            .Default("variant", "pill")
            .Default("orientation", "horizontal");

    public static ComponentTheme Accordion() =>
        new ComponentTheme("accordion", "root", "item", "header", "trigger", "leadingIcon", "label", "trailingIcon", "content", "body")
            .Slot("root", "w-full")
            .Slot("item", "group/item border-b border-(--klyroo-border) last:border-b-0")
            .Slot("header", "flex")
            .Slot("trigger", "flex min-w-0 flex-1 items-center gap-1.5 py-3.5 text-start text-sm font-medium text-(--klyroo-text) " +
                             "disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-(--klyroo-primary-text)")
            .Slot("leadingIcon", "size-5 shrink-0")
            .Slot("label", "text-start break-words")
            .Slot("trailingIcon", "ms-auto size-5 shrink-0 text-(--klyroo-text-muted) transition-transform duration-200 group-data-[state=open]/item:rotate-180")
            .Slot("content", "")
            .Slot("body", "pb-3.5 text-sm text-(--klyroo-text-muted)");

    public static ComponentTheme Table() =>
        new ComponentTheme("table", "root", "base", "caption", "thead", "tr", "th", "sortButton", "sortIcon", "tbody", "td", "empty", "tfoot", "tf", "cellEnd", "cellNumeric", "cellMono", "more", "moreIcon")
            .Slot("root", "relative overflow-auto")
            .Slot("base", "min-w-full text-sm")
            .Slot("caption", "sr-only")
            .Slot("thead", "")
            .Slot("tr", "border-b border-(--klyroo-border) last:border-b-0")
            .Slot("th", "whitespace-nowrap px-4 py-3.5 text-start font-semibold text-(--klyroo-text)")
            .Slot("sortButton", "-mx-1 inline-flex items-center gap-1 rounded-(--klyroo-radius) px-1 font-semibold hover:text-(--klyroo-text-muted) " +
                                "disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-(--klyroo-primary-text)")
            .Slot("sortIcon", "size-4 shrink-0 text-(--klyroo-text-dimmed)")
            .Slot("tbody", "")
            .Slot("td", "whitespace-nowrap px-4 py-4 text-(--klyroo-text-muted)")
            .Slot("empty", "py-6 text-center text-(--klyroo-text-muted)")
            .Slot("tfoot", "border-t border-(--klyroo-border-accented)")
            .Slot("tf", "whitespace-nowrap px-4 py-3.5 text-start font-semibold text-(--klyroo-text-highlighted)")
            .Slot("cellEnd", "text-end")
            .Slot("cellNumeric", "text-end tabular-nums")
            .Slot("cellMono", "font-mono")
            .Slot("more", "m-1 inline-flex items-center gap-1.5 rounded-(--klyroo-radius) px-2 py-1 text-xs font-medium text-(--klyroo-text-muted) hover:bg-(--klyroo-bg-muted) hover:text-(--klyroo-text) disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-(--klyroo-primary-text)")
            .Slot("moreIcon", "size-3.5")
            .Variant("sticky", "true", "thead", "sticky top-0 z-[1] bg-(--klyroo-bg)/75 backdrop-blur")
            .Default("sticky", "false");
}
