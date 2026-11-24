namespace Lux.UI;

/// <summary>A column of a <see cref="LxFooterColumns"/>: a heading and its links. Only the label, href, target, icon and disabled state of a link are used.</summary>
public sealed record LxFooterColumn(string Label)
{
    public IReadOnlyList<LxNavigationItem> Links { get; init; } = [];
}
