namespace Lux.UI.Theming;

// Themes of the "Form" family: SelectMenu, InputMenu, InputNumber, InputTags, PinInput, Slider, CheckboxGroup,
// Toggle, ToggleGroup, Listbox, InputRating and Form.
internal static partial class Themes
{
    private const string ContainerFocus = "has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-(--lx-color-text)";
    private const string ContainerInvalid = "has-[[aria-invalid=true]]:ring-2 has-[[aria-invalid=true]]:ring-(--lux-error)";
    private const string HiddenAttribute = "[&[hidden]]:hidden";

    private const string ListItem =
        "group relative flex cursor-pointer select-none items-center gap-1.5 rounded-[calc(var(--lux-radius)_-_2px)] px-2 py-1.5 text-sm " +
        "data-highlighted:bg-(--lux-bg-elevated) aria-disabled:cursor-not-allowed aria-disabled:opacity-60 " + HiddenAttribute;

    // With a count the count takes the free space (ms-auto), so the check mark must not take half of it.
    private const string ListItemIcon = "invisible ms-auto size-4 shrink-0 text-(--lx-color-text) group-aria-selected:visible group-data-has-count:ms-0";

    private const string ListItemLeadingIcon = "size-4 shrink-0 text-(--lux-text-muted)";

    private const string ListItemCount = "ms-auto shrink-0 ps-2 text-xs tabular-nums text-(--lux-text-muted)";

    // The icon of the selected item, put there by the browser; no icon, no space.
    private const string TriggerLeadingIcon = "shrink-0 text-(--lux-text-muted) empty:hidden [&>svg]:size-4";

    private const string ListGroup = "py-0.5";

    private const string ListGroupLabel = "px-2 py-1.5 text-xs font-semibold text-(--lux-text-muted)";

    private const string Popup =
        "m-0 p-0 inset-auto overflow-hidden rounded-(--lux-radius) border border-(--lux-border) bg-(--lux-bg-overlay) text-(--lux-text) shadow-lg";

    /// <summary>Visual variants of a field, for the slot that carries the ring and the background.</summary>
    private static ComponentTheme FieldLook(ComponentTheme t, string slot, string focus, string invalid) => t
        .Variant("variant", "outline", slot, "bg-(--lux-bg) text-(--lux-text) ring ring-inset ring-(--lux-border-accented) " + focus + " " + invalid)
        .Variant("variant", "soft", slot, "bg-(--lux-bg-elevated)/50 text-(--lux-text) hover:bg-(--lux-bg-elevated) " + focus + " " + invalid)
        .Variant("variant", "subtle", slot, "bg-(--lux-bg-elevated) text-(--lux-text) ring ring-inset ring-(--lux-border-accented) " + focus + " " + invalid)
        .Variant("variant", "ghost", slot, "bg-transparent text-(--lux-text) hover:bg-(--lux-bg-elevated) " + focus + " " + invalid)
        .Variant("variant", "none", slot, "bg-transparent text-(--lux-text)")
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
        .Slot("itemLeadingIcon", ListItemLeadingIcon)
        .Slot("itemCount", ListItemCount)
        .Slot("group", ListGroup)
        .Slot("groupLabel", ListGroupLabel)
        .Slot("empty", "px-3 py-2 text-sm text-(--lux-text-muted) " + HiddenAttribute);

    /// <summary>
    /// The states of a menu whose options come from an OptionsProvider. With one, the list scrolls inside "scroller" (the sentinel of the
    /// next page has to sit in the scrolled area, outside the listbox, which may only hold options), so the listbox stops scrolling.
    /// </summary>
    private static ComponentTheme RemoteSlots(ComponentTheme t) => t
        .Slot("scroller", "")
        .Variant("remote", "true", "scroller", "max-h-60 overflow-y-auto")
        .Variant("remote", "true", "listbox", "max-h-none overflow-visible")
        .Slot("loading", "flex items-center gap-2 px-3 py-2 text-sm text-(--lux-text-muted)")
        .Slot("spinner", "size-4 shrink-0 motion-safe:animate-spin")
        .Slot("error", "flex flex-col items-start gap-2 px-3 py-2 text-sm text-(--lux-error-text)")
        .Slot("announcer", "sr-only");

