namespace Klyroo.UI.Theming;

/// <summary>Themes of the content family: documentation navigation, table of contents, previous/next links, search button.</summary>
internal static partial class Themes
{
    private const string ContentFocus =
        "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--klyroo-primary-text)";

    private const string ContentLink =
        "flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-sm text-(--klyroo-text-muted) transition-colors " +
        "hover:bg-(--klyroo-bg-muted) hover:text-(--klyroo-text-highlighted) " + ContentFocus;

    public static ComponentTheme ContentNavigation() =>
        new ComponentTheme("contentNavigation", "root", "list", "item", "link", "disabled", "trigger", "section", "sectionList", "icon", "label", "badge", "chevron", "group", "groupList")
            .Slot("root", "text-sm")
            .Slot("list", "flex flex-col gap-4")
            .Slot("item", "min-w-0")
            // The mark of the current page is an inset line on the start edge: one utility instead of a pseudo-element.
            .Slot("link", ContentLink + " " +
                          "aria-[current=page]:bg-(--klyroo-bg-muted) aria-[current=page]:font-medium aria-[current=page]:text-(--klyroo-text-highlighted) " +
                          "aria-[current=page]:shadow-[inset_2px_0_0_var(--kly-color-text)]")
            .Slot("disabled", "cursor-not-allowed opacity-60")
            .Slot("trigger", ContentLink + " group/trigger text-start disabled:cursor-not-allowed disabled:opacity-60")
            .Slot("section", "group/trigger flex w-full items-center gap-2 rounded-md px-2 py-1 text-start font-mono text-[11px] uppercase tracking-[0.14em] text-(--klyroo-text-muted) " +
                             "hover:text-(--klyroo-text-highlighted) disabled:cursor-not-allowed disabled:opacity-60 " + ContentFocus)
            .Slot("sectionList", "mt-1 flex flex-col gap-0.5")
            .Slot("icon", "size-4 shrink-0")
            .Slot("label", "min-w-0 flex-1 truncate")
            .Slot("badge", "shrink-0")
            .Slot("chevron", "ms-auto size-4 shrink-0 transition-transform duration-200 group-aria-expanded/trigger:rotate-180 motion-reduce:transition-none")
            .Slot("group", "w-full")
            .Slot("groupList", "ms-3 mt-0.5 flex flex-col gap-0.5 border-s border-(--klyroo-border) ps-2");

    public static ComponentTheme ContentToc() =>
        new ComponentTheme("contentToc", "root", "trigger", "title", "chevron", "content", "list", "item", "link")
            .Slot("root", "text-sm")
            .Slot("trigger", "group/trigger flex w-full items-center justify-between gap-2 rounded-sm py-2 text-start text-sm font-semibold text-(--klyroo-text-highlighted) " + ContentFocus)
            .Slot("title", "py-2 text-sm font-semibold text-(--klyroo-text-highlighted)")
            .Slot("chevron", "size-4 shrink-0 transition-transform duration-200 group-aria-expanded/trigger:rotate-180 motion-reduce:transition-none")
            .Slot("content", "")
            .Slot("list", "flex flex-col border-s border-(--klyroo-border)")
            .Slot("item", "min-w-0")
            .Slot("link", "-ms-px block border-s-2 border-transparent py-1 ps-3 text-(--klyroo-text-muted) transition-colors " +
                          "hover:text-(--klyroo-text-highlighted) data-[level=3]:ps-6 data-[level=4]:ps-9 data-[level=5]:ps-12 " +
                          "data-active:border-(--kly-color-text) data-active:font-medium data-active:text-(--kly-color-text) " + ContentFocus)
            // The title and the content show below the breakpoint as a collapsible block, above it always.
            .Variant("breakpoint", "lg", "trigger", "lg:hidden")
            .Variant("breakpoint", "lg", "title", "hidden lg:block")
            .Variant("breakpoint", "lg", "content", "hidden data-[state=open]:block lg:block")
            .Variant("breakpoint", "xl", "trigger", "xl:hidden")
            .Variant("breakpoint", "xl", "title", "hidden xl:block")
            .Variant("breakpoint", "xl", "content", "hidden data-[state=open]:block xl:block")
            .Variant("breakpoint", "2xl", "trigger", "min-[96rem]:hidden")
            .Variant("breakpoint", "2xl", "title", "hidden min-[96rem]:block")
            .Variant("breakpoint", "2xl", "content", "hidden data-[state=open]:block min-[96rem]:block")
            .Variant("collapsible", "false", "trigger", "hidden")
            .Variant("collapsible", "false", "title", "block")
            .Variant("collapsible", "false", "content", "block")
            .Default("breakpoint", "lg")
            .Default("collapsible", "true");

    public static ComponentTheme ContentSurround() =>
        new ComponentTheme("contentSurround", "root", "link", "caption", "icon", "title", "description")
            .Slot("root", "grid gap-4 sm:grid-cols-2")
            .Slot("link", "group/link flex min-w-0 flex-col gap-1 rounded-(--klyroo-radius) border border-(--klyroo-border) p-4 transition-colors " +
                          "hover:border-(--klyroo-border-accented) hover:bg-(--klyroo-bg-muted) " + ContentFocus)
            .Slot("caption", "flex items-center gap-1.5 font-mono text-xs uppercase tracking-wider text-(--klyroo-text-muted)")
            .Slot("icon", "size-4 shrink-0 transition-transform motion-reduce:transition-none")
            .Slot("title", "font-medium text-(--klyroo-text-highlighted)")
            .Slot("description", "line-clamp-2 text-sm text-(--klyroo-text-muted)")
            .Variant("direction", "previous", "link", "items-start text-start")
            .Variant("direction", "previous", "icon", "group-hover/link:-translate-x-0.5 rtl:group-hover/link:translate-x-0.5")
            .Variant("direction", "next", "link", "items-end text-end sm:col-start-2")
            .Variant("direction", "next", "caption", "flex-row-reverse")
            .Variant("direction", "next", "icon", "group-hover/link:translate-x-0.5 rtl:group-hover/link:-translate-x-0.5")
            .Default("direction", "previous");

    public static ComponentTheme ContentSearchButton() =>
        new ComponentTheme("contentSearchButton", "root", "buttonLabel", "label", "kbds")
            .Slot("root", "min-w-0")
            .Slot("buttonLabel", "flex min-w-0 flex-1 items-center gap-2")
            .Slot("label", "min-w-0 flex-1 truncate text-start text-(--klyroo-text-muted)")
            .Slot("kbds", "ms-auto flex shrink-0 items-center gap-0.5 [&>kbd[data-kly-mod]]:before:content-[attr(data-label)]")
            .Variant("collapsed", "false", "root", "gap-2 pe-1.5")
            .Default("collapsed", "true");
}
