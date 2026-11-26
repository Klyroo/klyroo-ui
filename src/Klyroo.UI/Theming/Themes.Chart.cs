namespace Klyroo.UI.Theming;

/// <summary>
/// Theme of the chart family. Series colors come from <c>data-color</c> on the elements of a series (the
/// <c>--kly-color*</c> variables), never from a palette in the markup, so any color works without new classes.
/// </summary>
internal static partial class Themes
{
    private const string ChartFocus =
        "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--klyroo-primary-text)";

    public static ComponentTheme Chart() =>
        new ComponentTheme("chart", "root", "plot", "svg", "grid", "axisLine", "tick", "axisTitle", "line", "bar", "marker", "hit", "hitArea", "slice",
                "patternBase", "patternMark", "tooltip", "tooltipTitle", "tooltipRow", "tooltipIndicator", "tooltipLabel", "tooltipValue",
                "center", "status", "legend", "legendItem", "legendSwatch", "legendLabel", "legendValue",
                "details", "summary", "tableWrapper", "table", "caption", "tr", "th", "td")
            .Slot("root", "relative block w-full text-(--klyroo-text)")
            .Slot("plot", "relative mx-auto w-full rounded-sm " + ChartFocus)
            .Slot("svg", "block h-auto w-full overflow-visible")
            .Slot("grid", "stroke-(--klyroo-border)")
            .Slot("axisLine", "stroke-(--klyroo-border-accented)")
            .Slot("tick", "fill-(--klyroo-text-muted) text-[11px]")
            .Slot("axisTitle", "fill-(--klyroo-text-muted) text-xs font-medium")
            .Slot("line", "fill-none stroke-(--kly-color-text) stroke-2")
            .Slot("bar", "stroke-(--kly-color-text) stroke-1")
            .Slot("marker", "pointer-events-none fill-(--klyroo-bg) stroke-(--kly-color-text) stroke-2 opacity-0 group-data-active/col:opacity-100 motion-safe:transition-opacity")
            .Slot("hit", "group/col")
            .Slot("hitArea", "fill-transparent group-data-active/col:fill-(--klyroo-text)/5")
            .Slot("slice", "stroke-(--klyroo-bg) stroke-2 data-active:opacity-70 motion-safe:transition-opacity")
            .Slot("patternBase", "fill-(--kly-color-text)")
            .Slot("patternMark", "stroke-(--kly-color-text) fill-(--kly-color-text)")
            .Slot("tooltip", "pointer-events-none invisible absolute left-0 top-0 z-10 min-w-32 rounded-(--klyroo-radius) border border-(--klyroo-border) bg-(--klyroo-bg-overlay) px-2.5 py-1.5 text-xs text-(--klyroo-text) opacity-0 shadow-lg " +
                             "data-[state=open]:visible data-[state=open]:opacity-100 motion-safe:transition-opacity")
            .Slot("tooltipTitle", "mb-1 font-medium text-(--klyroo-text-highlighted)")
            .Slot("tooltipRow", "flex items-center gap-2")
            .Slot("tooltipIndicator", "size-2.5 shrink-0 rounded-[2px] bg-(--kly-color-text)")
            .Slot("tooltipLabel", "text-(--klyroo-text-muted)")
            .Slot("tooltipValue", "ms-auto ps-3 font-mono font-medium tabular-nums text-(--klyroo-text-highlighted)")
            .Slot("center", "pointer-events-none absolute inset-0 flex flex-col items-center justify-center text-center")
            .Slot("status", "sr-only")
            .Slot("legend", "mt-3 flex flex-wrap items-center justify-center gap-x-4 gap-y-1.5 text-xs text-(--klyroo-text-muted)")
            .Slot("legendItem", "flex items-center gap-1.5")
            .Slot("legendSwatch", "h-3 w-5 shrink-0 overflow-visible stroke-(--kly-color-text)")
            .Slot("legendLabel", "text-(--klyroo-text)")
            .Slot("legendValue", "font-mono tabular-nums")
            .Slot("details", "mt-4 text-sm")
            .Slot("summary", "w-fit cursor-pointer rounded-sm font-medium text-(--klyroo-text) " + ChartFocus)
            .Slot("table", "min-w-full text-sm")
            .Slot("caption", "sr-only")
            .Slot("tr", "border-b border-(--klyroo-border) last:border-b-0")
            .Slot("th", "px-3 py-2 text-start font-semibold text-(--klyroo-text-highlighted)")
            .Slot("td", "px-3 py-2 tabular-nums text-(--klyroo-text)")

            .Variant("table", "hidden", "tableWrapper", "sr-only")
            .Variant("table", "visible", "tableWrapper", "mt-4 overflow-x-auto rounded-(--klyroo-radius) border border-(--klyroo-border)")
            .Variant("table", "toggle", "tableWrapper", "mt-2 overflow-x-auto rounded-(--klyroo-radius) border border-(--klyroo-border)")
            .Variant("points", "true", "marker", "opacity-100")
            .Variant("selectable", "true", "hitArea", "cursor-pointer")
            .Variant("selectable", "true", "slice", "cursor-pointer")
            .Default("table", "hidden")
            .Default("points", "false")
            .Default("selectable", "false");
}
