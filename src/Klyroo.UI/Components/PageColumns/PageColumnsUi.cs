using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageColumns"/> slots.</summary>
public sealed record PageColumnsUi : ISlotOverrides
{
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        _ => null,
    };
}
