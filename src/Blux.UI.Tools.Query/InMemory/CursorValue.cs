using System.Globalization;

namespace Blux.UI.Tools.Query;

/// <summary>
/// The text form of a sort value inside a cursor. Unlike <see cref="TextConvert.Format"/> it keeps an empty string distinct from null (null sorts last,
/// an empty string sorts first, so mixing them up skips rows), writes an enum as its number (so undefined and [Flags] combinations survive) and a floating
/// point number with the round-trip format (NaN and the infinities included). Every type a cursor accepts (the ones of <see cref="TextConvert.IsSupported"/>) is
/// written so that it reads back to the same value: <see cref="TimeOnly"/> and <see cref="TimeSpan"/> to the tick, dates in ISO form, <see cref="decimal"/> with its scale.
/// </summary>
internal static class CursorValue
{
    public static string? Format(object? value) => value switch
    {
        null => null,
        string s => s,
        Enum e => Convert.ToString(Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType()), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        float f => f.ToString("R", CultureInfo.InvariantCulture),
        // TextConvert drops the seconds of a TimeOnly ("14:30") and the sub-second part of a TimeSpan is formatted by the current rules of its type: a cursor
        // needs a form that reads back to the very same value, so these are written in their round-trip formats
        TimeOnly t => t.ToString("O", CultureInfo.InvariantCulture),
        TimeSpan ts => ts.ToString("c", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        decimal m => m.ToString(CultureInfo.InvariantCulture),
        _ => TextConvert.Format(value),
    };

    public static bool TryParse(string text, Type type, out object? value)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string)) { value = text; return true; }
        if (type.IsEnum)
        {
            if (TextConvert.TryParse(text, Enum.GetUnderlyingType(type), out var number)) { value = Enum.ToObject(type, number!); return true; }
            value = null;
            return false;
        }
        if (type == typeof(double)) { var ok = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d); value = d; return ok; }
        if (type == typeof(float)) { var ok = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var f); value = f; return ok; }
        return TextConvert.TryParse(text, type, out value);
    }
}
