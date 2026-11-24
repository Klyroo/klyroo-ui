using Lux.UI.Theming;

namespace Lux.UI;

public sealed record SliderUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Track { get; init; }
    public string? Range { get; init; }
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
