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

/// <summary>The kinds of cell a <see cref="KlyPricingTableValue"/> can be.</summary>
internal enum PricingValueKind
{
    None,
    Included,
    Text,
    Number,
    Floating,
}

/// <summary>
/// The cell of a feature for one tier of a <see cref="KlyPricingTable"/>: a check mark (<c>true</c>), a minus sign (<c>false</c>, <c>null</c> or the default value), a text or a number.
/// <c>bool</c>, <c>string</c>, <c>int</c>, <c>long</c>, <c>decimal</c>, <c>double</c> and <c>float</c> convert to it implicitly, so <c>[true, "10 GB", false, 5, 2.5]</c> is a list of values.
/// Smaller integers (<c>short</c>, <c>byte</c>) and <c>uint</c> widen to these implicitly. Any other numeric type, such as <c>ulong</c>, is cast first or passed to <see cref="Number"/>.
/// A number is shown with the format culture of the locale in effect.
/// </summary>
public readonly record struct KlyPricingTableValue
{
    private readonly PricingValueKind _kind;
    private readonly string? _text;
    private readonly decimal _number;
    // A double or a float, kept as it is so it prints the way its own type does (0.1f stays "0.1").
    private readonly IFormattable? _floating;

    private KlyPricingTableValue(PricingValueKind kind, string? text, decimal number, IFormattable? floating = null)
    {
        _kind = kind;
        _text = text;
        _number = number;
        _floating = floating;
    }

    /// <summary>A check mark: the tier includes the feature.</summary>
    public static KlyPricingTableValue Included { get; } = new(PricingValueKind.Included, null, 0);

    /// <summary>A minus sign: the tier does not include the feature.</summary>
    public static KlyPricingTableValue NotIncluded { get; } = default;

    /// <summary>A text such as <c>"10 GB"</c>. <c>null</c> is a minus sign.</summary>
    /// <param name="text">The text shown in the cell.</param>
    public static KlyPricingTableValue Text(string? text) => text is null ? default : new(PricingValueKind.Text, text, 0);

    /// <summary>A number, shown with the format culture of the locale in effect.</summary>
    /// <param name="number">The number shown in the cell.</param>
    public static KlyPricingTableValue Number(decimal number) => new(PricingValueKind.Number, null, number);

    /// <summary>A check mark for <c>true</c>, a minus sign for <c>false</c>.</summary>
    public static implicit operator KlyPricingTableValue(bool included) => included ? Included : NotIncluded;

    /// <summary>A text, or a minus sign for <c>null</c>.</summary>
    public static implicit operator KlyPricingTableValue(string? text) => Text(text);

    /// <summary>A number.</summary>
    public static implicit operator KlyPricingTableValue(int number) => Number(number);

    /// <summary>A number.</summary>
    public static implicit operator KlyPricingTableValue(long number) => Number(number);

    /// <summary>A number.</summary>
    public static implicit operator KlyPricingTableValue(decimal number) => Number(number);

    /// <summary>A number, shown with the format culture of the locale in effect like a <c>double</c> would be.</summary>
    public static implicit operator KlyPricingTableValue(double number) => new(PricingValueKind.Floating, null, 0, number);

    /// <summary>A number, shown with the format culture of the locale in effect like a <c>float</c> would be.</summary>
    public static implicit operator KlyPricingTableValue(float number) => new(PricingValueKind.Floating, null, 0, number);

    /// <summary>The text of the cell, a number in the invariant culture: <c>"true"</c> for a check mark and an empty text for a minus sign.</summary>
    public override string ToString() => _kind switch
    {
        PricingValueKind.Included => "true",
        PricingValueKind.Text => _text!,
        PricingValueKind.Number => _number.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        PricingValueKind.Floating => _floating!.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => "",
    };

    internal bool IsIncluded => _kind == PricingValueKind.Included;

    internal bool IsNotIncluded => _kind == PricingValueKind.None;

    internal string TextOf(IFormatProvider culture) => _kind switch
    {
        PricingValueKind.Text => _text!,
        PricingValueKind.Number => _number.ToString(null, culture),
        PricingValueKind.Floating => _floating!.ToString(null, culture),
        _ => "",
    };
}

/// <summary>
/// A row of a <see cref="KlyPricingTable"/>. <see cref="Values"/> are the cells in the order of the tiers (or give
/// <see cref="ByTier"/>): <c>true</c> is a check mark, <c>false</c> or <c>null</c> a minus sign, a text or a number is shown as it is.
/// </summary>
/// <param name="Title">Name of the feature, shown in the first column.</param>
/// <param name="Values">The cells, in the order of the tiers: <c>true</c> is a check mark, <c>false</c> or <c>null</c> a minus sign, a text or a number is shown as it is.</param>
public sealed record KlyPricingTableFeature(string Title, params KlyPricingTableValue[] Values)
{
    /// <summary>Cells by tier id, instead of by position. A tier missing from it falls back to its position in <see cref="Values"/>.</summary>
    public IReadOnlyDictionary<string, KlyPricingTableValue>? ByTier { get; init; }
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
