using Microsoft.AspNetCore.Components;

namespace Lux.UI;

public enum BxMenuItemKind
{
    Item,
    Separator,
    Heading,
}

/// <summary>An entry of <see cref="BxDropdownMenu"/>: an action, a link (with <see cref="Href"/>), a heading or a separator.</summary>
public sealed record BxMenuItem
{
    public BxMenuItemKind Kind { get; init; } = BxMenuItemKind.Item;
    public string Label { get; init; } = "";

    /// <summary>Identifies the item in <see cref="BxDropdownMenu.OnSelect"/>. Defaults to the label.</summary>
    public string? Value { get; init; }
    public string? Href { get; init; }
    public RenderFragment? Icon { get; init; }
    public string? Shortcut { get; init; }
    public bool Disabled { get; init; }

    /// <summary>
    /// A hint for the item, such as why it is disabled: it is the title (the tooltip) of the entry and its accessible description. Not shown otherwise.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Items of a submenu. An item with children opens a nested menu (on the right arrow key, Enter, a click or a pause of the pointer) instead of
    /// being selected, so <see cref="Href"/> and <see cref="BxDropdownMenu.OnSelect"/> do not apply to it. Only <see cref="BxDropdownMenu"/> shows
    /// submenus: <see cref="BxContextMenu"/> renders such an item as a plain item.
    /// </summary>
    public IReadOnlyList<BxMenuItem>? Children { get; init; }

    /// <summary>"error" for destructive actions.</summary>
    public BxColor? Color { get; init; }

    public static BxMenuItem Separator() => new() { Kind = BxMenuItemKind.Separator };
    public static BxMenuItem Heading(string label) => new() { Kind = BxMenuItemKind.Heading, Label = label };
}
