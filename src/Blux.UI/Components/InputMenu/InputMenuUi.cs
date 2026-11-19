using Blux.UI.Theming;

namespace Blux.UI;

public sealed record InputMenuUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Base { get; init; }
    public string? Trailing { get; init; }
    public string? TrailingIcon { get; init; }
    public string? Content { get; init; }
    public string? Listbox { get; init; }
    public string? Item { get; init; }
    public string? ItemLabel { get; init; }
    public string? ItemIcon { get; init; }
    public string? Empty { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "trailing" => Trailing,
        "trailingIcon" => TrailingIcon,
        "content" => Content,
        "listbox" => Listbox,
        "item" => Item,
        "itemLabel" => ItemLabel,
        "itemIcon" => ItemIcon,
        "empty" => Empty,
        _ => null,
    };
}
