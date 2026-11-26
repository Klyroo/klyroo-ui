using System.Text;

namespace Klyroo.UI;

/// <summary>
/// Builds the options of <see cref="KlySelect{TValue}"/>, <see cref="KlySelectMenu{TValue}"/>, <see cref="KlyInputMenu{TValue}"/> and <see cref="KlyListbox{TValue}"/>
/// from an enum or from the rows of a list (the entities of a foreign key). None of it uses reflection beyond <see cref="Enum.GetValues{TEnum}()"/>,
/// so it is safe with trimming and native AOT.
/// </summary>
public static class KlySelectItems
{
    /// <summary>
    /// One option per member of an enum, in the order of its values, so <c>@bind-Value</c> binds to the enum property itself.
    /// </summary>
    /// <typeparam name="TEnum">The enum.</typeparam>
    /// <param name="label">The text of a member. Defaults to its name split into words: <c>InProgress</c> is "In progress".</param>
    /// <returns>The options, typed with the enum. For a <c>TEnum?</c> property, call <see cref="AsNullable{TValue}"/> on them.</returns>
    public static IReadOnlyList<KlySelectItem<TEnum>> FromEnum<TEnum>(Func<TEnum, string>? label = null) where TEnum : struct, Enum
    {
        label ??= Humanize;
        return [.. Enum.GetValues<TEnum>().Select(value => new KlySelectItem<TEnum>(value, label(value)))];
    }

    /// <summary>
    /// One option per row of a list, such as the rows of a lookup table: the key of the row is the value, a column is the label.
    /// </summary>
    /// <typeparam name="TSource">The row type.</typeparam>
    /// <typeparam name="TValue">The type of the key: the type the control is bound to (an <c>int</c> foreign key, a <see cref="Guid"/>...).</typeparam>
    /// <param name="source">The rows.</param>
    /// <param name="value">The value of a row: its key.</param>
    /// <param name="label">The text of a row.</param>
    /// <returns>The options, in the order of the rows.</returns>
    public static IReadOnlyList<KlySelectItem<TValue>> From<TSource, TValue>(IEnumerable<TSource> source, Func<TSource, TValue> value, Func<TSource, string> label)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(label);
        return [.. source.Select(row => new KlySelectItem<TValue>(value(row), label(row)))];
    }

    /// <summary>
    /// The same options for a nullable property: a control bound to an <c>int?</c> takes <c>KlySelectItem&lt;int?&gt;</c>, and a list of <c>KlySelectItem&lt;int&gt;</c>
    /// is not one (a generic class is not covariant for value types). Disabled, count, icon and group are kept.
    /// </summary>
    /// <typeparam name="TValue">The value type: the type of the property without its <c>?</c>.</typeparam>
    /// <param name="items">The options to convert.</param>
    /// <returns>The options, typed with the nullable value.</returns>
    public static IReadOnlyList<KlySelectItem<TValue?>> AsNullable<TValue>(this IEnumerable<KlySelectItem<TValue>> items) where TValue : struct
    {
        ArgumentNullException.ThrowIfNull(items);
        return [.. items.Select(item => new KlySelectItem<TValue?>(item.Value, item.Label)
        {
            Disabled = item.Disabled,
            Count = item.Count,
            Icon = item.Icon,
            Group = item.Group,
        })];
    }

    // "InProgress" is "In progress", "HTTPServer" is "HTTP server": a word starts at a lower to upper change, or before the last capital of a run of capitals that is followed by a lower case letter.
    private static string Humanize<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var name = value.ToString();
        var text = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            var startsWord = i > 0 && char.IsUpper(c) && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]) || (char.IsUpper(name[i - 1]) && i + 1 < name.Length && char.IsLower(name[i + 1])));
            if (startsWord) text.Append(' ');
            // A word after the first is lower case, unless it is an acronym (the capital that starts it is followed by another capital).
            var acronym = char.IsUpper(c) && i + 1 < name.Length && char.IsUpper(name[i + 1]);
            text.Append(startsWord && !acronym ? char.ToLowerInvariant(c) : c);
        }
        return text.ToString();
    }
}
