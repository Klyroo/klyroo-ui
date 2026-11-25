namespace Klyroo.UI;

/// <summary>An author of a <see cref="KlyBlogPost"/> or a <see cref="KlyChangelogVersion"/>, with the parameters of <see cref="KlyUser"/>.</summary>
public sealed record KlyPostAuthor(string Name)
{
    public string? Description { get; init; }
    public string? AvatarSrc { get; init; }

    /// <summary>Makes the author a link. Ignored when the post itself is a link: a link cannot contain another one.</summary>
    public string? Href { get; init; }
}
