namespace Blux.UI.Theming;

// Themes of the "Form" family: SelectMenu, InputMenu, InputNumber, InputTags, PinInput, Slider, CheckboxGroup,
// Toggle, ToggleGroup, Listbox, InputRating and Form.
internal static partial class Themes
{
    private const string ContainerFocus = "has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-(--bx-color-text)";
    private const string ContainerInvalid = "has-[[aria-invalid=true]]:ring-2 has-[[aria-invalid=true]]:ring-(--blux-error)";
    private const string HiddenAttribute = "[&[hidden]]:hidden";

    private const string ListItem =
        "group relative flex cursor-pointer select-none items-center gap-1.5 rounded-[calc(var(--blux-radius)_-_2px)] px-2 py-1.5 text-sm " +
        "data-highlighted:bg-(--blux-bg-elevated) aria-disabled:cursor-not-allowed aria-disabled:opacity-60 " + HiddenAttribute;

    private const string ListItemIcon = "invisible ms-auto size-4 shrink-0 text-(--bx-color-text) group-aria-selected:visible";

    private const string Popup =
        "m-0 p-0 inset-auto overflow-hidden rounded-(--blux-radius) border border-(--blux-border) bg-(--blux-bg-overlay) text-(--blux-text) shadow-lg";

    /// <summary>Visual variants of a field, for the slot that carries the ring and the background.</summary>
    private static ComponentTheme FieldLook(ComponentTheme t, string slot, string focus, string invalid) => t
        .Variant("variant", "outline", slot, "bg-(--blux-bg) text-(--blux-text) ring ring-inset ring-(--blux-border-accented) " + focus + " " + invalid)
        .Variant("variant", "soft", slot, "bg-(--blux-bg-elevated)/50 text-(--blux-text) hover:bg-(--blux-bg-elevated) " + focus + " " + invalid)
        .Variant("variant", "subtle", slot, "bg-(--blux-bg-elevated) text-(--blux-text) ring ring-inset ring-(--blux-border-accented) " + focus + " " + invalid)
        .Variant("variant", "ghost", slot, "bg-transparent text-(--blux-text) hover:bg-(--blux-bg-elevated) " + focus + " " + invalid)
        .Variant("variant", "none", slot, "bg-transparent text-(--blux-text)")
        .Default("variant", "outline");

    private static ComponentTheme FieldSizes(ComponentTheme t, string slot) => t
        .Variant("size", "xs", slot, "px-2 py-1 text-xs gap-1")
        .Variant("size", "sm", slot, "px-2.5 py-1.5 text-xs gap-1.5")
        .Variant("size", "md", slot, "px-2.5 py-1.5 text-sm gap-1.5")
        .Variant("size", "lg", slot, "px-3 py-2 text-sm gap-2")
        .Variant("size", "xl", slot, "px-3 py-2 text-base gap-2")
        .Default("size", "md");

    private static ComponentTheme PopupItems(ComponentTheme t, string listboxClass) => t
        .Slot("content", Popup)
        .Slot("listbox", listboxClass)
        .Slot("item", ListItem)
        .Slot("itemLabel", "truncate")
        .Slot("itemIcon", ListItemIcon)
        .Slot("empty", "px-3 py-2 text-sm text-(--blux-text-muted) " + HiddenAttribute);