    public static ComponentTheme SelectMenu()
    {
        var t = new ComponentTheme("selectMenu", "root", "base", "label", "trailingIcon", "content", "searchWrapper", "search", "listbox", "item", "itemLabel", "itemIcon", "itemLeadingIcon", "itemCount", "group", "groupLabel", "leadingIcon", "empty", "create", "scroller", "loading", "spinner", "error", "announcer")
            .Slot("create", "before:content-[attr(data-label)]")
            .Slot("leadingIcon", TriggerLeadingIcon)
            .Slot("root", "relative inline-flex w-full")
            .Slot("base", "relative inline-flex w-full items-center rounded-(--lux-radius) text-start cursor-pointer focus:outline-none " +
                          "disabled:cursor-not-allowed disabled:opacity-60 transition-colors " + ControlInvalid)
            .Slot("label", "min-w-0 flex-1 truncate before:content-[attr(data-label)] data-placeholder:text-(--lux-text-dimmed)")
            .Slot("trailingIcon", "ms-auto shrink-0 text-(--lux-text-dimmed)")
            .Slot("searchWrapper", "border-b border-(--lux-border) p-1")
            .Slot("search", "block w-full rounded-[calc(var(--lux-radius)_-_2px)] border-0 bg-transparent px-2 py-1.5 text-sm text-(--lux-text) " +
                            "placeholder:text-(--lux-text-dimmed) focus:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-(--lx-color-text)");
        PopupItems(t, "max-h-60 overflow-y-auto p-1 focus:outline-none");
        RemoteSlots(t);
        FieldVariants(t, "base");
        t.Variant("size", "xs", "trailingIcon", "size-4").Variant("size", "sm", "trailingIcon", "size-4").Variant("size", "md", "trailingIcon", "size-5")
         .Variant("size", "lg", "trailingIcon", "size-5").Variant("size", "xl", "trailingIcon", "size-6");
        return t;
    }

    public static ComponentTheme InputMenu()
    {
        var t = new ComponentTheme("inputMenu", "root", "base", "trailing", "trailingIcon", "content", "listbox", "item", "itemLabel", "itemIcon", "itemLeadingIcon", "itemCount", "group", "groupLabel", "empty", "scroller", "loading", "spinner", "error", "announcer")
            .Slot("root", "relative inline-flex w-full items-center")
            .Slot("base", "w-full rounded-(--lux-radius) border-0 appearance-none placeholder:text-(--lux-text-dimmed) focus:outline-none " +
                          "disabled:cursor-not-allowed disabled:opacity-60 transition-colors " + ControlInvalid)
            .Slot("trailing", "pointer-events-none absolute inset-y-0 end-0 flex items-center pe-2.5")
            .Slot("trailingIcon", "shrink-0 text-(--lux-text-dimmed)");
        PopupItems(t, "max-h-60 overflow-y-auto p-1");
        RemoteSlots(t);
        FieldVariants(t, "base");
        t.Variant("size", "xs", "base", "pe-7").Variant("size", "sm", "base", "pe-8").Variant("size", "md", "base", "pe-9")
         .Variant("size", "lg", "base", "pe-10").Variant("size", "xl", "base", "pe-11")
         .Variant("size", "xs", "trailingIcon", "size-4").Variant("size", "sm", "trailingIcon", "size-4").Variant("size", "md", "trailingIcon", "size-5")
         .Variant("size", "lg", "trailingIcon", "size-5").Variant("size", "xl", "trailingIcon", "size-6");
        return t;
    }

