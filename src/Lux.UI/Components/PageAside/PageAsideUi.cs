using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="LxPageAside"/> slots.</summary>
public sealed record PageAsideUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Top { get; init; }
    public string? Body { get; init; }
    public string? Bottom { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "top" => Top,
        "body" => Body,
        "bottom" => Bottom,
        _ => null,
    };
}
