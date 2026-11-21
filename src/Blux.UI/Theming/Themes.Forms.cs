namespace Blux.UI.Theming;

internal static partial class Themes
{
    private const string ControlFocus = "focus-visible:ring-2 focus-visible:ring-(--bx-color-text)";
    private const string ControlInvalid = "aria-invalid:ring-2 aria-invalid:ring-(--blux-error)";

    /// <summary>Text field look shared by Input, Textarea and the Select trigger.</summary>
    private static ComponentTheme FieldVariants(ComponentTheme t, string slot) => t
        .Variant("variant", "outline", slot, "bg-(--blux-bg) text-(--blux-text) ring ring-inset ring-(--blux-border-accented) " + ControlFocus)
        .Variant("variant", "soft", slot, "bg-(--blux-bg-elevated)/50 text-(--blux-text) hover:bg-(--blux-bg-elevated) focus:bg-(--blux-bg-elevated) " + ControlFocus)
        .Variant("variant", "subtle", slot, "bg-(--blux-bg-elevated) text-(--blux-text) ring ring-inset ring-(--blux-border-accented) " + ControlFocus)
        .Variant("variant", "ghost", slot, "bg-transparent text-(--blux-text) hover:bg-(--blux-bg-elevated) focus:bg-(--blux-bg-elevated) " + ControlFocus)
        .Variant("variant", "none", slot, "bg-transparent text-(--blux-text)")
        .Variant("size", "xs", slot, "px-2 py-1 text-xs gap-1")
        .Variant("size", "sm", slot, "px-2.5 py-1.5 text-xs gap-1.5")
        .Variant("size", "md", slot, "px-2.5 py-1.5 text-sm gap-1.5")
        .Variant("size", "lg", slot, "px-3 py-2 text-sm gap-2")
        .Variant("size", "xl", slot, "px-3 py-2 text-base gap-2")
        .Default("variant", "outline")
        .Default("size", "md");

    public static ComponentTheme Input()
    {
        var t = new ComponentTheme("input", "root", "base", "leading", "leadingIcon", "trailing", "trailingIcon")
            .Slot("root", "relative inline-flex w-full items-center")
            .Slot("base", "w-full rounded-(--blux-radius) border-0 appearance-none placeholder:text-(--blux-text-dimmed) focus:outline-none " +
                          "disabled:cursor-not-allowed disabled:opacity-60 transition-colors " + ControlInvalid)
            .Slot("leading", "pointer-events-none absolute inset-y-0 start-0 flex items-center")
            .Slot("leadingIcon", "shrink-0 text-(--blux-text-dimmed)")
            .Slot("trailing", "pointer-events-none absolute inset-y-0 end-0 flex items-center")
            .Slot("trailingIcon", "shrink-0 text-(--blux-text-dimmed)");
        FieldVariants(t, "base");
        foreach (var (size, padding, icon, inset) in new[]
        {
            ("xs", "ps-7", "size-4", "ps-2"), ("sm", "ps-8", "size-4", "ps-2.5"), ("md", "ps-9", "size-5", "ps-2.5"),
            ("lg", "ps-10", "size-5", "ps-3"), ("xl", "ps-11", "size-6", "ps-3"),
        })
        {
            t.Variant("size", size, "leadingIcon", icon).Variant("size", size, "trailingIcon", icon)
             .Variant("size", size, "leading", inset)
             .Compound(new() { ["leading"] = "true", ["size"] = size }, "base", padding);
        }
        t.Variant("size", "xs", "trailing", "pe-2").Variant("size", "sm", "trailing", "pe-2.5").Variant("size", "md", "trailing", "pe-2.5")
         .Variant("size", "lg", "trailing", "pe-3").Variant("size", "xl", "trailing", "pe-3")
         .Compound(new() { ["trailing"] = "true", ["size"] = "xs" }, "base", "pe-7")
         .Compound(new() { ["trailing"] = "true", ["size"] = "sm" }, "base", "pe-8")
         .Compound(new() { ["trailing"] = "true", ["size"] = "md" }, "base", "pe-9")
         .Compound(new() { ["trailing"] = "true", ["size"] = "lg" }, "base", "pe-10")
         .Compound(new() { ["trailing"] = "true", ["size"] = "xl" }, "base", "pe-11")
         .Default("leading", "false").Default("trailing", "false");
        return t;
    }

