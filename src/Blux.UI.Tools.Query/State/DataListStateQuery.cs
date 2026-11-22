using System.Globalization;

namespace Blux.UI.Tools.Query;

/// <summary>
/// Converts a <see cref="DataListState"/> to and from URL query keys, as plain functions (no Blazor, no <c>NavigationManager</c>). Keys, under an
/// optional prefix joined with a dot (<c>users.q</c>):
/// <list type="table">
/// <item><term><c>q</c></term><description>the search text</description></item>
/// <item><term><c>p</c></term><description>the 1-based page, at least 1 and at most <see cref="DataListState.MaxPage"/></description></item>
/// <item><term><c>ps</c></term><description>the page size, from 1 to the schema's maximum</description></item>
/// <item><term><c>s</c></term><description>the sort, <c>name|-age</c> (a leading <c>-</c> is descending); present and empty means "no sort"</description></item>
/// <item><term><c>f.&lt;key&gt;</c></term><description>a filter rule with the default operator of its kind</description></item>
/// <item><term><c>f.&lt;key&gt;.&lt;op&gt;</c></term><description>a filter rule with another operator (<c>contains is isAnyOf isNot gt gte lt lte between before after isEmpty isNotEmpty</c>)</description></item>
/// <item><term><c>c</c></term><description>the cursor</description></item>
/// <item><term><c>l</c></term><description><c>table</c> or <c>grid</c></description></item>
/// </list>
/// A filter value is a comma-separated list; <c>,</c> <c>~</c> and <c>%</c> in a value are escaped as <c>%2C</c> <c>%7E</c> <c>%25</c>; a bare
/// <c>~</c> token means "include empty rows"; a range is <c>from..to</c>. Values equal to the schema's <see cref="DataListSchema.DefaultState"/> are
/// omitted. A default filter rule that the user removed is written as its key with an empty value. The state keeps one rule per key; with two, the
/// default rule of that key is not restored. A filter key that is repeated (<c>f.status=Paid&amp;f.status=Draft</c>, what a form with a checkbox per option sends) is read as one value: an option filter takes all of them,
/// another kind keeps the first one that is not blank, plus a repeat that is exactly <c>~</c> (include empty rows); the address the app writes is always one comma list. A range may also be written as two keys, <c>f.&lt;key&gt;.from</c> and <c>f.&lt;key&gt;.to</c> (what a GET form can send): they read as <c>f.&lt;key&gt;.between</c>, and a state is always written back with the latter. Operators are matched ignoring case. A filter key may contain dots: the whole text after <c>f.</c> is tried
/// as a key first, then the part before the last dot as a key and the part after as an operator.
/// </summary>
public static class DataListStateQuery
{
    /// <summary>Writes the keys of a state that differ from the defaults. Rules the schema does not accept are left out, so the result always reads back.</summary>
    /// <param name="state">The state.</param>
    /// <param name="schema">The schema giving the defaults, the filter kinds and the sortable keys.</param>
    /// <param name="prefix">An optional key prefix for pages with several data lists.</param>
    /// <returns>Raw (not URL-encoded) keys and values, in a stable order.</returns>
    public static IReadOnlyDictionary<string, string> ToQuery(DataListState state, DataListSchema schema, string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(schema);
        var defaults = schema.DefaultState;
        var query = new Dictionary<string, string>(StringComparer.Ordinal);

        if (state.Search != defaults.Search) query[Key(prefix, "q")] = state.Search;

        if (string.IsNullOrEmpty(state.Cursor))
        {
            var page = Math.Clamp(state.Page, 1, DataListState.MaxPage);
            if (page != defaults.Page) query[Key(prefix, "p")] = page.ToString(CultureInfo.InvariantCulture);
        }

        var size = Math.Clamp(state.PageSize, 1, schema.MaxPageSize);
        if (size != defaults.PageSize) query[Key(prefix, "ps")] = size.ToString(CultureInfo.InvariantCulture);

        var sort = CleanSort(state.Sort, schema);
        if (!SequenceEquality.Equal(sort, defaults.Sort))
            query[Key(prefix, "s")] = string.Join('|', sort.Select(s => s.Descending ? "-" + s.Key : s.Key));

        WriteFilters(state, schema, prefix, query);

        if (!string.IsNullOrEmpty(state.Cursor)) query[Key(prefix, "c")] = state.Cursor;
        if (state.Layout != defaults.Layout) query[Key(prefix, "l")] = state.Layout == DataListLayout.Grid ? "grid" : "table";
        return query;
    }

