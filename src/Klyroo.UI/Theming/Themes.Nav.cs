namespace Klyroo.UI.Theming;

/// <summary>Themes of the navigation family: breadcrumb, pagination, stepper, timeline, navigation menu, collapsible, tree, marquee, scroll area, splitter.</summary>
internal static partial class Themes
{
    private const string NavLink =
        "inline-flex min-w-0 items-center gap-2 rounded-(--klyroo-radius) px-2.5 py-1.5 text-sm font-medium text-(--klyroo-text-muted) transition-colors " +
        "hover:bg-(--klyroo-bg-elevated) hover:text-(--klyroo-text) aria-[current=page]:bg-(--kly-color)/10 aria-[current=page]:text-(--kly-color-text) aria-[current=page]:ring aria-[current=page]:ring-inset aria-[current=page]:ring-(--kly-color-text)/75 " +
        "aria-disabled:cursor-not-allowed aria-disabled:opacity-60 " +
        "focus-visible:outline-2 focus-visible:outline-(--kly-color-text)";

    public static ComponentTheme Breadcrumb() =>
        new ComponentTheme("breadcrumb", "root", "list", "item", "link", "icon", "label", "separator", "separatorIcon")
            .Slot("root", "")
            .Slot("list", "flex flex-wrap items-center gap-1.5 text-sm")
            .Slot("item", "inline-flex min-w-0 items-center gap-1.5")
            .Slot("link", "inline-flex min-w-0 items-center gap-1.5 rounded-sm text-(--klyroo-text-muted) hover:text-(--klyroo-text) " +
                          "aria-[current=page]:font-medium aria-[current=page]:text-(--klyroo-text) " +
                          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--klyroo-primary-text)")
            .Slot("icon", "size-4 shrink-0")
            .Slot("label", "truncate")
            .Slot("separator", "inline-flex shrink-0 text-(--klyroo-text-dimmed)")
            .Slot("separatorIcon", "size-4 rtl:rotate-180");

