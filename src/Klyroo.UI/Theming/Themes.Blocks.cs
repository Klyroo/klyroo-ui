namespace Klyroo.UI.Theming;

// Themes of the read-only blocks: KlyHeading, KlyText, KlyStat, KlyStatGroup, KlyFacts, KlyBreakdown, KlyMeter, KlyDiff,
// KlyRecordList, KlyList, KlyDateTile and KlyImageGallery. Colors never appear here: a tone is a data-color attribute that sets --kly-color*.
internal static partial class Themes
{
    private const string BlockRoot = "flex w-full min-w-0 flex-col gap-3 text-sm text-(--klyroo-text)";

    private const string BlockMore =
        "inline-flex items-center gap-1.5 self-start rounded-(--klyroo-radius) px-2 py-1 text-xs font-medium text-(--klyroo-text-muted) " +
        "hover:bg-(--klyroo-bg-muted) hover:text-(--klyroo-text) disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-(--klyroo-primary-text)";

    private const string BlockCopy =
        "inline-flex size-6 shrink-0 items-center justify-center rounded-(--klyroo-radius) text-(--klyroo-text-muted) hover:bg-(--klyroo-bg-muted) " +
        "hover:text-(--klyroo-text) disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-(--klyroo-primary-text)";

    private const string BlockToggle =
        "inline-flex items-center gap-1 self-start rounded-(--klyroo-radius) text-xs font-medium text-(--klyroo-primary-text) hover:underline " +
        "disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-(--klyroo-primary-text)";

    // The four surfaces a block can have, on its root slot.
    private static ComponentTheme Surfaces(ComponentTheme theme, string defaultSurface) => theme
        .Variant("surface", "muted", "root", "rounded-(--klyroo-radius) bg-(--klyroo-bg-muted) p-4")
        .Variant("surface", "bordered", "root", "rounded-(--klyroo-radius) border border-(--klyroo-border) bg-(--klyroo-bg) p-4")
        .Variant("surface", "elevated", "root", "rounded-(--klyroo-radius) border border-(--klyroo-border-accented) bg-(--klyroo-bg-elevated) p-4 shadow-sm")
        .Default("surface", defaultSurface);

    public static ComponentTheme Heading() => Surfaces(
        new ComponentTheme("heading", "root", "lead", "icon", "text", "titleRow", "title", "count", "description", "skeleton")
            .Slot("root", "flex w-full min-w-0 flex-wrap items-center justify-between gap-2 text-sm text-(--klyroo-text)")
            .Slot("lead", "flex min-w-0 items-center gap-2.5")
            .Slot("icon", "inline-flex size-8 shrink-0 items-center justify-center rounded-(--klyroo-radius) bg-(--klyroo-bg-muted) p-2 text-(--klyroo-primary-text)")
            .Slot("text", "flex min-w-0 flex-col gap-0.5")
            .Slot("titleRow", "flex items-center gap-2")
            .Slot("title", "m-0 truncate text-base font-semibold text-(--klyroo-text-highlighted)")
            .Slot("count", "inline-flex items-center rounded-full border border-(--klyroo-border-accented) px-2 py-0.5 text-xs font-medium tabular-nums text-(--klyroo-text-muted)")
            .Slot("description", "m-0 text-xs text-(--klyroo-text-muted)")
            .Slot("skeleton", "flex flex-col gap-2.5"), "plain");

    public static ComponentTheme Text() => Surfaces(
        new ComponentTheme("text", "root", "title", "paragraph", "body", "toggle", "footer", "skeleton")
            .Slot("root", "text-sm text-(--klyroo-text)")
            .Slot("title", "m-0 font-semibold text-(--klyroo-text-highlighted)")
            .Slot("paragraph", "m-0 text-(--klyroo-text)")
            .Slot("body", "m-0")
            .Slot("toggle", "self-start rounded-(--klyroo-radius) text-xs font-medium text-(--klyroo-primary-text) hover:underline disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-(--klyroo-primary-text)")
            .Slot("footer", "text-xs not-italic text-(--klyroo-text-muted)")
            .Slot("skeleton", "flex flex-col gap-2.5")
            .Variant("variant", "default", "root", "flex flex-col gap-2")
            .Variant("variant", "quote", "root", "flex flex-col gap-2 border-s-2 border-(--klyroo-primary-text) ps-3 italic")
            .Variant("clamped", "true", "body", "line-clamp-2")
            .Default("variant", "default")
            .Default("clamped", "false"), "plain");

