namespace Klyroo.UI.Theming;

internal static partial class Themes
{
    private const string LinkFocus =
        "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--klyroo-primary-text)";

    public static ComponentTheme Kbd() =>
        new ComponentTheme("kbd", "base")
            .Slot("base", "inline-flex items-center justify-center px-1 font-medium font-sans rounded-sm whitespace-nowrap")
            .Variant("variant", "solid", "base", "bg-(--kly-color) ring ring-inset ring-(--kly-color-text) text-(--kly-color-contrast)")
            .Variant("variant", "outline", "base", "ring ring-inset ring-(--kly-color-text)/75 text-(--kly-color-text)")
            .Variant("variant", "soft", "base", "bg-(--kly-color)/10 text-(--kly-color-text)")
            .Variant("variant", "subtle", "base", "bg-(--kly-color)/10 ring ring-inset ring-(--kly-color-text)/75 text-(--kly-color-text)")
            .Variant("size", "xs", "base", "h-4 min-w-4 text-[10px]")
            .Variant("size", "sm", "base", "h-4 min-w-4 text-[11px]")
            .Variant("size", "md", "base", "h-5 min-w-5 text-xs")
            .Variant("size", "lg", "base", "h-6 min-w-6 text-sm")
            .Variant("size", "xl", "base", "h-7 min-w-7 text-base")
            .Default("variant", "outline")
            .Default("size", "md");

    public static ComponentTheme Separator() =>
        new ComponentTheme("separator", "root", "border", "container", "label", "icon")
            .Slot("root", "flex items-center text-center")
            .Slot("border", "flex-1 border-(--kly-color)/30")
            .Slot("container", "flex items-center gap-2 font-medium text-(--kly-color-text)")
            .Slot("label", "text-sm")
            .Slot("icon", "size-5 shrink-0")
            .Variant("orientation", "horizontal", "root", "w-full flex-row")
            .Variant("orientation", "horizontal", "border", "border-t-(length:--kly-sep)")
            .Variant("orientation", "horizontal", "container", "mx-3")
            .Variant("orientation", "vertical", "root", "min-h-4 flex-col self-stretch")
            .Variant("orientation", "vertical", "border", "border-s-(length:--kly-sep)")
            .Variant("orientation", "vertical", "container", "my-3 flex-col")
            .Variant("size", "xs", "root", "[--kly-sep:1px]")
            .Variant("size", "sm", "root", "[--kly-sep:2px]")
            .Variant("size", "md", "root", "[--kly-sep:3px]")
            .Variant("size", "lg", "root", "[--kly-sep:4px]")
            .Variant("size", "xl", "root", "[--kly-sep:5px]")
            .Variant("type", "solid", "border", "border-solid")
            .Variant("type", "dashed", "border", "border-dashed")
            .Variant("type", "dotted", "border", "border-dotted")
            .Default("orientation", "horizontal")
            .Default("size", "xs")
            .Default("type", "solid");

    public static ComponentTheme Skeleton() =>
        new ComponentTheme("skeleton", "base")
            .Slot("base", "animate-pulse rounded-(--klyroo-radius) bg-(--klyroo-bg-accented) motion-reduce:animate-none");

    /// <summary>The sentinel is a 1px strip the browser watches; the button (a <c>KlyButton</c>, styled by its own theme) is the manual way to load more.</summary>
    public static ComponentTheme InfiniteScroll() =>
        new ComponentTheme("infiniteScroll", "root", "sentinel", "trigger", "spinner", "status", "end", "marker")
            .Slot("root", "flex w-full flex-col items-center gap-2 py-2")
            .Slot("sentinel", "pointer-events-none h-px w-full shrink-0")
            .Slot("trigger", "shrink-0")
            .Slot("spinner", "size-full motion-safe:animate-spin")
            .Slot("status", "sr-only")
            .Slot("end", "text-sm text-(--klyroo-text-muted)")
            .Slot("marker", "sr-only");

