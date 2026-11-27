namespace Klyroo.UI;

/// <summary>One avatar of a <see cref="KlyAvatarGroup"/>, with the parameters of <see cref="KlyAvatar"/>.</summary>
/// <param name="Src">Address of the picture.</param>
/// <param name="Alt">Alternative text of the picture, which also names the avatar.</param>
/// <param name="Text">Initials shown when there is no picture.</param>
public sealed record KlyAvatarItem(string? Src = null, string? Alt = null, string? Text = null)
{
    /// <summary>A line about the person, such as a role. It goes in the tooltip and the accessible name of the avatar, after <see cref="Alt"/>.</summary>
    public string? Description { get; init; }

    /// <summary>A badge of the person. Its label goes in the tooltip and the accessible name of the avatar, after <see cref="Description"/>.</summary>
    public KlyBadgeItem? Badge { get; init; }

    /// <summary>A color that tints the initials avatar.</summary>
    public KlyColor? Color { get; init; }
}
