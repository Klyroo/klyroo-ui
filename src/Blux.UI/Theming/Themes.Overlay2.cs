namespace Blux.UI.Theming;

internal static partial class Themes
{
    private const string PanelBase =
        "fixed m-0 flex-col overflow-hidden p-0 open:flex bg-(--blux-bg-overlay) text-(--blux-text) shadow-xl ring ring-(--blux-border) " +
        "backdrop:bg-zinc-950/60 focus:outline-none transition-transform duration-200 motion-reduce:transition-none";

    private const string PanelClose =
        "rounded-(--blux-radius) p-1 text-(--blux-text-muted) hover:bg-(--blux-bg-elevated) hover:text-(--blux-text) " +
        "focus-visible:outline-2 focus-visible:outline-(--blux-primary-text)";

    /// <summary>
    /// A panel is a native modal dialog docked to one edge. It slides in with a transition from <c>starting:</c>,
    /// which <c>prefers-reduced-motion</c> turns off, and keeps the rest of the modal layout.
    /// </summary>
    private static ComponentTheme PanelParts(ComponentTheme t, string variant, string inset) => t
        .Slot("header", "flex items-start gap-1.5 border-b border-(--blux-border) p-4 sm:px-6")
        .Slot("wrapper", "min-w-0 flex-1")
        .Slot("title", "text-base font-semibold text-(--blux-text)")
        .Slot("description", "mt-1 text-sm text-(--blux-text-muted)")
        .Slot("close", "-me-1 -mt-0.5 shrink-0 " + PanelClose)
        .Slot("body", "flex-1 overflow-y-auto p-4 sm:p-6")
        .Slot("footer", "flex items-center justify-end gap-1.5 border-t border-(--blux-border) p-4 sm:px-6")
        .Variant(variant, "right", "content", "inset-y-0 end-0 start-auto h-dvh w-full max-w-md max-h-none pe-(--bx-safe-right) pt-(--bx-safe-top) pb-(--bx-safe-bottom) translate-x-full rtl:-translate-x-full open:translate-x-0 rtl:open:translate-x-0 starting:open:translate-x-full rtl:starting:open:-translate-x-full")
        .Variant(variant, "left", "content", "inset-y-0 start-0 end-auto h-dvh w-full max-w-md max-h-none ps-(--bx-safe-left) pt-(--bx-safe-top) pb-(--bx-safe-bottom) -translate-x-full rtl:translate-x-full open:translate-x-0 rtl:open:translate-x-0 starting:open:-translate-x-full rtl:starting:open:translate-x-full")
        .Variant(variant, "top", "content", "inset-x-0 top-0 bottom-auto w-full max-w-none max-h-[85dvh] pt-(--bx-safe-top) ps-(--bx-safe-left) pe-(--bx-safe-right) -translate-y-full open:translate-y-0 starting:open:-translate-y-full rounded-b-[calc(var(--blux-radius)*2)]")
        .Variant(variant, "bottom", "content", "inset-x-0 bottom-0 top-auto w-full max-w-none max-h-[85dvh] pb-(--bx-safe-bottom) ps-(--bx-safe-left) pe-(--bx-safe-right) translate-y-full open:translate-y-0 starting:open:translate-y-full rounded-t-[calc(var(--blux-radius)*2)]")
        .Compound(new() { [variant] = "right", [inset] = "true" }, "content", "inset-y-2 end-2 h-[calc(100dvh_-_1rem)] w-[calc(100%_-_1rem)] rounded-[calc(var(--blux-radius)*2)]")
        .Compound(new() { [variant] = "left", [inset] = "true" }, "content", "inset-y-2 start-2 h-[calc(100dvh_-_1rem)] w-[calc(100%_-_1rem)] rounded-[calc(var(--blux-radius)*2)]")
        .Compound(new() { [variant] = "top", [inset] = "true" }, "content", "top-2 inset-x-2 w-[calc(100%_-_1rem)] rounded-[calc(var(--blux-radius)*2)]")
        .Compound(new() { [variant] = "bottom", [inset] = "true" }, "content", "bottom-2 inset-x-2 w-[calc(100%_-_1rem)] rounded-[calc(var(--blux-radius)*2)]");