    public static ComponentTheme Textarea()
    {
        var t = new ComponentTheme("textarea", "base")
            .Slot("base", "block w-full rounded-(--blux-radius) border-0 appearance-none placeholder:text-(--blux-text-dimmed) focus:outline-none " +
                          "disabled:cursor-not-allowed disabled:opacity-60 transition-colors resize-y " + ControlInvalid)
            .Variant("autoresize", "true", "base", "field-sizing-content resize-none")
            .Default("autoresize", "false");
        return FieldVariants(t, "base");
    }

    public static ComponentTheme Checkbox() =>
        new ComponentTheme("checkbox", "root", "container", "base", "icon", "indeterminateIcon", "wrapper", "label", "description")
            .Slot("root", "group relative flex items-start gap-2")
            .Slot("container", "relative flex shrink-0 items-center justify-center")
            .Slot("base", "peer appearance-none shrink-0 cursor-pointer rounded-[calc(var(--blux-radius)_-_2px)] bg-(--blux-bg) ring ring-inset ring-(--blux-border-accented) transition-colors " +
                          "checked:bg-(--bx-color) checked:ring-(--bx-color-text) indeterminate:bg-(--bx-color) indeterminate:ring-(--bx-color-text) " +
                          "disabled:cursor-not-allowed disabled:opacity-60 " +
                          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--bx-color-text) " + ControlInvalid)
            .Slot("icon", "pointer-events-none absolute text-(--bx-color-contrast) opacity-0 peer-checked:opacity-100 peer-indeterminate:opacity-0")
            .Slot("indeterminateIcon", "pointer-events-none absolute text-(--bx-color-contrast) opacity-0 peer-indeterminate:opacity-100")
            .Slot("wrapper", "flex flex-col")
            .Slot("label", "font-medium text-(--blux-text) cursor-pointer group-data-disabled:cursor-not-allowed group-data-disabled:opacity-60")
            .Slot("description", "text-(--blux-text-muted)")
            .Variant("size", "xs", "base", "size-3").Variant("size", "xs", "icon", "size-2.5").Variant("size", "xs", "indeterminateIcon", "size-2.5").Variant("size", "xs", "wrapper", "text-xs").Variant("size", "xs", "container", "h-4")
            .Variant("size", "sm", "base", "size-3.5").Variant("size", "sm", "icon", "size-3").Variant("size", "sm", "indeterminateIcon", "size-3").Variant("size", "sm", "wrapper", "text-xs").Variant("size", "sm", "container", "h-4")
            .Variant("size", "md", "base", "size-4").Variant("size", "md", "icon", "size-3.5").Variant("size", "md", "indeterminateIcon", "size-3.5").Variant("size", "md", "wrapper", "text-sm").Variant("size", "md", "container", "h-5")
            .Variant("size", "lg", "base", "size-[18px]").Variant("size", "lg", "icon", "size-4").Variant("size", "lg", "indeterminateIcon", "size-4").Variant("size", "lg", "wrapper", "text-sm").Variant("size", "lg", "container", "h-5")
            .Variant("size", "xl", "base", "size-5").Variant("size", "xl", "icon", "size-4.5").Variant("size", "xl", "indeterminateIcon", "size-4.5").Variant("size", "xl", "wrapper", "text-base").Variant("size", "xl", "container", "h-6")
            .Default("size", "md");

