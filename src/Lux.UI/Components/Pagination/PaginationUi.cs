using Lux.UI.Theming;

namespace Lux.UI;

public sealed record PaginationUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? List { get; init; }
    public string? Item { get; init; }
    public string? Page { get; init; }
    public string? Control { get; init; }
    public string? Ellipsis { get; init; }
    public string? Icon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "list" => List,
        "item" => Item,
        "page" => Page,
        "control" => Control,
        "ellipsis" => Ellipsis,
        "icon" => Icon,
        _ => null,
    };
}
