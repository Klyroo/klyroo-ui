using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>A link of a <see cref="KlyNavigationMenu"/>, with optional sub-items.</summary>
/// <param name="Label">Text of the link.</param>
public sealed record KlyNavigationItem(string Label)
{
    /// <summary>Address the item links to. An item with children and no address is a group.</summary>
    public string? Href { get; init; }
    /// <summary>Icon shown before the label, as markup such as a <see cref="KlyIcon"/>.</summary>
    public RenderFragment? Icon { get; init; }

    /// <summary>
    /// Sub-items. Horizontally they open in a panel under the item, vertically they form a collapsible group.
    /// An item with children has no link of its own.
    /// </summary>
    public IReadOnlyList<KlyNavigationItem>? Children { get; init; }
    /// <summary>Disables the item: it is listed but cannot be followed.</summary>
    public bool Disabled { get; init; }

    /// <summary>The item is active only on exactly its address; otherwise it is also active on the addresses below it.</summary>
    public bool Exact { get; init; }

    /// <summary>Forces the active state instead of comparing <see cref="Href"/> with the current address.</summary>
    public bool? Active { get; init; }
    /// <summary>The <c>target</c> attribute of the link, such as <c>_blank</c>.</summary>
    public string? Target { get; init; }
}

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyNavigationMenu"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record NavigationMenuUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyNavigationMenu"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyNavigationMenu"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyNavigationMenu"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyNavigationMenu"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }
    /// <summary>Classes merged into the <c>trigger</c> slot of <see cref="KlyNavigationMenu"/>, after the theme and the app configuration.</summary>
    public string? Trigger { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyNavigationMenu"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyNavigationMenu"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>chevron</c> slot of <see cref="KlyNavigationMenu"/>, after the theme and the app configuration.</summary>
    public string? Chevron { get; init; }
    /// <summary>Classes merged into the <c>panel</c> slot of <see cref="KlyNavigationMenu"/>, after the theme and the app configuration.</summary>
    public string? Panel { get; init; }
    /// <summary>Classes merged into the <c>panelList</c> slot of <see cref="KlyNavigationMenu"/>, after the theme and the app configuration.</summary>
    public string? PanelList { get; init; }
    /// <summary>Classes merged into the <c>group</c> slot of <see cref="KlyNavigationMenu"/>, after the theme and the app configuration.</summary>
    public string? Group { get; init; }
    /// <summary>Classes merged into the <c>groupList</c> slot of <see cref="KlyNavigationMenu"/>, after the theme and the app configuration.</summary>
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