    public static ComponentTheme RadioGroup() =>
        new ComponentTheme("radioGroup", "root", "legend", "fieldset", "item", "container", "base", "indicator", "wrapper", "label", "description")
            .Slot("root", "group/radio relative")
            .Slot("legend", "mb-1 block font-medium text-(--blux-text)")
            .Slot("fieldset", "flex flex-col gap-1.5")
            .Slot("item", "group flex items-start gap-2")
            .Slot("container", "relative flex shrink-0 items-center justify-center")
            .Slot("base", "peer appearance-none shrink-0 cursor-pointer rounded-full bg-(--blux-bg) ring ring-inset ring-(--blux-border-accented) transition-colors " +
                          "checked:bg-(--bx-color) checked:ring-(--bx-color-text) disabled:cursor-not-allowed disabled:opacity-60 " +
                          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--bx-color-text) " +
                          // aria-invalid belongs to the radio group, not to each radio.
                          "group-aria-invalid/radio:ring-2 group-aria-invalid/radio:ring-(--blux-error)")
            .Slot("indicator", "pointer-events-none absolute rounded-full bg-(--bx-color-contrast) opacity-0 peer-checked:opacity-100")
            .Slot("wrapper", "flex flex-col")
            .Slot("label", "font-medium text-(--blux-text) cursor-pointer group-has-disabled:cursor-not-allowed group-has-disabled:opacity-60")
            .Slot("description", "text-(--blux-text-muted)")
            .Variant("orientation", "horizontal", "fieldset", "flex-row flex-wrap gap-x-4")
            .Variant("size", "xs", "base", "size-3").Variant("size", "xs", "indicator", "size-1").Variant("size", "xs", "root", "text-xs").Variant("size", "xs", "container", "h-4")
            .Variant("size", "sm", "base", "size-3.5").Variant("size", "sm", "indicator", "size-1.5").Variant("size", "sm", "root", "text-xs").Variant("size", "sm", "container", "h-4")
            .Variant("size", "md", "base", "size-4").Variant("size", "md", "indicator", "size-1.5").Variant("size", "md", "root", "text-sm").Variant("size", "md", "container", "h-5")
            .Variant("size", "lg", "base", "size-[18px]").Variant("size", "lg", "indicator", "size-2").Variant("size", "lg", "root", "text-sm").Variant("size", "lg", "container", "h-5")
            .Variant("size", "xl", "base", "size-5").Variant("size", "xl", "indicator", "size-2").Variant("size", "xl", "root", "text-base").Variant("size", "xl", "container", "h-6")
            .Default("size", "md")
            .Default("orientation", "vertical");

    public static ComponentTheme FormField() =>
        new ComponentTheme("formField", "root", "labelWrapper", "label", "hint", "description", "container", "error", "help")
            .Slot("root", "")
            .Slot("labelWrapper", "flex items-center justify-between gap-1")
            .Slot("label", "block font-medium text-(--blux-text)")
            .Slot("hint", "text-(--blux-text-muted)")
            .Slot("description", "text-(--blux-text-muted)")
            .Slot("container", "relative mt-1")
            .Slot("error", "mt-2 text-(--blux-error-text)")
            .Slot("help", "mt-2 text-(--blux-text-muted)")
            .Variant("required", "true", "label", "after:ms-0.5 after:text-(--blux-error-text) after:content-['*']")
            .Variant("size", "xs", "root", "text-xs")
            .Variant("size", "sm", "root", "text-xs")
            .Variant("size", "md", "root", "text-sm")
            .Variant("size", "lg", "root", "text-sm")
            .Variant("size", "xl", "root", "text-base")
            .Default("size", "md")
            .Default("required", "false");

    public static ComponentTheme Select()
    {
        var t = new ComponentTheme("select", "root", "base", "label", "trailingIcon", "content", "listbox", "item", "itemLabel", "itemIcon")
            .Slot("root", "relative inline-flex w-full")
            .Slot("base", "relative inline-flex w-full items-center rounded-(--blux-radius) text-start cursor-pointer focus:outline-none " +
                          "disabled:cursor-not-allowed disabled:opacity-60 transition-colors " + ControlInvalid)
            .Slot("label", "min-w-0 flex-1 truncate before:content-[attr(data-label)] data-placeholder:text-(--blux-text-dimmed)")
            .Slot("trailingIcon", "ms-auto shrink-0 text-(--blux-text-dimmed)")
            .Slot("content", "m-0 p-0 inset-auto max-h-60 overflow-y-auto rounded-(--blux-radius) border border-(--blux-border) bg-(--blux-bg-overlay) text-(--blux-text) shadow-lg")
            .Slot("listbox", "p-1 focus:outline-none")
            .Slot("item", "group relative flex cursor-pointer select-none items-center gap-1.5 rounded-[calc(var(--blux-radius)_-_2px)] px-2 py-1.5 text-sm " +
                          "data-highlighted:bg-(--blux-bg-elevated) aria-disabled:cursor-not-allowed aria-disabled:opacity-60")
            .Slot("itemLabel", "truncate")
            .Slot("itemIcon", "invisible ms-auto size-4 shrink-0 text-(--bx-color-text) group-aria-selected:visible");
        FieldVariants(t, "base");
        t.Variant("size", "xs", "trailingIcon", "size-4").Variant("size", "sm", "trailingIcon", "size-4").Variant("size", "md", "trailingIcon", "size-5")
         .Variant("size", "lg", "trailingIcon", "size-5").Variant("size", "xl", "trailingIcon", "size-6");
        return t;
    }
}
