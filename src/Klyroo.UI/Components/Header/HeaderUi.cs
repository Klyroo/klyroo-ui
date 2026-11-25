using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyHeader"/> slots.</summary>
public sealed record HeaderUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyHeader"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlyHeader"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>left</c> slot of <see cref="KlyHeader"/>, after the theme and the app configuration.</summary>
    public string? Left { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyHeader"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>center</c> slot of <see cref="KlyHeader"/>, after the theme and the app configuration.</summary>
    public string? Center { get; init; }
    /// <summary>Classes merged into the <c>right</c> slot of <see cref="KlyHeader"/>, after the theme and the app configuration.</summary>
    public string? Right { get; init; }
    /// <summary>Classes merged into the <c>menu</c> slot of <see cref="KlyHeader"/>, after the theme and the app configuration.</summary>
    public string? Menu { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "container" => Container,
        "left" => Left,
        "title" => Title,
        "center" => Center,
        "right" => Right,
        "menu" => Menu,
        _ => null,
    };
}
