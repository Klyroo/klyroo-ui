using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>A node of a <see cref="KlyTree"/>.</summary>
/// <param name="Label">Text of the node.</param>
public sealed record KlyTreeItem(string Label)
{
    /// <summary>Identifies the item in <see cref="KlyTree.Value"/>. Defaults to its label, so give one when labels repeat.</summary>
    public string? Value { get; init; }
    /// <summary>Icon shown before the label, as markup such as a <see cref="KlyIcon"/>.</summary>
    public RenderFragment? Icon { get; init; }
    /// <summary>The nodes below this one. A node with children can be expanded.</summary>
    public IReadOnlyList<KlyTreeItem>? Children { get; init; }

    /// <summary>Makes the label a link; Enter on the item follows it.</summary>
    public string? Href { get; init; }

    /// <summary>Initial state of an item with children.</summary>
    public bool Expanded { get; init; }
    /// <summary>Disables the node.</summary>
    public bool Disabled { get; init; }
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyTree"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record TreeUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyTree"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyTree"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyTree"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>row</c> slot of <see cref="KlyTree"/>, after the theme and the app configuration.</summary>
    public string? Row { get; init; }
    /// <summary>Classes merged into the <c>chevron</c> slot of <see cref="KlyTree"/>, after the theme and the app configuration.</summary>
    public string? Chevron { get; init; }
    /// <summary>Classes merged into the <c>spacer</c> slot of <see cref="KlyTree"/>, after the theme and the app configuration.</summary>
    public string? Spacer { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyTree"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyTree"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>group</c> slot of <see cref="KlyTree"/>, after the theme and the app configuration.</summary>
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
