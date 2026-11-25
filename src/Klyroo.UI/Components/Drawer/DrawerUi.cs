using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Edge of the screen a <see cref="KlyDrawer"/> is docked to.</summary>
public enum DrawerDirection
{
    /// <summary>The bottom edge.</summary>
    Bottom,
    /// <summary>The top edge.</summary>
    Top,
    /// <summary>The left edge.</summary>
    Left,
    /// <summary>The right edge.</summary>
    Right,
}

/// <summary>Per-instance class overrides of <see cref="KlyDrawer"/> slots.</summary>
public sealed record DrawerUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyDrawer"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>handle</c> slot of <see cref="KlyDrawer"/>, after the theme and the app configuration.</summary>
    public string? Handle { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyDrawer"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyDrawer"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyDrawer"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyDrawer"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>close</c> slot of <see cref="KlyDrawer"/>, after the theme and the app configuration.</summary>
    public string? Close { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyDrawer"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }
    /// <summary>Classes merged into the <c>footer</c> slot of <see cref="KlyDrawer"/>, after the theme and the app configuration.</summary>
    public string? Footer { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "content" => Content,
        "handle" => Handle,
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