    public static ComponentTheme Pagination()
    {
        var t = new ComponentTheme("pagination", "root", "list", "item", "page", "control", "ellipsis", "icon")
            .Slot("root", "")
            .Slot("list", "flex flex-wrap items-center gap-1")
            .Slot("item", "inline-flex")
            .Slot("page", "inline-flex items-center justify-center rounded-(--klyroo-radius) font-medium tabular-nums text-(--klyroo-text) transition-colors " +
                          "hover:bg-(--klyroo-bg-elevated) aria-[current=page]:bg-(--kly-color) aria-[current=page]:text-(--kly-color-contrast) aria-[current=page]:ring-1 aria-[current=page]:ring-inset aria-[current=page]:ring-(--kly-color-text) " +
                          "aria-[current=page]:hover:bg-(--kly-color) disabled:cursor-not-allowed disabled:opacity-60 " +
                          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--kly-color-text)")
            .Slot("control", "inline-flex items-center justify-center rounded-(--klyroo-radius) text-(--klyroo-text-muted) transition-colors " +
                             "hover:bg-(--klyroo-bg-elevated) hover:text-(--klyroo-text) disabled:cursor-not-allowed disabled:opacity-60 " +
                             "aria-disabled:cursor-not-allowed aria-disabled:opacity-60 aria-disabled:hover:bg-transparent " +
                             "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--kly-color-text)")
            .Slot("ellipsis", "inline-flex items-center justify-center text-(--klyroo-text-muted)")
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
            .Slot("indicator", "relative inline-flex shrink-0 items-center justify-center rounded-full border-2 border-(--klyroo-border-accented) bg-(--klyroo-bg) font-medium text-(--klyroo-text-muted) transition-colors " +
                               "data-[state=completed]:border-(--kly-color-text) data-[state=completed]:bg-(--kly-color) data-[state=completed]:text-(--kly-color-contrast) " +
                               "data-[state=active]:border-(--kly-color-text) data-[state=active]:bg-(--kly-color) data-[state=active]:text-(--kly-color-contrast) data-[state=active]:ring-4 data-[state=active]:ring-(--kly-color)/25 " +
                               "disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--kly-color-text)")
            .Slot("separator", "rounded-full bg-(--klyroo-border-accented) data-[state=completed]:bg-(--kly-color-text)")
            .Slot("text", "flex min-w-0 flex-col")
            .Slot("title", "text-sm font-medium text-(--klyroo-text)")
            .Slot("description", "text-sm text-(--klyroo-text-muted)")
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
        var t = new ComponentTheme("timeline", "root", "item", "track", "indicator", "icon", "separator", "text", "date", "title", "description", "actor", "number", "pipelineList", "pipelineItem", "logList", "logItem", "logTimestamp", "logMarker", "logDot", "logIcon", "logBody", "logTitle", "logDescription", "logActor", "schedule", "scheduleDay", "scheduleDayTitle", "scheduleUnscheduled", "scheduleList", "scheduleEvent", "scheduleCurrent", "scheduleCurrentTone", "scheduleDone", "scheduleDoneTone", "scheduleUpcoming", "scheduleUpcomingTone", "scheduleTimes", "scheduleTime", "scheduleDuration", "scheduleBody", "scheduleHeader", "scheduleIcon", "scheduleTitle", "scheduleDescription", "scheduleActor", "more", "moreIcon")
            .Slot("root", "flex w-full")
            .Slot("icon", "inline-flex size-1/2 items-center justify-center")
            .Slot("item", "relative flex")
            .Slot("track", "flex items-center gap-2")
            .Slot("indicator", "inline-flex shrink-0 items-center justify-center rounded-full border-2 border-(--klyroo-border-accented) bg-(--klyroo-bg) text-(--klyroo-text-muted) " +
                               "data-[state=active]:border-(--kly-color-text) data-[state=active]:bg-(--kly-color) data-[state=active]:text-(--kly-color-contrast) " +
                               "data-[state=done]:border-(--kly-color-text) data-[state=done]:bg-(--kly-color) data-[state=done]:text-(--kly-color-contrast) " +
                               "data-[state=current]:border-(--kly-color-text) data-[state=current]:text-(--kly-color-text)")
            .Slot("separator", "rounded-full bg-(--klyroo-border-accented) data-[state=active]:bg-(--kly-color-text) data-[state=done]:bg-(--kly-color-text)")
            .Slot("text", "flex min-w-0 flex-col gap-0.5")
            .Slot("date", "text-xs text-(--klyroo-text-muted)")
            .Slot("title", "text-sm font-medium text-(--klyroo-text)")
            .Slot("description", "text-sm text-(--klyroo-text-muted)")
            .Slot("actor", "text-xs font-medium text-(--klyroo-text-muted)")
            .Slot("number", "text-[11px] font-semibold")
            .Slot("pipelineList", "m-0 list-none flex-wrap items-center gap-4 p-0")
            .Slot("pipelineItem", "flex items-start gap-3")
            .Slot("logList", "m-0 list-none divide-y divide-(--klyroo-border) p-0 text-xs")
            .Slot("logItem", "flex items-baseline gap-3 py-1.5 first:pt-0 last:pb-0")
            .Slot("logTimestamp", "w-32 shrink-0 font-mono tabular-nums text-(--klyroo-text-muted)")
            .Slot("logMarker", "inline-flex shrink-0 self-center text-(--klyroo-text-muted) data-[color]:text-(--kly-color-text)")
            .Slot("logDot", "size-1.5 rounded-full bg-current")
            .Slot("logIcon", "size-3.5")
            .Slot("logBody", "flex min-w-0 flex-1 flex-wrap items-baseline gap-x-2")
            .Slot("logTitle", "font-medium text-(--klyroo-text-highlighted)")
            .Slot("logDescription", "text-(--klyroo-text-muted)")
            .Slot("logActor", "font-medium text-(--klyroo-text-muted)")
            .Slot("schedule", "gap-4")
            .Slot("scheduleDay", "flex flex-col gap-1.5")
            .Slot("scheduleDayTitle", "m-0 text-xs font-semibold text-(--klyroo-text-highlighted)")
            .Slot("scheduleUnscheduled", "m-0 text-xs font-semibold text-(--klyroo-text-muted)")
            .Slot("scheduleList", "m-0 flex list-none flex-col gap-1 p-0")
            .Slot("scheduleEvent", "flex flex-col gap-1 border-s-2 py-1.5 ps-3 sm:flex-row sm:gap-4")
            .Slot("scheduleCurrent", "border-(--klyroo-primary-text) bg-(--klyroo-bg-muted)")
            .Slot("scheduleCurrentTone", "border-(--kly-color-text) bg-(--klyroo-bg-muted)")
            .Slot("scheduleDone", "border-(--klyroo-border)")
            .Slot("scheduleDoneTone", "border-(--kly-color-text) opacity-70")
            .Slot("scheduleUpcoming", "border-(--klyroo-border-accented)")
            .Slot("scheduleUpcomingTone", "border-(--kly-color-text)")
            .Slot("scheduleTimes", "flex shrink-0 flex-row flex-wrap items-baseline gap-x-2 text-xs tabular-nums text-(--klyroo-text-muted) sm:w-40 sm:flex-col sm:gap-x-0 sm:gap-y-0.5")
            .Slot("scheduleTime", "font-medium text-(--klyroo-text)")
            .Slot("scheduleDuration", "")
            .Slot("scheduleBody", "flex min-w-0 flex-1 flex-col gap-0.5")
            .Slot("scheduleHeader", "flex min-w-0 items-center gap-1.5")
            .Slot("scheduleIcon", "inline-flex size-3.5 shrink-0 text-(--klyroo-text-muted) data-[color]:text-(--kly-color-text)")
            .Slot("scheduleTitle", "min-w-0 break-words font-medium text-(--klyroo-text-highlighted) data-[state=done]:text-(--klyroo-text-muted) data-[state=current]:font-semibold")
            .Slot("scheduleDescription", "m-0 break-words text-xs text-(--klyroo-text-muted)")
            .Slot("scheduleActor", "text-xs font-medium text-(--klyroo-text-muted)")
            .Slot("more", "inline-flex items-center gap-1.5 self-start rounded-(--klyroo-radius) px-2 py-1 text-xs font-medium text-(--klyroo-text-muted) hover:bg-(--klyroo-bg-muted) hover:text-(--klyroo-text) disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-(--klyroo-primary-text)")
            .Slot("moreIcon", "size-3.5")
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
            .Slot("chevron", "ms-auto size-4 shrink-0 text-(--klyroo-text-muted) transition-transform duration-200 group-aria-expanded/trigger:rotate-180")
            .Slot("panel", "min-w-48 p-1.5")
            .Slot("panelList", "flex flex-col gap-1")
            .Slot("group", "w-full")
            .Slot("groupList", "ms-3 mt-1 flex flex-col gap-1 border-s border-(--klyroo-border) ps-2")
            .Variant("orientation", "horizontal", "list", "flex-row items-center")
            .Variant("orientation", "vertical", "list", "flex-col")
            .Variant("orientation", "vertical", "link", "w-full")
            .Variant("orientation", "vertical", "trigger", "w-full")
            .Default("orientation", "horizontal");

    public static ComponentTheme Collapsible() =>
        new ComponentTheme("collapsible", "root", "trigger", "label", "icon", "content")
            .Slot("root", "w-full")
            .Slot("trigger", "group/trigger flex w-full items-center gap-2 rounded-sm text-start text-sm font-medium text-(--klyroo-text) " +
                             "disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--klyroo-primary-text)")
            .Slot("label", "min-w-0 flex-1 text-pretty")
            .Slot("icon", "ms-auto size-4 shrink-0 text-(--klyroo-text-muted) transition-transform duration-200 group-aria-expanded/trigger:rotate-180")
            .Slot("content", "overflow-hidden");

    public static ComponentTheme Tree() =>
        new ComponentTheme("tree", "root", "list", "item", "row", "chevron", "spacer", "icon", "label", "group")
            .Slot("root", "w-full")
            .Slot("list", "flex flex-col gap-0.5")
            .Slot("item", "")
            .Slot("row", "group/row flex cursor-pointer focus:outline-none focus-visible:outline-2 focus-visible:outline-(--kly-color-text) items-center gap-1.5 rounded-(--klyroo-radius) px-2 py-1 text-sm text-(--klyroo-text) " +
                         "hover:bg-(--klyroo-bg-elevated) data-[selected=true]:bg-(--kly-color)/10 data-[selected=true]:text-(--kly-color-text) " +
                         "data-[disabled=true]:cursor-not-allowed data-[disabled=true]:opacity-60")
            .Slot("chevron", "size-4 shrink-0 text-(--klyroo-text-muted) transition-transform duration-150 group-data-[expanded=true]/row:rotate-90 rtl:group-data-[expanded=true]/row:-rotate-90")
            .Slot("spacer", "size-4 shrink-0")
            .Slot("icon", "size-4 shrink-0")
            .Slot("label", "min-w-0 truncate")
            .Slot("group", "ms-3 mt-0.5 flex flex-col gap-0.5 border-s border-(--klyroo-border) ps-1.5");

    public static ComponentTheme Marquee() =>
        new ComponentTheme("marquee", "root", "group")
            .Slot("root", "relative flex gap-(--kly-gap) overflow-hidden")
            .Slot("group", "flex shrink-0 items-center justify-around gap-(--kly-gap)")
            .Variant("orientation", "horizontal", "root", "flex-row")
            .Variant("orientation", "horizontal", "group", "min-w-full flex-row")
            .Variant("orientation", "vertical", "root", "flex-col")
            .Variant("orientation", "vertical", "group", "min-h-full flex-col")
            .Default("orientation", "horizontal");

    public static ComponentTheme ScrollArea() =>
        new ComponentTheme("scrollArea", "root", "viewport")
            .Slot("root", "relative min-h-0")
            .Slot("viewport", "size-full [scrollbar-width:thin] [scrollbar-color:var(--klyroo-border-accented)_transparent] " +
                              "focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-(--klyroo-primary-text)")
            .Variant("orientation", "vertical", "viewport", "overflow-x-hidden overflow-y-auto")
            .Variant("orientation", "horizontal", "viewport", "overflow-x-auto overflow-y-hidden")
            .Variant("orientation", "both", "viewport", "overflow-auto")
            .Default("orientation", "vertical");

    public static ComponentTheme Splitter() =>
        new ComponentTheme("splitter", "root", "panel", "handle")
            .Slot("root", "flex size-full min-h-0 min-w-0")
            .Slot("panel", "min-h-0 min-w-0 overflow-auto")
            .Slot("handle", "relative z-10 flex shrink-0 touch-none items-center justify-center bg-(--klyroo-border-accented) transition-colors " +
                            "hover:bg-(--kly-color) data-[dragging=true]:bg-(--kly-color) focus-visible:bg-(--kly-color) focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--kly-color-text) " +
                            "after:absolute after:content-['']")
            .Variant("orientation", "horizontal", "root", "flex-row")
            .Variant("orientation", "horizontal", "handle", "w-px cursor-col-resize after:inset-y-0 after:-inset-x-1.5")
            .Variant("orientation", "vertical", "root", "flex-col")
            .Variant("orientation", "vertical", "handle", "h-px cursor-row-resize after:inset-x-0 after:-inset-y-1.5")
            .Default("orientation", "horizontal");
}
