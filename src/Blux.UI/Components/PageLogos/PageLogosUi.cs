using Blux.UI.Theming;

namespace Blux.UI;

/// <summary>Per-instance class overrides of <see cref="BxPageLogos"/> slots.</summary>
public sealed record PageLogosUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Title { get; init; }
    public string? Items { get; init; }
    public string? Item { get; init; }
    public string? Icon { get; init; }
    public string? Image { get; init; }
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
