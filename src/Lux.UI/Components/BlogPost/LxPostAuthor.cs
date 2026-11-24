namespace Lux.UI;

/// <summary>An author of a <see cref="LxBlogPost"/> or a <see cref="LxChangelogVersion"/>, with the parameters of <see cref="LxUser"/>.</summary>
public sealed record LxPostAuthor(string Name)
{
    public string? Description { get; init; }
    public string? AvatarSrc { get; init; }

    /// <summary>Makes the author a link. Ignored when the post itself is a link: a link cannot contain another one.</summary>
    public string? Href { get; init; }
}
