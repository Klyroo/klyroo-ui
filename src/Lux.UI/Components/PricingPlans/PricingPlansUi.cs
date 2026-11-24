using Lux.UI.Theming;

namespace Lux.UI;

public sealed record PricingPlansUi : ISlotOverrides
{
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot == "root" ? Root : null;
}
