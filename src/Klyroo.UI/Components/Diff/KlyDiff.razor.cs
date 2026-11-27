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

    /// <summary>
    /// Builds a field from two typed values: it is changed when they differ, and each value is shown with <paramref name="format"/>
    /// (<c>ToString()</c> of the value when not set; a <see langword="null"/> value shows nothing).
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="label">The field label.</param>
    /// <param name="before">The previous value.</param>
    /// <param name="after">The new value.</param>
    /// <param name="format">Turns a value into its text.</param>
    /// <param name="comparer">Decides whether the values are equal. The default equality comparer of <typeparamref name="T"/> when not set.</param>
    public static KlyDiffItem Compare<T>(string label, T? before, T? after, Func<T, string?>? format = null, IEqualityComparer<T?>? comparer = null)
    {
        string? Text(T? value) => value is null ? null : format is null ? value.ToString() : format(value);
        return new KlyDiffItem(label, Text(before), Text(after)) { Changed = !(comparer ?? EqualityComparer<T?>.Default).Equals(before, after) };
    }
}

/// <summary>Before and after values per field, with unchanged fields hidden unless <see cref="KlyDiff.ShowUnchanged"/> is set.</summary>
public partial class KlyDiff;
