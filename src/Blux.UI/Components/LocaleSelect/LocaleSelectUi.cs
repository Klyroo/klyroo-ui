using Blux.UI.Theming;

namespace Blux.UI;

/// <summary>Per-instance class overrides of <see cref="BxLocaleSelect"/> slots.</summary>
public sealed record LocaleSelectUi : ISlotOverrides
{
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        _ => null,
    };
}
