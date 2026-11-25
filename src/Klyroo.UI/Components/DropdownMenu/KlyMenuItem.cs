using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

public enum KlyMenuItemKind
{
    Item,
    Separator,
    Heading,
}

/// <summary>An entry of <see cref="KlyDropdownMenu"/>: an action, a link (with <see cref="Href"/>), a heading or a separator.</summary>
public sealed record KlyMenuItem
{
    public KlyMenuItemKind Kind { get; init; } = KlyMenuItemKind.Item;
    public string Label { get; init; } = "";

    /// <summary>Identifies the item in <see cref="KlyDropdownMenu.OnSelect"/>. Defaults to the label.</summary>
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
    /// being selected, so <see cref="Href"/> and <see cref="KlyDropdownMenu.OnSelect"/> do not apply to it. Only <see cref="KlyDropdownMenu"/> shows
    /// submenus: <see cref="KlyContextMenu"/> renders such an item as a plain item.
    /// </summary>
    public IReadOnlyList<KlyMenuItem>? Children { get; init; }

    /// <summary>"error" for destructive actions.</summary>
    public KlyColor? Color { get; init; }

    public static KlyMenuItem Separator() => new() { Kind = KlyMenuItemKind.Separator };
    public static KlyMenuItem Heading(string label) => new() { Kind = KlyMenuItemKind.Heading, Label = label };
}
