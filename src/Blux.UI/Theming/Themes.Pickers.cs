namespace Blux.UI.Theming;

internal static partial class Themes
{
    private const string PickerFocusWithin = "has-focus-visible:ring-2 has-focus-visible:ring-(--bx-color-text)";

    public static ComponentTheme Calendar() =>
        new ComponentTheme("calendar", "root", "header", "heading", "nav", "grid", "weekday", "cell", "day")
            .Slot("root", "inline-block w-fit text-(--blux-text)")
            .Slot("header", "mb-2 flex items-center justify-between gap-2")
            .Slot("heading", "flex-1 text-center font-medium text-(--blux-text-highlighted)")
            .Slot("nav", "inline-flex shrink-0 items-center justify-center rounded-(--blux-radius) p-1.5 text-(--blux-text-muted) transition-colors hover:bg-(--blux-bg-elevated) " +
                         "hover:text-(--blux-text-highlighted) aria-disabled:cursor-not-allowed aria-disabled:opacity-60 " + FocusRing)
            .Slot("grid", "border-separate border-spacing-0.5")
            .Slot("weekday", "p-0 text-center font-normal text-(--blux-text-muted)")
            .Slot("cell", "p-0 text-center")
            .Slot("day", "inline-flex cursor-pointer items-center justify-center rounded-(--blux-radius) transition-colors hover:bg-(--blux-bg-elevated) " + FocusRing + " " +
                         "data-today:ring-1 data-today:ring-inset data-today:ring-(--bx-color-text) data-in-range:rounded-none data-in-range:bg-(--bx-color)/15 " +
                         "data-selected:bg-(--bx-color) data-selected:text-(--bx-color-contrast) data-selected:hover:bg-(--bx-color)/90 " +
                         "aria-disabled:cursor-not-allowed aria-disabled:text-(--blux-text-dimmed) aria-disabled:line-through aria-disabled:hover:bg-transparent")
            .Variant("size", "xs", "day", "size-7 text-xs").Variant("size", "xs", "weekday", "text-xs").Variant("size", "xs", "heading", "text-xs")
            .Variant("size", "sm", "day", "size-8 text-xs").Variant("size", "sm", "weekday", "text-xs").Variant("size", "sm", "heading", "text-xs")
            .Variant("size", "md", "day", "size-9 text-sm").Variant("size", "md", "weekday", "text-xs").Variant("size", "md", "heading", "text-sm")
            .Variant("size", "lg", "day", "size-10 text-sm").Variant("size", "lg", "weekday", "text-sm").Variant("size", "lg", "heading", "text-sm")
            .Variant("size", "xl", "day", "size-11 text-base").Variant("size", "xl", "weekday", "text-sm").Variant("size", "xl", "heading", "text-base")
            .Default("size", "md");

    public static ComponentTheme InputDate()
    {
        var t = new ComponentTheme("inputDate", "root", "base", "trigger", "triggerButton", "triggerIcon", "content")
            .Slot("root", "relative inline-flex w-full items-center")
            .Slot("base", "w-full rounded-(--blux-radius) border-0 appearance-none focus:outline-none disabled:cursor-not-allowed disabled:opacity-60 transition-colors " +
                          "[&::-webkit-calendar-picker-indicator]:hidden " + ControlInvalid)
            .Slot("trigger", "absolute inset-y-0 end-0 flex items-center pe-1")
            .Slot("triggerButton", "inline-flex items-center justify-center rounded-[calc(var(--blux-radius)_-_2px)] p-1 text-(--blux-text-muted) transition-colors " +
                                   "hover:text-(--blux-text-highlighted) disabled:cursor-not-allowed disabled:opacity-60 " + FocusRing)
            .Slot("triggerIcon", "shrink-0")
            .Slot("content", "m-0 p-3 inset-auto rounded-(--blux-radius) border border-(--blux-border) bg-(--blux-bg-overlay) text-(--blux-text) shadow-lg");
        FieldVariants(t, "base");
        foreach (var (size, padding, icon) in new[] { ("xs", "pe-7", "size-4"), ("sm", "pe-8", "size-4"), ("md", "pe-9", "size-5"), ("lg", "pe-10", "size-5"), ("xl", "pe-11", "size-6") })
        {
            t.Variant("size", size, "base", padding).Variant("size", size, "triggerIcon", icon);
        }
        return t;
    }

    public static ComponentTheme InputTime()
    {
        var t = new ComponentTheme("inputTime", "root", "segment", "separator")
            .Slot("root", "inline-flex w-full items-center rounded-(--blux-radius) transition-colors data-disabled:cursor-not-allowed data-disabled:opacity-60 " +
                          "data-invalid:ring-2 data-invalid:ring-(--blux-error) " + PickerFocusWithin)
            .Slot("segment", "cursor-text select-none rounded-sm px-0.5 text-center tabular-nums caret-transparent before:content-[attr(data-label)] focus:outline-none " +
                             "focus:bg-(--bx-color) focus:text-(--bx-color-contrast) data-placeholder:text-(--blux-text-dimmed) focus:data-placeholder:text-(--bx-color-contrast) " +
                             "aria-disabled:cursor-not-allowed")
            .Slot("separator", "text-(--blux-text-dimmed)");
        FieldVariants(t, "root");
        return t;
    }

