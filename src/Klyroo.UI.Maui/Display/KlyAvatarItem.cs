namespace Klyroo.UI.Maui;

/// <summary>One avatar of a <see cref="KlyAvatarGroup"/>, with the parameters of <see cref="KlyAvatar"/>.</summary>
/// <param name="Src">The picture. A string or a <see cref="Uri"/> converts to an <see cref="ImageSource"/>.</param>
/// <param name="Alt">What a screen reader says for the avatar, which also gives its initials when <see cref="Text"/> is not set.</param>
/// <param name="Text">Initials shown when there is no picture: its first two characters.</param>
public sealed record KlyAvatarItem(ImageSource? Src = null, string? Alt = null, string? Text = null)
{
    /// <summary>A line about the person, such as a role. It goes in the accessible name of the avatar, after <see cref="Alt"/>.</summary>
    public string? Description { get; init; }

    /// <summary>A badge of the person. Its label goes in the accessible name of the avatar, after <see cref="Description"/>.</summary>
    public KlyBadgeItem? Badge { get; init; }

    /// <summary>A color that tints the initials avatar.</summary>
    public KlyColor? Color { get; init; }
}