    public static ComponentTheme Chip() =>
        new ComponentTheme("chip", "root", "base")
            .Slot("root", "relative inline-flex shrink-0 items-center justify-center")
            .Slot("base", "flex items-center justify-center rounded-full font-semibold leading-none whitespace-nowrap " +
                          "bg-(--kly-color) text-(--kly-color-contrast) inset-ring-1 inset-ring-(--kly-color-text) ring-2 ring-(--klyroo-bg)")
            .Variant("standalone", "false", "base", "absolute")
            .Variant("position", "top-right", "base", "top-0 end-0")
            .Variant("position", "top-left", "base", "top-0 start-0")
            .Variant("position", "bottom-right", "base", "bottom-0 end-0")
            .Variant("position", "bottom-left", "base", "bottom-0 start-0")
            .Variant("kind", "dot", "base", "p-0")
            .Variant("size", "xs", "base", "text-[10px]")
            .Variant("size", "sm", "base", "text-[10px]")
            .Variant("size", "md", "base", "text-[10px]")
            .Variant("size", "lg", "base", "text-xs")
            .Variant("size", "xl", "base", "text-sm")
            .Compound(new() { ["kind"] = "dot", ["size"] = "xs" }, "base", "size-1.5")
            .Compound(new() { ["kind"] = "dot", ["size"] = "sm" }, "base", "size-2")
            .Compound(new() { ["kind"] = "dot", ["size"] = "md" }, "base", "size-2.5")
            .Compound(new() { ["kind"] = "dot", ["size"] = "lg" }, "base", "size-3")
            .Compound(new() { ["kind"] = "dot", ["size"] = "xl" }, "base", "size-3.5")
            .Compound(new() { ["kind"] = "count", ["size"] = "xs" }, "base", "h-3.5 min-w-3.5 px-0.5")
            .Compound(new() { ["kind"] = "count", ["size"] = "sm" }, "base", "h-4 min-w-4 px-1")
            .Compound(new() { ["kind"] = "count", ["size"] = "md" }, "base", "h-4 min-w-4 px-1")
            .Compound(new() { ["kind"] = "count", ["size"] = "lg" }, "base", "h-5 min-w-5 px-1")
            .Compound(new() { ["kind"] = "count", ["size"] = "xl" }, "base", "h-6 min-w-6 px-1.5")
            .Compound(new() { ["standalone"] = "false", ["inset"] = "false", ["position"] = "top-right" }, "base", "-translate-y-1/2 translate-x-1/2 rtl:-translate-x-1/2")
            .Compound(new() { ["standalone"] = "false", ["inset"] = "false", ["position"] = "top-left" }, "base", "-translate-y-1/2 -translate-x-1/2 rtl:translate-x-1/2")
            .Compound(new() { ["standalone"] = "false", ["inset"] = "false", ["position"] = "bottom-right" }, "base", "translate-y-1/2 translate-x-1/2 rtl:-translate-x-1/2")
            .Compound(new() { ["standalone"] = "false", ["inset"] = "false", ["position"] = "bottom-left" }, "base", "translate-y-1/2 -translate-x-1/2 rtl:translate-x-1/2")
            .Default("standalone", "false")
            .Default("inset", "false")
            .Default("position", "top-right")
            .Default("kind", "count")
            .Default("size", "md");

    public static ComponentTheme Link() =>
        new ComponentTheme("link", "base")
            .Slot("base", "rounded-sm transition-colors " + LinkFocus)
            .Variant("active", "true", "base", "text-(--klyroo-primary-text)")
            .Variant("active", "false", "base", "text-(--klyroo-text-muted) hover:text-(--klyroo-text-highlighted)")
            .Variant("disabled", "true", "base", "cursor-not-allowed opacity-60")
            .Default("active", "false")
            .Default("disabled", "false");

    public static ComponentTheme Icon() =>
        new ComponentTheme("icon", "base")
            .Slot("base", "shrink-0 inline-block")
            .Variant("size", "xs", "base", "size-4")
            .Variant("size", "sm", "base", "size-4")
            .Variant("size", "md", "base", "size-5")
            .Variant("size", "lg", "base", "size-5")
            .Variant("size", "xl", "base", "size-6")
            .Default("size", "md");

    public static ComponentTheme Container() =>
        new ComponentTheme("container", "base")
            .Slot("base", "mx-auto w-full px-4 sm:px-6 lg:px-8")
            .Variant("size", "xs", "base", "max-w-2xl")
            .Variant("size", "sm", "base", "max-w-4xl")
            .Variant("size", "md", "base", "max-w-(--klyroo-container)")
            .Variant("size", "lg", "base", "max-w-(--breakpoint-2xl)")
            .Variant("size", "xl", "base", "max-w-none")
            .Default("size", "md");

