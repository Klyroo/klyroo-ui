using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageAnchors"/> slots.</summary>
public sealed record PageAnchorsUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyPageAnchors"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyPageAnchors"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyPageAnchors"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyPageAnchors"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }
    /// <summary>Classes merged into the <c>leading</c> slot of <see cref="KlyPageAnchors"/>, after the theme and the app configuration.</summary>
    public string? Leading { get; init; }
    /// <summary>Classes merged into the <c>leadingIcon</c> slot of <see cref="KlyPageAnchors"/>, after the theme and the app configuration.</summary>
    public string? LeadingIcon { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyPageAnchors"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>externalIcon</c> slot of <see cref="KlyPageAnchors"/>, after the theme and the app configuration.</summary>
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
