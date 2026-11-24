namespace Lux.UI.Theming;

/// <summary>
/// Theme of the chart family. Series colors come from <c>data-color</c> on the elements of a series (the
/// <c>--lx-color*</c> variables), never from a palette in the markup, so any color works without new classes.
/// </summary>
internal static partial class Themes
{
    private const string ChartFocus =
        "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--lux-primary-text)";

    public static ComponentTheme Chart() =>
        new ComponentTheme("chart", "root", "plot", "svg", "grid", "axisLine", "tick", "axisTitle", "line", "bar", "marker", "hit", "hitArea", "slice",
                "patternBase", "patternMark", "tooltip", "tooltipTitle", "tooltipRow", "tooltipIndicator", "tooltipLabel", "tooltipValue",
                "center", "status", "legend", "legendItem", "legendSwatch", "legendLabel", "legendValue",
                "details", "summary", "tableWrapper", "table", "caption", "tr", "th", "td")
            .Slot("root", "relative block w-full text-(--lux-text)")
            .Slot("plot", "relative mx-auto w-full rounded-sm " + ChartFocus)
            .Slot("svg", "block h-auto w-full overflow-visible")
            .Slot("grid", "stroke-(--lux-border)")
            .Slot("axisLine", "stroke-(--lux-border-accented)")
            .Slot("tick", "fill-(--lux-text-muted) text-[11px]")
            .Slot("axisTitle", "fill-(--lux-text-muted) text-xs font-medium")
            .Slot("line", "fill-none stroke-(--lx-color-text) stroke-2")
            .Slot("bar", "stroke-(--lx-color-text) stroke-1")
            .Slot("marker", "pointer-events-none fill-(--lux-bg) stroke-(--lx-color-text) stroke-2 opacity-0 group-data-active/col:opacity-100 motion-safe:transition-opacity")
            .Slot("hit", "group/col")
            .Slot("hitArea", "fill-transparent group-data-active/col:fill-(--lux-text)/5")
            .Slot("slice", "stroke-(--lux-bg) stroke-2 data-active:opacity-70 motion-safe:transition-opacity")
            .Slot("patternBase", "fill-(--lx-color-text)")
            .Slot("patternMark", "stroke-(--lx-color-text) fill-(--lx-color-text)")
            .Slot("tooltip", "pointer-events-none invisible absolute left-0 top-0 z-10 min-w-32 rounded-(--lux-radius) border border-(--lux-border) bg-(--lux-bg-overlay) px-2.5 py-1.5 text-xs text-(--lux-text) opacity-0 shadow-lg " +
                             "data-[state=open]:visible data-[state=open]:opacity-100 motion-safe:transition-opacity")
            .Slot("tooltipTitle", "mb-1 font-medium text-(--lux-text-highlighted)")
            .Slot("tooltipRow", "flex items-center gap-2")
            .Slot("tooltipIndicator", "size-2.5 shrink-0 rounded-[2px] bg-(--lx-color-text)")
            .Slot("tooltipLabel", "text-(--lux-text-muted)")
            .Slot("tooltipValue", "ms-auto ps-3 font-mono font-medium tabular-nums text-(--lux-text-highlighted)")
            .Slot("center", "pointer-events-none absolute inset-0 flex flex-col items-center justify-center text-center")
            .Slot("status", "sr-only")
            .Slot("legend", "mt-3 flex flex-wrap items-center justify-center gap-x-4 gap-y-1.5 text-xs text-(--lux-text-muted)")
            .Slot("legendItem", "flex items-center gap-1.5")
            .Slot("legendSwatch", "h-3 w-5 shrink-0 overflow-visible stroke-(--lx-color-text)")
            .Slot("legendLabel", "text-(--lux-text)")
            .Slot("legendValue", "font-mono tabular-nums")
            .Slot("details", "mt-4 text-sm")
            .Slot("summary", "w-fit cursor-pointer rounded-sm font-medium text-(--lux-text) " + ChartFocus)
            .Slot("table", "min-w-full text-sm")
            .Slot("caption", "sr-only")
            .Slot("tr", "border-b border-(--lux-border) last:border-b-0")
            .Slot("th", "px-3 py-2 text-start font-semibold text-(--lux-text-highlighted)")
            .Slot("td", "px-3 py-2 tabular-nums text-(--lux-text)")

            .Variant("table", "hidden", "tableWrapper", "sr-only")
            .Variant("table", "visible", "tableWrapper", "mt-4 overflow-x-auto rounded-(--lux-radius) border border-(--lux-border)")
            .Variant("table", "toggle", "tableWrapper", "mt-2 overflow-x-auto rounded-(--lux-radius) border border-(--lux-border)")
            .Variant("points", "true", "marker", "opacity-100")
            .Default("table", "hidden")
            .Default("points", "false");
}
