using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>A series as drawn: its name and its colors.</summary>
internal sealed record ChartDataSeries(string Name, KlyColorSet Colors);

/// <summary>One line of a tooltip: the name, the value and the colors of the series it belongs to.</summary>
internal sealed record ChartDataTipLine(string Label, string Value, KlyColorSet Colors);

/// <summary>What the canvas of a <see cref="KlyChart{TItem, TIndex}"/> draws: the options and the numbers, with no reference to the row type.</summary>
internal sealed class ChartDataModel
{
    public KlyChartType Type { get; init; }
    public bool Horizontal { get; init; }
    public bool Stacked { get; init; }
    public KlyChartCurve Curve { get; init; }
    public bool Points { get; init; }
    public bool Grid { get; init; } = true;
    public bool XAxis { get; init; } = true;
    public bool YAxis { get; init; } = true;
    public string? XLabel { get; init; }
    public string? YLabel { get; init; }
    public double Thickness { get; init; } = 24;
    public IReadOnlyList<string> Index { get; init; } = [];
    public IReadOnlyList<double?[]> Values { get; init; } = [];

    /// <summary>One per category, or with a donut one per slice.</summary>
    public IReadOnlyList<ChartDataSeries> Series { get; init; } = [];

    /// <summary>The name of the value of a donut: the first category.</summary>
    public string ValueName { get; init; } = string.Empty;
    public Func<double, string> Format { get; init; } = value => value.ToString("#,0.##", CultureInfo.CurrentCulture);
    public Func<double, string> Percent { get; init; } = value => value.ToString("P0", CultureInfo.CurrentCulture);

    public bool Donut => Type == KlyChartType.Donut;

    /// <summary>The tooltip of a row: a title and one line per value, with the color of its series.</summary>
    public (string Title, List<ChartDataTipLine> Lines) Tip(int row)
    {
        var lines = new List<ChartDataTipLine>();
        if (Donut)
        {
            var values = Values.Count > 0 ? Values[0] : [];
            var total = values.Where(v => v is > 0).Sum(v => v!.Value);
            var amount = values[row] is > 0 ? values[row]!.Value : 0;
            lines.Add(new ChartDataTipLine(ValueName, $"{Format(values[row] ?? 0)} ({Percent(total > 0 ? amount / total : 0)})", Series[row].Colors));
        }
        else
        {
            for (var s = 0; s < Series.Count; s++)
            {
                if (Values[s][row] is { } value) lines.Add(new ChartDataTipLine(Series[s].Name, Format(value), Series[s].Colors));
            }
        }
        return (Index[row], lines);
    }
}

/// <summary>The series colors of the charts: the chart tokens, which fall back to the semantic colors like <c>--klyroo-chart-N</c> on the web.</summary>
internal static class ChartDataColors
{
    /// <summary>The default series colors in order: the chart tokens, then neutral.</summary>
    public static readonly KlyColor[] Palette = ["chart-1", "chart-2", "chart-3", "chart-4", "chart-5", "chart-6", KlyColor.Neutral];

    /// <summary>The colors of a color: as registered, or for a chart token nobody registered the semantic color it follows (primary, secondary, info, error, success, warning).</summary>
    public static KlyColorSet Resolve(KlyColor color)
    {
        try
        {
            return KlyTheme.Resolve(color);
        }
        catch (InvalidOperationException) when (Fallback(color) is { } fallback)
        {
            return KlyTheme.Resolve(fallback);
        }
    }

    private static KlyColor? Fallback(KlyColor color) => color.Value switch
    {
        "chart-1" => (KlyColor?)KlyColor.Primary,
        "chart-2" => KlyColor.Secondary,
        "chart-3" => KlyColor.Info,
        "chart-4" => KlyColor.Error,
        "chart-5" => KlyColor.Success,
        "chart-6" => KlyColor.Warning,
        _ => (KlyColor?)null,
    };
}
