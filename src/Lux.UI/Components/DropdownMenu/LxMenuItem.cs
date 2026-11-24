using Microsoft.AspNetCore.Components;

namespace Lux.UI;

public enum LxMenuItemKind
{
    Item,
    Separator,
    Heading,
}

/// <summary>An entry of <see cref="LxDropdownMenu"/>: an action, a link (with <see cref="Href"/>), a heading or a separator.</summary>
public sealed record LxMenuItem
{
    public LxMenuItemKind Kind { get; init; } = LxMenuItemKind.Item;
    public string Label { get; init; } = "";

    /// <summary>Identifies the item in <see cref="LxDropdownMenu.OnSelect"/>. Defaults to the label.</summary>
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
    /// being selected, so <see cref="Href"/> and <see cref="LxDropdownMenu.OnSelect"/> do not apply to it. Only <see cref="LxDropdownMenu"/> shows
    /// submenus: <see cref="LxContextMenu"/> renders such an item as a plain item.
    /// </summary>
    public IReadOnlyList<LxMenuItem>? Children { get; init; }

    /// <summary>"error" for destructive actions.</summary>
    public LxColor? Color { get; init; }

    public static LxMenuItem Separator() => new() { Kind = LxMenuItemKind.Separator };
    public static LxMenuItem Heading(string label) => new() { Kind = LxMenuItemKind.Heading, Label = label };
}
