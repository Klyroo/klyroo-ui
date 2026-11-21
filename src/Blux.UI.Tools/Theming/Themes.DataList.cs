using Blux.UI.Theming;

namespace Blux.UI.Tools;

/// <summary>Default themes of Blux.UI.Tools. Every class is a literal, so the class extractor and Tailwind see it.</summary>
internal static partial class ToolsThemes
{
    public static ComponentTheme DataListEmpty() =>
        new ComponentTheme("dataListEmpty", "root", "icon", "title", "description", "actions")
            .Slot("root", "flex w-full min-w-0 flex-col items-center justify-center gap-3 px-6 py-12 text-center")
            .Slot("icon", "size-10 shrink-0 text-(--blux-text-muted)")
            .Slot("title", "max-w-full text-sm font-medium break-words text-(--blux-text-highlighted)")
            .Slot("description", "max-w-sm text-sm text-(--blux-text-muted)")
            .Slot("actions", "mt-2 flex flex-wrap items-center justify-center gap-2");

    /// <summary>The look of the table follows the one of <c>BxTable</c> in Blux.UI; the padding of the cells comes from the density variant.</summary>
    public static ComponentTheme DataList() =>
        new ComponentTheme("dataList",
                "root", "toolbar", "search", "searchField", "searchInput", "searchClear", "searchClearIcon", "searchSubmit", "resultCount", "refresh", "refreshIcon", "refreshIconBusy",
                "tableRoot", "table", "caption", "thead", "tr", "th", "thStart", "thCenter", "thEnd",
                "sortButton", "sortIcon", "sortIconActive", "tbody", "td", "tdStart", "tdCenter", "tdEnd",
                "empty", "skeleton", "pagination", "pageSize", "pageSizeLabel", "pageSizeSelect", "pageSizeForm", "error")
            .Slot("root", "flex w-full min-w-0 flex-col gap-3")
            .Slot("toolbar", "flex flex-wrap items-center justify-between gap-2")
            .Slot("search", "flex w-full min-w-0 items-center gap-2 sm:max-w-sm")
            .Slot("searchField", "group/search relative min-w-0 flex-1")
            .Slot("searchInput", "pe-9 [&::-webkit-search-cancel-button]:hidden [&::-webkit-search-decoration]:hidden")
            .Slot("searchClear", "absolute inset-y-0 end-1 my-auto inline-flex size-7 items-center justify-center rounded-(--blux-radius) text-(--blux-text-muted) hover:text-(--blux-text) " +
                                 "focus-visible:outline-2 focus-visible:outline-(--blux-primary-text) group-has-[input:placeholder-shown]/search:invisible")
            .Slot("searchClearIcon", "size-4 shrink-0")
            .Slot("searchSubmit", "shrink-0")
            .Slot("resultCount", "text-sm tabular-nums text-(--blux-text-muted)")
            .Slot("refresh", "")
            .Slot("refreshIcon", "size-4 shrink-0")
            .Slot("refreshIconBusy", "size-4 shrink-0 animate-spin motion-reduce:animate-none")
            .Slot("tableRoot", "relative w-full max-w-full overflow-x-auto rounded-(--blux-radius) border border-(--blux-border) focus-visible:outline-2 focus-visible:outline-(--blux-primary-text)")
            .Slot("table", "min-w-full text-sm")
            .Slot("caption", "sr-only")
            .Slot("thead", "bg-(--blux-bg-muted)")
            .Slot("tr", "border-b border-(--blux-border) last:border-b-0")
            .Slot("th", "whitespace-nowrap font-semibold text-(--blux-text)")
            .Slot("thStart", "text-start")
            .Slot("thCenter", "text-center")
            .Slot("thEnd", "text-end")
            .Slot("sortButton", "-mx-1 inline-flex items-center gap-1 rounded-(--blux-radius) px-1 font-semibold hover:text-(--blux-text-muted) " +
                                "disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-(--blux-primary-text)")
            .Slot("sortIcon", "size-4 shrink-0 text-(--blux-text-dimmed)")
            .Slot("sortIconActive", "size-4 shrink-0 text-(--blux-text)")
            .Slot("tbody", "transition-opacity aria-busy:opacity-60")
            .Slot("td", "whitespace-nowrap align-middle text-(--blux-text-muted)")
            .Slot("tdStart", "text-start")
            .Slot("tdCenter", "text-center")
            .Slot("tdEnd", "text-end tabular-nums")
            .Slot("empty", "p-0")
            .Slot("skeleton", "h-4 w-3/4 max-w-48")
            .Slot("pagination", "flex flex-wrap items-center justify-between gap-3")
            .Slot("pageSize", "flex items-center gap-2 text-sm text-(--blux-text-muted)")
            .Slot("pageSizeLabel", "whitespace-nowrap")
            .Slot("pageSizeSelect", "h-8 rounded-(--blux-radius) border border-(--blux-border-accented) bg-(--blux-bg) px-2 text-sm text-(--blux-text) " +
                                    "focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-(--blux-primary-text)")
            .Slot("pageSizeForm", "flex items-center gap-2")
            .Slot("error", "")
            .Variant("density", "compact", "th", "px-3 py-2")
            .Variant("density", "default", "th", "px-4 py-3")
            .Variant("density", "comfortable", "th", "px-4 py-4")
            .Variant("density", "compact", "td", "px-3 py-1.5")
            .Variant("density", "default", "td", "px-4 py-3")
            .Variant("density", "comfortable", "td", "px-4 py-5")
            .Default("density", "default");
}
