using Blux.UI.Theming;

namespace Blux.UI;

/// <summary>Per-instance class overrides of <see cref="BxSkipLink"/> slots.</summary>
public sealed record SkipLinkUi : ISlotOverrides
{
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        _ => null,
    };
}
