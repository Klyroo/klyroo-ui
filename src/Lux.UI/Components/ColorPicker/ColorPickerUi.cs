using Lux.UI.Theming;

namespace Lux.UI;

public sealed record ColorPickerUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Area { get; init; }
    public string? AreaThumb { get; init; }
    public string? Hue { get; init; }
    public string? Preview { get; init; }
    public string? Swatch { get; init; }
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
