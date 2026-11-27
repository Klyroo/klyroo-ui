namespace Klyroo.UI.Maui;

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
