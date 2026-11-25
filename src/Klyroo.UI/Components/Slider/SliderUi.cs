using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyRangeSlider"/> and <see cref="KlySlider"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record SliderUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyRangeSlider"/> and <see cref="KlySlider"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>track</c> slot of <see cref="KlyRangeSlider"/> and <see cref="KlySlider"/>, after the theme and the app configuration.</summary>
    public string? Track { get; init; }
    /// <summary>Classes merged into the <c>range</c> slot of <see cref="KlyRangeSlider"/> and <see cref="KlySlider"/>, after the theme and the app configuration.</summary>
    public string? Range { get; init; }
    /// <summary>Classes merged into the <c>thumb</c> slot of <see cref="KlyRangeSlider"/> and <see cref="KlySlider"/>, after the theme and the app configuration.</summary>
    public string? Thumb { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "track" => Track,
        "range" => Range,
        "thumb" => Thumb,
        _ => null,
    };
}
