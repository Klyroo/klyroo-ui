using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="BxAvatarGroup"/> slots.</summary>
public sealed record AvatarGroupUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Avatar { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "avatar" => Avatar,
        _ => null,
    };
}
