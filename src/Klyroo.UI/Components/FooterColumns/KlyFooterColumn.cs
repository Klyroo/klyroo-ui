namespace Klyroo.UI;

/// <summary>A column of a <see cref="KlyFooterColumns"/>: a heading and its links. Only the label, href, target, icon and disabled state of a link are used.</summary>
/// <param name="Label">Heading of the column.</param>
public sealed record KlyFooterColumn(string Label)
{
    /// <summary>The links of the column.</summary>
    public IReadOnlyList<KlyNavigationItem> Links { get; init; } = [];
}
