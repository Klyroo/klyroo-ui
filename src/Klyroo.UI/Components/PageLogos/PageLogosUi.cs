using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageLogos"/> slots.</summary>
public sealed record PageLogosUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyPageLogos"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyPageLogos"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>items</c> slot of <see cref="KlyPageLogos"/>, after the theme and the app configuration.</summary>
    public string? Items { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyPageLogos"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyPageLogos"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>image</c> slot of <see cref="KlyPageLogos"/>, after the theme and the app configuration.</summary>
    public string? Image { get; init; }
    /// <summary>Classes merged into the <c>marquee</c> slot of <see cref="KlyPageLogos"/>, after the theme and the app configuration.</summary>
    public string? Marquee { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "title" => Title,
        "items" => Items,
        "item" => Item,
        "icon" => Icon,
        "image" => Image,
        "marquee" => Marquee,
        _ => null,
    };
}
