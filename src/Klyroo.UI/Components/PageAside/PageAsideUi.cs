using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageAside"/> slots.</summary>
public sealed record PageAsideUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyPageAside"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>top</c> slot of <see cref="KlyPageAside"/>, after the theme and the app configuration.</summary>
    public string? Top { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyPageAside"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }
    /// <summary>Classes merged into the <c>bottom</c> slot of <see cref="KlyPageAside"/>, after the theme and the app configuration.</summary>
    public string? Bottom { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "top" => Top,
        "body" => Body,
        "bottom" => Bottom,
        _ => null,
    };
}
