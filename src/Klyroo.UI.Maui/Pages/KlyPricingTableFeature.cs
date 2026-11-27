namespace Klyroo.UI.Maui;

/// <summary>
/// A row of a <see cref="KlyPricingTable"/>. <see cref="Values"/> are the cells in the order of the tiers (or give
/// <see cref="ByTier"/>): <c>true</c> is a check mark, <c>false</c> or <c>null</c> a minus sign, a text or a number is shown as it is.
/// </summary>
/// <param name="Title">Name of the feature, shown in the first column.</param>
/// <param name="Values">The cells, in the order of the tiers.</param>
public sealed record KlyPricingTableFeature(string Title, params KlyPricingTableValue[] Values)
{
    /// <summary>Cells by tier id, instead of by position. A tier missing from it falls back to its position in <see cref="Values"/>.</summary>
    public IReadOnlyDictionary<string, KlyPricingTableValue>? ByTier { get; init; }
}
