using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlySeparator"/> slots.</summary>
public sealed record SeparatorUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlySeparator"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>border</c> slot of <see cref="KlySeparator"/>, after the theme and the app configuration.</summary>
    public string? Border { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlySeparator"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlySeparator"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlySeparator"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "border" => Border,
        "container" => Container,
        "label" => Label,
        "icon" => Icon,
        _ => null,
    };
}
