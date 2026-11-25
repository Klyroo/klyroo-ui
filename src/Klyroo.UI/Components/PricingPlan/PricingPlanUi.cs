using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>A line of the feature list of a <see cref="KlyPricingPlan"/>. A string converts to one.</summary>
/// <param name="Title">Text of the feature.</param>
public sealed record KlyPricingFeature(string Title)
{
    /// <summary>Name of a built-in icon (see <see cref="KlyIcon"/>) shown instead of the check mark.</summary>
    public string? Icon { get; init; }

    /// <summary>Converts a text into a feature with that title.</summary>
    public static implicit operator KlyPricingFeature(string title) => new(title);
}

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyPricingPlan"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record PricingPlanUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>intro</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? Intro { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>titleRow</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? TitleRow { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>badge</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? Badge { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>tagline</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? Tagline { get; init; }
    /// <summary>Classes merged into the <c>pricing</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? Pricing { get; init; }
    /// <summary>Classes merged into the <c>priceRow</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? PriceRow { get; init; }
    /// <summary>Classes merged into the <c>price</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? Price { get; init; }
    /// <summary>Classes merged into the <c>discount</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? Discount { get; init; }
    /// <summary>Classes merged into the <c>billingCycle</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? BillingCycle { get; init; }
    /// <summary>Classes merged into the <c>terms</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? Terms { get; init; }
    /// <summary>Classes merged into the <c>features</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? Features { get; init; }
    /// <summary>Classes merged into the <c>feature</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? Feature { get; init; }
    /// <summary>Classes merged into the <c>featureIcon</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
    public string? FeatureIcon { get; init; }
    /// <summary>Classes merged into the <c>footer</c> slot of <see cref="KlyPricingPlan"/>, after the theme and the app configuration.</summary>
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
