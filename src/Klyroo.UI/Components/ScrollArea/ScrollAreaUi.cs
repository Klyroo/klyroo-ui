using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed record ScrollAreaUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Viewport { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "viewport" => Viewport,
        _ => null,
    };
}
