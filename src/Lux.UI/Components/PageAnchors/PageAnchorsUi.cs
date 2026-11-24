using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="BxPageAnchors"/> slots.</summary>
public sealed record PageAnchorsUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? List { get; init; }
    public string? Item { get; init; }
    public string? Link { get; init; }
    public string? Leading { get; init; }
    public string? LeadingIcon { get; init; }
    public string? Label { get; init; }
    public string? ExternalIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "list" => List,
        "item" => Item,
        "link" => Link,
        "leading" => Leading,
        "leadingIcon" => LeadingIcon,
        "label" => Label,
        "externalIcon" => ExternalIcon,
        _ => null,
    };
}
