using System.Globalization;
using System.Text;

namespace Blux.UI.Tools.Query;

/// <summary>
/// The text form of a filter rule's value in a URL: tokens separated by commas, where <c>,</c> <c>~</c> and <c>%</c> inside a value are written
/// <c>%2C</c> <c>%7E</c> <c>%25</c>, a bare <c>~</c> token means "include empty rows", a range is <c>from..to</c> (either side may be missing),
/// numbers use the invariant culture (a dot) and dates are ISO 8601 (<c>yyyy-MM-dd</c> for a day, the round-trip format for an instant).
/// </summary>
internal static class FilterRuleText
{
    public const string IncludeEmptyToken = "~";

    public static string OperatorName(FilterOperator op) => FieldAccessor.CamelCase(op.ToString());

    public static bool TryParseOperator(string text, out FilterOperator op) =>
        Enum.TryParse(text, ignoreCase: true, out op) && Enum.IsDefined(op) && !int.TryParse(text, out _);

    /// <summary>The value text of a rule: payload tokens, then the include-empty token when set.</summary>
    public static string Format(FilterRule rule)
    {
        if (rule.Operator is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty) return IncludeEmptyToken;
        var payload = rule.Value switch
        {
            FilterValue.List list => string.Join(',', list.Values.Where(v => v.Length > 0).Select(Escape)),
            var other => FormatScalar(other),
        };
        if (!rule.IncludeEmpty) return payload;
        return payload.Length == 0 ? IncludeEmptyToken : payload + "," + IncludeEmptyToken;
    }

    private static string FormatScalar(FilterValue? value) => value switch
    {
        null => "",
        FilterValue.Text t => Escape(t.Value),
        FilterValue.Number n => n.Value.ToString(CultureInfo.InvariantCulture),
        FilterValue.Boolean b => b.Value ? "true" : "false",
        FilterValue.Date d => d.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        FilterValue.Moment m => m.Value.ToString("O", CultureInfo.InvariantCulture),
        FilterValue.Range r => FormatScalar(r.From) + ".." + FormatScalar(r.To),
        _ => "",
    };

    /// <summary>
    /// Reads a rule from its URL text, or returns <see langword="null"/> when the text does not fit the filter. A date-time without an offset (<c>2026-11-05T14:00</c>, what a <c>datetime-local</c> field of a static form
    /// posts) is the wall clock of <paramref name="zone"/> (UTC when it is null), like the time field of the interactive editor; with an offset (<c>Z</c>, <c>+01:00</c>) it is that instant.
    /// </summary>
    public static FilterRule? Parse(DataListFilter filter, string key, FilterOperator op, string raw, TimeZoneInfo? zone = null)
    {
        if (op is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty)
        {
            var bare = new FilterRule(key, op);
            return raw.Length > 0 && filter.IsValid(bare) ? bare : null;
        }

        var includeEmpty = false;
        var payload = new List<string>();
        foreach (var token in raw.Split(','))
        {
            if (token == IncludeEmptyToken) includeEmpty = true;
            else payload.Add(Unescape(token));
        }

        FilterValue? value = filter.Kind switch
        {
            FilterKind.Text => ParseText(payload),
            FilterKind.Option => ParseOption(op, payload),
            FilterKind.Boolean => payload is [var b] && bool.TryParse(b, out var flag) ? new FilterValue.Boolean(flag) : null,
            FilterKind.Number => op == FilterOperator.Between ? ParseRange(payload, ParseNumber) : payload is [var n] ? ParseNumber(n) : null,
            _ => op == FilterOperator.Between ? ParseRange(payload, t => ParseDate(t, zone, filter.Granularity)) : payload is [var d] ? ParseDate(d, zone, filter.Granularity) : null,
        };
        if (value is null) return null;

        var rule = new FilterRule(key, op, value, includeEmpty);
        return filter.IsValid(rule) ? rule : null;
    }

    private static FilterValue? ParseText(List<string> payload)
    {
        var text = string.Join(',', payload);
        return text.Length == 0 ? null : new FilterValue.Text(text);
    }

    private static FilterValue ParseOption(FilterOperator op, List<string> payload)
    {
        var items = payload.Where(p => p.Length > 0).ToList();
        return op == FilterOperator.Is && items.Count == 1 ? new FilterValue.Text(items[0]) : new FilterValue.List(items);
    }

    private static FilterValue? ParseRange(List<string> payload, Func<string, FilterValue?> parse)
    {
        var text = string.Join(',', payload);
        var split = text.IndexOf("..", StringComparison.Ordinal);
        if (split < 0) return null;
        var left = text[..split];
        var right = text[(split + 2)..];
        FilterValue? from = null, to = null;
        if (left.Length > 0 && (from = parse(left)) is null) return null;
        if (right.Length > 0 && (to = parse(right)) is null) return null;
        return from is null && to is null ? null : new FilterValue.Range(from, to);
    }

    private static FilterValue? ParseNumber(string text) =>
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? new FilterValue.Number(n) : null;

    private static readonly string[] LocalFormats = ["yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF"];

    private static FilterValue? ParseDate(string text, TimeZoneInfo? zone, DateGranularity granularity)
    {
        if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            return new FilterValue.Date(day);
        // No offset: the wall clock of the zone of the request, cut to the unit of the filter like the editor does.
        if (DateTime.TryParseExact(text, LocalFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            var time = new TimeOnly(local.Hour, granularity == DateGranularity.Hour ? 0 : local.Minute, granularity == DateGranularity.Second ? local.Second : 0);
            return new FilterValue.Moment(LocalInstants.At(DateOnly.FromDateTime(local), time, zone ?? TimeZoneInfo.Utc));
        }
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal, out var moment)
            ? new FilterValue.Moment(moment)
            : null;
    }

    public static string Escape(string value)
    {
        if (value.AsSpan().IndexOfAny(",~%") < 0) return value;
        var sb = new StringBuilder(value.Length + 8);
        foreach (var c in value)
        {
            switch (c)
            {
                case ',': sb.Append("%2C"); break;
                case '~': sb.Append("%7E"); break;
                case '%': sb.Append("%25"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    public static string Unescape(string value)
    {
        if (!value.Contains('%')) return value;
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '%' && i + 2 < value.Length && TryEscape(value.AsSpan(i + 1, 2), out var c))
            {
                sb.Append(c);
                i += 2;
            }
            else
            {
                sb.Append(value[i]);
            }
        }
        return sb.ToString();
    }

    private static bool TryEscape(ReadOnlySpan<char> hex, out char c)
    {
        if (hex.Equals("2C", StringComparison.OrdinalIgnoreCase)) { c = ','; return true; }
        if (hex.Equals("7E", StringComparison.OrdinalIgnoreCase)) { c = '~'; return true; }
        if (hex.Equals("25", StringComparison.OrdinalIgnoreCase)) { c = '%'; return true; }
        c = default;
        return false;
    }
}
