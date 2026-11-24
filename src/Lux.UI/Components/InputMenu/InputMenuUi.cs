using Lux.UI.Theming;

namespace Lux.UI;

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
    public string? ItemLeadingIcon { get; init; }
    public string? ItemCount { get; init; }
    public string? Group { get; init; }
    public string? GroupLabel { get; init; }
    public string? Empty { get; init; }

    public string? Scroller { get; init; }
    public string? Loading { get; init; }
    public string? Spinner { get; init; }
    public string? Error { get; init; }
    public string? Announcer { get; init; }

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
        "itemLeadingIcon" => ItemLeadingIcon,
        "itemCount" => ItemCount,
        "group" => Group,
        "groupLabel" => GroupLabel,
        "empty" => Empty,
        "scroller" => Scroller,
        "loading" => Loading,
        "spinner" => Spinner,
        "error" => Error,
        "announcer" => Announcer,
        _ => null,
    };
}
