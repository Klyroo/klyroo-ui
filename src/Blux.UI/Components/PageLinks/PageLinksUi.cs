using Blux.UI.Theming;

namespace Blux.UI;

/// <summary>Per-instance class overrides of <see cref="BxPageLinks"/> slots.</summary>
public sealed record PageLinksUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Title { get; init; }
    public string? List { get; init; }
    public string? Item { get; init; }
    public string? Link { get; init; }
    public string? Icon { get; init; }
    public string? Label { get; init; }
    public string? ExternalIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "title" => Title,
        "list" => List,
        "item" => Item,
        "link" => Link,
        "icon" => Icon,
        "label" => Label,
        "externalIcon" => ExternalIcon,
        _ => null,
    };
}
