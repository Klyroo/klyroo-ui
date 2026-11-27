namespace Klyroo.UI;

/// <summary>One field of a <see cref="KlyDiff"/>.</summary>
/// <param name="Label">The field label.</param>
/// <param name="Before">The previous value, shown struck through.</param>
/// <param name="After">The new value.</param>
public sealed record KlyDiffItem(string Label, string? Before, string? After)
{
    /// <summary>Whether the field changed. Unchanged fields are hidden unless <see cref="KlyDiff.ShowUnchanged"/> is set.</summary>
    public bool Changed { get; init; } = true;

    /// <summary>A badge shown after the label, such as <c>"Requires approval"</c>.</summary>
    public KlyBadgeItem? Note { get; init; }

    internal Func<System.Globalization.CultureInfo, string?>? BeforeFor { get; init; }

    internal Func<System.Globalization.CultureInfo, string?>? AfterFor { get; init; }

    /// <summary>
    /// Builds a field from two typed values: it is changed when they differ, and each value is formatted when the block renders, with
    /// <paramref name="format"/> (a .NET format string such as <c>"C2"</c> or <c>"yyyy-MM-dd"</c>, for a type that is <see cref="IFormattable"/>) and the <see cref="KlyDiff.Culture"/> of the block.
    /// A value that is not formattable, or no format, uses <c>ToString()</c>; a <see langword="null"/> value shows nothing.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="label">The field label.</param>
    /// <param name="before">The previous value.</param>
    /// <param name="after">The new value.</param>
    /// <param name="format">The format string of the values.</param>
    /// <param name="comparer">Decides whether the values are equal. The default equality comparer of <typeparamref name="T"/> when not set.</param>
    public static KlyDiffItem Compare<T>(string label, T? before, T? after, string? format = null, IEqualityComparer<T?>? comparer = null)
    {
        string? Text(T? value, System.Globalization.CultureInfo culture) => value switch
        {
            null => null,
            IFormattable formattable => formattable.ToString(format, culture),
            _ => value.ToString(),
        };
        return new KlyDiffItem(label, Text(before, System.Globalization.CultureInfo.InvariantCulture), Text(after, System.Globalization.CultureInfo.InvariantCulture))
        {
            Changed = !(comparer ?? EqualityComparer<T?>.Default).Equals(before, after),
            BeforeFor = culture => Text(before, culture),
            AfterFor = culture => Text(after, culture),
        };
    }

    /// <summary>
    /// Builds a field from two typed values shown with your own formatter (it receives the culture of the block). It is changed when the values differ.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="label">The field label.</param>
    /// <param name="before">The previous value.</param>
    /// <param name="after">The new value.</param>
    /// <param name="format">Turns a value and the culture of the block into text.</param>
    /// <param name="comparer">Decides whether the values are equal. The default equality comparer of <typeparamref name="T"/> when not set.</param>
    public static KlyDiffItem Compare<T>(string label, T? before, T? after, Func<T, System.Globalization.CultureInfo, string?> format, IEqualityComparer<T?>? comparer = null)
    {
        string? Text(T? value, System.Globalization.CultureInfo culture) => value is null ? null : format(value, culture);
        return new KlyDiffItem(label, Text(before, System.Globalization.CultureInfo.InvariantCulture), Text(after, System.Globalization.CultureInfo.InvariantCulture))
        {
            Changed = !(comparer ?? EqualityComparer<T?>.Default).Equals(before, after),
            BeforeFor = culture => Text(before, culture),
            AfterFor = culture => Text(after, culture),
        };
    }
}

/// <summary>Before and after values per field, with unchanged fields hidden unless <see cref="KlyDiff.ShowUnchanged"/> is set.</summary>
public partial class KlyDiff;
