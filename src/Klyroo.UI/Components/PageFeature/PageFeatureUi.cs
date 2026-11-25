using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageFeature"/> slots.</summary>
public sealed record PageFeatureUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyPageFeature"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>leading</c> slot of <see cref="KlyPageFeature"/>, after the theme and the app configuration.</summary>
    public string? Leading { get; init; }
    /// <summary>Classes merged into the <c>leadingIcon</c> slot of <see cref="KlyPageFeature"/>, after the theme and the app configuration.</summary>
    public string? LeadingIcon { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyPageFeature"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyPageFeature"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyPageFeature"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyPageFeature"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "leading" => Leading,
        "leadingIcon" => LeadingIcon,
        "wrapper" => Wrapper,
        "title" => Title,
        "description" => Description,
        "link" => Link,
        _ => null,
    };
}
