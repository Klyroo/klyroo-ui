using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="LxContextMenu"/> slots. <c>Trigger</c> is the area around the content.</summary>
public sealed record ContextMenuUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Trigger { get; init; }
    public string? Content { get; init; }
    public string? Group { get; init; }
    public string? Label { get; init; }
    public string? Separator { get; init; }
    public string? Item { get; init; }
    public string? ItemIcon { get; init; }
    public string? ItemLabel { get; init; }
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