    public static ComponentTheme Stat() => Surfaces(
        new ComponentTheme("stat", "root", "label", "row", "valueGroup", "value", "unit", "delta", "sparkline", "caption", "skeleton")
            .Slot("root", "flex w-full min-w-0 flex-col gap-2 text-sm text-(--klyroo-text)")
            .Slot("label", "text-xs font-medium text-(--klyroo-text-muted)")
            .Slot("row", "flex flex-wrap items-baseline justify-between gap-2")
            .Slot("valueGroup", "flex items-baseline gap-1.5")
            .Slot("value", "text-2xl font-bold tabular-nums text-(--klyroo-text-highlighted)")
            .Slot("unit", "text-xs font-medium text-(--klyroo-text-muted)")
            .Slot("delta", "")
            .Slot("sparkline", "h-8 w-full overflow-visible text-(--klyroo-primary-text)")
            .Slot("caption", "text-xs text-(--klyroo-text-muted)")
            .Slot("skeleton", "flex flex-col gap-2.5"), "muted");

    public static ComponentTheme StatGroup() => Surfaces(
        new ComponentTheme("statGroup", "root", "title", "grid", "item", "label", "value", "caption", "skeleton")
            .Slot("root", "flex w-full min-w-0 flex-col gap-2 text-sm text-(--klyroo-text)")
            .Slot("title", "text-xs font-semibold text-(--klyroo-text-highlighted)")
            .Slot("grid", "grid grid-cols-1 gap-4 sm:grid-cols-3")
            .Slot("item", "flex flex-col gap-0.5")
            .Slot("label", "text-xs font-medium text-(--klyroo-text-muted)")
            .Slot("value", "text-xl font-bold tabular-nums text-(--klyroo-text-highlighted) data-[color]:text-(--kly-color-text)")
            .Slot("caption", "text-xs text-(--klyroo-text-muted)")
            .Slot("skeleton", "flex flex-col gap-2.5"), "bordered");

    public static ComponentTheme Facts() => Surfaces(
        new ComponentTheme("facts", "root", "group", "groupHeader", "groupTitle", "groupAside", "list", "item", "label", "labelRow", "labelIcon", "hint",
                "value", "valueMono", "valueEmphasis", "valueSkeleton", "empty", "link", "copy", "copyIcon", "more", "moreIcon", "none", "skeleton")
            .Slot("root", BlockRoot)
            .Slot("group", "flex flex-col gap-2")
            .Slot("groupHeader", "flex items-center justify-between gap-2 border-b border-(--klyroo-border) pb-1")
            .Slot("groupTitle", "text-xs font-semibold text-(--klyroo-text-highlighted)")
            .Slot("groupAside", "text-xs text-(--klyroo-text-muted)")
            .Slot("list", "m-0 divide-y divide-(--klyroo-border)")
            .Slot("item", "flex items-center justify-between gap-3 py-2 first:pt-0 last:pb-0")
            .Slot("label", "flex min-w-0 flex-col gap-0.5 text-xs font-medium text-(--klyroo-text-muted)")
            .Slot("labelRow", "flex items-center gap-1.5")
            .Slot("labelIcon", "size-3.5 shrink-0")
            .Slot("hint", "font-normal text-(--klyroo-text-dimmed)")
            .Slot("value", "m-0 flex items-center gap-1.5 text-end text-(--klyroo-text) data-[color]:text-(--kly-color-text)")
            .Slot("valueMono", "font-mono")
            .Slot("valueEmphasis", "font-semibold text-(--klyroo-text-highlighted)")
            .Slot("valueSkeleton", "h-4 w-20")
            .Slot("empty", "text-(--klyroo-text-dimmed)")
            .Slot("link", "text-(--klyroo-primary-text) hover:underline")
            .Slot("copy", BlockCopy)
            .Slot("copyIcon", "size-3.5")
            .Slot("more", BlockMore)
            .Slot("moreIcon", "size-3.5")
            .Slot("none", "m-0 text-xs text-(--klyroo-text-muted)")
            .Slot("skeleton", "flex flex-col gap-2.5"), "bordered");

