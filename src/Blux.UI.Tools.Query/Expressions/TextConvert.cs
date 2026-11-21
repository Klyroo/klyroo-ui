using System.Globalization;

namespace Blux.UI.Tools.Query;

/// <summary>
/// Converts between the text form of a value (URL, option values, facet values, cursors) and a CLR value of the field type, always in the invariant
/// culture: numbers use a dot, booleans are <c>true</c>/<c>false</c>, enums use their name, dates are ISO 8601.
/// </summary>
internal static class TextConvert
{
    public static bool IsSupported(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(string) || type == typeof(bool) || type == typeof(char) || type.IsEnum
            || type == typeof(Guid) || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(DateTime)
            || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || IsNumeric(type);
    }

    public static bool IsNumeric(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(int) || type == typeof(long) || type == typeof(decimal) || type == typeof(double)
            || type == typeof(float) || type == typeof(short) || type == typeof(byte) || type == typeof(sbyte)
            || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong);
    }

    public static bool IsDate(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(DateOnly) || type == typeof(DateTime) || type == typeof(DateTimeOffset);
    }

    /// <summary>Parses <paramref name="text"/> as <paramref name="type"/> (a nullable type is unwrapped first).</summary>
    public static bool TryParse(string text, Type type, out object? value)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        var inv = CultureInfo.InvariantCulture;
        value = null;
        if (type == typeof(string)) { value = text; return true; }
        if (type == typeof(bool)) { if (bool.TryParse(text, out var b)) { value = b; return true; } return false; }
        if (type == typeof(char)) { if (text.Length == 1) { value = text[0]; return true; } return false; }
        if (type.IsEnum)
        {
            if (Enum.TryParse(type, text, ignoreCase: true, out var e) && Enum.IsDefined(type, e!)) { value = e; return true; }
            return false;
        }
        if (type == typeof(Guid)) { if (Guid.TryParse(text, out var g)) { value = g; return true; } return false; }
        if (type == typeof(DateOnly)) { if (DateOnly.TryParseExact(text, "yyyy-MM-dd", inv, DateTimeStyles.None, out var d)) { value = d; return true; } return false; }
        if (type == typeof(TimeOnly)) { if (TimeOnly.TryParse(text, inv, DateTimeStyles.None, out var t)) { value = t; return true; } return false; }
        if (type == typeof(DateTime)) { if (DateTime.TryParse(text, inv, DateTimeStyles.RoundtripKind, out var dt)) { value = dt; return true; } return false; }
        if (type == typeof(DateTimeOffset)) { if (DateTimeOffset.TryParse(text, inv, DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal, out var dto)) { value = dto; return true; } return false; }
        if (type == typeof(TimeSpan)) { if (TimeSpan.TryParse(text, inv, out var ts)) { value = ts; return true; } return false; }
        if (type == typeof(decimal)) { if (decimal.TryParse(text, NumberStyles.Float, inv, out var m)) { value = m; return true; } return false; }
        if (type == typeof(double)) { if (double.TryParse(text, NumberStyles.Float, inv, out var d2) && double.IsFinite(d2)) { value = d2; return true; } return false; }
        if (type == typeof(float)) { if (float.TryParse(text, NumberStyles.Float, inv, out var f) && float.IsFinite(f)) { value = f; return true; } return false; }
        if (type == typeof(long)) { if (long.TryParse(text, NumberStyles.AllowLeadingSign, inv, out var l)) { value = l; return true; } return false; }
        if (type == typeof(int)) { if (int.TryParse(text, NumberStyles.AllowLeadingSign, inv, out var i)) { value = i; return true; } return false; }
        if (type == typeof(short)) { if (short.TryParse(text, NumberStyles.AllowLeadingSign, inv, out var s)) { value = s; return true; } return false; }
        if (type == typeof(sbyte)) { if (sbyte.TryParse(text, NumberStyles.AllowLeadingSign, inv, out var sb)) { value = sb; return true; } return false; }
        if (type == typeof(byte)) { if (byte.TryParse(text, NumberStyles.None, inv, out var by)) { value = by; return true; } return false; }
        if (type == typeof(ushort)) { if (ushort.TryParse(text, NumberStyles.None, inv, out var us)) { value = us; return true; } return false; }
        if (type == typeof(uint)) { if (uint.TryParse(text, NumberStyles.None, inv, out var ui)) { value = ui; return true; } return false; }
        if (type == typeof(ulong)) { if (ulong.TryParse(text, NumberStyles.None, inv, out var ul)) { value = ul; return true; } return false; }
        return false;
    }

    /// <summary>Formats a value as text, or <see langword="null"/> for null and empty strings.</summary>
    public static string? Format(object? value) => value switch
    {
        null => null,
        string { Length: 0 } => null,
        string s => s,
        bool b => b ? "true" : "false",
        Enum e => e.ToString(),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    /// <summary>Tries to convert a decimal to <paramref name="numericType"/> without losing anything.</summary>
    public static bool TryConvertExact(decimal value, Type numericType, out object? converted)
    {
        numericType = Nullable.GetUnderlyingType(numericType) ?? numericType;
        converted = null;
        try
        {
            if (numericType == typeof(decimal)) { converted = value; return true; }
            if (numericType == typeof(double)) { converted = (double)value; return true; }
            if (numericType == typeof(float)) { converted = (float)value; return true; }
            var c = Convert.ChangeType(value, numericType, CultureInfo.InvariantCulture);
            if (Convert.ToDecimal(c, CultureInfo.InvariantCulture) != value) return false;
            converted = c;
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}
