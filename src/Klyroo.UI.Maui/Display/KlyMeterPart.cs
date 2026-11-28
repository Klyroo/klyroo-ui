namespace Klyroo.UI.Maui;

/// <summary>One segment of a <see cref="KlyMeter"/>.</summary>
/// <param name="Label">The segment label, shown in the legend.</param>
/// <param name="Value">The segment size, in the same unit as <see cref="KlyMeter.Max"/>.</param>
public sealed record KlyMeterPart(string Label, double Value)
{
    /// <summary>The value as displayed in the legend, such as <c>"3.2 GB"</c>. When <see langword="null"/>, the number is shown.</summary>
    public string? ValueLabel { get; init; }

    /// <summary>The segment color (primary when <see langword="null"/>).</summary>
    public KlyColor? Color { get; init; }

    /// <summary>The name of an icon (see <see cref="KlyIcon"/>) shown in the legend instead of the color dot.</summary>
    public string? Icon { get; init; }

    /// <summary>A maximum for this segment: the segment never grows past it, and the legend shows the value out of it.</summary>
    public double? Max { get; init; }
}