    public static ComponentTheme Breakdown() => Surfaces(
        new ComponentTheme("breakdown", "root", "title", "toggle", "toggleIcon", "columns", "columnHeader", "lines", "row", "subtotal", "muted", "total",
                "label", "labelRow", "detail", "amount", "compareAmount", "footnote", "skeleton")
            .Slot("root", "flex w-full min-w-0 flex-col gap-2 text-sm text-(--klyroo-text)")
            .Slot("title", "m-0 font-semibold text-(--klyroo-text-highlighted)")
            .Slot("toggle", BlockToggle)
            .Slot("toggleIcon", "size-3.5")
            .Slot("columns", "grid grid-cols-[minmax(0,1fr)_6rem_6rem] gap-2 text-xs font-medium text-(--klyroo-text-muted)")
            .Slot("columnHeader", "text-end")
            .Slot("lines", "flex flex-col gap-1.5")
            .Slot("row", "flex items-baseline justify-between gap-2")
            .Slot("subtotal", "border-t border-(--klyroo-border) pt-1.5 font-semibold text-(--klyroo-text-highlighted)")
            .Slot("muted", "text-(--klyroo-text-dimmed)")
            .Slot("total", "border-t border-(--klyroo-border-accented) pt-2 font-semibold text-(--klyroo-text-highlighted)")
            .Slot("label", "flex min-w-0 flex-col")
            .Slot("labelRow", "inline-flex flex-wrap items-center gap-1.5")
            .Slot("detail", "text-xs font-normal text-(--klyroo-text-muted)")
            .Slot("amount", "text-end tabular-nums data-[color]:text-(--kly-color-text)")
            .Slot("compareAmount", "text-end tabular-nums text-(--klyroo-text-muted)")
            .Slot("footnote", "m-0 text-xs text-(--klyroo-text-muted)")
            .Slot("skeleton", "flex flex-col gap-2.5")
            .Variant("compare", "true", "row", "grid grid-cols-[minmax(0,1fr)_6rem_6rem] items-baseline gap-2"), "bordered");

    public static ComponentTheme Diff() => Surfaces(
        new ComponentTheme("diff", "root", "title", "list", "item", "label", "values", "before", "arrow", "after", "unchanged", "skeleton")
            .Slot("root", "flex w-full min-w-0 flex-col gap-2 text-sm text-(--klyroo-text)")
            .Slot("title", "m-0 font-semibold text-(--klyroo-text-highlighted)")
            .Slot("list", "divide-y divide-(--klyroo-border)")
            .Slot("item", "flex flex-wrap items-center justify-between gap-2 py-2 first:pt-0 last:pb-0")
            .Slot("label", "inline-flex items-baseline gap-2 text-xs font-medium text-(--klyroo-text-muted)")
            .Slot("values", "flex items-center gap-2 text-xs")
            .Slot("before", "line-through text-(--klyroo-error-text)")
            .Slot("arrow", "text-(--klyroo-text-muted)")
            .Slot("after", "font-semibold no-underline text-(--klyroo-success-text)")
            .Slot("unchanged", "text-(--klyroo-text-muted)")
            .Slot("skeleton", "flex flex-col gap-2.5"), "plain");

    public static ComponentTheme RecordList() => Surfaces(
        new ComponentTheme("recordList", "root", "header", "title", "count", "item", "lead", "image", "avatar", "avatarIcon", "body", "reference", "name", "link",
                "meta", "metaIcon", "metaText", "trail", "trailing", "trailingBlock", "trailingCaption", "more", "moreIcon", "skeleton")
            .Slot("root", "flex w-full min-w-0 flex-col gap-2.5 text-sm text-(--klyroo-text)")
            .Slot("header", "flex items-center justify-between gap-2")
            .Slot("title", "font-semibold text-(--klyroo-text-highlighted)")
            .Slot("count", "text-xs tabular-nums text-(--klyroo-text-muted)")
            .Slot("item", "flex items-center justify-between gap-3 py-1.5")
            .Slot("lead", "flex min-w-0 items-center gap-3")
            .Slot("image", "size-8 shrink-0 rounded-(--klyroo-radius) object-cover")
            .Slot("avatar", "inline-flex size-8 shrink-0 items-center justify-center rounded-(--klyroo-radius) bg-(--klyroo-bg-muted) text-xs font-semibold text-(--klyroo-text-highlighted) data-[color]:bg-(--kly-color)/15 data-[color]:text-(--kly-color-text)")
            .Slot("avatarIcon", "size-4")
            .Slot("body", "flex min-w-0 flex-col")
            .Slot("reference", "text-[11px] font-mono text-(--klyroo-text-muted)")
            .Slot("name", "truncate font-medium text-(--klyroo-text-highlighted)")
            .Slot("link", "truncate text-start font-medium text-(--klyroo-text-highlighted) hover:underline focus-visible:outline-2 focus-visible:outline-(--klyroo-primary-text)")
            .Slot("meta", "flex min-w-0 items-center gap-1 text-xs text-(--klyroo-text-muted)")
            .Slot("metaIcon", "size-3 shrink-0")
            .Slot("metaText", "truncate")
            .Slot("trail", "flex shrink-0 items-center gap-2")
            .Slot("trailing", "font-semibold tabular-nums text-(--klyroo-text-highlighted) data-[color]:text-(--kly-color-text)")
            .Slot("trailingBlock", "flex flex-col items-end")
            .Slot("trailingCaption", "text-xs text-(--klyroo-text-muted)")
            .Slot("more", BlockMore)
            .Slot("moreIcon", "size-3.5")
            .Slot("skeleton", "flex flex-col gap-2.5"), "bordered");

