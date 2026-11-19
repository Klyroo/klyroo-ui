namespace Blux.UI;

/// <summary>An author of a <see cref="BxBlogPost"/> or a <see cref="BxChangelogVersion"/>, with the parameters of <see cref="BxUser"/>.</summary>
public sealed record BxPostAuthor(string Name)
{
    public string? Description { get; init; }
    public string? AvatarSrc { get; init; }

    /// <summary>Makes the author a link. Ignored when the post itself is a link: a link cannot contain another one.</summary>
    public string? Href { get; init; }
}
