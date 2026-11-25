using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyChangelogVersion"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record ChangelogVersionUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>indicator</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Indicator { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>meta</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Meta { get; init; }
    /// <summary>Classes merged into the <c>date</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Date { get; init; }
    /// <summary>Classes merged into the <c>badge</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Badge { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>image</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Image { get; init; }
    /// <summary>Classes merged into the <c>imageElement</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? ImageElement { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }
    /// <summary>Classes merged into the <c>authors</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Authors { get; init; }
    /// <summary>Classes merged into the <c>actions</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
    public string? Actions { get; init; }
    /// <summary>Classes merged into the <c>footer</c> slot of <see cref="KlyChangelogVersion"/>, after the theme and the app configuration.</summary>
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
