namespace Klyroo.UI.Maui;

/// <summary>An author of a <see cref="KlyBlogPost"/> or a <see cref="KlyChangelogVersion"/>: an avatar, a name and an optional role.</summary>
/// <param name="Name">Name of the author.</param>
public sealed record KlyPostAuthor(string Name)
{
    /// <summary>Text under the name, such as a role.</summary>
    public string? Description { get; init; }

    /// <summary>The avatar picture. Without it the avatar shows the initials of the name.</summary>
    public ImageSource? AvatarSource { get; init; }
}
