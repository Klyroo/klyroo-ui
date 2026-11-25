using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageLinks"/> slots.</summary>
public sealed record PageLinksUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyPageLinks"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyPageLinks"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyPageLinks"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyPageLinks"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyPageLinks"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyPageLinks"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyPageLinks"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>externalIcon</c> slot of <see cref="KlyPageLinks"/>, after the theme and the app configuration.</summary>
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
