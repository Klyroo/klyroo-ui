using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageHero"/> slots.</summary>
public sealed record PageHeroUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyPageHero"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlyPageHero"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyPageHero"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>headline</c> slot of <see cref="KlyPageHero"/>, after the theme and the app configuration.</summary>
    public string? Headline { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyPageHero"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyPageHero"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>links</c> slot of <see cref="KlyPageHero"/>, after the theme and the app configuration.</summary>
    public string? Links { get; init; }
    /// <summary>Classes merged into the <c>media</c> slot of <see cref="KlyPageHero"/>, after the theme and the app configuration.</summary>
    public string? Media { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "container" => Container,
        "wrapper" => Wrapper,
        "headline" => Headline,
        "title" => Title,
        "description" => Description,
        "links" => Links,
        "media" => Media,
        _ => null,
    };
}
