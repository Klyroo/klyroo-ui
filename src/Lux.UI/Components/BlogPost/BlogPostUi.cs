using Lux.UI.Theming;

namespace Lux.UI;

public enum BlogPostVariant
{
    Outline,
    Soft,
    Subtle,
    Ghost,
    Naked,
}

public sealed record BlogPostUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Header { get; init; }
    public string? Image { get; init; }
    public string? Body { get; init; }
    public string? Meta { get; init; }
    public string? Date { get; init; }
    public string? Badge { get; init; }
    public string? Title { get; init; }
    public string? Link { get; init; }
    public string? Description { get; init; }
    public string? Authors { get; init; }
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