    public static ComponentTheme SelectMenu()
    {
        var t = new ComponentTheme("selectMenu", "root", "base", "label", "trailingIcon", "content", "searchWrapper", "search", "listbox", "item", "itemLabel", "itemIcon", "empty", "create")
            .Slot("create", "before:content-[attr(data-label)]")
            .Slot("root", "relative inline-flex w-full")
            .Slot("base", "relative inline-flex w-full items-center rounded-(--blux-radius) text-start cursor-pointer focus:outline-none " +
                          "disabled:cursor-not-allowed disabled:opacity-60 transition-colors " + ControlInvalid)
            .Slot("label", "min-w-0 flex-1 truncate before:content-[attr(data-label)] data-placeholder:text-(--blux-text-dimmed)")
            .Slot("trailingIcon", "ms-auto shrink-0 text-(--blux-text-dimmed)")
            .Slot("searchWrapper", "border-b border-(--blux-border) p-1")
            .Slot("search", "block w-full rounded-[calc(var(--blux-radius)_-_2px)] border-0 bg-transparent px-2 py-1.5 text-sm text-(--blux-text) " +
                            "placeholder:text-(--blux-text-dimmed) focus:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-(--bx-color-text)");
        PopupItems(t, "max-h-60 overflow-y-auto p-1 focus:outline-none");
        FieldVariants(t, "base");
        t.Variant("size", "xs", "trailingIcon", "size-4").Variant("size", "sm", "trailingIcon", "size-4").Variant("size", "md", "trailingIcon", "size-5")
         .Variant("size", "lg", "trailingIcon", "size-5").Variant("size", "xl", "trailingIcon", "size-6");
        return t;
    }

    public static ComponentTheme InputMenu()
    {
        var t = new ComponentTheme("inputMenu", "root", "base", "trailing", "trailingIcon", "content", "listbox", "item", "itemLabel", "itemIcon", "empty")
            .Slot("root", "relative inline-flex w-full items-center")
            .Slot("base", "w-full rounded-(--blux-radius) border-0 appearance-none placeholder:text-(--blux-text-dimmed) focus:outline-none " +
                          "disabled:cursor-not-allowed disabled:opacity-60 transition-colors " + ControlInvalid)
            .Slot("trailing", "pointer-events-none absolute inset-y-0 end-0 flex items-center pe-2.5")
            .Slot("trailingIcon", "shrink-0 text-(--blux-text-dimmed)");
        PopupItems(t, "max-h-60 overflow-y-auto p-1");
        FieldVariants(t, "base");
        t.Variant("size", "xs", "base", "pe-7").Variant("size", "sm", "base", "pe-8").Variant("size", "md", "base", "pe-9")
         .Variant("size", "lg", "base", "pe-10").Variant("size", "xl", "base", "pe-11")
         .Variant("size", "xs", "trailingIcon", "size-4").Variant("size", "sm", "trailingIcon", "size-4").Variant("size", "md", "trailingIcon", "size-5")
         .Variant("size", "lg", "trailingIcon", "size-5").Variant("size", "xl", "trailingIcon", "size-6");
        return t;
    }

    public static ComponentTheme InputNumber()
    {
        const string button = "absolute flex items-center justify-center text-(--blux-text-muted) transition-colors hover:text-(--blux-text) disabled:cursor-not-allowed disabled:opacity-40 cursor-pointer focus:outline-none";
        var t = new ComponentTheme("inputNumber", "root", "base", "decrement", "increment", "icon")
            .Slot("root", "relative inline-flex w-full items-center")
            .Slot("base", "w-full rounded-(--blux-radius) border-0 appearance-none placeholder:text-(--blux-text-dimmed) focus:outline-none " +
                          "disabled:cursor-not-allowed disabled:opacity-60 transition-colors " + ControlInvalid)
            .Slot("decrement", button)
            .Slot("increment", button)
            .Slot("icon", "pointer-events-none")
            .Variant("orientation", "horizontal", "base", "px-9 text-center")
            .Variant("orientation", "horizontal", "decrement", "inset-y-0 start-0 w-9")
            .Variant("orientation", "horizontal", "increment", "inset-y-0 end-0 w-9")
            .Variant("orientation", "vertical", "base", "ps-2.5 pe-9")
            .Variant("orientation", "vertical", "decrement", "bottom-0 end-0 h-1/2 w-8")
            .Variant("orientation", "vertical", "increment", "top-0 end-0 h-1/2 w-8")
            .Variant("size", "xs", "icon", "size-3.5").Variant("size", "sm", "icon", "size-4").Variant("size", "md", "icon", "size-4")
            .Variant("size", "lg", "icon", "size-5").Variant("size", "xl", "icon", "size-5")
            .Variant("size", "xs", "base", "py-1 text-xs").Variant("size", "sm", "base", "py-1.5 text-xs").Variant("size", "md", "base", "py-1.5 text-sm")
            .Variant("size", "lg", "base", "py-2 text-sm").Variant("size", "xl", "base", "py-2 text-base")
            .Default("size", "md").Default("orientation", "horizontal");
        return FieldLook(t, "base", ControlFocus, "");
    }

