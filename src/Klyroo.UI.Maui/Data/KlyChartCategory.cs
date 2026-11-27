namespace Klyroo.UI.Maui;

/// <summary>One series of a chart: where its value comes from in a row, and how it is named and colored.</summary>
/// <typeparam name="TItem">The row type of the chart.</typeparam>
/// <param name="Key">Identifies the series. Also its name when there is no <see cref="Label"/>.</param>
/// <param name="Value">The value of the series in a row. <c>null</c> leaves a gap.</param>
public sealed record KlyChartCategory<TItem>(string Key, Func<TItem, double?> Value)
{
    /// <summary>The name shown in the legend, the tooltip and the table. Defaults to <see cref="Key"/>.</summary>
    public string? Label { get; init; }

    /// <summary>The color of the series. Defaults to the palette of the chart, in order.</summary>
    public KlyColor? Color { get; init; }

    internal string Name => Label ?? Key;
}
