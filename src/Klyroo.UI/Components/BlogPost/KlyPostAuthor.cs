namespace Klyroo.UI;

/// <summary>An author of a <see cref="KlyBlogPost"/> or a <see cref="KlyChangelogVersion"/>, with the parameters of <see cref="KlyUser"/>.</summary>
/// <param name="Name">Name of the author.</param>
public sealed record KlyPostAuthor(string Name)
{
    /// <summary>Text under the name, such as a role.</summary>
    public string? Description { get; init; }
    /// <summary>Address of the avatar picture.</summary>
    public string? AvatarSrc { get; init; }

    /// <summary>Makes the author a link. Ignored when the post itself is a link: a link cannot contain another one.</summary>
    public string? Href { get; init; }
}
