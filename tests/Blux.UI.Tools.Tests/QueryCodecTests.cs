using Blux.UI.Tools.Query;

namespace Blux.UI.Tools.Tests;

public class QueryCodecTests
{
    private enum Level { Low, Medium, High }

    private static void RoundTrips<T>(IQueryCodec<T> codec, T value, Func<T, T, bool>? same = null)
    {
        var text = codec.Format(value);
        Assert.NotNull(text);
        Assert.True(codec.TryParse(text, out var parsed), $"'{text}' did not parse back");
        Assert.True(same?.Invoke(value, parsed!) ?? EqualityComparer<T>.Default.Equals(value, parsed), $"'{text}' gave a different value");
    }

    private static void Rejects<T>(IQueryCodec<T> codec, params string[] inputs)
    {
        foreach (var input in inputs)
        {
            Assert.False(codec.TryParse(input, out _), $"'{input}' must not parse");
        }
    }

    [Fact]
    public void String_keeps_any_text_including_the_empty_one()
    {
        foreach (var value in new[] { "", " ", "a b", "é€😀", "a&b=c?d#e", "%41", "+", "~", "\n" })
        {
            RoundTrips(QueryCodecs.String, value);
        }
    }

    [Fact]
    public void Int32_round_trips_a_spread_of_values_and_rejects_anything_else()
    {
        foreach (var value in new[] { 0, 1, -1, 42, int.MaxValue, int.MinValue }.Concat(Enumerable.Range(-1000, 2001).Where(i => i % 37 == 0)))
        {
            RoundTrips(QueryCodecs.Int32, value);
        }
        Rejects(QueryCodecs.Int32, "", " 1", "1 ", "1.5", "1e3", "abc", "0x10", "2147483648", "-2147483649", "1,000", "--1", "+", "٣");
        Assert.True(QueryCodecs.Int32.TryParse("+7", out var plus));
        Assert.Equal(7, plus);
    }

    [Fact]
    public void Int64_round_trips_its_extremes_and_rejects_anything_else()
    {
        foreach (var value in new[] { 0L, 1L, -1L, long.MaxValue, long.MinValue, 9_007_199_254_740_993L })
        {
            RoundTrips(QueryCodecs.Int64, value);
        }
        Rejects(QueryCodecs.Int64, "", "9223372036854775808", "1.0", "x", " 5");
    }

    [Fact]
    public void Decimal_uses_a_dot_whatever_the_culture_and_keeps_its_scale()
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
        try
        {
            foreach (var value in new[] { 0m, 1m, -1m, 12.50m, 0.0000001m, -0.5m, decimal.MaxValue, decimal.MinValue, 1234567.891m })
            {
                RoundTrips(QueryCodecs.Decimal, value);
            }
            Assert.Equal("12.50", QueryCodecs.Decimal.Format(12.50m));
            Rejects(QueryCodecs.Decimal, "", "1,5", "1e3", "1 000", "NaN", ".", "abc", " 1");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Double_round_trips_exactly_and_never_accepts_non_finite_text()
    {
        var random = new Random(1234);
        var values = new List<double> { 0, -0.0, 1, -1, 0.1, 1.5, 1e-300, 1e300, double.Epsilon, double.MaxValue, double.MinValue, Math.PI };
        for (var i = 0; i < 500; i++) values.Add(BitConverter.Int64BitsToDouble(random.NextInt64()));
        foreach (var value in values.Where(double.IsFinite))
        {
            RoundTrips(QueryCodecs.Double, value, (a, b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b) || a == b);
        }
        Rejects(QueryCodecs.Double, "", "NaN", "Infinity", "-Infinity", "∞", "1,5", "abc", "1e999", " 1");
        Assert.Null(QueryCodecs.Double.Format(double.NaN));
        Assert.Null(QueryCodecs.Double.Format(double.PositiveInfinity));
    }

    [Fact]
    public void Boolean_reads_true_and_false_in_any_case_and_nothing_else()
    {
        RoundTrips(QueryCodecs.Boolean, true);
        RoundTrips(QueryCodecs.Boolean, false);
        Assert.True(QueryCodecs.Boolean.TryParse("TRUE", out var upper) && upper);
        Assert.True(QueryCodecs.Boolean.TryParse("False", out var mixed) && !mixed);
        Rejects(QueryCodecs.Boolean, "", "1", "0", "yes", "on", "t", " true", "truee");
    }

    [Fact]
    public void DateOnly_round_trips_every_day_of_a_leap_year_and_the_extremes()
    {
        for (var day = new DateOnly(2024, 1, 1); day.Year == 2024; day = day.AddDays(1))
        {
            RoundTrips(QueryCodecs.DateOnly, day);
        }
        RoundTrips(QueryCodecs.DateOnly, DateOnly.MinValue);
        RoundTrips(QueryCodecs.DateOnly, DateOnly.MaxValue);
        Assert.Equal("2026-03-07", QueryCodecs.DateOnly.Format(new DateOnly(2026, 3, 7)));
        Rejects(QueryCodecs.DateOnly, "", "2026-3-7", "2026/03/07", "07-03-2026", "2026-02-30", "2026-13-01", "20260307", "2026-03-07T00:00:00", "today", " 2026-03-07");
    }

    [Fact]
    public void DateTimeOffset_round_trips_with_its_offset_in_the_round_trip_format()
    {
        var random = new Random(99);
        for (var i = 0; i < 200; i++)
        {
            var offset = TimeSpan.FromMinutes(random.Next(-14 * 4, 14 * 4 + 1) * 15);
            var ticks = random.NextInt64(DateTime.MinValue.Ticks + TimeSpan.FromDays(2).Ticks, DateTime.MaxValue.Ticks - TimeSpan.FromDays(2).Ticks);
            var value = new DateTimeOffset(ticks, offset);
            RoundTrips(QueryCodecs.DateTimeOffset, value, (a, b) => a == b && a.Offset == b.Offset);
        }
        Assert.Equal("2026-01-02T03:04:05.0000000+01:00", QueryCodecs.DateTimeOffset.Format(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(1))));
        Assert.True(QueryCodecs.DateTimeOffset.TryParse("2026-01-02T03:04:05.0000000Z", out var utc));
        Assert.Equal(TimeSpan.Zero, utc.Offset);
        Rejects(QueryCodecs.DateTimeOffset, "", "2026-01-02", "2026-01-02 03:04:05", "yesterday", "2026-01-02T03:04:05", "1767323045");
    }

