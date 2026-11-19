using Blux.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Blux.UI;

public sealed record BxBreadcrumbItem(string Label)
{
    /// <summary>Makes the item a link. The last item is the current page whether it has one or not.</summary>
    public string? Href { get; init; }
    public RenderFragment? Icon { get; init; }
}

public sealed record BreadcrumbUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? List { get; init; }
    public string? Item { get; init; }
    public string? Link { get; init; }
    public string? Icon { get; init; }
    public string? Label { get; init; }
    public string? Separator { get; init; }
    public string? SeparatorIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "list" => List,
        "item" => Item,
        "link" => Link,
        "icon" => Icon,
        "label" => Label,
        "separator" => Separator,
        "separatorIcon" => SeparatorIcon,
        _ => null,
    };
}
