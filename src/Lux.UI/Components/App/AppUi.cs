using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="BxApp"/> slots.</summary>
public sealed record AppUi : ISlotOverrides
{
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        _ => null,
    };
}
