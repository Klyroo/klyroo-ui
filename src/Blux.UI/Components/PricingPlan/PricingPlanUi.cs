using Blux.UI.Theming;

namespace Blux.UI;

/// <summary>A line of the feature list of a <see cref="BxPricingPlan"/>. A string converts to one.</summary>
public sealed record BxPricingFeature(string Title)
{
    /// <summary>Name of a built-in icon (see <see cref="BxIcon"/>) shown instead of the check mark.</summary>
    public string? Icon { get; init; }

    public static implicit operator BxPricingFeature(string title) => new(title);
}

public sealed record PricingPlanUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Intro { get; init; }
    public string? Header { get; init; }
    public string? TitleRow { get; init; }
    public string? Title { get; init; }
    public string? Badge { get; init; }
    public string? Description { get; init; }
    public string? Tagline { get; init; }
    public string? Pricing { get; init; }
    public string? PriceRow { get; init; }
    public string? Price { get; init; }
    public string? Discount { get; init; }
    public string? BillingCycle { get; init; }
    public string? Terms { get; init; }
    public string? Features { get; init; }
    public string? Feature { get; init; }
    public string? FeatureIcon { get; init; }
    public string? Footer { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "intro" => Intro,
        "header" => Header,
        "titleRow" => TitleRow,
        "title" => Title,
        "badge" => Badge,
        "description" => Description,
        "tagline" => Tagline,
        "pricing" => Pricing,
        "priceRow" => PriceRow,
        "price" => Price,
        "discount" => Discount,
        "billingCycle" => BillingCycle,
        "terms" => Terms,
        "features" => Features,
        "feature" => Feature,
        "featureIcon" => FeatureIcon,
        "footer" => Footer,
        _ => null,
    };
}
