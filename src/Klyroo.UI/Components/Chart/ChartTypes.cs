using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>The kind of chart <see cref="KlyChart{TItem, TIndex}"/> draws.</summary>
public enum KlyChartType
{
    /// <summary>Values joined by a line.</summary>
    Line,
    /// <summary>Values joined by a line with the area under it filled.</summary>
    Area,
    /// <summary>One bar per value, vertical or, with <c>Horizontal</c>, horizontal.</summary>
    Bar,

    /// <summary>One slice per row, sized by the first category.</summary>
    Donut,

    /// <summary>One horizontal bar per row, sized by the first category relative to the largest one, with the share kept from the previous step: a conversion funnel. Rows are the steps in order.</summary>
    Funnel,
}

/// <summary>The shape of the line between values of a line or area chart.</summary>
public enum KlyChartCurve
{
    /// <summary>Straight segments between values.</summary>
    Linear,

    /// <summary>A smooth curve that never overshoots the values.</summary>
    Monotone,
    /// <summary>The line holds each value until the next one, with right angles.</summary>
    Step,
}

/// <summary>Where the table with the chart's data goes.</summary>
public enum KlyChartTable
{
    /// <summary>Visually hidden, still read by assistive technology.</summary>
    Hidden,

    /// <summary>Shown under the chart.</summary>
    Visible,

    /// <summary>Behind a disclosure under the chart.</summary>
    Toggle,
}

/// <summary>One series of a chart: where its value comes from in a row, and how it is named and colored.</summary>
/// <typeparam name="TItem">The row type of the chart.</typeparam>
/// <param name="Key">Identifies the series. Also its name when there is no <see cref="Label"/>.</param>
/// <param name="Value">The value of the series in a row. <c>null</c> leaves a gap.</param>
public sealed record KlyChartCategory<TItem>(string Key, Func<TItem, double?> Value)
{
    /// <summary>The name shown in the legend, the tooltip and the table. Defaults to <see cref="Key"/>.</summary>
    public string? Label { get; init; }

    /// <summary>The color of the series, through the <c>--kly-color</c> tokens. Defaults to the chart's palette, in order.</summary>
    public KlyColor? Color { get; init; }

    internal string Name => Label ?? Key;
}

/// <summary>What <see cref="KlyChart{TItem, TIndex}.OnSelect"/> reports: the row of the bar, point or slice that was clicked or chosen with Enter or Space.</summary>
/// <typeparam name="TItem">The row type of the chart.</typeparam>
/// <typeparam name="TIndex">The type of the x value of a row (<see cref="KlyChart{TItem, TIndex}.Index"/>).</typeparam>
/// <param name="Row">The position of the row in <see cref="KlyChart{TItem, TIndex}.Data"/>.</param>
/// <param name="Index">The value of the row on the x axis (<see cref="KlyChart{TItem, TIndex}.Index"/>): a date, a number, a label. The label of the slice with a donut.</param>
/// <param name="SeriesKey">The <see cref="KlyChartCategory{TItem}.Key"/> of the series that was picked (the only category of a donut or of a chart with one series), or null when the chart has none or the pick named none (the keyboard on a chart with several series: the handler chooses from <paramref name="Item"/>).</param>
/// <param name="SeriesIndex">The position of that series in <see cref="KlyChart{TItem, TIndex}.Categories"/>, or -1 when the chart has none or the pick named none.</param>
/// <param name="Value">The value of that series in the row, or null for a gap or when no series was named.</param>
/// <param name="Item">The row of <see cref="KlyChart{TItem, TIndex}.Data"/>.</param>
public sealed record KlyChartSelectEventArgs<TItem, TIndex>(int Row, TIndex Index, string? SeriesKey, int SeriesIndex, double? Value, TItem Item);

