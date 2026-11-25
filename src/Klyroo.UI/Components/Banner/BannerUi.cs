using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyBanner"/> slots.</summary>
public sealed record BannerUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyBanner"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlyBanner"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyBanner"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyBanner"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyBanner"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyBanner"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>actions</c> slot of <see cref="KlyBanner"/>, after the theme and the app configuration.</summary>
    public string? Actions { get; init; }
    /// <summary>Classes merged into the <c>close</c> slot of <see cref="KlyBanner"/>, after the theme and the app configuration.</summary>
    public string? Close { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "container" => Container,
        "icon" => Icon,
        "content" => Content,
        "title" => Title,
        "description" => Description,
        "actions" => Actions,
        "close" => Close,
        _ => null,
    };
}
