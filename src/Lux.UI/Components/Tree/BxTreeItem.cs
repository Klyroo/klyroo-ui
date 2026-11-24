using Lux.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Lux.UI;

public sealed record BxTreeItem(string Label)
{
    /// <summary>Identifies the item in <see cref="BxTree.Value"/>. Defaults to its label, so give one when labels repeat.</summary>
    public string? Value { get; init; }
    public RenderFragment? Icon { get; init; }
    public IReadOnlyList<BxTreeItem>? Children { get; init; }

    /// <summary>Makes the label a link; Enter on the item follows it.</summary>
    public string? Href { get; init; }

    /// <summary>Initial state of an item with children.</summary>
    public bool Expanded { get; init; }
    public bool Disabled { get; init; }
}

public sealed record TreeUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? List { get; init; }
    public string? Item { get; init; }
    public string? Row { get; init; }
    public string? Chevron { get; init; }
    public string? Spacer { get; init; }
    public string? Icon { get; init; }
    public string? Label { get; init; }
    public string? Group { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "list" => List,
        "item" => Item,
        "row" => Row,
        "chevron" => Chevron,
        "spacer" => Spacer,
        "icon" => Icon,
        "label" => Label,
        "group" => Group,
        _ => null,
    };
}
