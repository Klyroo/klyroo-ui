using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyContextMenu"/> slots. <c>Trigger</c> is the area around the content.</summary>
public sealed record ContextMenuUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyContextMenu"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>trigger</c> slot of <see cref="KlyContextMenu"/>, after the theme and the app configuration.</summary>
    public string? Trigger { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyContextMenu"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>group</c> slot of <see cref="KlyContextMenu"/>, after the theme and the app configuration.</summary>
    public string? Group { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyContextMenu"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>separator</c> slot of <see cref="KlyContextMenu"/>, after the theme and the app configuration.</summary>
    public string? Separator { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyContextMenu"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>itemIcon</c> slot of <see cref="KlyContextMenu"/>, after the theme and the app configuration.</summary>
    public string? ItemIcon { get; init; }
    /// <summary>Classes merged into the <c>itemLabel</c> slot of <see cref="KlyContextMenu"/>, after the theme and the app configuration.</summary>
    public string? ItemLabel { get; init; }
    /// <summary>Classes merged into the <c>itemShortcut</c> slot of <see cref="KlyContextMenu"/>, after the theme and the app configuration.</summary>
    public string? ItemShortcut { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "trigger" => Trigger,
        "content" => Content,
        "group" => Group,
        "label" => Label,
        "separator" => Separator,
        "item" => Item,
        "itemIcon" => ItemIcon,
        "itemLabel" => ItemLabel,
        "itemShortcut" => ItemShortcut,
        _ => null,
    };
}