    public static ComponentTheme InputNumber()
    {
        const string button = "absolute flex items-center justify-center text-(--lux-text-muted) transition-colors hover:text-(--lux-text) disabled:cursor-not-allowed disabled:opacity-40 cursor-pointer focus:outline-none";
        var t = new ComponentTheme("inputNumber", "root", "base", "decrement", "increment", "icon")
            .Slot("root", "relative inline-flex w-full items-center")
            .Slot("base", "w-full rounded-(--lux-radius) border-0 appearance-none placeholder:text-(--lux-text-dimmed) focus:outline-none " +
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
            .Slot("root", "relative flex w-full flex-wrap items-center rounded-(--lux-radius) transition-colors has-disabled:cursor-not-allowed has-disabled:opacity-60 cursor-text " + ContainerInvalid)
            .Slot("tag", "inline-flex max-w-full items-center gap-1 rounded-[calc(var(--lux-radius)_-_2px)] bg-(--lx-color)/10 py-0.5 ps-2 pe-1 text-xs font-medium text-(--lx-color-text) ring ring-inset ring-(--lx-color-text)/75")
            .Slot("remove", "inline-flex shrink-0 cursor-pointer rounded-full p-0.5 hover:bg-(--lx-color)/20 focus-visible:outline-2 focus-visible:outline-(--lx-color-text) [&>svg]:size-3")
            .Slot("input", "min-w-20 flex-1 border-0 bg-transparent p-0 py-0.5 placeholder:text-(--lux-text-dimmed) focus:outline-none focus-visible:outline-none disabled:cursor-not-allowed");
        FieldLook(t, "root", ContainerFocus, "");
        return FieldSizes(t, "root");
    }

    public static ComponentTheme PinInput()
    {
        var t = new ComponentTheme("pinInput", "root", "cell")
            .Slot("root", "inline-flex items-center")
            .Slot("cell", "appearance-none rounded-(--lux-radius) border-0 p-0 text-center focus:outline-none disabled:cursor-not-allowed disabled:opacity-60 transition-colors " + ControlInvalid)
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
            "[&::-webkit-slider-thumb]:bg-(--lux-bg) [&::-webkit-slider-thumb]:ring-2 [&::-webkit-slider-thumb]:ring-(--lx-color-text) [&::-webkit-slider-thumb]:shadow-sm " +
            "[&::-moz-range-thumb]:pointer-events-auto [&::-moz-range-thumb]:rounded-full [&::-moz-range-thumb]:border-2 [&::-moz-range-thumb]:border-solid " +
            "[&::-moz-range-thumb]:border-(--lx-color-text) [&::-moz-range-thumb]:bg-(--lux-bg) " +
            "focus-visible:[&::-webkit-slider-thumb]:outline-2 focus-visible:[&::-webkit-slider-thumb]:outline-offset-2 focus-visible:[&::-webkit-slider-thumb]:outline-(--lx-color-text) " +
            "focus-visible:[&::-moz-range-thumb]:outline-2 focus-visible:[&::-moz-range-thumb]:outline-offset-2 focus-visible:[&::-moz-range-thumb]:outline-(--lx-color-text)";
        var t = new ComponentTheme("slider", "root", "track", "range", "thumb")
            .Slot("root", "relative flex touch-none select-none items-center has-disabled:opacity-60")
            .Slot("track", "absolute overflow-hidden rounded-full bg-(--lux-bg-accented)")
            .Slot("range", "absolute bg-(--lx-color) ring-1 ring-inset ring-(--lx-color-text)")
            .Slot("thumb", "absolute " + thumb)
            .Variant("range", "true", "thumb", "pointer-events-none")
            .Variant("orientation", "horizontal", "root", "w-full")
            .Variant("orientation", "horizontal", "track", "inset-x-0 top-1/2 h-1.5 -translate-y-1/2")
            .Variant("orientation", "horizontal", "range", "inset-y-0 start-(--lx-lo) end-[calc(100%-var(--lx-hi))]")
            .Variant("orientation", "horizontal", "thumb", "inset-x-0 w-full")
            .Variant("orientation", "vertical", "root", "h-48 justify-center")
            .Variant("orientation", "vertical", "track", "inset-y-0 left-1/2 w-1.5 -translate-x-1/2")
            .Variant("orientation", "vertical", "range", "inset-x-0 bottom-(--lx-lo) top-[calc(100%-var(--lx-hi))]")
            .Variant("orientation", "vertical", "thumb", "inset-y-0 h-full [direction:rtl] [writing-mode:vertical-lr]")
            .Default("orientation", "horizontal").Default("size", "md").Default("range", "false");
        foreach (var (size, dimension, rem) in new[] { ("xs", "3", "0.75rem"), ("sm", "3.5", "0.875rem"), ("md", "4", "1rem"), ("lg", "5", "1.25rem"), ("xl", "6", "1.5rem") })
        {
            t.Variant("size", size, "root", $"[--lx-thumb:{rem}]")
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
            .Slot("legend", "mb-1 block font-medium text-(--lux-text)")
            .Slot("fieldset", "flex flex-col gap-1.5")
            .Slot("item", "group flex items-start gap-2")
            .Slot("container", "relative flex shrink-0 items-center justify-center")
            .Slot("base", "peer appearance-none shrink-0 cursor-pointer rounded-[calc(var(--lux-radius)_-_2px)] bg-(--lux-bg) ring ring-inset ring-(--lux-border-accented) transition-colors " +
                          "checked:bg-(--lx-color) checked:ring-(--lx-color-text) disabled:cursor-not-allowed disabled:opacity-60 " +
                          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--lx-color-text) " + ControlInvalid)
            .Slot("icon", "pointer-events-none absolute text-(--lx-color-contrast) opacity-0 peer-checked:opacity-100")
            .Slot("wrapper", "flex flex-col")
            .Slot("label", "font-medium text-(--lux-text) cursor-pointer group-has-disabled:cursor-not-allowed group-has-disabled:opacity-60")
            .Slot("description", "text-(--lux-text-muted)")
            .Variant("orientation", "horizontal", "fieldset", "flex-row flex-wrap gap-x-4")
            .Variant("size", "xs", "base", "size-3").Variant("size", "xs", "icon", "size-2.5").Variant("size", "xs", "root", "text-xs").Variant("size", "xs", "container", "h-4")
            .Variant("size", "sm", "base", "size-3.5").Variant("size", "sm", "icon", "size-3").Variant("size", "sm", "root", "text-xs").Variant("size", "sm", "container", "h-4")
            .Variant("size", "md", "base", "size-4").Variant("size", "md", "icon", "size-3.5").Variant("size", "md", "root", "text-sm").Variant("size", "md", "container", "h-5")
            .Variant("size", "lg", "base", "size-[18px]").Variant("size", "lg", "icon", "size-4").Variant("size", "lg", "root", "text-sm").Variant("size", "lg", "container", "h-5")
            .Variant("size", "xl", "base", "size-5").Variant("size", "xl", "icon", "size-4.5").Variant("size", "xl", "root", "text-base").Variant("size", "xl", "container", "h-6")
            .Default("size", "md").Default("orientation", "vertical");

    private static ComponentTheme ToggleLook(ComponentTheme t, string slot) => t
        .Slot(slot, "inline-flex shrink-0 cursor-pointer select-none items-center justify-center rounded-(--lux-radius) font-medium transition-colors " +
                    "disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--lx-color-text)")
        .Variant("variant", "outline", slot, "bg-(--lux-bg) text-(--lux-text) ring ring-inset ring-(--lux-border-accented) hover:bg-(--lux-bg-elevated) aria-pressed:bg-(--lx-color)/15 aria-pressed:text-(--lx-color-text) aria-pressed:ring-(--lx-color-text)/75")
        .Variant("variant", "soft", slot, "bg-(--lux-bg-elevated) text-(--lux-text) hover:bg-(--lux-bg-accented) aria-pressed:bg-(--lx-color)/20 aria-pressed:text-(--lx-color-text) aria-pressed:ring aria-pressed:ring-inset aria-pressed:ring-(--lx-color-text)/75")
        .Variant("variant", "ghost", slot, "text-(--lux-text-muted) hover:bg-(--lux-bg-elevated) hover:text-(--lux-text) aria-pressed:bg-(--lx-color)/15 aria-pressed:text-(--lx-color-text) aria-pressed:ring aria-pressed:ring-inset aria-pressed:ring-(--lx-color-text)/75")
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
        new ComponentTheme("listbox", "root", "base", "item", "itemLabel", "itemIcon", "itemLeadingIcon", "itemCount", "group", "groupLabel")
            .Slot("root", "block w-full")
            .Slot("base", "max-h-60 w-full overflow-y-auto rounded-(--lux-radius) border border-(--lux-border-accented) bg-(--lux-bg) p-1 text-(--lux-text) " +
                          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--lx-color-text) aria-invalid:border-(--lux-error) aria-invalid:ring-1 aria-invalid:ring-(--lux-error)")
            .Slot("item", ListItem)
            .Slot("itemLabel", "min-w-0 flex-1 truncate")
            .Slot("itemIcon", ListItemIcon)
            .Slot("itemLeadingIcon", ListItemLeadingIcon)
            .Slot("itemCount", ListItemCount)
            .Slot("group", ListGroup)
            .Slot("groupLabel", ListGroupLabel)
            .Variant("size", "xs", "item", "text-xs").Variant("size", "sm", "item", "text-xs").Variant("size", "md", "item", "text-sm")
            .Variant("size", "lg", "item", "text-sm").Variant("size", "xl", "item", "text-base")
            .Default("size", "md");

    public static ComponentTheme InputRating()
    {
        var t = new ComponentTheme("inputRating", "root", "input", "star", "icon", "fill", "filledIcon")
            .Slot("root", "relative inline-flex rounded has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-(--lx-color-text) " +
                          "has-disabled:opacity-60 has-[[aria-invalid=true]]:outline-2 has-[[aria-invalid=true]]:outline-(--lux-error)")
            .Slot("input", "peer sr-only")
            .Slot("star", "group relative block cursor-pointer overflow-hidden peer-disabled:cursor-not-allowed " +
                          // Filled when its own radio (just before it) or a later one is checked. peer-checked would
                          // also match every star after the checked one.
                          "[input:checked+&_svg_*]:fill-(--lx-color) [input:checked+&_svg_*]:stroke-(--lx-color-text) " +
                          "[&:has(~input:checked)_svg_*]:fill-(--lx-color) [&:has(~input:checked)_svg_*]:stroke-(--lx-color-text)")
            .Slot("icon", "block **:fill-transparent **:stroke-(--lux-text-dimmed) **:stroke-[1.5] **:transition-colors")
            .Slot("fill", "absolute inset-y-0 start-0 overflow-hidden data-[fill=full]:w-full data-[fill=half]:w-1/2")
            .Slot("filledIcon", "block max-w-none **:fill-(--lx-color) **:stroke-(--lx-color-text) **:stroke-[1.5]")
            .Variant("readonly", "false", "root", "flex-row")
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
            .Slot("summary", "text-sm text-(--lux-error-text)")
            .Slot("summaryTitle", "font-medium")
            .Slot("summaryList", "list-disc rounded-(--lux-radius) border border-(--lux-error) bg-(--lux-error)/10 py-3 pe-3 ps-8");
}
