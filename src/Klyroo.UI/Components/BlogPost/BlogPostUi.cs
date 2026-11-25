using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Look of a <see cref="KlyBlogPost"/> card.</summary>
public enum BlogPostVariant
{
    /// <summary>A border around the card.</summary>
    Outline,
    /// <summary>A tinted background.</summary>
    Soft,
    /// <summary>A tinted background with a border.</summary>
    Subtle,
    /// <summary>A transparent card with the usual padding.</summary>
    Ghost,
    /// <summary>A transparent card that does not clip its content.</summary>
    Naked,
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyBlogPost"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record BlogPostUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyBlogPost"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyBlogPost"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>image</c> slot of <see cref="KlyBlogPost"/>, after the theme and the app configuration.</summary>
    public string? Image { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyBlogPost"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }
    /// <summary>Classes merged into the <c>meta</c> slot of <see cref="KlyBlogPost"/>, after the theme and the app configuration.</summary>
    public string? Meta { get; init; }
    /// <summary>Classes merged into the <c>date</c> slot of <see cref="KlyBlogPost"/>, after the theme and the app configuration.</summary>
    public string? Date { get; init; }
    /// <summary>Classes merged into the <c>badge</c> slot of <see cref="KlyBlogPost"/>, after the theme and the app configuration.</summary>
    public string? Badge { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyBlogPost"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyBlogPost"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyBlogPost"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>authors</c> slot of <see cref="KlyBlogPost"/>, after the theme and the app configuration.</summary>
    public string? Authors { get; init; }
    /// <summary>Classes merged into the <c>footer</c> slot of <see cref="KlyBlogPost"/>, after the theme and the app configuration.</summary>
    public string? Footer { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "header" => Header,
        "image" => Image,
        "body" => Body,
        "meta" => Meta,
        "date" => Date,
        "badge" => Badge,
        "title" => Title,
        "link" => Link,
        "description" => Description,
        "authors" => Authors,
        "footer" => Footer,
        _ => null,
    };
}
