using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageCard"/> slots.</summary>
public sealed record PageCardUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyPageCard"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>spotlight</c> slot of <see cref="KlyPageCard"/>, after the theme and the app configuration.</summary>
    public string? Spotlight { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyPageCard"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlyPageCard"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyPageCard"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyPageCard"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }
    /// <summary>Classes merged into the <c>leading</c> slot of <see cref="KlyPageCard"/>, after the theme and the app configuration.</summary>
    public string? Leading { get; init; }
    /// <summary>Classes merged into the <c>leadingIcon</c> slot of <see cref="KlyPageCard"/>, after the theme and the app configuration.</summary>
    public string? LeadingIcon { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyPageCard"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyPageCard"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>footer</c> slot of <see cref="KlyPageCard"/>, after the theme and the app configuration.</summary>
    public string? Footer { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyPageCard"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "spotlight" => Spotlight,
        "header" => Header,
        "container" => Container,
        "wrapper" => Wrapper,
        "body" => Body,
        "leading" => Leading,
        "leadingIcon" => LeadingIcon,
        "title" => Title,
        "description" => Description,
        "footer" => Footer,
        "link" => Link,
        _ => null,
    };
}
