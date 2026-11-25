using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyApp"/> slots.</summary>
public sealed record AppUi : ISlotOverrides
{
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        _ => null,
    };
}
