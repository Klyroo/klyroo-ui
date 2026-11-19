namespace Blux.UI.Theming;

/// <summary>Themes of the navigation family: breadcrumb, pagination, stepper, timeline, navigation menu, collapsible, tree, marquee, scroll area, splitter.</summary>
internal static partial class Themes
{
    private const string NavLink =
        "inline-flex min-w-0 items-center gap-2 rounded-(--blux-radius) px-2.5 py-1.5 text-sm font-medium text-(--blux-text-muted) transition-colors " +
        "hover:bg-(--blux-bg-elevated) hover:text-(--blux-text) aria-[current=page]:bg-(--bx-color)/10 aria-[current=page]:text-(--bx-color-text) " +
        "aria-disabled:cursor-not-allowed aria-disabled:opacity-60 " +
        "focus-visible:outline-2 focus-visible:outline-(--bx-color-text)";

    public static ComponentTheme Breadcrumb() =>
        new ComponentTheme("breadcrumb", "root", "list", "item", "link", "icon", "label", "separator", "separatorIcon")
            .Slot("root", "")
            .Slot("list", "flex flex-wrap items-center gap-1.5 text-sm")
            .Slot("item", "inline-flex min-w-0 items-center gap-1.5")
            .Slot("link", "inline-flex min-w-0 items-center gap-1.5 rounded-sm text-(--blux-text-muted) hover:text-(--blux-text) " +
                          "aria-[current=page]:font-medium aria-[current=page]:text-(--blux-text) " +
                          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--blux-primary-text)")
            .Slot("icon", "size-4 shrink-0")
            .Slot("label", "truncate")
            .Slot("separator", "inline-flex shrink-0 text-(--blux-text-dimmed)")
            .Slot("separatorIcon", "size-4 rtl:rotate-180");

    public static ComponentTheme Pagination()
    {
        var t = new ComponentTheme("pagination", "root", "list", "item", "page", "control", "ellipsis", "icon")
            .Slot("root", "")
            .Slot("list", "flex flex-wrap items-center gap-1")
            .Slot("item", "inline-flex")
            .Slot("page", "inline-flex items-center justify-center rounded-(--blux-radius) font-medium tabular-nums text-(--blux-text) transition-colors " +
                          "hover:bg-(--blux-bg-elevated) aria-[current=page]:bg-(--bx-color) aria-[current=page]:text-(--bx-color-contrast) " +
                          "aria-[current=page]:hover:bg-(--bx-color) disabled:cursor-not-allowed disabled:opacity-60 " +
                          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--bx-color-text)")
            .Slot("control", "inline-flex items-center justify-center rounded-(--blux-radius) text-(--blux-text-muted) transition-colors " +
                             "hover:bg-(--blux-bg-elevated) hover:text-(--blux-text) disabled:cursor-not-allowed disabled:opacity-60 " +
                             "aria-disabled:cursor-not-allowed aria-disabled:opacity-60 aria-disabled:hover:bg-transparent " +
                             "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--bx-color-text)")
            .Slot("ellipsis", "inline-flex items-center justify-center text-(--blux-text-muted)")
            .Slot("icon", "size-4 shrink-0 rtl:rotate-180")
            .Default("size", "md");
        t.Variant("size", "xs", "page", "h-6 min-w-6 px-1 text-xs").Variant("size", "xs", "control", "h-6 min-w-6 px-1 text-xs").Variant("size", "xs", "ellipsis", "h-6 min-w-6 text-xs");
        t.Variant("size", "sm", "page", "h-7 min-w-7 px-1.5 text-xs").Variant("size", "sm", "control", "h-7 min-w-7 px-1.5 text-xs").Variant("size", "sm", "ellipsis", "h-7 min-w-7 text-xs");
        t.Variant("size", "md", "page", "h-8 min-w-8 px-2 text-sm").Variant("size", "md", "control", "h-8 min-w-8 px-2 text-sm").Variant("size", "md", "ellipsis", "h-8 min-w-8 text-sm");
        t.Variant("size", "lg", "page", "h-9 min-w-9 px-2 text-sm").Variant("size", "lg", "control", "h-9 min-w-9 px-2 text-sm").Variant("size", "lg", "ellipsis", "h-9 min-w-9 text-sm");
        t.Variant("size", "xl", "page", "h-10 min-w-10 px-2.5 text-base").Variant("size", "xl", "control", "h-10 min-w-10 px-2.5 text-base").Variant("size", "xl", "ellipsis", "h-10 min-w-10 text-base");
        return t;
    }

