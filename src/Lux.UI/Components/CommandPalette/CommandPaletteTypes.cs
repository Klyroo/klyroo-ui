using Lux.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Lux.UI;

/// <summary>A command of <see cref="LxCommandPalette"/>: an action, or a link when <see cref="Href"/> is set.</summary>
public sealed record LxCommandItem
{
    public string Label { get; init; } = "";

    /// <summary>Identifies the command in <see cref="LxCommandPalette.OnSelect"/>. Defaults to the label.</summary>
    public string? Value { get; init; }
    public string? Description { get; init; }

    /// <summary>Extra words the search matches, that are not shown.</summary>
    public string? Keywords { get; init; }
    public string? Href { get; init; }
    public RenderFragment? Icon { get; init; }
    public string? Shortcut { get; init; }
    public bool Disabled { get; init; }

    /// <summary>A number shown at the end of the row, before the shortcut (matches, unread, items): formatted for the locale and not searched.</summary>
    public int? Count { get; init; }

    internal string Key => Value ?? Label;
}

/// <summary>A headed set of commands. Without a <see cref="Label"/> the commands are listed with no heading.</summary>
public sealed record LxCommandGroup(string? Label, IReadOnlyList<LxCommandItem> Items);

/// <summary>Per-instance class overrides of <see cref="LxCommandPalette"/> slots.</summary>
public sealed record CommandPaletteUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? InputWrapper { get; init; }
    public string? InputIcon { get; init; }
    public string? Input { get; init; }
    public string? Listbox { get; init; }
    public string? Group { get; init; }
    public string? GroupLabel { get; init; }
    public string? Item { get; init; }
    public string? ItemIcon { get; init; }
    public string? ItemWrapper { get; init; }
    public string? ItemLabel { get; init; }
    public string? ItemDescription { get; init; }
    public string? ItemShortcut { get; init; }
    public string? ItemCount { get; init; }
    public string? Empty { get; init; }
    public string? Status { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "inputWrapper" => InputWrapper,
        "inputIcon" => InputIcon,
        "input" => Input,
        "listbox" => Listbox,
        "group" => Group,
        "groupLabel" => GroupLabel,
        "item" => Item,
        "itemIcon" => ItemIcon,
        "itemWrapper" => ItemWrapper,
        "itemLabel" => ItemLabel,
        "itemDescription" => ItemDescription,
        "itemShortcut" => ItemShortcut,
        "itemCount" => ItemCount,
        "empty" => Empty,
        "status" => Status,
        _ => null,
    };
}
