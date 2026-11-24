namespace Lux.UI.Theming;

internal static partial class Themes
{
    private const string PickerFocusWithin = "has-focus-visible:ring-2 has-focus-visible:ring-(--lx-color-text)";

    public static ComponentTheme Calendar() =>
        new ComponentTheme("calendar", "root", "header", "heading", "nav", "grid", "weekday", "cell", "day")
            .Slot("root", "inline-block w-fit text-(--lux-text)")
            .Slot("header", "mb-2 flex items-center justify-between gap-2")
            .Slot("heading", "flex-1 text-center font-medium text-(--lux-text-highlighted)")
            .Slot("nav", "inline-flex shrink-0 items-center justify-center rounded-(--lux-radius) p-1.5 text-(--lux-text-muted) transition-colors hover:bg-(--lux-bg-elevated) " +
                         "hover:text-(--lux-text-highlighted) aria-disabled:cursor-not-allowed aria-disabled:opacity-60 rtl:[&>svg]:rotate-180 " + FocusRing)
            .Slot("grid", "border-separate border-spacing-0.5")
            .Slot("weekday", "p-0 text-center font-normal text-(--lux-text-muted)")
            .Slot("cell", "p-0 text-center")
            .Slot("day", "inline-flex cursor-pointer items-center justify-center rounded-(--lux-radius) transition-colors hover:bg-(--lux-bg-elevated) " + FocusRing + " " +
                         "data-today:ring-1 data-today:ring-inset data-today:ring-(--lx-color-text) data-in-range:rounded-none data-in-range:bg-(--lx-color)/15 " +
                         "data-selected:bg-(--lx-color) data-selected:ring-1 data-selected:ring-inset data-selected:ring-(--lx-color-text) data-selected:text-(--lx-color-contrast) data-selected:hover:bg-(--lx-color)/90 " +
                         "aria-disabled:cursor-not-allowed aria-disabled:text-(--lux-text-dimmed) aria-disabled:line-through aria-disabled:hover:bg-transparent")
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
            .Slot("base", "w-full rounded-(--lux-radius) border-0 appearance-none focus:outline-none disabled:cursor-not-allowed disabled:opacity-60 transition-colors " +
                          "[&::-webkit-calendar-picker-indicator]:hidden " + ControlInvalid)
            .Slot("trigger", "absolute inset-y-0 end-0 flex items-center pe-1")
            .Slot("triggerButton", "inline-flex items-center justify-center rounded-[calc(var(--lux-radius)_-_2px)] p-1 text-(--lux-text-muted) transition-colors " +
                                   "hover:text-(--lux-text-highlighted) disabled:cursor-not-allowed disabled:opacity-60 " + FocusRing)
            .Slot("triggerIcon", "shrink-0")
            .Slot("content", "m-0 p-3 inset-auto rounded-(--lux-radius) border border-(--lux-border) bg-(--lux-bg-overlay) text-(--lux-text) shadow-lg");
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
            .Slot("root", "inline-flex w-full items-center rounded-(--lux-radius) transition-colors data-disabled:cursor-not-allowed data-disabled:opacity-60 " +
                          "data-invalid:ring-2 data-invalid:ring-(--lux-error) " + PickerFocusWithin)
            .Slot("segment", "cursor-text select-none rounded-sm px-0.5 text-center tabular-nums caret-transparent before:content-[attr(data-label)] focus:outline-none " +
                             "focus:bg-(--lx-color) focus:text-(--lx-color-contrast) data-placeholder:text-(--lux-text-dimmed) focus:data-placeholder:text-(--lx-color-contrast) " +
                             "aria-disabled:cursor-not-allowed")
            .Slot("separator", "text-(--lux-text-dimmed)");
        FieldVariants(t, "root");
        return t;
    }

    public static ComponentTheme ColorPicker()
    {
        var t = new ComponentTheme("colorPicker", "root", "area", "areaThumb", "range", "hue", "preview", "swatch", "hex")
            .Slot("root", "flex w-64 max-w-full flex-col gap-3")
            .Slot("area", "relative h-40 w-full touch-none overflow-hidden rounded-(--lux-radius) ring ring-inset ring-(--lux-border-accented) " +
                          "[background-image:linear-gradient(to_top,black,transparent),linear-gradient(to_right,white,transparent)] " +
                          "[background-color:hsl(var(--lx-hue,0)_100%_50%)] has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-(--lx-color-text)")
            .Slot("areaThumb", "pointer-events-none absolute size-4 -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-white shadow-[0_0_0_1px_black]")
            .Slot("range", "peer sr-only")
            .Slot("hue", "h-3 w-full cursor-pointer appearance-none rounded-full ring ring-inset ring-(--lux-border-accented) focus-visible:outline-2 focus-visible:outline-offset-2 " +
                         "focus-visible:outline-(--lx-color-text) [background-image:linear-gradient(to_right,red,yellow,lime,cyan,blue,magenta,red)] " +
                         "[&::-webkit-slider-thumb]:size-4 [&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:border-2 " +
                         "[&::-webkit-slider-thumb]:border-white [&::-webkit-slider-thumb]:bg-transparent [&::-webkit-slider-thumb]:shadow-[0_0_0_1px_black] " +
                         "[&::-moz-range-thumb]:size-4 [&::-moz-range-thumb]:rounded-full [&::-moz-range-thumb]:border-2 [&::-moz-range-thumb]:border-white " +
                         "[&::-moz-range-thumb]:bg-transparent [&::-moz-range-thumb]:shadow-[0_0_0_1px_black]")
            .Slot("preview", "flex items-center gap-2")
            .Slot("swatch", "size-8 shrink-0 rounded-(--lux-radius) ring ring-inset ring-(--lux-border-accented) bg-(--lx-swatch,transparent)")
            .Slot("hex", "w-full min-w-0 rounded-(--lux-radius) border-0 font-mono focus:outline-none disabled:cursor-not-allowed disabled:opacity-60 transition-colors " + ControlInvalid);
        return FieldVariants(t, "hex");
    }

    public static ComponentTheme FileUpload() =>
        new ComponentTheme("fileUpload", "root", "dropzone", "input", "icon", "label", "description", "files", "file", "fileName", "fileSize", "remove", "error")
            .Slot("root", "flex w-full flex-col gap-2")
            .Slot("dropzone", "relative flex cursor-pointer flex-col items-center justify-center gap-1 rounded-(--lux-radius) border-2 border-dashed border-(--lux-border-accented) " +
                              "bg-(--lux-bg) p-6 text-center text-(--lux-text) transition-colors hover:bg-(--lux-bg-elevated) has-disabled:cursor-not-allowed has-disabled:opacity-60 " +
                              "data-dragging:border-(--lx-color-text) data-dragging:bg-(--lux-bg-elevated) has-focus-visible:outline-2 has-focus-visible:outline-offset-2 " +
                              "has-focus-visible:outline-(--lx-color-text) has-aria-invalid:border-(--lux-error)")
            .Slot("input", "sr-only")
            .Slot("icon", "size-8 text-(--lux-text-muted)")
            .Slot("label", "font-medium")
            .Slot("description", "text-(--lux-text-muted)")
            .Slot("files", "flex flex-col gap-1 empty:hidden")
            .Slot("file", "flex items-center gap-2 rounded-(--lux-radius) border border-(--lux-border) bg-(--lux-bg-muted) px-2 py-1.5")
            .Slot("fileName", "min-w-0 flex-1 truncate")
            .Slot("fileSize", "shrink-0 text-(--lux-text-muted)")
            .Slot("remove", "inline-flex shrink-0 items-center justify-center rounded-[calc(var(--lux-radius)_-_2px)] p-1 text-(--lux-text-muted) transition-colors " +
                             "hover:bg-(--lux-bg-elevated) hover:text-(--lux-text-highlighted) " + FocusRing)
            .Slot("error", "text-(--lux-error-text) empty:hidden")
            .Variant("size", "xs", "root", "text-xs").Variant("size", "sm", "root", "text-xs").Variant("size", "md", "root", "text-sm")
            .Variant("size", "lg", "root", "text-sm").Variant("size", "xl", "root", "text-base")
            .Default("size", "md");

    public static ComponentTheme Carousel() =>
        new ComponentTheme("carousel", "root", "viewport", "slide", "controls", "control", "prevIcon", "nextIcon", "pauseIcon", "playIcon", "dots", "dot")
            .Slot("root", "group/carousel relative flex w-full flex-col-reverse gap-3")
            .Slot("viewport", "flex snap-x snap-mandatory overflow-x-auto overscroll-x-contain scroll-smooth rounded-(--lux-radius) [scrollbar-width:none] [&::-webkit-scrollbar]:hidden " +
                              "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--lx-color-text) motion-reduce:scroll-auto")
            .Slot("slide", "min-w-0 shrink-0 basis-full snap-center")
            .Slot("controls", "flex items-center justify-center gap-2")
            .Slot("control", "inline-flex items-center justify-center rounded-full border border-(--lux-border-accented) bg-(--lux-bg) p-2 text-(--lux-text) transition-colors " +
                             "hover:bg-(--lux-bg-elevated) aria-disabled:cursor-not-allowed aria-disabled:opacity-60 " + FocusRing)
            .Slot("prevIcon", "size-4 rotate-90 rtl:-rotate-90")
            .Slot("nextIcon", "size-4 -rotate-90 rtl:rotate-90")
            .Slot("pauseIcon", "size-4 group-data-paused/carousel:hidden")
            .Slot("playIcon", "hidden size-4 group-data-paused/carousel:block")
            .Slot("dots", "flex items-center gap-1")
            .Slot("dot", "relative size-3 rounded-full bg-(--lux-bg-accented) ring ring-inset ring-(--lux-border-accented) transition-colors hover:bg-(--lux-border-accented) " +
                         "after:absolute after:-inset-x-0.5 after:-inset-y-2 after:content-[''] aria-current:bg-(--lx-color) aria-current:ring-(--lx-color-text) " + FocusRing);
}
