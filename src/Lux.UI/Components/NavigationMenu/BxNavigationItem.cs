using Lux.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Lux.UI;

public sealed record BxNavigationItem(string Label)
{
    public string? Href { get; init; }
    public RenderFragment? Icon { get; init; }

    /// <summary>
    /// Sub-items. Horizontally they open in a panel under the item, vertically they form a collapsible group.
    /// An item with children has no link of its own.
    /// </summary>
    public IReadOnlyList<BxNavigationItem>? Children { get; init; }
    public bool Disabled { get; init; }

    /// <summary>The item is active only on exactly its address; otherwise it is also active on the addresses below it.</summary>
    public bool Exact { get; init; }

    /// <summary>Forces the active state instead of comparing <see cref="Href"/> with the current address.</summary>
    public bool? Active { get; init; }
    public string? Target { get; init; }
}

public sealed record NavigationMenuUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? List { get; init; }
    public string? Item { get; init; }
    public string? Link { get; init; }
    public string? Trigger { get; init; }
    public string? Icon { get; init; }
    public string? Label { get; init; }
    public string? Chevron { get; init; }
    public string? Panel { get; init; }
    public string? PanelList { get; init; }
    public string? Group { get; init; }
    public string? GroupList { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "list" => List,
        "item" => Item,
        "link" => Link,
        "trigger" => Trigger,
        "icon" => Icon,
        "label" => Label,
        "chevron" => Chevron,
        "panel" => Panel,
        "panelList" => PanelList,
        "group" => Group,
        "groupList" => GroupList,
        _ => null,
    };
}
