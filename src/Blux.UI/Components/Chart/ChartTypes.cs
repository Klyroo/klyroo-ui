using Blux.UI.Theming;

namespace Blux.UI;

/// <summary>The kind of chart <see cref="BxChart{TItem}"/> draws.</summary>
public enum BxChartType
{
    Line,
    Area,
    Bar,

    /// <summary>One slice per row, sized by the first category.</summary>
    Donut,
}

/// <summary>The shape of the line between values of a line or area chart.</summary>
public enum BxChartCurve
{
    Linear,

    /// <summary>A smooth curve that never overshoots the values.</summary>
    Monotone,
    Step,
}

/// <summary>Where the table with the chart's data goes.</summary>
public enum BxChartTable
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
public sealed record BxChartCategory<TItem>(string Key, Func<TItem, double?> Value)
{
    /// <summary>The name shown in the legend, the tooltip and the table. Defaults to <see cref="Key"/>.</summary>
    public string? Label { get; init; }

    /// <summary>The color of the series, through the <c>--bx-color</c> tokens. Defaults to the chart's palette, in order.</summary>
    public BxColor? Color { get; init; }

    internal string Name => Label ?? Key;
}

/// <summary>Per-instance class overrides of <see cref="BxChart{TItem}"/> slots.</summary>
public sealed record ChartUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Plot { get; init; }
    public string? Svg { get; init; }
    public string? Grid { get; init; }
    public string? AxisLine { get; init; }
    public string? Tick { get; init; }
    public string? AxisTitle { get; init; }
    public string? Line { get; init; }
    public string? Bar { get; init; }
    public string? Marker { get; init; }
    public string? Hit { get; init; }
    public string? HitArea { get; init; }
    public string? Slice { get; init; }
    public string? Tooltip { get; init; }
    public string? TooltipTitle { get; init; }
    public string? TooltipRow { get; init; }
    public string? TooltipIndicator { get; init; }
    public string? TooltipLabel { get; init; }
    public string? TooltipValue { get; init; }
    public string? Center { get; init; }
    public string? Legend { get; init; }
    public string? LegendItem { get; init; }
    public string? LegendSwatch { get; init; }
    public string? LegendLabel { get; init; }
    public string? LegendValue { get; init; }
    public string? Details { get; init; }
    public string? Summary { get; init; }
    public string? TableWrapper { get; init; }
    public string? Table { get; init; }
    public string? Th { get; init; }
    public string? Td { get; init; }

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
        _ => null,
    };
}
