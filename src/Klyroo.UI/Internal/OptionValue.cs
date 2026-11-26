using System.Globalization;

namespace Klyroo.UI.Internal;

/// <summary>
/// How the value of an option travels between .NET and the browser: as invariant text, the same for the <c>data-value</c> of an option,
/// the posted field and the event the browser raises. The text is not parsed back: a control finds the option whose text equals the one the browser reports.
/// </summary>
internal static class OptionValue
{
    /// <summary>
    /// The one text of a value, used for every comparison (the selected option, <c>aria-selected</c>, the posted field, the selection list): strings as they are, numbers and
    /// <see cref="Guid"/>s in the invariant culture, enums by name, and equal values always give the same text. A <see cref="decimal"/> has no trailing zeros (<c>1.00m</c> and
    /// <c>1m</c> are "1"), a <see cref="DateTime"/> is its ticks without its <see cref="DateTimeKind"/>, a <see cref="DateTimeOffset"/> is its UTC instant. Null is the empty text.
    /// </summary>
    public static string Key<T>(T? value)
    {
        if (value is null) return "";
        return value switch
        {
            string text => text,
            decimal number => (number / 1.000000000000000000000000000000000m).ToString(CultureInfo.InvariantCulture),
            DateTime dateTime => new DateTime(dateTime.Ticks, DateTimeKind.Unspecified).ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset offset => offset.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            Enum enumeration => enumeration.ToString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "",
        };
    }

    /// <summary>Whether <typeparamref name="T"/> is <see cref="string"/>: the only type whose free text is a value (created options, a non strict input menu).</summary>
    public static bool IsText<T>() => typeof(T) == typeof(string);

    /// <summary>The value for the text the browser reported: the option that has that text, else, for strings, the text itself, else the empty value (null, or the default of a value type).</summary>
    public static T? FromKey<T>(string? key, IEnumerable<IKlySelectItem> items)
    {
        if (string.IsNullOrEmpty(key)) return default;
        return TryFind<T>(key, items, out var value) ? value : IsText<T>() ? (T)(object)key : default;
    }

    /// <summary>Whether an option has that text, and its value if so.</summary>
    public static bool TryFind<T>(string? key, IEnumerable<IKlySelectItem> items, out T? value)
    {
        if (!string.IsNullOrEmpty(key))
        {
            foreach (var item in items)
            {
                if (Key(item.Value) == key)
                {
                    value = Cast<T>(item.Value);
                    return true;
                }
            }
        }
        value = default;
        return false;
    }

    /// <summary>The value of an option as the type the control is bound to. An option of another type is a mistake of the caller, said plainly.</summary>
    public static T? Cast<T>(object? value) => value switch
    {
        null => default,
        T typed => typed,
        _ => throw new InvalidOperationException($"An option has a value of type {value.GetType().Name} but the control is bound to {typeof(T).Name}: build the options with KlySelectItem<{typeof(T).Name}>."),
    };

    /// <summary>Whether two values are the same option: they have the same <see cref="Key{T}"/>.</summary>
    public static bool Same<T>(T? a, T? b) => Key(a) == Key(b);

    /// <summary>Whether <typeparamref name="T"/> can hold no value: a reference type or <see cref="Nullable{T}"/>. Any other value type has only <c>default</c>, which is a value.</summary>
    public static bool CanBeEmpty<T>() => !typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) is not null;

    /// <summary>Whether a value is "no value": null, or the empty text of a string.</summary>
    public static bool IsEmpty<T>(T? value) => value is null || (value is string text && text.Length == 0);
}