    public static ComponentTheme ColorPicker()
    {
        var t = new ComponentTheme("colorPicker", "root", "area", "areaThumb", "range", "hue", "preview", "swatch", "hex")
            .Slot("root", "flex w-64 max-w-full flex-col gap-3")
            .Slot("area", "relative h-40 w-full touch-none overflow-hidden rounded-(--blux-radius) ring ring-inset ring-(--blux-border-accented) " +
                          "[background-image:linear-gradient(to_top,black,transparent),linear-gradient(to_right,white,transparent)] " +
                          "[background-color:hsl(var(--bx-hue,0)_100%_50%)] has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-(--bx-color-text)")
            .Slot("areaThumb", "pointer-events-none absolute size-4 -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-white shadow-[0_0_0_1px_black]")
            .Slot("range", "peer sr-only")
            .Slot("hue", "h-3 w-full cursor-pointer appearance-none rounded-full ring ring-inset ring-(--blux-border-accented) focus-visible:outline-2 focus-visible:outline-offset-2 " +
                         "focus-visible:outline-(--bx-color-text) [background-image:linear-gradient(to_right,red,yellow,lime,cyan,blue,magenta,red)] " +
                         "[&::-webkit-slider-thumb]:size-4 [&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:border-2 " +
                         "[&::-webkit-slider-thumb]:border-white [&::-webkit-slider-thumb]:bg-transparent [&::-webkit-slider-thumb]:shadow-[0_0_0_1px_black] " +
                         "[&::-moz-range-thumb]:size-4 [&::-moz-range-thumb]:rounded-full [&::-moz-range-thumb]:border-2 [&::-moz-range-thumb]:border-white " +
                         "[&::-moz-range-thumb]:bg-transparent [&::-moz-range-thumb]:shadow-[0_0_0_1px_black]")
            .Slot("preview", "flex items-center gap-2")
            .Slot("swatch", "size-8 shrink-0 rounded-(--blux-radius) ring ring-inset ring-(--blux-border-accented) bg-(--bx-swatch,transparent)")
            .Slot("hex", "w-full min-w-0 rounded-(--blux-radius) border-0 font-mono focus:outline-none disabled:cursor-not-allowed disabled:opacity-60 transition-colors " + ControlInvalid);
        return FieldVariants(t, "hex");
    }

    public static ComponentTheme FileUpload() =>
        new ComponentTheme("fileUpload", "root", "dropzone", "input", "icon", "label", "description", "files", "file", "fileName", "fileSize", "remove", "error")
            .Slot("root", "flex w-full flex-col gap-2")
            .Slot("dropzone", "relative flex cursor-pointer flex-col items-center justify-center gap-1 rounded-(--blux-radius) border-2 border-dashed border-(--blux-border-accented) " +
                              "bg-(--blux-bg) p-6 text-center text-(--blux-text) transition-colors hover:bg-(--blux-bg-elevated) has-disabled:cursor-not-allowed has-disabled:opacity-60 " +
                              "data-dragging:border-(--bx-color-text) data-dragging:bg-(--blux-bg-elevated) has-focus-visible:outline-2 has-focus-visible:outline-offset-2 " +
                              "has-focus-visible:outline-(--bx-color-text) has-aria-invalid:border-(--blux-error)")
            .Slot("input", "sr-only")
            .Slot("icon", "size-8 text-(--blux-text-muted)")
            .Slot("label", "font-medium")
            .Slot("description", "text-(--blux-text-muted)")
            .Slot("files", "flex flex-col gap-1 empty:hidden")
            .Slot("file", "flex items-center gap-2 rounded-(--blux-radius) border border-(--blux-border) bg-(--blux-bg-muted) px-2 py-1.5")
            .Slot("fileName", "min-w-0 flex-1 truncate")
            .Slot("fileSize", "shrink-0 text-(--blux-text-muted)")
            .Slot("remove", "inline-flex shrink-0 items-center justify-center rounded-[calc(var(--blux-radius)_-_2px)] p-1 text-(--blux-text-muted) transition-colors " +
                             "hover:bg-(--blux-bg-elevated) hover:text-(--blux-text-highlighted) " + FocusRing)
            .Slot("error", "text-(--blux-error-text) empty:hidden")
            .Variant("size", "xs", "root", "text-xs").Variant("size", "sm", "root", "text-xs").Variant("size", "md", "root", "text-sm")
            .Variant("size", "lg", "root", "text-sm").Variant("size", "xl", "root", "text-base")
            .Default("size", "md");

    public static ComponentTheme Carousel() =>
        new ComponentTheme("carousel", "root", "viewport", "slide", "controls", "control", "prevIcon", "nextIcon", "pauseIcon", "playIcon", "dots", "dot")
            .Slot("root", "group/carousel relative flex w-full flex-col-reverse gap-3")
            .Slot("viewport", "flex snap-x snap-mandatory overflow-x-auto overscroll-x-contain scroll-smooth rounded-(--blux-radius) [scrollbar-width:none] [&::-webkit-scrollbar]:hidden " +
                              "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--bx-color-text) motion-reduce:scroll-auto")
            .Slot("slide", "min-w-0 shrink-0 basis-full snap-center")
            .Slot("controls", "flex items-center justify-center gap-2")
            .Slot("control", "inline-flex items-center justify-center rounded-full border border-(--blux-border-accented) bg-(--blux-bg) p-2 text-(--blux-text) transition-colors " +
                             "hover:bg-(--blux-bg-elevated) aria-disabled:cursor-not-allowed aria-disabled:opacity-60 " + FocusRing)
            .Slot("prevIcon", "size-4 rotate-90")
            .Slot("nextIcon", "size-4 -rotate-90")
            .Slot("pauseIcon", "size-4 group-data-paused/carousel:hidden")
            .Slot("playIcon", "hidden size-4 group-data-paused/carousel:block")
            .Slot("dots", "flex items-center gap-1")
            .Slot("dot", "relative size-3 rounded-full bg-(--blux-bg-accented) ring ring-inset ring-(--blux-border-accented) transition-colors hover:bg-(--blux-border-accented) " +
                         "after:absolute after:-inset-2 after:content-[''] aria-current:bg-(--bx-color) aria-current:ring-(--bx-color) " + FocusRing);
}
