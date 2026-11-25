using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageGrid"/> slots.</summary>
public sealed record PageGridUi : ISlotOverrides
{
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        _ => null,
    };
}
