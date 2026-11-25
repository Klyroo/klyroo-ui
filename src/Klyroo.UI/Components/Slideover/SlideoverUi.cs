using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Edge of the screen a <see cref="KlySlideover"/> comes from.</summary>
public enum SlideoverSide
{
    /// <summary>The right edge.</summary>
    Right,
    /// <summary>The left edge.</summary>
    Left,
    /// <summary>The top edge.</summary>
    Top,
    /// <summary>The bottom edge.</summary>
    Bottom,
}

/// <summary>Per-instance class overrides of <see cref="KlySlideover"/> slots.</summary>
public sealed record SlideoverUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlySlideover"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlySlideover"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlySlideover"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlySlideover"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlySlideover"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>close</c> slot of <see cref="KlySlideover"/>, after the theme and the app configuration.</summary>
    public string? Close { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlySlideover"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }
    /// <summary>Classes merged into the <c>footer</c> slot of <see cref="KlySlideover"/>, after the theme and the app configuration.</summary>
    public string? Footer { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "content" => Content,
        "header" => Header,
        "wrapper" => Wrapper,
        "title" => Title,
        "description" => Description,
        "close" => Close,
        "body" => Body,
        "footer" => Footer,
        _ => null,
    };
}
