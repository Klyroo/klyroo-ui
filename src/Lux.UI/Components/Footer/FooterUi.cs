using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="BxFooter"/> slots.</summary>
public sealed record FooterUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Top { get; init; }
    public string? Container { get; init; }
    public string? Left { get; init; }
    public string? Center { get; init; }
    public string? Right { get; init; }
    public string? Bottom { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "top" => Top,
        "container" => Container,
        "left" => Left,
        "center" => Center,
        "right" => Right,
        "bottom" => Bottom,
        _ => null,
    };
}
