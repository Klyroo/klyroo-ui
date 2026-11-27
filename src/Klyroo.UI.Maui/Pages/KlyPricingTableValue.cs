using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// The cell of a feature for one tier of a <see cref="KlyPricingTable"/>: a check mark (<c>true</c>), a minus sign (<c>false</c>, <c>null</c> or the default value), a text or a number.
/// <c>bool</c>, <c>string</c>, <c>int</c>, <c>long</c>, <c>decimal</c>, <c>double</c> and <c>float</c> convert to it implicitly, so <c>[true, "10 GB", false, 5, 2.5]</c> is a list of values.
/// A number is shown with the current culture.
/// </summary>
public readonly record struct KlyPricingTableValue
{
    private enum Kind
    {
        None,
        Included,
        Text,
        Number,
        Floating,
    }

    private readonly Kind _kind;
    private readonly string? _text;
    private readonly decimal _number;

    // A double or a float, kept as it is so it prints the way its own type does (0.1f stays "0.1").
    private readonly IFormattable? _floating;

    private KlyPricingTableValue(Kind kind, string? text, decimal number, IFormattable? floating = null)
    {
        _kind = kind;
        _text = text;
        _number = number;
        _floating = floating;
    }

    /// <summary>A check mark: the tier includes the feature.</summary>
    public static KlyPricingTableValue Included { get; } = new(Kind.Included, null, 0);

    /// <summary>A minus sign: the tier does not include the feature.</summary>
    public static KlyPricingTableValue NotIncluded { get; } = default;

    /// <summary>A text such as <c>"10 GB"</c>. <c>null</c> is a minus sign.</summary>
    /// <param name="text">The text shown in the cell.</param>
    public static KlyPricingTableValue Text(string? text) => text is null ? default : new(Kind.Text, text, 0);

    /// <summary>A number, shown with the current culture.</summary>
    /// <param name="number">The number shown in the cell.</param>
    public static KlyPricingTableValue Number(decimal number) => new(Kind.Number, null, number);

    /// <summary>A check mark for <c>true</c>, a minus sign for <c>false</c>.</summary>
    /// <param name="included">Whether the tier includes the feature.</param>
    public static implicit operator KlyPricingTableValue(bool included) => included ? Included : NotIncluded;

    /// <summary>A text, or a minus sign for <c>null</c>.</summary>
    /// <param name="text">The text shown in the cell.</param>
    public static implicit operator KlyPricingTableValue(string? text) => Text(text);

    /// <summary>A number.</summary>
    /// <param name="number">The number shown in the cell.</param>
    public static implicit operator KlyPricingTableValue(int number) => Number(number);

    /// <summary>A number.</summary>
    /// <param name="number">The number shown in the cell.</param>
    public static implicit operator KlyPricingTableValue(long number) => Number(number);

    /// <summary>A number.</summary>
    /// <param name="number">The number shown in the cell.</param>
    public static implicit operator KlyPricingTableValue(decimal number) => Number(number);

    /// <summary>A number, shown the way a <c>double</c> is.</summary>
    /// <param name="number">The number shown in the cell.</param>
    public static implicit operator KlyPricingTableValue(double number) => new(Kind.Floating, null, 0, number);

    /// <summary>A number, shown the way a <c>float</c> is.</summary>
    /// <param name="number">The number shown in the cell.</param>
    public static implicit operator KlyPricingTableValue(float number) => new(Kind.Floating, null, 0, number);

    /// <summary>The text of the cell, a number in the invariant culture: <c>"true"</c> for a check mark and an empty text for a minus sign.</summary>
    public override string ToString() => _kind switch
    {
        Kind.Included => "true",
        Kind.Text => _text!,
        Kind.Number => _number.ToString(null, CultureInfo.InvariantCulture),
        Kind.Floating => _floating!.ToString(null, CultureInfo.InvariantCulture),
        _ => "",
    };

    internal bool IsIncluded => _kind == Kind.Included;

    internal bool IsNotIncluded => _kind == Kind.None;

    internal string TextOf(IFormatProvider culture) => _kind switch
    {
        Kind.Text => _text!,
        Kind.Number => _number.ToString(null, culture),
        Kind.Floating => _floating!.ToString(null, culture),
        _ => "",
    };
}
