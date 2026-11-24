namespace Lux.UI.Theming;

/// <summary>
/// Default themes. Colors never appear here: components render <c>data-color</c> and themes use the
/// <c>--lx-color*</c> variables it sets, so any color works without new classes.
/// </summary>
internal static partial class Themes
{
    private const string FocusRing =
        "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--lx-color-text)";

    public static ComponentTheme Button()
    {
        var t = new ComponentTheme("button", "base", "label", "leadingIcon", "trailingIcon")
            .Slot("base", "inline-flex items-center justify-center font-medium rounded-(--lux-radius) transition-colors select-none " +
                          "disabled:cursor-not-allowed disabled:opacity-60 aria-disabled:cursor-not-allowed aria-disabled:opacity-60 " + FocusRing)
            .Slot("label", "truncate")
            .Slot("leadingIcon", "shrink-0")
            .Slot("trailingIcon", "shrink-0")

            .Variant("variant", "solid", "base", "bg-(--lx-color) ring ring-inset ring-(--lx-color-text) text-(--lx-color-contrast) hover:bg-(--lx-color)/90 active:bg-(--lx-color)/80 disabled:bg-(--lx-color) aria-disabled:bg-(--lx-color)")
            .Variant("variant", "outline", "base", "ring ring-inset ring-(--lx-color-text)/75 text-(--lx-color-text) hover:bg-(--lx-color)/10 active:bg-(--lx-color)/15 disabled:bg-transparent")
            .Variant("variant", "soft", "base", "bg-(--lx-color)/10 text-(--lx-color-text) hover:bg-(--lx-color)/15 active:bg-(--lx-color)/20 disabled:bg-(--lx-color)/10")
            .Variant("variant", "subtle", "base", "bg-(--lx-color)/10 ring ring-inset ring-(--lx-color-text)/75 text-(--lx-color-text) hover:bg-(--lx-color)/15 active:bg-(--lx-color)/20 disabled:bg-(--lx-color)/10")
            .Variant("variant", "ghost", "base", "text-(--lx-color-text) hover:bg-(--lx-color)/10 active:bg-(--lx-color)/15 disabled:bg-transparent")
            .Variant("variant", "link", "base", "text-(--lx-color-text) underline-offset-4 hover:underline disabled:no-underline")

            .Variant("size", "xs", "base", "px-2 py-1 text-xs gap-1")
            .Variant("size", "xs", "leadingIcon", "size-4")
            .Variant("size", "xs", "trailingIcon", "size-4")
            .Variant("size", "sm", "base", "px-2.5 py-1.5 text-xs gap-1.5")
            .Variant("size", "sm", "leadingIcon", "size-4")
            .Variant("size", "sm", "trailingIcon", "size-4")
            .Variant("size", "md", "base", "px-2.5 py-1.5 text-sm gap-1.5")
            .Variant("size", "md", "leadingIcon", "size-5")
            .Variant("size", "md", "trailingIcon", "size-5")
            .Variant("size", "lg", "base", "px-3 py-2 text-sm gap-2")
            .Variant("size", "lg", "leadingIcon", "size-5")
            .Variant("size", "lg", "trailingIcon", "size-5")
            .Variant("size", "xl", "base", "px-3 py-2 text-base gap-2")
            .Variant("size", "xl", "leadingIcon", "size-6")
            .Variant("size", "xl", "trailingIcon", "size-6")

            .Variant("block", "true", "base", "w-full")
            .Variant("loading", "true", "leadingIcon", "animate-spin")

            .Compound(new() { ["square"] = "true", ["size"] = "xs" }, "base", "p-1")
            .Compound(new() { ["square"] = "true", ["size"] = "sm" }, "base", "p-1.5")
            .Compound(new() { ["square"] = "true", ["size"] = "md" }, "base", "p-1.5")
            .Compound(new() { ["square"] = "true", ["size"] = "lg" }, "base", "p-2")
            .Compound(new() { ["square"] = "true", ["size"] = "xl" }, "base", "p-2")

            .Default("variant", "solid")
            .Default("size", "md")
            .Default("block", "false")
            .Default("square", "false")
            .Default("loading", "false");
        return t;
    }

    /// <summary>
    /// The visual state comes from the native checkbox (<c>:checked</c>, <c>:disabled</c>), never from the C# value,
    /// so static server rendering and a form post show the real state.
    /// </summary>
    public static ComponentTheme Switch() =>
        new ComponentTheme("switch", "root", "container", "input", "thumb", "wrapper", "label", "description")
            .Slot("root", "group relative flex items-start gap-2.5")
            .Slot("container", "relative inline-flex shrink-0 items-center")
            .Slot("input", "peer appearance-none shrink-0 cursor-pointer rounded-full bg-(--lux-control) transition-colors " +
                           "checked:bg-(--lx-color) checked:ring-1 checked:ring-inset checked:ring-(--lx-color-text) disabled:cursor-not-allowed disabled:opacity-60 " +
                           "aria-invalid:ring-2 aria-invalid:ring-inset aria-invalid:ring-(--lux-error) checked:aria-invalid:ring-2 checked:aria-invalid:ring-(--lux-error) " + FocusRing)
            .Slot("thumb", "pointer-events-none absolute start-0.5 rounded-full bg-white shadow-sm ring-0 transition peer-checked:bg-(--lx-color-contrast) peer-disabled:opacity-60")
            .Slot("wrapper", "flex flex-col")
            .Slot("label", "font-medium text-(--lux-text) cursor-pointer group-data-disabled:cursor-not-allowed group-data-disabled:opacity-60")
            .Slot("description", "text-(--lux-text-muted)")

            .Variant("size", "xs", "input", "h-4 w-7")
            .Variant("size", "xs", "thumb", "size-3 peer-checked:translate-x-3 rtl:peer-checked:-translate-x-3")
            .Variant("size", "xs", "wrapper", "text-xs")
            .Variant("size", "sm", "input", "h-[18px] w-8")
            .Variant("size", "sm", "thumb", "size-3.5 peer-checked:translate-x-3.5 rtl:peer-checked:-translate-x-3.5")
            .Variant("size", "sm", "wrapper", "text-xs")
            .Variant("size", "md", "input", "h-5 w-9")
            .Variant("size", "md", "thumb", "size-4 peer-checked:translate-x-4 rtl:peer-checked:-translate-x-4")
            .Variant("size", "md", "wrapper", "text-sm")
            .Variant("size", "lg", "input", "h-[22px] w-10")
            .Variant("size", "lg", "thumb", "size-[18px] peer-checked:translate-x-[18px] rtl:peer-checked:-translate-x-[18px]")
            .Variant("size", "lg", "wrapper", "text-sm")
            .Variant("size", "xl", "input", "h-6 w-11")
            .Variant("size", "xl", "thumb", "size-5 peer-checked:translate-x-5 rtl:peer-checked:-translate-x-5")
            .Variant("size", "xl", "wrapper", "text-base")

            .Default("size", "md");

    public static ComponentTheme Popover() =>
        new ComponentTheme("popover", "content")
            .Slot("content", "m-0 p-0 inset-auto overflow-visible min-w-32 max-w-[calc(100vw-1rem)] " +
                             "rounded-(--lux-radius) border border-(--lux-border) bg-(--lux-bg-overlay) text-(--lux-text) shadow-lg " +
                             "focus:outline-none");
}
