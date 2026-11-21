namespace Blux.UI.Tools;

/// <summary>One parameter of a query string: the decoded key and value, and the text it was read from (kept so an unknown parameter is written back untouched).</summary>
internal readonly record struct QueryPair(string Key, string Value, string Raw)
{
    public static QueryPair Create(string key, string value) =>
        new(key, value, Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value));
}

/// <summary>Reading and writing the query string of a URL as a list of pairs, in order, with repeated keys.</summary>
internal static class QueryPairs
{
    /// <summary>The query of an absolute or relative URI (what follows the first '?', up to a '#').</summary>
    public static List<QueryPair> Parse(string uri)
    {
        var pairs = new List<QueryPair>();
        var start = uri.IndexOf('?');
        if (start < 0) return pairs;
        var end = uri.IndexOf('#', start);
        var query = end < 0 ? uri.AsSpan(start + 1) : uri.AsSpan(start + 1, end - start - 1);
        foreach (var range in query.Split('&'))
        {
            var segment = query[range];
            if (segment.IsEmpty) continue;
            var equals = segment.IndexOf('=');
            var key = equals < 0 ? segment : segment[..equals];
            var value = equals < 0 ? default : segment[(equals + 1)..];
            pairs.Add(new QueryPair(Decode(key), Decode(value), segment.ToString()));
        }
        return pairs;
    }

    // Form encoding: '+' is a space, and a malformed escape stays as typed.
    private static string Decode(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return "";
        var plain = text.Contains('+') ? text.ToString().Replace('+', ' ') : text.ToString();
        return Uri.UnescapeDataString(plain);
    }

    /// <summary>The first value of a key, or null.</summary>
    public static string? First(List<QueryPair> pairs, string key)
    {
        foreach (var pair in pairs)
        {
            if (pair.Key == key) return pair.Value;
        }
        return null;
    }

    /// <summary>
    /// The pairs after the changes: a null value removes the key, anything else sets its first occurrence (in place, to keep the order of the URL)
    /// and drops the others; a key not there is added at the end. Pairs of other keys, and a pair whose value does not change, keep their text.
    /// </summary>
    public static List<QueryPair> Apply(List<QueryPair> pairs, IReadOnlyDictionary<string, string?> changes)
    {
        if (changes.Count == 0) return [.. pairs];
        var result = new List<QueryPair>(pairs.Count + changes.Count);
        var written = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in pairs)
        {
            if (!changes.TryGetValue(pair.Key, out var value)) result.Add(pair);
            else if (value is not null && written.Add(pair.Key)) result.Add(value == pair.Value ? pair : QueryPair.Create(pair.Key, value));
        }
        foreach (var (key, value) in changes)
        {
            if (value is not null && written.Add(key)) result.Add(QueryPair.Create(key, value));
        }
        return result;
    }

    /// <summary>"?a=1&amp;b=2", or an empty string for no pair.</summary>
    public static string ToQuery(List<QueryPair> pairs) =>
        pairs.Count == 0 ? "" : "?" + string.Join('&', pairs.Select(p => p.Raw));

    /// <summary>The URI without its query and fragment.</summary>
    public static string WithoutQuery(string uri)
    {
        var end = uri.IndexOfAny(['?', '#']);
        return end < 0 ? uri : uri[..end];
    }

    /// <summary>The fragment of a URI with its '#', or an empty string.</summary>
    public static string Fragment(string uri)
    {
        var hash = uri.IndexOf('#');
        return hash < 0 ? "" : uri[hash..];
    }

    /// <summary>A text equal for two lists that hold the same decoded pairs, whatever their order and encoding.</summary>
    public static string Canonical(List<QueryPair> pairs) =>
        string.Join('&', pairs.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)).Order(StringComparer.Ordinal));

    /// <summary>The keys whose first value differs between two lists (a key in only one of them differs).</summary>
    public static List<string> ChangedKeys(List<QueryPair> before, List<QueryPair> after)
    {
        var changed = new List<string>();
        foreach (var key in before.Select(p => p.Key).Concat(after.Select(p => p.Key)).Distinct(StringComparer.Ordinal))
        {
            if (First(before, key) != First(after, key)) changed.Add(key);
        }
        return changed;
    }
}
