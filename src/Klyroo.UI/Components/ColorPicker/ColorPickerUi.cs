using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyColorPicker"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record ColorPickerUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyColorPicker"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>area</c> slot of <see cref="KlyColorPicker"/>, after the theme and the app configuration.</summary>
    public string? Area { get; init; }
    /// <summary>Classes merged into the <c>areaThumb</c> slot of <see cref="KlyColorPicker"/>, after the theme and the app configuration.</summary>
    public string? AreaThumb { get; init; }
    /// <summary>Classes merged into the <c>hue</c> slot of <see cref="KlyColorPicker"/>, after the theme and the app configuration.</summary>
    public string? Hue { get; init; }
    /// <summary>Classes merged into the <c>preview</c> slot of <see cref="KlyColorPicker"/>, after the theme and the app configuration.</summary>
    public string? Preview { get; init; }
    /// <summary>Classes merged into the <c>swatch</c> slot of <see cref="KlyColorPicker"/>, after the theme and the app configuration.</summary>
    public string? Swatch { get; init; }
    /// <summary>Classes merged into the <c>hex</c> slot of <see cref="KlyColorPicker"/>, after the theme and the app configuration.</summary>
    public string? Hex { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "area" => Area,
        "areaThumb" => AreaThumb,
        "hue" => Hue,
        "preview" => Preview,
        "swatch" => Swatch,
        "hex" => Hex,
        _ => null,
    };
}
