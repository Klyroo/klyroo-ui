namespace Klyroo.UI;

/// <summary>One metric of a <see cref="KlyStatGroup"/>.</summary>
/// <param name="Label">The metric label.</param>
/// <param name="Value">The metric value, displayed as given.</param>
public sealed record KlyStatItem(string Label, string Value)
{
    /// <summary>A line of text below the value.</summary>
    public string? Caption { get; init; }

    /// <summary>A color for the value, such as <see cref="KlyColor.Error"/> for a metric that went wrong.</summary>
    public KlyColor? Color { get; init; }
}

/// <summary>Several small metrics side by side under an optional title (the "row" of statistics).</summary>
public partial class KlyStatGroup;
