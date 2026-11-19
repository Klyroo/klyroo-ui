namespace Blux.UI;

/// <summary>A column of a <see cref="BxFooterColumns"/>: a heading and its links. Only the label, href, target, icon and disabled state of a link are used.</summary>
public sealed record BxFooterColumn(string Label)
{
    public IReadOnlyList<BxNavigationItem> Links { get; init; } = [];
}
