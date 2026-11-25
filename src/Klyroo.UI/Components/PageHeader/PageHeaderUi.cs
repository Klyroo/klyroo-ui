using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageHeader"/> slots.</summary>
public sealed record PageHeaderUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyPageHeader"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyPageHeader"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>headline</c> slot of <see cref="KlyPageHeader"/>, after the theme and the app configuration.</summary>
    public string? Headline { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyPageHeader"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyPageHeader"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>links</c> slot of <see cref="KlyPageHeader"/>, after the theme and the app configuration.</summary>
    public string? Links { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyPageHeader"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "wrapper" => Wrapper,
        "headline" => Headline,
        "title" => Title,
        "description" => Description,
        "links" => Links,
        "body" => Body,
        _ => null,
    };
}
