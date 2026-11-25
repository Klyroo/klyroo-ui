using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>What a <see cref="KlyMenuItem"/> is.</summary>
public enum KlyMenuItemKind
{
    /// <summary>An action, or a link when <c>Href</c> is set.</summary>
    Item,
    /// <summary>A line between two groups of items.</summary>
    Separator,
    /// <summary>A non-interactive title of a group of items.</summary>
    Heading,
}

/// <summary>An entry of <see cref="KlyDropdownMenu"/>: an action, a link (with <see cref="Href"/>), a heading or a separator.</summary>
public sealed record KlyMenuItem
{
    /// <summary>Whether the entry is an item, a separator or a heading. Defaults to an item.</summary>
    public KlyMenuItemKind Kind { get; init; } = KlyMenuItemKind.Item;
    /// <summary>Text of the item or of the heading.</summary>
    public string Label { get; init; } = "";

    /// <summary>Identifies the item in <see cref="KlyDropdownMenu.OnSelect"/>. Defaults to the label.</summary>
    public string? Value { get; init; }
    /// <summary>Address to go to. When set the item is a link.</summary>
    public string? Href { get; init; }
    /// <summary>Icon shown before the label, as markup such as a <see cref="KlyIcon"/>.</summary>
    public RenderFragment? Icon { get; init; }
    /// <summary>Keyboard shortcut shown at the end of the row, such as <c>Ctrl+S</c>.</summary>
    public string? Shortcut { get; init; }
    /// <summary>Disables the item: it is listed but cannot be activated.</summary>
    public bool Disabled { get; init; }

    /// <summary>
    /// A hint for the item, such as why it is disabled: it is the title (the tooltip) of the entry and its accessible description. Not shown otherwise.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Items of a submenu. An item with children opens a nested menu (on the right arrow key, Enter, a click or a pause of the pointer) instead of
    /// being selected, so <see cref="Href"/> and <see cref="KlyDropdownMenu.OnSelect"/> do not apply to it. Only <see cref="KlyDropdownMenu"/> shows
    /// submenus: <see cref="KlyContextMenu"/> renders such an item as a plain item.
    /// </summary>
    public IReadOnlyList<KlyMenuItem>? Children { get; init; }

    /// <summary>"error" for destructive actions.</summary>
    public KlyColor? Color { get; init; }

    /// <summary>Creates a separator between two groups of items.</summary>
    public static KlyMenuItem Separator() => new() { Kind = KlyMenuItemKind.Separator };
    /// <summary>Creates a heading that names the group of items below it.</summary>
    public static KlyMenuItem Heading(string label) => new() { Kind = KlyMenuItemKind.Heading, Label = label };
}