    public static ComponentTheme List() => Surfaces(
        new ComponentTheme("list", "root", "title", "list", "item", "lead", "marker", "markerIcon", "icon", "bullet", "body", "text", "link", "description", "meta", "more", "moreIcon", "skeleton")
            .Slot("root", "flex w-full min-w-0 flex-col gap-2 text-sm text-(--klyroo-text)")
            .Slot("title", "m-0 font-semibold text-(--klyroo-text-highlighted)")
            .Slot("list", "m-0 flex list-none flex-col gap-2 p-0")
            .Slot("item", "flex items-center justify-between gap-3")
            .Slot("lead", "flex min-w-0 items-center gap-2.5")
            .Slot("marker", "inline-flex size-5 shrink-0 items-center justify-center rounded-full bg-(--klyroo-bg-muted) text-xs font-semibold text-(--klyroo-text-muted) " +
                            "data-[state=done]:bg-(--klyroo-success)/15 data-[state=done]:text-(--klyroo-success-text) data-[state=current]:bg-(--klyroo-primary) data-[state=current]:text-(--klyroo-primary-contrast)")
            .Slot("markerIcon", "size-3")
            .Slot("icon", "inline-flex size-4 shrink-0 text-(--klyroo-text-muted) data-[color]:text-(--kly-color-text)")
            .Slot("bullet", "size-1.5 shrink-0 rounded-full bg-(--klyroo-text-muted) data-[color]:bg-(--kly-color)")
            .Slot("body", "flex min-w-0 flex-col")
            .Slot("text", "truncate text-(--klyroo-text)")
            .Slot("link", "truncate font-medium text-(--klyroo-text-highlighted) hover:underline")
            .Slot("description", "text-xs text-(--klyroo-text-muted)")
            .Slot("meta", "shrink-0 text-xs text-(--klyroo-text-muted)")
            .Slot("more", BlockMore)
            .Slot("moreIcon", "size-3.5")
            .Slot("skeleton", "flex flex-col gap-2.5"), "plain");

    public static ComponentTheme DateTile() => Surfaces(
        new ComponentTheme("dateTile", "root", "tile", "month", "day", "text", "title", "caption", "description", "skeleton")
            .Slot("root", "flex items-center gap-3 text-sm text-(--klyroo-text)")
            .Slot("tile", "flex size-12 shrink-0 flex-col items-center justify-center rounded-(--klyroo-radius) border border-(--klyroo-border) bg-(--klyroo-bg-muted) text-center")
            .Slot("month", "text-[10px] font-bold uppercase text-(--klyroo-primary-text)")
            .Slot("day", "text-base font-bold leading-none tabular-nums text-(--klyroo-text-highlighted)")
            .Slot("text", "flex min-w-0 flex-col gap-0.5")
            .Slot("title", "font-semibold text-(--klyroo-text-highlighted)")
            .Slot("caption", "text-xs font-medium text-(--klyroo-text-muted)")
            .Slot("description", "text-xs text-(--klyroo-text-muted)")
            .Slot("skeleton", "flex flex-col gap-2.5"), "plain");