    /// <summary>
    /// Reads a state from query keys. Starts from the schema defaults, so an empty query gives <see cref="DataListSchema.DefaultState"/>. Never throws on
    /// bad input: unknown filter keys, operators the filter does not allow, values that do not parse, unknown or repeated sort keys and an unknown layout
    /// are dropped (the default applies); the page is clamped to 1..<see cref="DataListState.MaxPage"/> and the page size to 1..<see cref="DataListSchema.MaxPageSize"/>.
    /// </summary>
    /// <param name="query">The decoded key and value pairs; when a key repeats, the first one counts, except that the repeats of a filter key are merged (see the remarks). Null values are ignored.</param>
    /// <param name="schema">The schema giving the defaults, the filter kinds and the sortable keys.</param>
    /// <param name="prefix">The key prefix given to <see cref="ToQuery"/>.</param>
    public static DataListState FromQuery(IEnumerable<KeyValuePair<string, string?>> query, DataListSchema schema, string? prefix = null) =>
        FromQuery(query, schema, prefix, null);

    /// <summary>
    /// <see cref="FromQuery(IEnumerable{KeyValuePair{string, string?}}, DataListSchema, string?)"/> for a request that has a time zone: a date-time in a filter value without an offset (<c>2026-11-05T14:00</c>, what the
    /// <c>datetime-local</c> field of a static form posts) is the wall clock of that zone, not UTC. A date (<c>2026-11-05</c>) and a value with an offset do not depend on it.
    /// </summary>
    /// <param name="query">The decoded key and value pairs; when a key repeats, the first one counts, except that the repeats of a filter key are merged (see the remarks). Null values are ignored.</param>
    /// <param name="schema">The schema giving the defaults, the filter kinds and the sortable keys.</param>
    /// <param name="prefix">The key prefix given to <see cref="ToQuery"/>.</param>
    /// <param name="timeZone">The IANA id of the zone of the user; null or an unknown id means UTC.</param>
    public static DataListState FromQuery(IEnumerable<KeyValuePair<string, string?>> query, DataListSchema schema, string? prefix, string? timeZone)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(schema);
        var defaults = schema.DefaultState;
        var pairs = new Dictionary<string, string>(StringComparer.Ordinal);
        var filterNames = Key(prefix, "f.");
        Dictionary<string, List<string>>? repeated = null;
        foreach (var (key, value) in query)
        {
            if (key is null || value is null) continue;
            if (pairs.TryAdd(key, value)) continue;
            // A GET form sends a checkbox per option under one name: the repeats of a filter key are read as one value.
            if (!key.StartsWith(filterNames, StringComparison.Ordinal)) continue;
            repeated ??= new(StringComparer.Ordinal);
            if (!repeated.TryGetValue(key, out var all)) repeated[key] = all = [pairs[key]];
            all.Add(value);
        }
        if (repeated is not null)
        {
            foreach (var (name, values) in repeated)
                if (TryResolve(name[filterNames.Length..], schema, out _, out _, out var repeatedFilter)) pairs[name] = MergeRepeated(repeatedFilter.Kind, values);
        }

        var search = pairs.GetValueOrDefault(Key(prefix, "q")) ?? defaults.Search;

        var page = defaults.Page;
        if (pairs.TryGetValue(Key(prefix, "p"), out var p) && int.TryParse(p, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var pn))
            page = Math.Clamp(pn, 1, DataListState.MaxPage);

        var size = defaults.PageSize;
        if (pairs.TryGetValue(Key(prefix, "ps"), out var ps) && int.TryParse(ps, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var sn))
            size = Math.Clamp(sn, 1, schema.MaxPageSize);

        var sort = defaults.Sort;
        if (pairs.TryGetValue(Key(prefix, "s"), out var s))
        {
            var parsed = ParseSort(s, schema);
            sort = s.Length == 0 ? [] : parsed.Count > 0 ? parsed : defaults.Sort;
        }

