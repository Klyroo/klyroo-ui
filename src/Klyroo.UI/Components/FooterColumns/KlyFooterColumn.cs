namespace Klyroo.UI;

/// <summary>A column of a <see cref="KlyFooterColumns"/>: a heading and its links. Only the label, href, target, icon and disabled state of a link are used.</summary>
public sealed record KlyFooterColumn(string Label)
{
    public IReadOnlyList<KlyNavigationItem> Links { get; init; } = [];
}
