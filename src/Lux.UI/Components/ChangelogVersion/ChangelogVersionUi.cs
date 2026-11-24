using Lux.UI.Theming;

namespace Lux.UI;

public sealed record ChangelogVersionUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Indicator { get; init; }
    public string? Container { get; init; }
    public string? Header { get; init; }
    public string? Meta { get; init; }
    public string? Date { get; init; }
    public string? Badge { get; init; }
    public string? Title { get; init; }
    public string? Link { get; init; }
    public string? Description { get; init; }
    public string? Image { get; init; }
    public string? ImageElement { get; init; }
    public string? Body { get; init; }
    public string? Authors { get; init; }
    public string? Actions { get; init; }
    public string? Footer { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "indicator" => Indicator,
        "container" => Container,
        "header" => Header,
        "meta" => Meta,
        "date" => Date,
        "badge" => Badge,
        "title" => Title,
        "link" => Link,
        "description" => Description,
        "image" => Image,
        "imageElement" => ImageElement,
        "body" => Body,
        "authors" => Authors,
        "actions" => Actions,
        "footer" => Footer,
        _ => null,
    };
}