/// <summary>Per-instance class overrides of <see cref="KlyChart{TItem, TIndex}"/> slots.</summary>
public sealed record ChartUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>plot</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Plot { get; init; }
    /// <summary>Classes merged into the <c>svg</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Svg { get; init; }
    /// <summary>Classes merged into the <c>grid</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Grid { get; init; }
    /// <summary>Classes merged into the <c>axisLine</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? AxisLine { get; init; }
    /// <summary>Classes merged into the <c>tick</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Tick { get; init; }
    /// <summary>Classes merged into the <c>axisTitle</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? AxisTitle { get; init; }
    /// <summary>Classes merged into the <c>line</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Line { get; init; }
    /// <summary>Classes merged into the <c>bar</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Bar { get; init; }
    /// <summary>Classes merged into the <c>marker</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Marker { get; init; }
    /// <summary>Classes merged into the <c>hit</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Hit { get; init; }
    /// <summary>Classes merged into the <c>hitArea</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? HitArea { get; init; }
    /// <summary>Classes merged into the <c>slice</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Slice { get; init; }
    /// <summary>Classes merged into the <c>tooltip</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Tooltip { get; init; }
    /// <summary>Classes merged into the <c>tooltipTitle</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? TooltipTitle { get; init; }
    /// <summary>Classes merged into the <c>tooltipRow</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? TooltipRow { get; init; }
    /// <summary>Classes merged into the <c>tooltipIndicator</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? TooltipIndicator { get; init; }
    /// <summary>Classes merged into the <c>tooltipLabel</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? TooltipLabel { get; init; }
    /// <summary>Classes merged into the <c>tooltipValue</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? TooltipValue { get; init; }
    /// <summary>Classes merged into the <c>center</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Center { get; init; }
    /// <summary>Classes merged into the <c>legend</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Legend { get; init; }
    /// <summary>Classes merged into the <c>legendItem</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? LegendItem { get; init; }
    /// <summary>Classes merged into the <c>legendSwatch</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? LegendSwatch { get; init; }
    /// <summary>Classes merged into the <c>legendLabel</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? LegendLabel { get; init; }
    /// <summary>Classes merged into the <c>legendValue</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? LegendValue { get; init; }
    /// <summary>Classes merged into the <c>details</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Details { get; init; }
    /// <summary>Classes merged into the <c>summary</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Summary { get; init; }
    /// <summary>Classes merged into the <c>tableWrapper</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? TableWrapper { get; init; }
    /// <summary>Classes merged into the <c>table</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Table { get; init; }
    /// <summary>Classes merged into the <c>th</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Th { get; init; }
    /// <summary>Classes merged into the <c>td</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Td { get; init; }

    /// <summary>Classes merged into the <c>funnel</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? Funnel { get; init; }
    /// <summary>Classes merged into the <c>funnelStep</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? FunnelStep { get; init; }
    /// <summary>Classes merged into the <c>funnelHeader</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? FunnelHeader { get; init; }
    /// <summary>Classes merged into the <c>funnelLabel</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? FunnelLabel { get; init; }
    /// <summary>Classes merged into the <c>funnelValue</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? FunnelValue { get; init; }
    /// <summary>Classes merged into the <c>funnelBar</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? FunnelBar { get; init; }
    /// <summary>Classes merged into the <c>funnelEmpty</c> slot of <see cref="KlyChart{TItem, TIndex}"/>, after the theme and the app configuration.</summary>
    public string? FunnelEmpty { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "plot" => Plot,
        "svg" => Svg,
        "grid" => Grid,
        "axisLine" => AxisLine,
        "tick" => Tick,
        "axisTitle" => AxisTitle,
        "line" => Line,
        "bar" => Bar,
        "marker" => Marker,
        "hit" => Hit,
        "hitArea" => HitArea,
        "slice" => Slice,
        "tooltip" => Tooltip,
        "tooltipTitle" => TooltipTitle,
        "tooltipRow" => TooltipRow,
        "tooltipIndicator" => TooltipIndicator,
        "tooltipLabel" => TooltipLabel,
        "tooltipValue" => TooltipValue,
        "center" => Center,
        "legend" => Legend,
        "legendItem" => LegendItem,
        "legendSwatch" => LegendSwatch,
        "legendLabel" => LegendLabel,
        "legendValue" => LegendValue,
        "details" => Details,
        "summary" => Summary,
        "tableWrapper" => TableWrapper,
        "table" => Table,
        "th" => Th,
        "td" => Td,
        "funnel" => Funnel,
        "funnelStep" => FunnelStep,
        "funnelHeader" => FunnelHeader,
        "funnelLabel" => FunnelLabel,
        "funnelValue" => FunnelValue,
        "funnelBar" => FunnelBar,
        "funnelEmpty" => FunnelEmpty,
        _ => null,
    };
}
