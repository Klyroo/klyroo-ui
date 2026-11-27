using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>One arc of a ring drawn by <see cref="MiniCharts.Ring"/>: a share of the circle from an offset, in percent.</summary>
internal sealed record RingArc(double Percent, double Offset, string Color, string Slot, string? Title = null, bool RoundCap = false);

// Small drawings of data that are not a KlyChart: the sparkline of KlyStat and the ring of KlyMeter. They live with the chart code, the only place besides KlyIcon that draws SVG.
internal static class MiniCharts
{
    private static string Num(double value, string format = "0.##") => value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>A line through the points, scaled to their range, decorative (hidden from assistive technology).</summary>
    public static RenderFragment Sparkline(IReadOnlyList<double> points, string? @class) => builder =>
    {
        var min = points.Min();
        var max = points.Max();
        var range = max - min <= 0 ? 1 : max - min;
        var coordinates = new List<string>(points.Count);
        for (var i = 0; i < points.Count; i++)
        {
            var x = Num(i * 100.0 / (points.Count - 1), "0.#");
            var y = Num(28.0 - ((points[i] - min) / range * 24.0), "0.#");
            coordinates.Add($"{x},{y}");
        }
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "viewBox", "0 0 100 30");
        builder.AddAttribute(2, "class", @class);
        builder.AddAttribute(3, "data-slot", "stat-sparkline");
        builder.AddAttribute(4, "aria-hidden", "true");
        builder.OpenElement(5, "polyline");
        builder.AddAttribute(6, "fill", "none");
        builder.AddAttribute(7, "stroke", "currentColor");
        builder.AddAttribute(8, "stroke-width", "2");
        builder.AddAttribute(9, "stroke-linecap", "round");
        builder.AddAttribute(10, "stroke-linejoin", "round");
        builder.AddAttribute(11, "points", string.Join(' ', coordinates));
        builder.CloseElement();
        builder.CloseElement();
    };

    /// <summary>A ring: a track and one arc per share, drawn from the top clockwise. Percentages count a circle as 100.</summary>
    public static RenderFragment Ring(IReadOnlyList<RingArc> arcs, string? svgClass, string? trackClass, string? arcClass) => builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "viewBox", "0 0 36 36");
        builder.AddAttribute(2, "class", svgClass);
        builder.AddAttribute(3, "aria-hidden", "true");
        Circle(builder, 4, trackClass);
        foreach (var arc in arcs)
        {
            Circle(builder, 5, arcClass, arc);
        }
        builder.CloseElement();
    };

    private static void Circle(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, int sequence, string? @class, RingArc? arc = null)
    {
        builder.OpenElement(sequence, "circle");
        builder.AddAttribute(10, "cx", "18");
        builder.AddAttribute(11, "cy", "18");
        builder.AddAttribute(12, "r", "15.9155");
        builder.AddAttribute(13, "fill", "none");
        builder.AddAttribute(14, "stroke", "currentColor");
        builder.AddAttribute(15, "stroke-width", "3.5");
        builder.AddAttribute(16, "class", @class);
        if (arc is not null)
        {
            if (arc.RoundCap) builder.AddAttribute(17, "stroke-linecap", "round");
            builder.AddAttribute(18, "data-color", arc.Color);
            builder.AddAttribute(19, "stroke-dasharray", $"{Num(arc.Percent)} {Num(100 - arc.Percent)}");
            if (arc.Offset > 0) builder.AddAttribute(20, "stroke-dashoffset", Num(-arc.Offset));
            builder.AddAttribute(21, "data-slot", arc.Slot);
            if (arc.Title is not null)
            {
                builder.OpenElement(22, "title");
                builder.AddContent(23, arc.Title);
                builder.CloseElement();
            }
        }
        builder.CloseElement();
    }
}
