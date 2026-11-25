using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Klyroo.UI;

/// <summary>A command of <see cref="KlyCommandPalette"/>: an action, or a link when <see cref="Href"/> is set.</summary>
public sealed record KlyCommandItem
{
    /// <summary>Text of the command.</summary>
    public string Label { get; init; } = "";

    /// <summary>Identifies the command in <see cref="KlyCommandPalette.OnSelect"/>. Defaults to the label.</summary>
    public string? Value { get; init; }
    /// <summary>Text shown under the label.</summary>
    public string? Description { get; init; }

    /// <summary>Extra words the search matches, that are not shown.</summary>
    public string? Keywords { get; init; }
    /// <summary>Address to go to. When set the command is a link.</summary>
    public string? Href { get; init; }
    /// <summary>Icon shown before the label, as markup such as a <see cref="KlyIcon"/>.</summary>
    public RenderFragment? Icon { get; init; }
    /// <summary>Keyboard shortcut shown at the end of the row, such as <c>Ctrl+K</c>.</summary>
    public string? Shortcut { get; init; }
    /// <summary>Disables the command: it is listed but cannot be activated.</summary>
    public bool Disabled { get; init; }

    /// <summary>A number shown at the end of the row, before the shortcut (matches, unread, items): formatted for the locale and not searched.</summary>
    public int? Count { get; init; }

    internal string Key => Value ?? Label;
}

/// <summary>A headed set of commands. Without a <see cref="Label"/> the commands are listed with no heading.</summary>
/// <param name="Label">Heading of the group. Null lists the commands with no heading.</param>
/// <param name="Items">The commands of the group.</param>
public sealed record KlyCommandGroup(string? Label, IReadOnlyList<KlyCommandItem> Items);

/// <summary>Per-instance class overrides of <see cref="KlyCommandPalette"/> slots.</summary>
public sealed record CommandPaletteUi : ISlotOverrides
{
    /// <summary>
    /// Classes merged into the <c>root</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? Root { get; init; }
    /// <summary>
    /// Classes merged into the <c>inputWrapper</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? InputWrapper { get; init; }
    /// <summary>
    /// Classes merged into the <c>inputIcon</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? InputIcon { get; init; }
    /// <summary>
    /// Classes merged into the <c>input</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? Input { get; init; }
    /// <summary>
    /// Classes merged into the <c>listbox</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? Listbox { get; init; }
    /// <summary>
    /// Classes merged into the <c>group</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? Group { get; init; }
    /// <summary>
    /// Classes merged into the <c>groupLabel</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? GroupLabel { get; init; }
    /// <summary>
    /// Classes merged into the <c>item</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? Item { get; init; }
    /// <summary>
    /// Classes merged into the <c>itemIcon</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? ItemIcon { get; init; }
    /// <summary>
    /// Classes merged into the <c>itemWrapper</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? ItemWrapper { get; init; }
    /// <summary>
    /// Classes merged into the <c>itemLabel</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? ItemLabel { get; init; }
    /// <summary>
    /// Classes merged into the <c>itemDescription</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? ItemDescription { get; init; }
    /// <summary>
    /// Classes merged into the <c>itemShortcut</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? ItemShortcut { get; init; }
    /// <summary>
    /// Classes merged into the <c>itemCount</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? ItemCount { get; init; }
    /// <summary>
    /// Classes merged into the <c>empty</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
    public string? Empty { get; init; }
    /// <summary>
    /// Classes merged into the <c>status</c> slot of <see cref="KlyCommandPalette"/>, <see cref="KlyContentSearch"/> and <see cref="KlyDashboardSearch"/>, after the theme and the app configuration.
    /// </summary>
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
