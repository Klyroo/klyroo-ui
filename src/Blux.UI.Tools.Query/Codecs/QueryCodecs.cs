using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Blux.UI.Tools.Query;

/// <summary>
/// The built-in <see cref="IQueryCodec{T}"/>s. Numbers and dates use the invariant culture, so a URL means the same thing on every machine.
/// A codec never throws on input: text that is not a value makes <see cref="IQueryCodec{T}.TryParse"/> return false.
/// </summary>
public static class QueryCodecs
{
    /// <summary>Text as is.</summary>
    public static IQueryCodec<string> String { get; } = new StringCodec();

    /// <summary>A 32-bit integer: an optional sign and digits, nothing else.</summary>
    public static IQueryCodec<int> Int32 { get; } = new NumberCodec<int>(
        static (string s, out int v) => int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out v),
        static v => v.ToString(CultureInfo.InvariantCulture));

    /// <summary>A 64-bit integer: an optional sign and digits, nothing else.</summary>
    public static IQueryCodec<long> Int64 { get; } = new NumberCodec<long>(
        static (string s, out long v) => long.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out v),
        static v => v.ToString(CultureInfo.InvariantCulture));

    /// <summary>A decimal with a dot as the separator ("12.50"); no exponent and no thousands separator.</summary>
    public static IQueryCodec<decimal> Decimal { get; } = new NumberCodec<decimal>(
        static (string s, out decimal v) => decimal.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out v),
        static v => v.ToString(CultureInfo.InvariantCulture));

    /// <summary>A finite double with a dot as the separator; "NaN" and "Infinity" are not values (<see cref="IQueryCodec{T}.Format"/> leaves them out).</summary>
    public static IQueryCodec<double> Double { get; } = new FiniteDoubleCodec();

    /// <summary>"true" or "false", in any case.</summary>
    public static IQueryCodec<bool> Boolean { get; } = new BooleanCodec();

    /// <summary>A date as <c>yyyy-MM-dd</c>.</summary>
    public static IQueryCodec<DateOnly> DateOnly { get; } = new DateOnlyCodec();

    /// <summary>
    /// A moment in the round-trip format ("O", for example <c>2026-01-02T03:04:05.0000000+01:00</c>). A URL that carries the offset unescaped
    /// ("+01:00") reaches the server with a space where the plus sign was: that space is read as the plus sign.
    /// </summary>
    public static IQueryCodec<DateTimeOffset> DateTimeOffset { get; } = new DateTimeOffsetCodec();

    /// <summary>
    /// An enum by member name, case-insensitive on input. Numbers and names that are not members are not values. A <c>[Flags]</c> enum is written as its names
    /// separated by commas (<c>Read,Write</c>) and read back the same way; a combination with a bit that has no name is left out of the URL.
    /// </summary>
    /// <param name="lowercase">Writes the names in lower case ("desc"); input is case-insensitive either way.</param>
    public static IQueryCodec<TEnum> Enum<TEnum>(bool lowercase = false) where TEnum : struct, Enum => new EnumCodec<TEnum>(lowercase);

    /// <summary>
    /// A list: the items joined by <paramref name="separator"/> (a comma). The characters that would be mistaken for structure are written as
    /// percent escapes inside an item: the separator, <c>~</c> and <c>%</c>; an empty item is a lone <c>~</c>. An empty list is left out of the URL.
    /// </summary>
    /// <param name="item">The codec of an item.</param>
    /// <param name="separator">The separator, a character that is not <c>~</c> or <c>%</c>.</param>
    public static IQueryCodec<T[]> Array<T>(IQueryCodec<T> item, char separator = ',')
    {
        ArgumentNullException.ThrowIfNull(item);
        if (separator is '~' or '%' or > '\u007f' or < ' ')
        {
            throw new ArgumentException("The separator must be a printable ASCII character other than '~' and '%', the escape characters.", nameof(separator));
        }
        return new ArrayCodec<T>(item, separator);
    }

    /// <summary>A range written <c>from..to</c>; either end can be missing ("5..", "..9"). A range with no end is left out of the URL.</summary>
    /// <param name="item">The codec of an end.</param>
    public static IQueryCodec<QueryRange<T>> Range<T>(IQueryCodec<T> item) where T : struct
    {
        ArgumentNullException.ThrowIfNull(item);
        return new RangeCodec<T>(item);
    }

    /// <summary>Makes the codec of a struct a codec of its nullable: a null value is left out of the URL.</summary>
    /// <param name="item">The codec of the value.</param>
    public static IQueryCodec<T?> Optional<T>(IQueryCodec<T> item) where T : struct
    {
        ArgumentNullException.ThrowIfNull(item);
        return new OptionalCodec<T>(item);
    }

    private delegate bool TryParseDelegate<T>(string raw, out T value);

    private static bool Trimmed(string? raw) => raw is { Length: > 0 } && !char.IsWhiteSpace(raw[0]) && !char.IsWhiteSpace(raw[^1]);

    private sealed class StringCodec : IQueryCodec<string>
    {
        public bool TryParse(string raw, [MaybeNullWhen(false)] out string value)
        {
            value = raw;
            return raw is not null;
        }

        public string? Format(string value) => value;
    }

    private sealed class NumberCodec<T>(TryParseDelegate<T> parse, Func<T, string> format) : IQueryCodec<T>
    {
        public bool TryParse(string raw, [MaybeNullWhen(false)] out T value)
        {
            if (Trimmed(raw) && parse(raw, out var parsed))
            {
                value = parsed;
                return true;
            }
            value = default;
            return false;
        }

        public string? Format(T value) => format(value);
    }

    private sealed class FiniteDoubleCodec : IQueryCodec<double>
    {
        public bool TryParse(string raw, out double value)
        {
            if (Trimmed(raw)
                && double.TryParse(raw, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out value)
                && double.IsFinite(value))
            {
                return true;
            }
            value = 0;
            return false;
        }

        public string? Format(double value) => double.IsFinite(value) ? value.ToString("R", CultureInfo.InvariantCulture) : null;
    }

    private sealed class BooleanCodec : IQueryCodec<bool>
    {
        public bool TryParse(string raw, out bool value)
        {
            if (string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }
            if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }
            value = false;
            return false;
        }

        public string? Format(bool value) => value ? "true" : "false";
    }

    private sealed class DateOnlyCodec : IQueryCodec<DateOnly>
    {
        private const string Pattern = "yyyy-MM-dd";

        public bool TryParse(string raw, out DateOnly value) =>
            System.DateOnly.TryParseExact(raw, Pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);

        public string? Format(DateOnly value) => value.ToString(Pattern, CultureInfo.InvariantCulture);
    }

    private sealed class DateTimeOffsetCodec : IQueryCodec<DateTimeOffset>
    {
        public bool TryParse(string raw, out DateTimeOffset value)
        {
            if (Try(raw, out value)) return true;
            // "+01:00" typed in an address bar arrives as " 01:00": put the plus sign back, only where an offset belongs.
            if (raw is { Length: > 6 } && raw[^6] == ' ' && raw[^3] == ':')
            {
                return Try(string.Concat(raw.AsSpan(0, raw.Length - 6), "+", raw.AsSpan(raw.Length - 5)), out value);
            }
            return false;
        }

        private static bool Try(string raw, out DateTimeOffset value) =>
            System.DateTimeOffset.TryParseExact(raw, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);

        public string? Format(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);
    }

    private sealed class EnumCodec<TEnum>(bool lowercase) : IQueryCodec<TEnum> where TEnum : struct, Enum
    {
        private static readonly bool Flags = typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false);
        private static readonly HashSet<string> Names = new(System.Enum.GetNames<TEnum>(), StringComparer.OrdinalIgnoreCase);

        public bool TryParse(string raw, out TEnum value)
        {
            // Enum.TryParse also takes numbers ("1") and, for any enum, lists ("A,B"): only names are values, and lists only of a [Flags] enum.
            value = default;
            if (!Trimmed(raw)) return false;
            var tokens = raw.Split(',');
            if (tokens.Length > 1 && !Flags) return false;
            foreach (var token in tokens)
            {
                if (token.Length == 0 || !(char.IsLetter(token[0]) || token[0] == '_') || !Names.Contains(token)) return false;
            }
            return System.Enum.TryParse(string.Join(',', tokens), ignoreCase: true, out value);
        }

        public string? Format(TEnum value)
        {
            // ToString gives "A, B" for a combination of flags and a number when some bit has no name.
            var names = value.ToString().Split(", ");
            if (!names.All(Names.Contains)) return null;
            var text = string.Join(',', names);
            return lowercase ? text.ToLowerInvariant() : text;
        }
    }

    private sealed class OptionalCodec<T>(IQueryCodec<T> item) : IQueryCodec<T?> where T : struct
    {
        public bool TryParse(string raw, out T? value)
        {
            // An empty text is no value: it is how a null item of a list is written ("~").
            if (raw is { Length: 0 })
            {
                value = null;
                return true;
            }
            if (item.TryParse(raw, out var parsed))
            {
                value = parsed;
                return true;
            }
            value = null;
            return false;
        }

        public string? Format(T? value) => value is { } v ? item.Format(v) : null;
    }

    private sealed class ArrayCodec<T>(IQueryCodec<T> item, char separator) : IQueryCodec<T[]>
    {
        public bool TryParse(string raw, [MaybeNullWhen(false)] out T[] value)
        {
            value = null;
            if (raw is null) return false;
            if (raw.Length == 0)
            {
                value = [];
                return true;
            }
            var result = new List<T>();
            foreach (var token in raw.Split(separator))
            {
                if (!Unescape(token, out var text) || !item.TryParse(text, out var parsed)) return false;
                result.Add(parsed);
            }
            value = [.. result];
            return true;
        }

        public string? Format(T[] value)
        {
            if (value is null || value.Length == 0) return null;
            var builder = new StringBuilder();
            for (var i = 0; i < value.Length; i++)
            {
                if (i > 0) builder.Append(separator);
                var text = item.Format(value[i]) ?? "";
                if (text.Length == 0)
                {
                    builder.Append('~');
                    continue;
                }
                foreach (var c in text)
                {
                    if (c == '%') builder.Append("%25");
                    else if (c == '~') builder.Append("%7E");
                    else if (c == separator) builder.Append('%').Append(((int)c).ToString("X2", CultureInfo.InvariantCulture));
                    else builder.Append(c);
                }
            }
            return builder.ToString();
        }

        // Only the escapes Format writes are decoded; a '%' that starts anything else stays as typed. An empty token between two
        // separators is not something Format writes (an empty item is "~"), so it is not a value.
        private bool Unescape(string token, out string text)
        {
            if (token == "~")
            {
                text = "";
                return true;
            }
            text = token;
            if (token.Length == 0) return false;
            var builder = new StringBuilder(token.Length);
            for (var i = 0; i < token.Length; i++)
            {
                if (token[i] == '%' && i + 2 < token.Length && TryHex(token.AsSpan(i + 1, 2), out var decoded) && (decoded == '%' || decoded == '~' || decoded == separator))
                {
                    builder.Append(decoded);
                    i += 2;
                }
                else
                {
                    builder.Append(token[i]);
                }
            }
            text = builder.ToString();
            return true;
        }

        private static bool TryHex(ReadOnlySpan<char> digits, out char decoded)
        {
            decoded = '\0';
            if (!int.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code)) return false;
            decoded = (char)code;
            return true;
        }
    }

    private sealed class RangeCodec<T>(IQueryCodec<T> item) : IQueryCodec<QueryRange<T>> where T : struct
    {
        private const string Separator = "..";

        public bool TryParse(string raw, [MaybeNullWhen(false)] out QueryRange<T> value)
        {
            value = null;
            var at = raw?.IndexOf(Separator, StringComparison.Ordinal) ?? -1;
            if (at < 0) return false;
            var from = raw![..at];
            var to = raw[(at + Separator.Length)..];
            if (from.Length == 0 && to.Length == 0) return false;
            T? start = null, end = null;
            if (from.Length > 0)
            {
                if (!item.TryParse(from, out var parsed)) return false;
                start = parsed;
            }
            if (to.Length > 0)
            {
                if (!item.TryParse(to, out var parsed)) return false;
                end = parsed;
            }
            value = new QueryRange<T>(start, end);
            return true;
        }

        public string? Format(QueryRange<T> value)
        {
            if (value is null) return null;
            var from = value.From is { } a ? item.Format(a) : null;
            var to = value.To is { } b ? item.Format(b) : null;
            return from is null && to is null ? null : $"{from}{Separator}{to}";
        }
    }
}

/// <summary>A range of values with either end optional, as <see cref="QueryCodecs.Range{T}"/> reads and writes it.</summary>
/// <param name="From">The lower end, or null for no lower bound.</param>
/// <param name="To">The upper end, or null for no upper bound.</param>
public sealed record QueryRange<T>(T? From, T? To) where T : struct;