    public static ComponentTheme InputTags()
    {
        var t = new ComponentTheme("inputTags", "root", "tags", "tag", "remove", "input")
            .Slot("tags", "contents")
            .Slot("root", "relative flex w-full flex-wrap items-center rounded-(--blux-radius) transition-colors has-disabled:cursor-not-allowed has-disabled:opacity-60 cursor-text " + ContainerInvalid)
            .Slot("tag", "inline-flex max-w-full items-center gap-1 rounded-[calc(var(--blux-radius)_-_2px)] bg-(--bx-color)/10 py-0.5 ps-2 pe-1 text-xs font-medium text-(--bx-color-text) ring ring-inset ring-(--bx-color)/25")
            .Slot("remove", "inline-flex shrink-0 cursor-pointer rounded-full p-0.5 hover:bg-(--bx-color)/20 focus-visible:outline-2 focus-visible:outline-(--bx-color-text) [&>svg]:size-3")
            .Slot("input", "min-w-20 flex-1 border-0 bg-transparent p-0 py-0.5 placeholder:text-(--blux-text-dimmed) focus:outline-none focus-visible:outline-none disabled:cursor-not-allowed");
        FieldLook(t, "root", ContainerFocus, "");
        return FieldSizes(t, "root");
    }

    public static ComponentTheme PinInput()
    {
        var t = new ComponentTheme("pinInput", "root", "cell")
            .Slot("root", "inline-flex items-center")
            .Slot("cell", "appearance-none rounded-(--blux-radius) border-0 p-0 text-center focus:outline-none disabled:cursor-not-allowed disabled:opacity-60 transition-colors " + ControlInvalid)
            .Variant("size", "xs", "cell", "size-7 text-xs").Variant("size", "xs", "root", "gap-1")
            .Variant("size", "sm", "cell", "size-8 text-xs").Variant("size", "sm", "root", "gap-1.5")
            .Variant("size", "md", "cell", "size-9 text-sm").Variant("size", "md", "root", "gap-1.5")
            .Variant("size", "lg", "cell", "size-10 text-sm").Variant("size", "lg", "root", "gap-2")
            .Variant("size", "xl", "cell", "size-11 text-base").Variant("size", "xl", "root", "gap-2")
            .Default("size", "md");
        return FieldLook(t, "cell", ControlFocus, "");
    }

