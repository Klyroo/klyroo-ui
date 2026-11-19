using Blux.UI.Theming;

namespace Blux.UI;

public sealed record ListboxUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Base { get; init; }
    public string? Item { get; init; }
    public string? ItemLabel { get; init; }
    public string? ItemIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "item" => Item,
        "itemLabel" => ItemLabel,
        "itemIcon" => ItemIcon,
        _ => null,
    };
}
