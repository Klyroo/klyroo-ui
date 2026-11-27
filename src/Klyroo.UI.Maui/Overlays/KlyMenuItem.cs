namespace Klyroo.UI.Maui;

/// <summary>An entry of <see cref="KlyDropdownMenu"/> or <see cref="KlyContextMenu"/>: an action, a link (with <see cref="Href"/>), a heading or a separator.</summary>
public sealed record KlyMenuItem
{
    /// <summary>Whether the entry is an item, a separator or a heading. Defaults to an item.</summary>
    public KlyMenuItemKind Kind { get; init; } = KlyMenuItemKind.Item;

    /// <summary>Text of the item or of the heading.</summary>
    public string Label { get; init; } = "";

    /// <summary>Identifies the item to the code that handles the selection. Defaults to the label.</summary>
    public string? Value { get; init; }

    /// <summary>
    /// Address to go to. When set the item is a link: a web, mail, phone or sms address is opened with the system, and the selection event is raised for every address,
    /// so the app can route a relative one itself (a Shell route, for instance).
    /// </summary>
    public string? Href { get; init; }

    /// <summary>Icon shown before the label: a name of <see cref="KlyIconNames"/>, or a registered one.</summary>
    public string? Icon { get; init; }

    /// <summary>Keyboard shortcut shown at the end of the row, such as <c>Ctrl+S</c>.</summary>
    public string? Shortcut { get; init; }

    /// <summary>Whether the item can be activated. Off lists it dimmed (the <c>Disabled</c> of the Blazor item).</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>A hint for the item, such as why it is disabled: it is the accessible hint of the row. Not shown otherwise.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Items of a submenu. An item with children opens a nested menu instead of being selected, so <see cref="Href"/> does not apply to it.
    /// Only <see cref="KlyDropdownMenu"/> shows submenus: <see cref="KlyContextMenu"/> renders such an item as a plain item.
    /// </summary>
    public IReadOnlyList<KlyMenuItem>? Children { get; init; }

    /// <summary><see cref="KlyColor.Error"/> for destructive actions.</summary>
    public KlyColor? Color { get; init; }

    /// <summary>Creates a separator between two groups of items.</summary>
    public static KlyMenuItem Separator() => new() { Kind = KlyMenuItemKind.Separator };

    /// <summary>Creates a heading that names the group of items below it.</summary>
    /// <param name="label">Text of the heading.</param>
    public static KlyMenuItem Heading(string label) => new() { Kind = KlyMenuItemKind.Heading, Label = label };
}