    public static ComponentTheme Slider()
    {
        const string thumb =
            "m-0 cursor-pointer appearance-none bg-transparent focus:outline-none disabled:cursor-not-allowed " +
            "[&::-webkit-slider-runnable-track]:bg-transparent [&::-moz-range-track]:bg-transparent " +
            "[&::-webkit-slider-thumb]:pointer-events-auto [&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:border-0 " +
            "[&::-webkit-slider-thumb]:bg-(--blux-bg) [&::-webkit-slider-thumb]:ring-2 [&::-webkit-slider-thumb]:ring-(--bx-color-text) [&::-webkit-slider-thumb]:shadow-sm " +
            "[&::-moz-range-thumb]:pointer-events-auto [&::-moz-range-thumb]:rounded-full [&::-moz-range-thumb]:border-2 [&::-moz-range-thumb]:border-solid " +
            "[&::-moz-range-thumb]:border-(--bx-color-text) [&::-moz-range-thumb]:bg-(--blux-bg) " +
            "focus-visible:[&::-webkit-slider-thumb]:outline-2 focus-visible:[&::-webkit-slider-thumb]:outline-offset-2 focus-visible:[&::-webkit-slider-thumb]:outline-(--bx-color-text) " +
            "focus-visible:[&::-moz-range-thumb]:outline-2 focus-visible:[&::-moz-range-thumb]:outline-offset-2 focus-visible:[&::-moz-range-thumb]:outline-(--bx-color-text)";
        var t = new ComponentTheme("slider", "root", "track", "range", "thumb")
            .Slot("root", "relative flex touch-none select-none items-center has-disabled:opacity-60")
            .Slot("track", "absolute overflow-hidden rounded-full bg-(--blux-bg-accented)")
            .Slot("range", "absolute bg-(--bx-color)")
            .Slot("thumb", "absolute " + thumb)
            .Variant("range", "true", "thumb", "pointer-events-none")
            .Variant("orientation", "horizontal", "root", "w-full")
            .Variant("orientation", "horizontal", "track", "inset-x-0 top-1/2 h-1.5 -translate-y-1/2")
            .Variant("orientation", "horizontal", "range", "inset-y-0 start-(--bx-lo) end-[calc(100%-var(--bx-hi))]")
            .Variant("orientation", "horizontal", "thumb", "inset-x-0 w-full")
            .Variant("orientation", "vertical", "root", "h-48 justify-center")
            .Variant("orientation", "vertical", "track", "inset-y-0 left-1/2 w-1.5 -translate-x-1/2")
            .Variant("orientation", "vertical", "range", "inset-x-0 bottom-(--bx-lo) top-[calc(100%-var(--bx-hi))]")
            .Variant("orientation", "vertical", "thumb", "inset-y-0 h-full [direction:rtl] [writing-mode:vertical-lr]")
            .Default("orientation", "horizontal").Default("size", "md").Default("range", "false");
        foreach (var (size, dimension, rem) in new[] { ("xs", "3", "0.75rem"), ("sm", "3.5", "0.875rem"), ("md", "4", "1rem"), ("lg", "5", "1.25rem"), ("xl", "6", "1.5rem") })
        {
            t.Variant("size", size, "root", $"[--bx-thumb:{rem}]")
             .Variant("size", size, "thumb", $"[&::-webkit-slider-thumb]:size-{dimension} [&::-moz-range-thumb]:size-{dimension}")
             .Compound(new() { ["size"] = size, ["orientation"] = "horizontal" }, "root", $"h-{dimension}")
             .Compound(new() { ["size"] = size, ["orientation"] = "horizontal" }, "thumb", $"h-{dimension} [&::-webkit-slider-runnable-track]:h-{dimension}")
             .Compound(new() { ["size"] = size, ["orientation"] = "vertical" }, "root", $"w-{dimension}")
             .Compound(new() { ["size"] = size, ["orientation"] = "vertical" }, "thumb", $"w-{dimension} [&::-webkit-slider-runnable-track]:w-{dimension}");
        }
        return t;
    }

