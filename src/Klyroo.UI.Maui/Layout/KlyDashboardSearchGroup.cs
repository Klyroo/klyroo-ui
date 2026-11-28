namespace Klyroo.UI.Maui;

/// <summary>A headed set of commands of a <see cref="KlyDashboardSearch"/>. Without a <see cref="Label"/> the commands are listed with no heading.</summary>
/// <param name="Label">Heading of the group. Null lists the commands with no heading.</param>
/// <param name="Items">The commands of the group.</param>
public sealed record KlyDashboardSearchGroup(string? Label, IReadOnlyList<KlyDashboardSearchItem> Items);
