using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyFooter"/> slots.</summary>
public sealed record FooterUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyFooter"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>top</c> slot of <see cref="KlyFooter"/>, after the theme and the app configuration.</summary>
    public string? Top { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlyFooter"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>left</c> slot of <see cref="KlyFooter"/>, after the theme and the app configuration.</summary>
    public string? Left { get; init; }
    /// <summary>Classes merged into the <c>center</c> slot of <see cref="KlyFooter"/>, after the theme and the app configuration.</summary>
    public string? Center { get; init; }
    /// <summary>Classes merged into the <c>right</c> slot of <see cref="KlyFooter"/>, after the theme and the app configuration.</summary>
    public string? Right { get; init; }
    /// <summary>Classes merged into the <c>bottom</c> slot of <see cref="KlyFooter"/>, after the theme and the app configuration.</summary>
    public string? Bottom { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "top" => Top,
        "container" => Container,
        "left" => Left,
        "center" => Center,
        "right" => Right,
        "bottom" => Bottom,
        _ => null,
    };
}
