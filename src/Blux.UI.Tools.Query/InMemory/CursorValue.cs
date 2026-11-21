using System.Globalization;

namespace Blux.UI.Tools.Query;

/// <summary>
/// The text form of a sort value inside a cursor. Unlike <see cref="TextConvert.Format"/> it keeps an empty string distinct from null (null sorts last,
/// an empty string sorts first, so mixing them up skips rows), writes an enum as its number (so undefined and [Flags] combinations survive) and a floating
/// point number with the round-trip format (NaN and the infinities included).
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