        var filters = ReadFilters(pairs, schema, prefix, defaults.Filters, TimeZones.Resolve(timeZone));

        var layout = defaults.Layout;
        if (pairs.TryGetValue(Key(prefix, "l"), out var l) && Enum.TryParse<DataListLayout>(l, ignoreCase: true, out var parsedLayout)
            && Enum.IsDefined(parsedLayout) && !int.TryParse(l, out _))
            layout = parsedLayout;

        var cursor = pairs.GetValueOrDefault(Key(prefix, "c"));
        return new DataListState(search, filters, sort, page, size, layout, string.IsNullOrEmpty(cursor) ? null : cursor);
    }

    // The value of a filter key that appears several times. An option filter takes every value (the tokens of all of them, in order); another kind keeps the first value that is not blank (the first one counts, as before)
    // plus the include-empty token if any repeat is exactly that token, which is how a form says "this rule also matches empty rows" next to the field that holds the value.
    private static string MergeRepeated(FilterKind kind, List<string> values)
    {
        var filled = values.Where(v => v.Length > 0).ToList();
        if (kind == FilterKind.Option) return string.Join(',', filled);
        var payload = filled.FirstOrDefault(v => v != FilterRuleText.IncludeEmptyToken);
        var includeEmpty = filled.Contains(FilterRuleText.IncludeEmptyToken);
        if (payload is null) return includeEmpty ? FilterRuleText.IncludeEmptyToken : "";
        return includeEmpty ? payload + "," + FilterRuleText.IncludeEmptyToken : payload;
    }

    private static string Key(string? prefix, string name) => string.IsNullOrEmpty(prefix) ? name : prefix + "." + name;

    private static List<DataListSort> CleanSort(IReadOnlyList<DataListSort> sort, DataListSchema schema)
    {
        var clean = new List<DataListSort>();
        foreach (var rule in sort)
            if (rule is not null && schema.SortKeys.Contains(rule.Key) && clean.All(c => c.Key != rule.Key)) clean.Add(rule);
        return clean;
    }

    private static List<DataListSort> ParseSort(string text, DataListSchema schema)
    {
        var sort = new List<DataListSort>();
        foreach (var token in text.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var descending = token.StartsWith('-');
            var key = descending ? token[1..] : token;
            if (schema.SortKeys.Contains(key) && sort.All(r => r.Key != key)) sort.Add(new DataListSort(key, descending));
        }
        return sort;
    }

    private static string FilterKey(string? prefix, DataListFilter filter, string key, FilterOperator op) =>
        Key(prefix, op == filter.DefaultOperator ? "f." + key : "f." + key + "." + FilterRuleText.OperatorName(op));

    private static void WriteFilters(DataListState state, DataListSchema schema, string? prefix, Dictionary<string, string> query)
    {
        var rules = new List<FilterRule>();
        foreach (var rule in state.Filters)
        {
            if (rule is null || !schema.TryGetFilter(rule.Key, out var filter) || !filter.IsValid(rule)) continue;
            var normalized = rule.Operator is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty && rule.IncludeEmpty ? rule with { IncludeEmpty = false } : rule;
            if (rules.Any(r => r.Key == normalized.Key && r.Operator == normalized.Operator)) continue;
            rules.Add(normalized);
        }

        var defaults = schema.DefaultState.Filters;
        var overridden = rules.Where(r => !defaults.Contains(r)).Select(r => r.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var rule in rules)
        {
            if (defaults.Contains(rule) && !overridden.Contains(rule.Key)) continue;
            schema.TryGetFilter(rule.Key, out var filter);
            query[FilterKey(prefix, filter!, rule.Key, rule.Operator)] = FilterRuleText.Format(rule);
        }
        foreach (var def in defaults)
        {
            if (rules.Any(r => r.Key == def.Key)) continue;
            schema.TryGetFilter(def.Key, out var filter);
            query[FilterKey(prefix, filter!, def.Key, def.Operator)] = "";
        }
    }

    private static List<FilterRule> ReadFilters(Dictionary<string, string> pairs, DataListSchema schema, string? prefix, IReadOnlyList<FilterRule> defaults, TimeZoneInfo? zone)
    {
        var fromUrl = new List<FilterRule>();
        var cleared = new HashSet<string>(StringComparer.Ordinal);
        var filterPrefix = Key(prefix, "f.");
        foreach (var (name, raw) in pairs)
        {
            if (!name.StartsWith(filterPrefix, StringComparison.Ordinal)) continue;
            if (!TryResolve(name[filterPrefix.Length..], schema, out var key, out var op, out var filter)) continue;
            if (raw.Length == 0) { cleared.Add(key); continue; }
            var rule = FilterRuleText.Parse(filter, key, op ?? filter.DefaultOperator, raw, zone);
            if (rule is null || fromUrl.Any(r => r.Key == rule.Key && r.Operator == rule.Operator)) continue;
            fromUrl.Add(rule);
        }

        ReadRangeFields(pairs, schema, filterPrefix, fromUrl, cleared, zone);

        var overridden = fromUrl.Select(r => r.Key).ToHashSet(StringComparer.Ordinal);
        var result = defaults.Where(d => !overridden.Contains(d.Key) && !cleared.Contains(d.Key)).ToList();
        result.AddRange(fromUrl);
        return result;
    }

    // A GET form cannot write the "from..to" of a range in one field, so it sends two: f.<key>.from and f.<key>.to, which mean f.<key>.between. Both empty clears the filter, one empty is an open end.
    // The keys the app writes (f.<key>.between) win, and a filter whose key really ends in ".from" or ".to" keeps that meaning.
    private static void ReadRangeFields(Dictionary<string, string> pairs, DataListSchema schema, string filterPrefix, List<FilterRule> fromUrl, HashSet<string> cleared, TimeZoneInfo? zone)
    {
        Dictionary<string, (string From, string To, DataListFilter Filter)>? ranges = null;
        foreach (var (name, raw) in pairs)
        {
            if (!name.StartsWith(filterPrefix, StringComparison.Ordinal)) continue;
            var rest = name[filterPrefix.Length..];
            var dot = rest.LastIndexOf('.');
            if (dot <= 0 || schema.TryGetFilter(rest, out _)) continue;
            var part = rest[(dot + 1)..];
            var isFrom = part.Equals("from", StringComparison.OrdinalIgnoreCase);
            if (!isFrom && !part.Equals("to", StringComparison.OrdinalIgnoreCase)) continue;
            var key = rest[..dot];
            if (!schema.TryGetFilter(key, out var filter) || !filter.AllowedOperators.Contains(FilterOperator.Between)) continue;
            ranges ??= new(StringComparer.Ordinal);
            var current = ranges.GetValueOrDefault(key, ("", "", filter));
            ranges[key] = isFrom ? (raw, current.Item2, filter) : (current.Item1, raw, filter);
        }
        if (ranges is null) return;
        foreach (var (key, (from, to, filter)) in ranges)
        {
            if (fromUrl.Any(r => r.Key == key && r.Operator == FilterOperator.Between)) continue;
            if (from.Length == 0 && to.Length == 0) { cleared.Add(key); continue; }
            // The include-empty token of a range travels in a companion field with the key a rule of the app uses (f.<key>.between=~): two ends cannot carry it.
            var includeEmpty = pairs.TryGetValue(filterPrefix + key + ".between", out var companion) && companion.Split(',').Contains(FilterRuleText.IncludeEmptyToken);
            var rule = FilterRuleText.Parse(filter, key, FilterOperator.Between, from + ".." + to + (includeEmpty ? "," + FilterRuleText.IncludeEmptyToken : ""), zone);
            if (rule is not null) fromUrl.Add(rule);
        }
    }

    private static bool TryResolve(string rest, DataListSchema schema, out string key, out FilterOperator? op, out DataListFilter filter)
    {
        if (schema.TryGetFilter(rest, out filter!))
        {
            key = rest;
            op = null;
            return true;
        }
        var dot = rest.LastIndexOf('.');
        if (dot > 0 && FilterRuleText.TryParseOperator(rest[(dot + 1)..], out var parsed) && schema.TryGetFilter(rest[..dot], out filter!))
        {
            key = rest[..dot];
            op = parsed;
            return true;
        }
        key = "";
        op = null;
        filter = null!;
        return false;
    }
}
