using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>Formats the number types a <see cref="KlyStat{TValue}"/> or a <see cref="KlyStatGroup{TValue}"/> accepts.</summary>
internal static class DisplayNumber
{
    private static readonly Type[] Supported =
    [
        typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal),
    ];

    /// <summary>Throws when <paramref name="type"/> is not a built-in number type or its nullable form.</summary>
    public static void EnsureSupported(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (Array.IndexOf(Supported, underlying) < 0)
        {
            throw new InvalidOperationException(
                $"{type.Name} is not a number type Klyroo.UI.Maui can show: use sbyte, byte, short, ushort, int, uint, long, ulong, float, double or decimal, or the nullable form of one of them.");
        }
    }

    /// <summary>The text of a value with a .NET format string and a culture. No value, NaN and the infinities are <see langword="null"/>.</summary>
    public static string? Display(object? value, string? format, CultureInfo culture) => value switch
    {
        null => null,
        double d => double.IsFinite(d) ? d.ToString(format, culture) : null,
        float f => float.IsFinite(f) ? f.ToString(format, culture) : null,
        IFormattable formattable => formattable.ToString(format, culture),
        _ => null,
    };

    /// <summary>The sign of a value: -1, 0 or 1. No value, NaN and the infinities are <see langword="null"/>.</summary>
    public static int? Sign(object? value) => value switch
    {
        null => null,
        double d => double.IsFinite(d) ? Math.Sign(d) : null,
        float f => float.IsFinite(f) ? Math.Sign(f) : null,
        IConvertible convertible => Math.Sign(convertible.ToDecimal(CultureInfo.InvariantCulture)),
        _ => null,
    };
}