    public static ComponentTheme Drawer() =>
        PanelParts(
            new ComponentTheme("drawer", "content", "handle", "header", "wrapper", "title", "description", "close", "body", "footer")
                .Slot("content", PanelBase)
                .Slot("handle", "mx-auto my-3 h-1.5 w-12 shrink-0 rounded-full bg-(--blux-border-accented)")
                .Variant("direction", "left", "handle", "hidden")
                .Variant("direction", "right", "handle", "hidden")
                .Variant("direction", "top", "handle", "order-last")
                .Default("direction", "bottom")
                .Default("inset", "false"),
            "direction", "inset");

    public static ComponentTheme Slideover() =>
        PanelParts(
            new ComponentTheme("slideover", "content", "header", "wrapper", "title", "description", "close", "body", "footer")
                .Slot("content", PanelBase)
                .Default("side", "right")
                .Default("inset", "false"),
            "side", "inset");

    /// <summary>The menu look is the DropdownMenu one, so both menus stay identical.</summary>
    public static ComponentTheme ContextMenu()
    {
        var menu = DropdownMenu();
        var t = new ComponentTheme("contextMenu", "root", "trigger", "content", "group", "label", "separator", "item", "itemIcon", "itemLabel", "itemShortcut")
            .Slot("root", "block")
            .Slot("trigger", "[-webkit-touch-callout:none] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--blux-primary-text)");
        foreach (var (slot, classes) in menu.Slots) t.Slot(slot, classes);
        return t;
    }

    public static ComponentTheme CommandPalette() =>
        new ComponentTheme("commandPalette", "root", "inputWrapper", "inputIcon", "input", "listbox", "group", "groupLabel",
                "item", "itemIcon", "itemWrapper", "itemLabel", "itemDescription", "itemShortcut", "itemCount", "empty", "status")
            .Slot("root", "group/cmd block w-full min-w-0 text-(--blux-text)")
            .Slot("inputWrapper", "flex items-center gap-2 border-b border-(--blux-border) px-4")
            .Slot("inputIcon", "size-5 shrink-0 text-(--blux-text-muted)")
            .Slot("input", "min-w-0 flex-1 bg-transparent py-3.5 text-sm text-(--blux-text) placeholder:text-(--blux-text-dimmed) " +
                           "focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-(--blux-primary-text)")
            .Slot("listbox", "max-h-96 scroll-py-1 overflow-y-auto p-1 focus:outline-none")
            .Slot("group", "data-filtered:hidden")
            .Slot("groupLabel", "px-2 py-1.5 text-xs font-semibold text-(--blux-text-muted)")
            .Slot("item", "relative flex cursor-pointer select-none items-center gap-2 rounded-[calc(var(--blux-radius)_-_2px)] px-2 py-2 text-sm text-(--blux-text) " +
                          "data-highlighted:bg-(--blux-bg-elevated) data-filtered:hidden aria-disabled:cursor-not-allowed aria-disabled:opacity-60")
            .Slot("itemIcon", "size-5 shrink-0 text-(--blux-text-muted)")
            .Slot("itemWrapper", "flex min-w-0 flex-1 flex-col")
            .Slot("itemLabel", "truncate")
            .Slot("itemDescription", "truncate text-xs text-(--blux-text-muted)")
            .Slot("itemShortcut", "ms-auto ps-4 text-xs text-(--blux-text-muted)")
            .Slot("itemCount", "shrink-0 ps-2 text-xs tabular-nums text-(--blux-text-muted)")
            .Slot("empty", "hidden px-6 py-10 text-center text-sm text-(--blux-text-muted) group-data-[empty]/cmd:block")
            .Slot("status", "sr-only");

    public static ComponentTheme ColorModeButton() =>
        new ComponentTheme("colorModeButton", "moon", "sun")
            .Slot("moon", "size-full dark:hidden")
            .Slot("sun", "hidden size-full dark:block");

