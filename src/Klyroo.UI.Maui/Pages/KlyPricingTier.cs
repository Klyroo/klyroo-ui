namespace Klyroo.UI.Maui;

/// <summary>A column of a <see cref="KlyPricingTable"/>.</summary>
/// <param name="Id">Identifies the tier, to give its cells in <see cref="KlyPricingTableFeature.ByTier"/>.</param>
/// <param name="Title">Name of the tier.</param>
public sealed record KlyPricingTier(string Id, string Title)
{
    /// <summary>Text under the title of the tier.</summary>
    public string? Description { get; init; }

    /// <summary>Price of the tier, as the text to show.</summary>
    public string? Price { get; init; }

    /// <summary>The discounted price; <see cref="Price"/> then is the original one, struck through.</summary>
    public string? Discount { get; init; }

    /// <summary>Period the price covers, such as "/month".</summary>
    public string? BillingCycle { get; init; }

    /// <summary>Text of a badge next to the title.</summary>
    public string? Badge { get; init; }

    /// <summary>Label of the call to action button.</summary>
    public string? ButtonLabel { get; init; }

    /// <summary>Address the call to action button links to: reported by <see cref="KlyPricingTable.TierButtonClicked"/>.</summary>
    public string? ButtonHref { get; init; }

    /// <summary>Emphasizes the recommended tier.</summary>
    public bool Highlight { get; init; }
}
