using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlySkeleton"/> slots.</summary>
public sealed record SkeletonUi : ISlotOverrides
{
    public string? Base { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "base" => Base,
        _ => null,
    };
}
