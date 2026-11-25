using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed record ListboxUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Base { get; init; }
    public string? Item { get; init; }
    public string? ItemLabel { get; init; }
    public string? ItemIcon { get; init; }
    public string? ItemLeadingIcon { get; init; }
    public string? ItemCount { get; init; }
    public string? Group { get; init; }
    public string? GroupLabel { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "item" => Item,
        "itemLabel" => ItemLabel,
        "itemIcon" => ItemIcon,
        "itemLeadingIcon" => ItemLeadingIcon,
        "itemCount" => ItemCount,
        "group" => Group,
        "groupLabel" => GroupLabel,
        _ => null,
    };
}