    public static ComponentTheme CheckboxGroup() =>
        new ComponentTheme("checkboxGroup", "root", "legend", "fieldset", "item", "container", "base", "icon", "wrapper", "label", "description")
            .Slot("root", "relative min-w-0 border-0 p-0 m-0")
            .Slot("legend", "mb-1 block font-medium text-(--blux-text)")
            .Slot("fieldset", "flex flex-col gap-1.5")
            .Slot("item", "group flex items-start gap-2")
            .Slot("container", "relative flex shrink-0 items-center justify-center")
            .Slot("base", "peer appearance-none shrink-0 cursor-pointer rounded-[calc(var(--blux-radius)_-_2px)] bg-(--blux-bg) ring ring-inset ring-(--blux-border-accented) transition-colors " +
                          "checked:bg-(--bx-color) checked:ring-(--bx-color) disabled:cursor-not-allowed disabled:opacity-60 " +
                          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--bx-color-text) " + ControlInvalid)
            .Slot("icon", "pointer-events-none absolute text-(--bx-color-contrast) opacity-0 peer-checked:opacity-100")
            .Slot("wrapper", "flex flex-col")
            .Slot("label", "font-medium text-(--blux-text) cursor-pointer group-has-disabled:cursor-not-allowed group-has-disabled:opacity-60")
            .Slot("description", "text-(--blux-text-muted)")
            .Variant("orientation", "horizontal", "fieldset", "flex-row flex-wrap gap-x-4")
            .Variant("size", "xs", "base", "size-3").Variant("size", "xs", "icon", "size-2.5").Variant("size", "xs", "root", "text-xs").Variant("size", "xs", "container", "h-4")
            .Variant("size", "sm", "base", "size-3.5").Variant("size", "sm", "icon", "size-3").Variant("size", "sm", "root", "text-xs").Variant("size", "sm", "container", "h-4")
            .Variant("size", "md", "base", "size-4").Variant("size", "md", "icon", "size-3.5").Variant("size", "md", "root", "text-sm").Variant("size", "md", "container", "h-5")
            .Variant("size", "lg", "base", "size-[18px]").Variant("size", "lg", "icon", "size-4").Variant("size", "lg", "root", "text-sm").Variant("size", "lg", "container", "h-5")
            .Variant("size", "xl", "base", "size-5").Variant("size", "xl", "icon", "size-4.5").Variant("size", "xl", "root", "text-base").Variant("size", "xl", "container", "h-6")
            .Default("size", "md").Default("orientation", "vertical");

    private static ComponentTheme ToggleLook(ComponentTheme t, string slot) => t
        .Slot(slot, "inline-flex shrink-0 cursor-pointer select-none items-center justify-center rounded-(--blux-radius) font-medium transition-colors " +
                    "disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--bx-color-text)")
        .Variant("variant", "outline", slot, "bg-(--blux-bg) text-(--blux-text) ring ring-inset ring-(--blux-border-accented) hover:bg-(--blux-bg-elevated) aria-pressed:bg-(--bx-color)/15 aria-pressed:text-(--bx-color-text) aria-pressed:ring-(--bx-color)/50")
        .Variant("variant", "soft", slot, "bg-(--blux-bg-elevated) text-(--blux-text) hover:bg-(--blux-bg-accented) aria-pressed:bg-(--bx-color)/20 aria-pressed:text-(--bx-color-text)")
        .Variant("variant", "ghost", slot, "text-(--blux-text-muted) hover:bg-(--blux-bg-elevated) hover:text-(--blux-text) aria-pressed:bg-(--bx-color)/15 aria-pressed:text-(--bx-color-text)")
        .Variant("size", "xs", slot, "px-2 py-1 text-xs gap-1")
        .Variant("size", "sm", slot, "px-2.5 py-1.5 text-xs gap-1.5")
        .Variant("size", "md", slot, "px-2.5 py-1.5 text-sm gap-1.5")
        .Variant("size", "lg", slot, "px-3 py-2 text-sm gap-2")
        .Variant("size", "xl", slot, "px-3 py-2 text-base gap-2")
        .Default("variant", "outline").Default("size", "md");

    public static ComponentTheme Toggle() =>
        ToggleLook(new ComponentTheme("toggle", "base", "label", "icon"), "base")
            .Slot("label", "truncate")
            .Slot("icon", "shrink-0 size-4");

    public static ComponentTheme ToggleGroup() =>
        ToggleLook(new ComponentTheme("toggleGroup", "root", "item", "icon"), "item")
            .Slot("root", "inline-flex gap-1")
            .Slot("icon", "shrink-0 size-4")
            .Variant("orientation", "vertical", "root", "flex-col items-stretch")
            .Default("orientation", "horizontal");

