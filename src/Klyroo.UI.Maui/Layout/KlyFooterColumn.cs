namespace Klyroo.UI.Maui;

/// <summary>A column of a <see cref="KlyFooterColumns"/>: a heading and its links.</summary>
/// <param name="Label">Heading of the column.</param>
public sealed record KlyFooterColumn(string Label)
{
    /// <summary>The links of the column.</summary>
    public IReadOnlyList<KlyFooterLink> Links { get; init; } = [];
}