    public static ComponentTheme Progress() =>
        new ComponentTheme("progress", "root", "status", "base", "indicator", "steps", "step")
            .Slot("root", "flex w-full flex-col gap-2")
            .Slot("status", "text-end font-medium text-(--klyroo-text-muted)")
            .Slot("base", "relative w-full overflow-hidden rounded-full bg-(--klyroo-bg-elevated) ring-1 ring-inset ring-(--klyroo-border-accented)")
            .Slot("indicator", "h-full rounded-full bg-(--kly-color) ring-1 ring-inset ring-(--kly-color-text)")
            .Slot("steps", "flex justify-between text-sm")
            .Slot("step", "text-(--klyroo-text-muted) data-[state=active]:font-medium data-[state=active]:text-(--kly-color-text) data-[state=completed]:text-(--klyroo-text)")
            .Variant("size", "xs", "base", "h-1")
            .Variant("size", "xs", "status", "text-xs")
            .Variant("size", "sm", "base", "h-1.5")
            .Variant("size", "sm", "status", "text-xs")
            .Variant("size", "md", "base", "h-2")
            .Variant("size", "md", "status", "text-sm")
            .Variant("size", "lg", "base", "h-3")
            .Variant("size", "lg", "status", "text-sm")
            .Variant("size", "xl", "base", "h-4")
            .Variant("size", "xl", "status", "text-base")
            .Variant("animation", "none", "indicator", "transition-[width] duration-200 ease-out motion-reduce:transition-none")
            .Variant("animation", "carousel", "indicator", "w-2/5 animate-[kly-progress-carousel_1.5s_ease-in-out_infinite] motion-reduce:w-full motion-reduce:animate-none motion-reduce:opacity-60")
            .Variant("animation", "swing", "indicator", "w-2/5 animate-[kly-progress-swing_2s_ease-in-out_infinite] motion-reduce:w-full motion-reduce:animate-none motion-reduce:opacity-60")
            .Default("size", "md")
            .Default("animation", "none");

    public static ComponentTheme Banner() =>
        new ComponentTheme("banner", "root", "container", "icon", "content", "title", "description", "actions", "close")
            .Slot("root", "relative z-50 w-full bg-(--kly-color) text-(--kly-color-contrast) ring ring-inset ring-(--kly-color-text)")
            .Slot("container", "flex items-center gap-3 px-4 py-1.5 sm:px-6 lg:px-8")
            .Slot("icon", "size-5 shrink-0")
            .Slot("content", "flex min-w-0 flex-1 flex-wrap items-center justify-center gap-x-3 gap-y-1 text-center")
            .Slot("title", "text-sm font-medium")
            .Slot("description", "text-sm")
            .Slot("actions", "relative z-10 flex shrink-0 items-center gap-1.5")
            .Slot("close", "relative z-10 -m-1 ms-auto shrink-0 rounded-(--klyroo-radius) p-1 opacity-80 hover:opacity-100 hover:bg-(--kly-color-contrast)/10 focus-visible:outline-2 focus-visible:outline-current")
            .Variant("link", "true", "title", "hover:underline focus-visible:outline-2 focus-visible:outline-current after:absolute after:inset-0")
            .Default("link", "false");

    public static ComponentTheme Empty() =>
        new ComponentTheme("empty", "root", "header", "icon", "title", "description", "actions")
            .Slot("root", "relative flex w-full min-w-0 flex-col items-center justify-center text-center")
            .Slot("header", "flex max-w-md flex-col items-center")
            .Slot("icon", "shrink-0 text-(--klyroo-text-muted)")
            .Slot("title", "font-medium text-(--klyroo-text-highlighted)")
            .Slot("description", "text-(--klyroo-text-muted)")
            .Slot("actions", "flex flex-wrap items-center justify-center gap-2")
            .Variant("variant", "outline", "root", "rounded-(--klyroo-radius) border border-dashed border-(--klyroo-border-accented)")
            .Variant("variant", "soft", "root", "rounded-(--klyroo-radius) bg-(--klyroo-bg-elevated)/50")
            .Variant("variant", "subtle", "root", "rounded-(--klyroo-radius) border border-(--klyroo-border) bg-(--klyroo-bg-elevated)/50")
            .Variant("size", "xs", "root", "gap-2 p-4")
            .Variant("size", "xs", "header", "gap-1")
            .Variant("size", "xs", "icon", "size-8")
            .Variant("size", "xs", "title", "text-xs")
            .Variant("size", "xs", "description", "text-xs")
            .Variant("size", "sm", "root", "gap-3 p-4")
            .Variant("size", "sm", "header", "gap-1")
            .Variant("size", "sm", "icon", "size-9")
            .Variant("size", "sm", "title", "text-sm")
            .Variant("size", "sm", "description", "text-xs")
            .Variant("size", "md", "root", "gap-4 p-6")
            .Variant("size", "md", "header", "gap-1.5")
            .Variant("size", "md", "icon", "size-10")
            .Variant("size", "md", "title", "text-base")
            .Variant("size", "md", "description", "text-sm")
            .Variant("size", "lg", "root", "gap-4 p-8")
            .Variant("size", "lg", "header", "gap-2")
            .Variant("size", "lg", "icon", "size-11")
            .Variant("size", "lg", "title", "text-lg")
            .Variant("size", "lg", "description", "text-base")
            .Variant("size", "xl", "root", "gap-5 p-10")
            .Variant("size", "xl", "header", "gap-2")
            .Variant("size", "xl", "icon", "size-12")
            .Variant("size", "xl", "title", "text-xl")
            .Variant("size", "xl", "description", "text-base")
            .Default("variant", "outline")
            .Default("size", "md");

