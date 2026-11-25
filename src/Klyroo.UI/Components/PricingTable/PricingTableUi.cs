using Klyroo.UI.Theming;

namespace Klyroo.UI;

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
    /// <summary>Address the call to action button links to.</summary>
    public string? ButtonHref { get; init; }

    /// <summary>Emphasizes the recommended tier.</summary>
    public bool Highlight { get; init; }
}

/// <summary>A group of rows of a <see cref="KlyPricingTable"/>.</summary>
/// <param name="Title">Heading of the group of rows.</param>
/// <param name="Features">The rows of the group.</param>
public sealed record KlyPricingSection(string Title, IReadOnlyList<KlyPricingTableFeature> Features);

/// <summary>
/// A row of a <see cref="KlyPricingTable"/>. <see cref="Values"/> are the cells in the order of the tiers (or give
/// <see cref="ByTier"/>): <c>true</c> is a check mark, <c>false</c> or <c>null</c> a minus sign, anything else is shown as text.
/// </summary>
/// <param name="Title">Name of the feature, shown in the first column.</param>
/// <param name="Values">The cells, in the order of the tiers: <c>true</c> is a check mark, <c>false</c> or <c>null</c> a minus sign, anything else is shown as text.</param>
public sealed record KlyPricingTableFeature(string Title, params object?[] Values)
{
    /// <summary>Cells by tier id, instead of by position. A tier missing from it falls back to its position in <see cref="Values"/>.</summary>
    public IReadOnlyDictionary<string, object?>? ByTier { get; init; }
}

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyPricingTable"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record PricingTableUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>table</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? Table { get; init; }
    /// <summary>Classes merged into the <c>caption</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? Caption { get; init; }
    /// <summary>Classes merged into the <c>thead</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? Thead { get; init; }
    /// <summary>Classes merged into the <c>headRow</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? HeadRow { get; init; }
    /// <summary>Classes merged into the <c>corner</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? Corner { get; init; }
    /// <summary>Classes merged into the <c>tier</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? Tier { get; init; }
    /// <summary>Classes merged into the <c>tierTitle</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? TierTitle { get; init; }
    /// <summary>Classes merged into the <c>tierBadge</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? TierBadge { get; init; }
    /// <summary>Classes merged into the <c>tierDescription</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? TierDescription { get; init; }
    /// <summary>Classes merged into the <c>tierPrice</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? TierPrice { get; init; }
    /// <summary>Classes merged into the <c>tierDiscount</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? TierDiscount { get; init; }
    /// <summary>Classes merged into the <c>tierCycle</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? TierCycle { get; init; }
    /// <summary>Classes merged into the <c>tierButton</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? TierButton { get; init; }
    /// <summary>Classes merged into the <c>tbody</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? Tbody { get; init; }
    /// <summary>Classes merged into the <c>sectionRow</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? SectionRow { get; init; }
    /// <summary>Classes merged into the <c>section</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? Section { get; init; }
    /// <summary>Classes merged into the <c>row</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? Row { get; init; }
    /// <summary>Classes merged into the <c>feature</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? Feature { get; init; }
    /// <summary>Classes merged into the <c>cell</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? Cell { get; init; }
    /// <summary>Classes merged into the <c>valueIcon</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? ValueIcon { get; init; }
    /// <summary>Classes merged into the <c>included</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? Included { get; init; }
    /// <summary>Classes merged into the <c>notIncluded</c> slot of <see cref="KlyPricingTable"/>, after the theme and the app configuration.</summary>
    public string? NotIncluded { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "table" => Table,
        "caption" => Caption,
        "thead" => Thead,
        "headRow" => HeadRow,
        "corner" => Corner,
        "tier" => Tier,
        "tierTitle" => TierTitle,
        "tierBadge" => TierBadge,
        "tierDescription" => TierDescription,
        "tierPrice" => TierPrice,
        "tierDiscount" => TierDiscount,
        "tierCycle" => TierCycle,
        "tierButton" => TierButton,
        "tbody" => Tbody,
        "sectionRow" => SectionRow,
        "section" => Section,
        "row" => Row,
        "feature" => Feature,
        "cell" => Cell,
        "valueIcon" => ValueIcon,
        "included" => Included,
        "notIncluded" => NotIncluded,
        _ => null,
    };
}