    public static ComponentTheme Stepper()
    {
        var t = new ComponentTheme("stepper", "root", "list", "item", "track", "indicator", "separator", "icon", "text", "title", "description", "status", "content")
            .Slot("root", "flex w-full flex-col gap-4")
            .Slot("icon", "inline-flex size-1/2 items-center justify-center")
            .Slot("list", "flex w-full")
            .Slot("item", "relative flex")
            .Slot("track", "flex items-center gap-2")
            .Slot("indicator", "relative inline-flex shrink-0 items-center justify-center rounded-full border-2 border-(--blux-border-accented) bg-(--blux-bg) font-medium text-(--blux-text-muted) transition-colors " +
                               "data-[state=completed]:border-transparent data-[state=completed]:bg-(--bx-color) data-[state=completed]:text-(--bx-color-contrast) " +
                               "data-[state=active]:border-transparent data-[state=active]:bg-(--bx-color) data-[state=active]:text-(--bx-color-contrast) data-[state=active]:ring-4 data-[state=active]:ring-(--bx-color)/25 " +
                               "disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--bx-color-text)")
            .Slot("separator", "rounded-full bg-(--blux-border-accented) data-[state=completed]:bg-(--bx-color)")
            .Slot("text", "flex min-w-0 flex-col")
            .Slot("title", "text-sm font-medium text-(--blux-text)")
            .Slot("description", "text-sm text-(--blux-text-muted)")
            .Slot("status", "sr-only")
            .Slot("content", "")
            .Variant("orientation", "horizontal", "list", "flex-row")
            .Variant("orientation", "horizontal", "item", "flex-1 flex-col gap-2 last:flex-none")
            .Variant("orientation", "horizontal", "separator", "h-0.5 min-w-4 flex-1")
            .Variant("orientation", "vertical", "list", "flex-col")
            .Variant("orientation", "vertical", "item", "flex-row gap-3 pb-6 last:pb-0")
            .Variant("orientation", "vertical", "track", "flex-col")
            .Variant("orientation", "vertical", "separator", "min-h-6 w-0.5 flex-1")
            .Default("orientation", "horizontal")
            .Default("size", "md");
        t.Variant("size", "xs", "indicator", "size-6 text-xs").Variant("size", "sm", "indicator", "size-7 text-xs").Variant("size", "md", "indicator", "size-8 text-sm")
         .Variant("size", "lg", "indicator", "size-9 text-sm").Variant("size", "xl", "indicator", "size-10 text-base");
        return t;
    }

    public static ComponentTheme Timeline()
    {
        var t = new ComponentTheme("timeline", "root", "item", "track", "indicator", "icon", "separator", "text", "date", "title", "description")
            .Slot("root", "flex w-full")
            .Slot("icon", "inline-flex size-1/2 items-center justify-center")
            .Slot("item", "relative flex")
            .Slot("track", "flex items-center gap-2")
            .Slot("indicator", "inline-flex shrink-0 items-center justify-center rounded-full border-2 border-(--blux-border-accented) bg-(--blux-bg) text-(--blux-text-muted) " +
                               "data-[state=active]:border-transparent data-[state=active]:bg-(--bx-color) data-[state=active]:text-(--bx-color-contrast)")
            .Slot("separator", "rounded-full bg-(--blux-border-accented) data-[state=active]:bg-(--bx-color)")
            .Slot("text", "flex min-w-0 flex-col gap-0.5")
            .Slot("date", "text-xs text-(--blux-text-muted)")
            .Slot("title", "text-sm font-medium text-(--blux-text)")
            .Slot("description", "text-sm text-(--blux-text-muted)")
            .Variant("orientation", "horizontal", "root", "flex-row")
            .Variant("orientation", "horizontal", "item", "flex-1 flex-col gap-2 last:flex-none")
            .Variant("orientation", "horizontal", "separator", "h-0.5 min-w-4 flex-1")
            .Variant("orientation", "vertical", "root", "flex-col")
            .Variant("orientation", "vertical", "item", "flex-row gap-3 pb-6 last:pb-0")
            .Variant("orientation", "vertical", "track", "flex-col")
            .Variant("orientation", "vertical", "separator", "min-h-6 w-0.5 flex-1")
            .Default("orientation", "vertical")
            .Default("size", "md");
        t.Variant("size", "xs", "indicator", "size-5 text-xs").Variant("size", "sm", "indicator", "size-6 text-xs").Variant("size", "md", "indicator", "size-7 text-sm")
         .Variant("size", "lg", "indicator", "size-8 text-sm").Variant("size", "xl", "indicator", "size-9 text-base");
        return t;
    }

    public static ComponentTheme NavigationMenu() =>
        new ComponentTheme("navigationMenu", "root", "list", "item", "link", "trigger", "icon", "label", "chevron", "panel", "panelList", "group", "groupList")
            .Slot("root", "")
            .Slot("list", "flex gap-1")
            .Slot("item", "min-w-0")
            .Slot("link", NavLink)
            .Slot("trigger", NavLink + " group/trigger")
            .Slot("icon", "size-5 shrink-0")
            .Slot("label", "truncate")
            .Slot("chevron", "ms-auto size-4 shrink-0 text-(--blux-text-muted) transition-transform duration-200 group-aria-expanded/trigger:rotate-180")
            .Slot("panel", "min-w-48 p-1.5")
            .Slot("panelList", "flex flex-col gap-1")
            .Slot("group", "w-full")
            .Slot("groupList", "ms-3 mt-1 flex flex-col gap-1 border-s border-(--blux-border) ps-2")
            .Variant("orientation", "horizontal", "list", "flex-row items-center")
            .Variant("orientation", "vertical", "list", "flex-col")
            .Variant("orientation", "vertical", "link", "w-full")
            .Variant("orientation", "vertical", "trigger", "w-full")
            .Default("orientation", "horizontal");