    public static ComponentTheme Listbox() =>
        new ComponentTheme("listbox", "root", "base", "item", "itemLabel", "itemIcon")
            .Slot("root", "block w-full")
            .Slot("base", "max-h-60 w-full overflow-y-auto rounded-(--blux-radius) border border-(--blux-border-accented) bg-(--blux-bg) p-1 text-(--blux-text) " +
                          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--bx-color-text) aria-invalid:border-(--blux-error) aria-invalid:ring-1 aria-invalid:ring-(--blux-error)")
            .Slot("item", ListItem)
            .Slot("itemLabel", "min-w-0 flex-1 truncate")
            .Slot("itemIcon", ListItemIcon)
            .Variant("size", "xs", "item", "text-xs").Variant("size", "sm", "item", "text-xs").Variant("size", "md", "item", "text-sm")
            .Variant("size", "lg", "item", "text-sm").Variant("size", "xl", "item", "text-base")
            .Default("size", "md");

    public static ComponentTheme InputRating()
    {
        var t = new ComponentTheme("inputRating", "root", "input", "star", "icon", "fill", "filledIcon")
            .Slot("root", "relative inline-flex rounded has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-(--bx-color-text) " +
                          "has-disabled:opacity-60 has-[[aria-invalid=true]]:outline-2 has-[[aria-invalid=true]]:outline-(--blux-error)")
            .Slot("input", "peer sr-only")
            .Slot("star", "group relative block cursor-pointer overflow-hidden peer-disabled:cursor-not-allowed " +
                          "peer-checked:[&_svg]:fill-(--bx-color) peer-checked:[&_svg]:stroke-(--bx-color-text)")
            .Slot("icon", "block fill-transparent stroke-(--blux-text-dimmed) transition-colors")
            .Slot("fill", "absolute inset-y-0 start-0 overflow-hidden data-[fill=full]:w-full data-[fill=half]:w-1/2")
            .Slot("filledIcon", "block max-w-none fill-(--bx-color) stroke-(--bx-color-text)")
            .Variant("readonly", "false", "root", "flex-row-reverse")
            .Variant("readonly", "true", "root", "flex-row")
            .Variant("half", "true", "icon", "absolute inset-y-0 max-w-none group-data-[half=left]:start-0 group-data-[half=right]:end-0")
            .Default("readonly", "false").Default("half", "false").Default("size", "md");
        foreach (var (size, dimension, half) in new[] { ("xs", "4", "2"), ("sm", "5", "2.5"), ("md", "6", "3"), ("lg", "7", "3.5"), ("xl", "8", "4") })
        {
            t.Variant("size", size, "filledIcon", $"size-{dimension}")
             .Compound(new() { ["half"] = "false", ["size"] = size }, "star", $"size-{dimension}")
             .Compound(new() { ["half"] = "false", ["size"] = size }, "icon", $"size-{dimension}")
             .Compound(new() { ["half"] = "true", ["size"] = size }, "star", $"h-{dimension} w-{half}")
             .Compound(new() { ["half"] = "true", ["size"] = size }, "icon", $"h-{dimension} w-{dimension}");
        }
        return t;
    }

    public static ComponentTheme Form() =>
        new ComponentTheme("form", "root", "fieldset", "summary", "summaryTitle", "summaryList")
            .Slot("root", "w-full")
            .Slot("fieldset", "m-0 flex min-w-0 flex-col gap-4 border-0 p-0 disabled:opacity-60")
            .Slot("summary", "text-sm text-(--blux-error-text)")
            .Slot("summaryTitle", "font-medium")
            .Slot("summaryList", "list-disc rounded-(--blux-radius) border border-(--blux-error) bg-(--blux-error)/10 py-3 pe-3 ps-8");
}