    public static ComponentTheme User() =>
        new ComponentTheme("user", "root", "avatar", "wrapper", "name", "description")
            .Slot("root", "relative flex min-w-0 items-center")
            .Slot("avatar", "shrink-0")
            .Slot("wrapper", "flex min-w-0 flex-col")
            .Slot("name", "truncate font-medium text-(--klyroo-text-highlighted)")
            .Slot("description", "truncate text-(--klyroo-text-muted)")
            .Variant("orientation", "horizontal", "root", "flex-row")
            .Variant("orientation", "vertical", "root", "flex-col text-center")
            .Variant("link", "true", "root", "rounded-(--klyroo-radius) " + LinkFocus)
            .Variant("link", "true", "name", "after:absolute after:inset-0")
            .Variant("size", "xs", "root", "gap-1.5")
            .Variant("size", "xs", "name", "text-xs")
            .Variant("size", "xs", "description", "text-[11px]")
            .Variant("size", "sm", "root", "gap-2")
            .Variant("size", "sm", "name", "text-xs")
            .Variant("size", "sm", "description", "text-xs")
            .Variant("size", "md", "root", "gap-2.5")
            .Variant("size", "md", "name", "text-sm")
            .Variant("size", "md", "description", "text-xs")
            .Variant("size", "lg", "root", "gap-3")
            .Variant("size", "lg", "name", "text-sm")
            .Variant("size", "lg", "description", "text-sm")
            .Variant("size", "xl", "root", "gap-3.5")
            .Variant("size", "xl", "name", "text-base")
            .Variant("size", "xl", "description", "text-sm")
            .Default("orientation", "horizontal")
            .Default("link", "false")
            .Default("size", "md");

    public static ComponentTheme AvatarGroup() =>
        new ComponentTheme("avatarGroup", "root", "avatar")
            .Slot("root", "inline-flex items-center -space-x-2 rtl:space-x-reverse")
            .Slot("avatar", "ring-2 ring-(--klyroo-bg)");

    /// <summary>
    /// Pure CSS on the container: direct children (and the field inside a wrapper) lose the radii and ring overlap on
    /// the sides where they touch a neighbor. No API of the children changes.
    /// </summary>
    public static ComponentTheme FieldGroup() =>
        new ComponentTheme("fieldGroup", "base")
            .Slot("base", "relative inline-flex *:relative *:focus-within:z-10")
            .Variant("orientation", "horizontal", "base", "flex-row " +
                "[&>:not(:first-child)]:rounded-s-none [&>:not(:last-child)]:rounded-e-none " +
                "[&>:not(:first-child)_:is(input,select,textarea,button)]:rounded-s-none [&>:not(:last-child)_:is(input,select,textarea,button)]:rounded-e-none " +
                "[&>:not(:first-child)]:-ms-px")
            .Variant("orientation", "vertical", "base", "flex-col " +
                "[&>:not(:first-child)]:rounded-t-none [&>:not(:last-child)]:rounded-b-none " +
                "[&>:not(:first-child)_:is(input,select,textarea,button)]:rounded-t-none [&>:not(:last-child)_:is(input,select,textarea,button)]:rounded-b-none " +
                "[&>:not(:first-child)]:-mt-px")
            .Default("orientation", "horizontal");
}
