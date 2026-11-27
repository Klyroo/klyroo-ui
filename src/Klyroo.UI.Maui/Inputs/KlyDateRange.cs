namespace Klyroo.UI.Maui;

/// <summary>A range of dates. <see cref="End"/> is <c>null</c> while only the start has been chosen.</summary>
/// <param name="Start">First date of the range, or <c>null</c> when none was chosen.</param>
/// <param name="End">Last date of the range, or <c>null</c> while only the start has been chosen.</param>
public sealed record KlyDateRange(DateOnly? Start, DateOnly? End);