    public static ComponentTheme Meter() => Surfaces(
        new ComponentTheme("meter", "root", "header", "label", "value", "track", "bar", "part", "ring", "ringSvg", "ringDrawing", "ringTrack", "ringArc", "ringValue", "ringText",
                "caption", "scale", "level", "legend", "legendItem", "legendIcon", "legendDot", "legendLabel", "legendValue", "skeleton")
            .Slot("root", "flex w-full min-w-0 flex-col gap-2 text-sm text-(--klyroo-text)")
            .Slot("header", "flex items-baseline justify-between gap-2")
            .Slot("label", "text-xs font-medium text-(--klyroo-text-muted)")
            .Slot("value", "text-xs font-semibold tabular-nums text-(--klyroo-text-highlighted)")
            .Slot("track", "flex h-2.5 w-full overflow-hidden rounded-full bg-(--klyroo-bg-muted)")
            .Slot("bar", "h-full rounded-full bg-(--kly-color) ring ring-inset ring-(--kly-color-text)")
            .Slot("part", "h-full bg-(--kly-color) ring ring-inset ring-(--kly-color-text) first:rounded-s-full last:rounded-e-full")
            .Slot("ring", "flex items-center gap-4")
            .Slot("ringSvg", "relative size-20 shrink-0")
            .Slot("ringDrawing", "size-full -rotate-90")
            .Slot("ringTrack", "text-(--klyroo-bg-muted)")
            .Slot("ringArc", "text-(--kly-color)")
            .Slot("ringValue", "absolute inset-0 flex items-center justify-center text-sm font-semibold tabular-nums text-(--klyroo-text-highlighted)")
            .Slot("ringText", "flex min-w-0 flex-col gap-0.5")
            .Slot("caption", "text-xs text-(--klyroo-text-muted)")
            .Slot("scale", "grid gap-1.5")
            .Slot("level", "rounded-(--klyroo-radius) border border-(--klyroo-border) px-2 py-1 text-center text-xs font-medium text-(--klyroo-text-muted) " +
                           "data-[reached=true]:border-(--klyroo-primary-text) data-[reached=true]:bg-(--klyroo-bg-muted) data-[reached=true]:text-(--klyroo-text-highlighted)")
            .Slot("legend", "m-0 flex list-none flex-wrap gap-x-4 gap-y-1 p-0 text-xs")
            .Slot("legendItem", "inline-flex items-center gap-1.5")
            .Slot("legendIcon", "size-3.5 text-(--kly-color-text)")
            .Slot("legendDot", "size-2 rounded-full bg-(--kly-color) ring ring-inset ring-(--kly-color-text)")
            .Slot("legendLabel", "text-(--klyroo-text-muted)")
            .Slot("legendValue", "font-semibold tabular-nums text-(--klyroo-text-highlighted)")
            .Slot("skeleton", "flex flex-col gap-2.5"), "plain");

    public static ComponentTheme ImageFigure() =>
        new ComponentTheme("imageFigure", "figure", "caption")
            .Slot("figure", "m-0 flex flex-col gap-2")
            .Slot("caption", "text-xs text-(--klyroo-text-muted)");

    public static ComponentTheme ImageGallery() => Surfaces(
        new ComponentTheme("imageGallery", "root", "grid", "tile", "image", "overflow", "caption", "skeleton")
            .Slot("root", "flex w-full min-w-0 flex-col gap-2 text-sm text-(--klyroo-text)")
            .Slot("grid", "grid gap-2")
            .Slot("tile", "relative overflow-hidden rounded-(--klyroo-radius) border border-(--klyroo-border)")
            .Slot("image", "h-24 w-full object-cover")
            .Slot("overflow", "absolute inset-0 flex items-center justify-center bg-black/60 text-sm font-bold text-white")
            .Slot("caption", "m-0 text-xs text-(--klyroo-text-muted)")
            .Slot("skeleton", "flex flex-col gap-2.5")
            .Variant("columns", "1", "grid", "grid-cols-1")
            .Variant("columns", "2", "grid", "grid-cols-2")
            .Variant("columns", "3", "grid", "grid-cols-3")
            .Variant("columns", "4", "grid", "grid-cols-4")
            .Variant("columns", "5", "grid", "grid-cols-5")
            .Variant("columns", "6", "grid", "grid-cols-6")
            .Default("columns", "3"), "plain");
}
