using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="LxPage"/> slots.</summary>
public sealed record PageUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Left { get; init; }
    public string? Center { get; init; }
    public string? Right { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "left" => Left,
        "center" => Center,
        "right" => Right,
        _ => null,
    };
}
