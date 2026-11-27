namespace Klyroo.UI.Maui;

/// <summary>The headers of the two amount columns of a <see cref="KlyBreakdown"/>.</summary>
/// <param name="First">The header over <see cref="KlyBreakdownLine.Amount"/>.</param>
/// <param name="Second">The header over <see cref="KlyBreakdownLine.CompareAmount"/>.</param>
public sealed record KlyBreakdownColumns(string First, string Second);
