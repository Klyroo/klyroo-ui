namespace Klyroo.UI;

/// <summary>One avatar of a <see cref="KlyAvatarGroup"/>, with the parameters of <see cref="KlyAvatar"/>.</summary>
public sealed record KlyAvatarItem(string? Src = null, string? Alt = null, string? Text = null);
