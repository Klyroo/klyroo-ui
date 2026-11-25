namespace Klyroo.UI;

/// <summary>One avatar of a <see cref="KlyAvatarGroup"/>, with the parameters of <see cref="KlyAvatar"/>.</summary>
/// <param name="Src">Address of the picture.</param>
/// <param name="Alt">Alternative text of the picture, which also names the avatar.</param>
/// <param name="Text">Initials shown when there is no picture.</param>
public sealed record KlyAvatarItem(string? Src = null, string? Alt = null, string? Text = null);
