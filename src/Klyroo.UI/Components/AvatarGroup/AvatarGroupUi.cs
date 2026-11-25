using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyAvatarGroup"/> slots.</summary>
public sealed record AvatarGroupUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyAvatarGroup"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>avatar</c> slot of <see cref="KlyAvatarGroup"/>, after the theme and the app configuration.</summary>
    public string? Avatar { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "avatar" => Avatar,
        _ => null,
    };
}
