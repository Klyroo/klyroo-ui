using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyUser"/> slots.</summary>
public sealed record UserUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>avatar</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Avatar { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>name</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Name { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "avatar" => Avatar,
        "wrapper" => Wrapper,
        "name" => Name,
        "description" => Description,
        _ => null,
    };
}
