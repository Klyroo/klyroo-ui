namespace Klyroo.UI.Maui;

/// <summary>A group of rows of a <see cref="KlyPricingTable"/>.</summary>
/// <param name="Title">Heading of the group of rows.</param>
/// <param name="Features">The rows of the group.</param>
public sealed record KlyPricingSection(string Title, IReadOnlyList<KlyPricingTableFeature> Features);
