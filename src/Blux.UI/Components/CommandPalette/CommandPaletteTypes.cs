using Blux.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI;

/// <summary>A command of <see cref="BxCommandPalette"/>: an action, or a link when <see cref="Href"/> is set.</summary>
public sealed record BxCommandItem
{
    public string Label { get; init; } = "";

    /// <summary>Identifies the command in <see cref="BxCommandPalette.OnSelect"/>. Defaults to the label.</summary>
    public string? Value { get; init; }
    public string? Description { get; init; }

    /// <summary>Extra words the search matches, that are not shown.</summary>
    public string? Keywords { get; init; }
    public string? Href { get; init; }
    public RenderFragment? Icon { get; init; }
    public string? Shortcut { get; init; }
    public bool Disabled { get; init; }

    internal string Key => Value ?? Label;
}

/// <summary>A headed set of commands. Without a <see cref="Label"/> the commands are listed with no heading.</summary>
public sealed record BxCommandGroup(string? Label, IReadOnlyList<BxCommandItem> Items);

/// <summary>Per-instance class overrides of <see cref="BxCommandPalette"/> slots.</summary>
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
        "empty" => Empty,
        "status" => Status,
        _ => null,
    };
}

internal static class BxCommandPaletteIcons
{
    public static RenderFragment Search(string? @class) => builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "viewBox", "0 0 24 24");
        builder.AddAttribute(2, "fill", "none");
        builder.AddAttribute(3, "stroke", "currentColor");
        builder.AddAttribute(4, "stroke-width", "2");
        builder.AddAttribute(5, "stroke-linecap", "round");
        builder.AddAttribute(6, "stroke-linejoin", "round");
        builder.AddAttribute(7, "aria-hidden", "true");
        builder.AddAttribute(8, "class", @class);
        builder.OpenElement(9, "path");
        builder.AddAttribute(10, "d", "m21 21-4.3-4.3M17 11a6 6 0 1 1-12 0 6 6 0 0 1 12 0Z");
        builder.CloseElement();
        builder.CloseElement();
    };
}
