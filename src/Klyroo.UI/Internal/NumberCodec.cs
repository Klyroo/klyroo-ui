using System.Globalization;
using System.Numerics;

namespace Klyroo.UI.Internal;

/// <summary>
/// What a number field does with the type it is bound to, chosen once per type with no reflection: it turns the number into the invariant text
/// the browser element reads and reads that text back. Supports the built-in numeric types (<see cref="sbyte"/> to <see cref="ulong"/>,
/// <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/>) and their nullable forms; each is written out below so the trimmer and the
/// AOT compiler see every instantiation (a generic method over the type, made at run time, would not be safe).
/// </summary>
internal abstract class NumberCodec<T>
{
    private static NumberCodec<T>? _instance;

    /// <summary>The codec of <typeparamref name="T"/>.</summary>
    /// <exception cref="InvalidOperationException">The type is not a supported numeric type.</exception>
    public static NumberCodec<T> Instance => _instance ??= Create();

    /// <summary>The invariant text of a value (what the element and the posted field carry), or null for no value.</summary>
    public abstract string? Format(T? value);

    /// <summary>
    /// Reads the text the browser reported. An empty or unreadable text is no value: null for a nullable type, zero for the others. A text outside
    /// the range of the type (even beyond decimal) is clamped to its MinValue or MaxValue; a fraction of an integer type is rounded, halves away from zero.
    /// </summary>
    public abstract T? Parse(string? text);

    /// <summary>The value as a double, for the track of a slider. No value is null.</summary>
    public abstract double? ToDouble(T? value);

    /// <summary>Whether the type holds whole numbers only, so the field asks the keyboard for digits without a decimal separator.</summary>
    public abstract bool IsInteger { get; }

    private static NumberCodec<T> Create() =>
        Of<sbyte>() ?? Of<byte>() ?? Of<short>() ?? Of<ushort>() ?? Of<int>() ?? Of<uint>() ?? Of<long>() ?? Of<ulong>()
        ?? Of<float>() ?? Of<double>() ?? Of<decimal>()
        ?? throw new InvalidOperationException(
            $"{typeof(T).Name} is not a number type Klyroo.UI can bind: use sbyte, byte, short, ushort, int, uint, long, ulong, float, double or decimal, or the nullable form of one of them.");

    private static NumberCodec<T>? Of<TNumber>() where TNumber : struct, INumber<TNumber>
    {
        if (typeof(T) == typeof(TNumber)) return (NumberCodec<T>)(object)new NumberCodecs.Plain<TNumber>();
        if (typeof(T) == typeof(TNumber?)) return (NumberCodec<T>)(object)new NumberCodecs.Lifted<TNumber>();
        return null;
    }
}

/// <summary>The codecs of the supported numeric types, apart from <see cref="NumberCodec{T}"/> so that each is one type, not one per bound type.</summary>
internal static class NumberCodecs
{
    internal static TNumber Read<TNumber>(string? text, out bool found) where TNumber : struct, INumber<TNumber>
    {
        found = false;
        if (string.IsNullOrWhiteSpace(text)) return default;
        if (TNumber.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            found = true;
            return value;
        }
        // An integer type reads "2.5" as 3 (halves go away from zero) and a number beyond its range as its MinValue or MaxValue, as the field showed it, instead of dropping the text.
        if (IsIntegerType<TNumber>() && decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var wide))
        {
            found = true;
            return TNumber.CreateSaturating(Math.Round(wide, MidpointRounding.AwayFromZero));
        }
        // Beyond decimal (1e40): the double says which side of the range it is on, and the type takes its limit. A decimal field clamps the same way.
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var huge) && !double.IsNaN(huge))
        {
            found = true;
            return TNumber.CreateSaturating(IsIntegerType<TNumber>() ? Math.Round(huge, MidpointRounding.AwayFromZero) : huge);
        }
        return default;
    }

    // Of the supported types, the three that are not whole numbers; INumberBase.IsInteger tells about a value (zero is one in every type), not a type.
    internal static bool IsIntegerType<TNumber>() where TNumber : struct, INumber<TNumber> =>
        typeof(TNumber) != typeof(float) && typeof(TNumber) != typeof(double) && typeof(TNumber) != typeof(decimal);

    internal static string Write<TNumber>(TNumber value) where TNumber : struct, INumber<TNumber> => value.ToString(null, CultureInfo.InvariantCulture);

    internal sealed class Plain<TNumber> : NumberCodec<TNumber> where TNumber : struct, INumber<TNumber>
    {
        public override string? Format(TNumber value) => Write(value);

        public override TNumber Parse(string? text) => Read<TNumber>(text, out _);

        public override double? ToDouble(TNumber value) => double.CreateSaturating(value);

        public override bool IsInteger => IsIntegerType<TNumber>();
    }

    internal sealed class Lifted<TNumber> : NumberCodec<TNumber?> where TNumber : struct, INumber<TNumber>
    {
        public override string? Format(TNumber? value) => value is { } number ? Write(number) : null;

        public override TNumber? Parse(string? text)
        {
            var value = Read<TNumber>(text, out var found);
            return found ? value : null;
        }

        public override double? ToDouble(TNumber? value) => value is { } number ? double.CreateSaturating(number) : null;

        public override bool IsInteger => IsIntegerType<TNumber>();
    }
}