    [Fact]
    public void DateTimeOffset_reads_a_plus_sign_that_arrived_as_a_space()
    {
        // "+01:00" typed in an address bar is decoded as " 01:00" by a query parser.
        Assert.True(QueryCodecs.DateTimeOffset.TryParse("2026-01-02T03:04:05.0000000 01:00", out var value));
        Assert.Equal(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(1)), value);
        Assert.Equal(TimeSpan.FromHours(1), value.Offset);
    }

    [Fact]
    public void Enum_reads_names_in_any_case_and_writes_them_as_declared_or_lowercase()
    {
        var codec = QueryCodecs.Enum<Level>();
        foreach (var level in Enum.GetValues<Level>())
        {
            RoundTrips(codec, level);
            RoundTrips(QueryCodecs.Enum<Level>(lowercase: true), level);
            Assert.True(codec.TryParse(level.ToString().ToUpperInvariant(), out var upper) && upper == level);
            Assert.True(codec.TryParse(level.ToString().ToLowerInvariant(), out var lower) && lower == level);
        }
        Assert.Equal("high", QueryCodecs.Enum<Level>(lowercase: true).Format(Level.High));
        Rejects(codec, "", "1", "0", "-1", "99", "Low,High", "Extreme", " low", "Low ", "nope");
        Assert.Null(codec.Format((Level)42));
    }

    [Fact]
    public void Array_round_trips_awkward_items_whatever_their_characters()
    {
        var codec = QueryCodecs.Array(QueryCodecs.String);
        var alphabet = new[] { 'a', 'Z', ',', '~', '%', '2', 'C', '7', 'E', ' ', '&', '=', '.', '+', 'é', '"' };
        var random = new Random(7);
        var cases = new List<string[]>
        {
            new[] { "a" }, new[] { "a", "b" }, new[] { "" }, new[] { "", "" }, new[] { "a", "", "b" }, new[] { "," }, new[] { "~" }, new[] { "%" },
            new[] { "%2C" }, new[] { "%7E" }, new[] { "%25" }, new[] { ",,," }, new[] { "~,%" }, new[] { "a,b", "c~d", "e%f" }, new[] { "%%2C,~" },
        };
        for (var i = 0; i < 2000; i++)
        {
            var items = new string[random.Next(1, 6)];
            for (var j = 0; j < items.Length; j++)
            {
                items[j] = new string(Enumerable.Range(0, random.Next(0, 6)).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
            }
            cases.Add(items);
        }
        foreach (var items in cases)
        {
            RoundTrips(codec, items, (a, b) => a.SequenceEqual(b));
        }
    }

    [Fact]
    public void Array_text_never_holds_a_raw_separator_inside_an_item_or_a_stray_tilde()
    {
        var text = QueryCodecs.Array(QueryCodecs.String).Format(["a,b", "c~d", "50%", ""])!;

        Assert.Equal("a%2Cb,c%7Ed,50%25,~", text);
    }

    [Fact]
    public void Array_leaves_an_empty_list_out_and_reads_an_empty_value_as_an_empty_list()
    {
        var codec = QueryCodecs.Array(QueryCodecs.Int32);

        Assert.Null(codec.Format([]));
        Assert.True(codec.TryParse("", out var empty));
        Assert.Empty(empty);
    }

    [Fact]
    public void Array_of_numbers_rejects_a_list_with_one_bad_item_and_empty_tokens()
    {
        var codec = QueryCodecs.Array(QueryCodecs.Int32);

        RoundTrips(codec, [1, -2, 3], (a, b) => a.SequenceEqual(b));
        Rejects(codec, "1,x", "1,,2", ",", "1,", ",1", "1, 2");
    }

    [Fact]
    public void Array_keeps_a_percent_sign_that_is_not_one_of_its_escapes()
    {
        Assert.True(QueryCodecs.Array(QueryCodecs.String).TryParse("100%,%zz,%2", out var items));
        Assert.Equal(["100%", "%zz", "%2"], items);
    }

    [Fact]
    public void Array_supports_another_separator_and_refuses_the_escape_characters()
    {
        var codec = QueryCodecs.Array(QueryCodecs.String, '|');

        RoundTrips(codec, ["a|b", "c,d", "|", ""], (a, b) => a.SequenceEqual(b));
        Assert.Equal("a%7Cb,c", codec.Format(["a|b,c"]));
        foreach (var bad in new[] { '~', '%', '\n', 'é' })
        {
            Assert.Throws<ArgumentException>(() => QueryCodecs.Array(QueryCodecs.String, bad));
        }
    }

    [Fact]
    public void Array_of_dates_round_trips()
    {
        var codec = QueryCodecs.Array(QueryCodecs.DateOnly);

        RoundTrips(codec, [new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)], (a, b) => a.SequenceEqual(b));
        Rejects(codec, "2026-01-01,nope");
    }

    [Fact]
    public void Range_round_trips_with_either_end_missing()
    {
        var codec = QueryCodecs.Range(QueryCodecs.Int32);
        foreach (var range in new[]
        {
            new QueryRange<int>(1, 5), new QueryRange<int>(-5, -1), new QueryRange<int>(0, 0), new QueryRange<int>(null, 9), new QueryRange<int>(3, null),
            new QueryRange<int>(int.MinValue, int.MaxValue),
        })
        {
            RoundTrips(codec, range);
        }
        Assert.Equal("1..5", codec.Format(new(1, 5)));
        Assert.Equal("..9", codec.Format(new(null, 9)));
        Assert.Equal("3..", codec.Format(new(3, null)));
        Assert.Null(codec.Format(new(null, null)));
        Assert.Null(codec.Format(null!));
    }

    [Fact]
    public void Range_of_decimals_and_dates_keeps_the_dots_of_its_ends()
    {
        RoundTrips(QueryCodecs.Range(QueryCodecs.Decimal), new QueryRange<decimal>(1.5m, 2.25m));
        RoundTrips(QueryCodecs.Range(QueryCodecs.Decimal), new QueryRange<decimal>(null, 0.5m));
        RoundTrips(QueryCodecs.Range(QueryCodecs.Double), new QueryRange<double>(0.1, 1e-7));
        RoundTrips(QueryCodecs.Range(QueryCodecs.DateOnly), new QueryRange<DateOnly>(new(2026, 1, 1), new(2026, 2, 1)));
        RoundTrips(QueryCodecs.Range(QueryCodecs.DateTimeOffset), new QueryRange<DateTimeOffset>(new(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(1)), new(2026, 5, 6, 7, 8, 9, TimeSpan.Zero)),
            (a, b) => a.From == b.From && a.To == b.To);
    }

    [Fact]
    public void Range_rejects_text_without_two_dots_or_with_a_bad_end()
    {
        Rejects(QueryCodecs.Range(QueryCodecs.Int32), "", "5", "..", "1..x", "x..1", "1.5", "1-5", "1...5", " 1..2");
    }

    [Fact]
    public void Optional_makes_a_nullable_that_is_left_out_when_null()
    {
        var codec = QueryCodecs.Optional(QueryCodecs.Int32);

        RoundTrips(codec, (int?)7);
        Assert.Null(codec.Format(null));
        Assert.False(codec.TryParse("x", out var value));
        Assert.Null(value);
    }

    [Fact]
    public void No_codec_throws_on_hostile_input()
    {
        var inputs = new[] { "", "\0", new string('9', 5000), "%", "%%%", "..", ",,,,", "~", "\uD800", "😀", "1e99999", "-", "+", ".", "T", "0001-01-01T00:00:00.0000000+99:99" };
        Action<string>[] parsers =
        [
            s => QueryCodecs.Int32.TryParse(s, out _), s => QueryCodecs.Int64.TryParse(s, out _), s => QueryCodecs.Decimal.TryParse(s, out _),
            s => QueryCodecs.Double.TryParse(s, out _), s => QueryCodecs.Boolean.TryParse(s, out _), s => QueryCodecs.DateOnly.TryParse(s, out _),
            s => QueryCodecs.DateTimeOffset.TryParse(s, out _), s => QueryCodecs.Enum<Level>().TryParse(s, out _),
            s => QueryCodecs.Array(QueryCodecs.Int32).TryParse(s, out _), s => QueryCodecs.Array(QueryCodecs.String).TryParse(s, out _),
            s => QueryCodecs.Range(QueryCodecs.Int32).TryParse(s, out _), s => QueryCodecs.Range(QueryCodecs.DateOnly).TryParse(s, out _),
        ];

        foreach (var input in inputs)
        {
            foreach (var parse in parsers) parse(input);
        }
    }
}
