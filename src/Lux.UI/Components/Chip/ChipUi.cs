using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="LxChip"/> slots.</summary>
public sealed record ChipUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Base { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        _ => null,
    };
}