    public static ComponentTheme Collapsible() =>
        new ComponentTheme("collapsible", "root", "trigger", "label", "icon", "content")
            .Slot("root", "w-full")
            .Slot("trigger", "group/trigger flex w-full items-center gap-2 rounded-sm text-start text-sm font-medium text-(--blux-text) " +
                             "disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--blux-primary-text)")
            .Slot("label", "min-w-0 flex-1 truncate")
            .Slot("icon", "ms-auto size-4 shrink-0 text-(--blux-text-muted) transition-transform duration-200 group-aria-expanded/trigger:rotate-180")
            .Slot("content", "overflow-hidden");

    public static ComponentTheme Tree() =>
        new ComponentTheme("tree", "root", "list", "item", "row", "chevron", "spacer", "icon", "label", "group")
            .Slot("root", "w-full")
            .Slot("list", "flex flex-col gap-0.5")
            .Slot("item", "focus:outline-none focus-visible:[&>div]:outline-2 focus-visible:[&>div]:outline-(--bx-color-text)")
            .Slot("row", "group/row flex cursor-pointer items-center gap-1.5 rounded-(--blux-radius) px-2 py-1 text-sm text-(--blux-text) " +
                         "hover:bg-(--blux-bg-elevated) data-[selected=true]:bg-(--bx-color)/10 data-[selected=true]:text-(--bx-color-text) " +
                         "data-[disabled=true]:cursor-not-allowed data-[disabled=true]:opacity-60")
            .Slot("chevron", "size-4 shrink-0 text-(--blux-text-muted) transition-transform duration-150 group-data-[expanded=true]/row:rotate-90 rtl:group-data-[expanded=true]/row:-rotate-90")
            .Slot("spacer", "size-4 shrink-0")
            .Slot("icon", "size-4 shrink-0")
            .Slot("label", "min-w-0 truncate")
            .Slot("group", "ms-3 mt-0.5 flex flex-col gap-0.5 border-s border-(--blux-border) ps-1.5");

    public static ComponentTheme Marquee() =>
        new ComponentTheme("marquee", "root", "group")
            .Slot("root", "relative flex gap-(--bx-gap) overflow-hidden")
            .Slot("group", "flex shrink-0 items-center justify-around gap-(--bx-gap)")
            .Variant("orientation", "horizontal", "root", "flex-row")
            .Variant("orientation", "horizontal", "group", "min-w-full flex-row")
            .Variant("orientation", "vertical", "root", "flex-col")
            .Variant("orientation", "vertical", "group", "min-h-full flex-col")
            .Default("orientation", "horizontal");

    public static ComponentTheme ScrollArea() =>
        new ComponentTheme("scrollArea", "root", "viewport")
            .Slot("root", "relative min-h-0")
            .Slot("viewport", "size-full [scrollbar-width:thin] [scrollbar-color:var(--blux-border-accented)_transparent] " +
                              "focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-(--blux-primary-text)")
            .Variant("orientation", "vertical", "viewport", "overflow-x-hidden overflow-y-auto")
            .Variant("orientation", "horizontal", "viewport", "overflow-x-auto overflow-y-hidden")
            .Variant("orientation", "both", "viewport", "overflow-auto")
            .Default("orientation", "vertical");

    public static ComponentTheme Splitter() =>
        new ComponentTheme("splitter", "root", "panel", "handle")
            .Slot("root", "flex size-full min-h-0 min-w-0")
            .Slot("panel", "min-h-0 min-w-0 overflow-auto")
            .Slot("handle", "relative z-10 flex shrink-0 touch-none items-center justify-center bg-(--blux-border-accented) transition-colors " +
                            "hover:bg-(--bx-color) data-[dragging=true]:bg-(--bx-color) focus-visible:bg-(--bx-color) focus-visible:outline-none " +
                            "after:absolute after:content-['']")
            .Variant("orientation", "horizontal", "root", "flex-row")
            .Variant("orientation", "horizontal", "handle", "w-px cursor-col-resize after:inset-y-0 after:-inset-x-1.5")
            .Variant("orientation", "vertical", "root", "flex-col")
            .Variant("orientation", "vertical", "handle", "h-px cursor-row-resize after:inset-x-0 after:-inset-y-1.5")
            .Default("orientation", "horizontal");
}
