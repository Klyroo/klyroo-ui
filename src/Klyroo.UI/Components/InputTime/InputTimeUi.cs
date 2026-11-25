using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed record InputTimeUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Segment { get; init; }
    public string? Separator { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "segment" => Segment,
        "separator" => Separator,
        _ => null,
    };
}
