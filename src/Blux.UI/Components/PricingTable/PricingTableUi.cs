using Blux.UI.Theming;

namespace Blux.UI;

/// <summary>A column of a <see cref="BxPricingTable"/>.</summary>
public sealed record BxPricingTier(string Id, string Title)
{
    public string? Description { get; init; }
    public string? Price { get; init; }

    /// <summary>The discounted price; <see cref="Price"/> then is the original one, struck through.</summary>
    public string? Discount { get; init; }
    public string? BillingCycle { get; init; }
    public string? Badge { get; init; }
    public string? ButtonLabel { get; init; }
    public string? ButtonHref { get; init; }

    /// <summary>Emphasizes the recommended tier.</summary>
    public bool Highlight { get; init; }
}

/// <summary>A group of rows of a <see cref="BxPricingTable"/>.</summary>
public sealed record BxPricingSection(string Title, IReadOnlyList<BxPricingTableFeature> Features);

/// <summary>
/// A row of a <see cref="BxPricingTable"/>. <see cref="Values"/> are the cells in the order of the tiers (or give
/// <see cref="ByTier"/>): <c>true</c> is a check mark, <c>false</c> or <c>null</c> a minus sign, anything else is shown as text.
/// </summary>
public sealed record BxPricingTableFeature(string Title, params object?[] Values)
{
    /// <summary>Cells by tier id, instead of by position. A tier missing from it falls back to its position in <see cref="Values"/>.</summary>
    public IReadOnlyDictionary<string, object?>? ByTier { get; init; }
}

public sealed record PricingTableUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Table { get; init; }
    public string? Caption { get; init; }
    public string? Thead { get; init; }
    public string? HeadRow { get; init; }
    public string? Corner { get; init; }
    public string? Tier { get; init; }
    public string? TierTitle { get; init; }
    public string? TierBadge { get; init; }
    public string? TierDescription { get; init; }
    public string? TierPrice { get; init; }
    public string? TierDiscount { get; init; }
    public string? TierCycle { get; init; }
    public string? TierButton { get; init; }
    public string? Tbody { get; init; }
    public string? SectionRow { get; init; }
    public string? Section { get; init; }
    public string? Row { get; init; }
    public string? Feature { get; init; }
    public string? Cell { get; init; }
    public string? ValueIcon { get; init; }
    public string? Included { get; init; }
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