    /// <summary>
    /// The visual state follows the <c>dark</c> class on the page, not the checkbox, so a static page is correct from the
    /// first paint. The checkbox is kept in sync by the browser script for assistive technology.
    /// </summary>
    public static ComponentTheme ColorModeSwitch() =>
        new ComponentTheme("colorModeSwitch", "root", "container", "input", "thumb", "moon", "sun", "wrapper", "label")
            .Slot("root", "group relative flex items-center gap-2.5")
            .Slot("container", "relative inline-flex shrink-0 items-center")
            .Slot("input", "peer appearance-none shrink-0 cursor-pointer rounded-full bg-(--blux-control) transition-colors dark:bg-(--bx-color) " + FocusRing)
            .Slot("thumb", "pointer-events-none absolute start-0.5 flex items-center justify-center rounded-full bg-(--blux-bg) text-(--blux-text) shadow-sm transition " +
                           "dark:bg-(--bx-color-contrast) dark:text-(--bx-color)")
            .Slot("moon", "size-3/4 dark:hidden")
            .Slot("sun", "hidden size-3/4 dark:block")
            .Slot("wrapper", "flex flex-col")
            .Slot("label", "font-medium text-(--blux-text) cursor-pointer")
            .Variant("size", "xs", "input", "h-4 w-7")
            .Variant("size", "xs", "thumb", "size-3 dark:translate-x-3 rtl:dark:-translate-x-3")
            .Variant("size", "xs", "wrapper", "text-xs")
            .Variant("size", "sm", "input", "h-[18px] w-8")
            .Variant("size", "sm", "thumb", "size-3.5 dark:translate-x-3.5 rtl:dark:-translate-x-3.5")
            .Variant("size", "sm", "wrapper", "text-xs")
            .Variant("size", "md", "input", "h-5 w-9")
            .Variant("size", "md", "thumb", "size-4 dark:translate-x-4 rtl:dark:-translate-x-4")
            .Variant("size", "md", "wrapper", "text-sm")
            .Variant("size", "lg", "input", "h-[22px] w-10")
            .Variant("size", "lg", "thumb", "size-[18px] dark:translate-x-[18px] rtl:dark:-translate-x-[18px]")
            .Variant("size", "lg", "wrapper", "text-sm")
            .Variant("size", "xl", "input", "h-6 w-11")
            .Variant("size", "xl", "thumb", "size-5 dark:translate-x-5 rtl:dark:-translate-x-5")
            .Variant("size", "xl", "wrapper", "text-base")
            .Default("size", "md");

    public static ComponentTheme ColorModeSelect()
    {
        var t = new ComponentTheme("colorModeSelect", "root", "base", "trailingIcon")
            .Slot("root", "relative inline-flex w-full")
            .Slot("base", "w-full cursor-pointer appearance-none rounded-(--blux-radius) border-0 transition-colors focus:outline-none")
            .Slot("trailingIcon", "pointer-events-none absolute inset-y-0 end-2.5 my-auto shrink-0 text-(--blux-text-dimmed)");
        FieldVariants(t, "base");
        return t
            .Variant("size", "xs", "base", "pe-7").Variant("size", "sm", "base", "pe-8").Variant("size", "md", "base", "pe-9")
            .Variant("size", "lg", "base", "pe-10").Variant("size", "xl", "base", "pe-11")
            .Variant("size", "xs", "trailingIcon", "size-4").Variant("size", "sm", "trailingIcon", "size-4").Variant("size", "md", "trailingIcon", "size-5")
            .Variant("size", "lg", "trailingIcon", "size-5").Variant("size", "xl", "trailingIcon", "size-6");
    }

    public static ComponentTheme ColorModeAvatar() =>
        new ComponentTheme("colorModeAvatar", "light", "dark")
            .Slot("light", "dark:hidden")
            .Slot("dark", "hidden dark:inline-flex");

    public static ComponentTheme ColorModeImage() =>
        new ComponentTheme("colorModeImage", "light", "dark")
            .Slot("light", "dark:hidden")
            .Slot("dark", "hidden dark:block");
}
