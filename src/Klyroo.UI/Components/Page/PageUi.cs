using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPage"/> slots.</summary>
public sealed record PageUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyPage"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>left</c> slot of <see cref="KlyPage"/>, after the theme and the app configuration.</summary>
    public string? Left { get; init; }
    /// <summary>Classes merged into the <c>center</c> slot of <see cref="KlyPage"/>, after the theme and the app configuration.</summary>
    public string? Center { get; init; }
    /// <summary>Classes merged into the <c>right</c> slot of <see cref="KlyPage"/>, after the theme and the app configuration.</summary>
    public string? Right { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "left" => Left,
        "center" => Center,
        "right" => Right,
        _ => null,
    };
}
