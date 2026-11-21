using Blux.UI.Theming;

namespace Blux.UI;

public sealed record SelectMenuUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Base { get; init; }
    public string? Label { get; init; }
    public string? TrailingIcon { get; init; }
    public string? Content { get; init; }
    public string? SearchWrapper { get; init; }
    public string? Search { get; init; }
    public string? Listbox { get; init; }
    public string? Item { get; init; }
    public string? ItemLabel { get; init; }
    public string? ItemIcon { get; init; }
    public string? ItemLeadingIcon { get; init; }
    public string? ItemCount { get; init; }
    public string? Group { get; init; }
    public string? GroupLabel { get; init; }
    public string? LeadingIcon { get; init; }
    public string? Empty { get; init; }
    public string? Create { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "label" => Label,
        "trailingIcon" => TrailingIcon,
        "content" => Content,
        "searchWrapper" => SearchWrapper,
        "search" => Search,
        "listbox" => Listbox,
        "item" => Item,
        "itemLabel" => ItemLabel,
        "itemIcon" => ItemIcon,
        "itemLeadingIcon" => ItemLeadingIcon,
        "itemCount" => ItemCount,
        "group" => Group,
        "groupLabel" => GroupLabel,
        "leadingIcon" => LeadingIcon,
        "empty" => Empty,
        "create" => Create,
        _ => null,
    };
}
