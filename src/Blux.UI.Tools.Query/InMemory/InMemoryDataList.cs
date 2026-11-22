namespace Blux.UI.Tools.Query;

/// <summary>
/// Runs a <see cref="DataListRequest"/> against an in-memory list: search, filter tree, stable multi-key sort, offset or cursor paging, total count
/// and facets. The predicate is built as an expression (see the internal <c>FilterExpressions</c>) and compiled, so the IQueryable applier can
/// reuse the same definition of every operator.
/// </summary>
public static class InMemoryDataList
{
    /// <summary>
    /// Executes a request. The request is first passed through <see cref="DataListSchema.Sanitize(DataListRequest)"/>. Query-only filters are skipped.
    /// Rows with a null along a field path (<c>e.Department.Name</c> with no department) are read as empty rather than throwing.
    /// </summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="schema">The schema of the rows.</param>
    /// <param name="items">The rows; enumerated once.</param>
    /// <param name="request">What to compute.</param>
    /// <exception cref="InvalidOperationException">Cursor paging on a schema without a row key, or a sort field of a type the cursor cannot carry.</exception>
    public static DataListResult<T> ExecuteInMemory<T>(this DataListSchema<T> schema, IEnumerable<T> items, DataListRequest request)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(request);

        var clean = schema.Sanitize(request);
        var rows = items as IReadOnlyList<T> ?? [.. items];

        var zone = TimeZones.Resolve(clean.TimeZone) ?? TimeZoneInfo.Utc;
        var predicate = schema.GetPredicate(clean.Filter, clean.Search, excludeKey: null, zone);
        var matching = predicate is null ? rows : rows.Where(predicate).ToList();

        IReadOnlyList<DataListFacetResult>? facets = clean.Facets.Count == 0
            ? null
            : [.. clean.Facets.Select(f => CountFacet(schema, rows, clean, f, zone))];

        var sort = clean.Sort.Count > 0 ? clean.Sort : schema.DefaultSort;
        var sortFields = sort.Select(s => schema.TryGetField(s.Key, out var f) ? (Field: f, Rule: s) : throw new InvalidOperationException()).ToList();

        switch (clean.Paging)
        {
            case DataListPaging.Offset offset:
            {
                var ordered = Order(schema, matching, sortFields, rowKeyTieBreak: schema.HasRowKey);
                var page = ordered.Skip(offset.Skip).Take(offset.Take).ToList();
                return new DataListResult<T>(page, matching.Count, null, facets);
            }
            case DataListPaging.Cursor cursor:
                return ExecuteCursor(schema, matching, sortFields, cursor, facets);
            default:
                return new DataListResult<T>(Order(schema, matching, sortFields, rowKeyTieBreak: schema.HasRowKey).ToList(), matching.Count, null, facets);
        }
    }

    /// <summary>A <see cref="DataListProvider{T}"/> over a list, for a data list whose rows are in memory.</summary>
    public static DataListProvider<T> InMemoryProvider<T>(this DataListSchema<T> schema, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(items);
        var rows = items as IReadOnlyList<T> ?? [.. items];
        return (request, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(schema.ExecuteInMemory(rows, request));
        };
    }

    private static IEnumerable<T> Order<T>(
        DataListSchema<T> schema,
        IReadOnlyList<T> rows,
        List<(DataListField<T> Field, DataListSort Rule)> sort,
        bool rowKeyTieBreak)
    {
        IOrderedEnumerable<T>? ordered = null;
        foreach (var (field, rule) in sort)
        {
            var comparer = new SortValueComparer(rule.Descending);
            ordered = ordered is null ? rows.OrderBy(field.Getter, comparer) : ordered.ThenBy(field.Getter, comparer);
        }
        if (rowKeyTieBreak)
            ordered = ordered is null ? rows.OrderBy(schema.RowKeyValue, RowKeyComparer.Instance) : ordered.ThenBy(schema.RowKeyValue, RowKeyComparer.Instance);
        return ordered is null ? rows : ordered;
    }

    private static DataListResult<T> ExecuteCursor<T>(
        DataListSchema<T> schema,
        IReadOnlyList<T> matching,
        List<(DataListField<T> Field, DataListSort Rule)> sort,
        DataListPaging.Cursor cursor,
        IReadOnlyList<DataListFacetResult>? facets)
    {
        if (!schema.HasRowKey) throw new InvalidOperationException("Cursor paging needs a row key: declare RowKey(e => e.Id) in the schema.");
        foreach (var (field, _) in sort)
        {
            if (!TextConvert.IsSupported(field.Accessor.UnderlyingType))
                throw new InvalidOperationException($"The sort field '{field.Key}' ({field.Accessor.UnderlyingType.Name}) cannot be carried by a cursor.");
        }

        var keyType = schema.RowKeyAccessor!.UnderlyingType;
        if (!TextConvert.IsSupported(keyType))
            throw new InvalidOperationException($"The row key ({keyType.Name}) cannot be carried by a cursor.");

        var signature = string.Join('|', sort.Select(s => s.Rule.Descending ? "-" + s.Rule.Key : s.Rule.Key));
        var ordered = Order(schema, matching, sort, rowKeyTieBreak: true).ToList();

        var start = 0;
        if (!string.IsNullOrEmpty(cursor.After))
        {
            // A cursor that does not decode, or was made for another sort, ends the walk: an empty page and no next cursor. Restarting at the first
            // page would make an infinite scroll loop on the same rows forever.
            if (!DataListCursor.TryDecode(cursor.After, signature, sort.Count, out var afterKeyText, out var afterValues)
                || !TryParseValues(sort, afterValues, out var parsed)
                || !TextConvert.TryParse(afterKeyText, keyType, out var afterKey))
                return new DataListResult<T>([], cursor.Count ? matching.Count : null, null, facets);

            var comparers = sort.Select(s => new SortValueComparer(s.Rule.Descending)).ToArray();
            start = ordered.FindIndex(row =>
            {
                for (var i = 0; i < sort.Count; i++)
                {
                    var c = comparers[i].Compare(sort[i].Field.Getter(row), parsed[i]);
                    if (c != 0) return c > 0;
                }
                return RowKeyComparer.Instance.Compare(schema.RowKeyValue(row), afterKey) > 0;
            });
            if (start < 0) start = ordered.Count;
        }

        var page = ordered.Skip(start).Take(cursor.Take).ToList();
        string? next = null;
        if (page.Count > 0 && start + page.Count < ordered.Count)
        {
            var last = page[^1];
            next = DataListCursor.Encode(
                signature,
                schema.GetRowKey(last),
                [.. sort.Select(s => CursorValue.Format(s.Field.Getter(last)))]);
        }
        return new DataListResult<T>(page, cursor.Count ? matching.Count : null, next, facets);
    }

    private static bool TryParseValues<T>(
        List<(DataListField<T> Field, DataListSort Rule)> sort,
        string?[] texts,
        out object?[] values)
    {
        values = new object?[texts.Length];
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i] is null) continue;
            if (!CursorValue.TryParse(texts[i]!, sort[i].Field.Accessor.UnderlyingType, out values[i])) return false;
        }
        return true;
    }

    private static DataListFacetResult CountFacet<T>(DataListSchema<T> schema, IReadOnlyList<T> rows, DataListRequest request, DataListFacetRequest facet, TimeZoneInfo zone)
    {
        schema.TryGetField(facet.Key, out var field);
        var exclude = facet.Mode == FacetMode.ExcludeSelf ? facet.Key : null;
        var predicate = schema.GetPredicate(request.Filter, request.Search, exclude, zone);

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var missing = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        // Only some values: the texts a rule would match ("12.50" is the 12.5 of a number field), compared the way the options are written.
        HashSet<string>? wanted = null;
        if (facet.Values is { Count: > 0 })
        {
            wanted = new HashSet<string>(StringComparer.Ordinal);
            foreach (var text in facet.Values)
            {
                if (TextConvert.TryParse(text, field!.Accessor.UnderlyingType, out var converted) && TextConvert.Format(converted) is { } formatted) wanted.Add(formatted);
            }
        }
        foreach (var row in rows)
        {
            if (predicate is not null && !predicate(row)) continue;
            // a row counts once per distinct value, whatever the number of elements that carry it
            seen.Clear();
            foreach (var value in field!.Values(row))
                if (TextConvert.Format(value) is { } text) seen.Add(text);
            if (seen.Count == 0) missing++;
            foreach (var text in seen)
            {
                if (wanted is null || wanted.Contains(text)) counts[text] = counts.GetValueOrDefault(text) + 1;
            }
        }

        return FacetResults.Create(facet, counts, missing, schema.MaxFacetBuckets);
    }
}

/// <summary>The part of a facet shared by the in-memory executor and the IQueryable applier: search among the options, order, window, totals.</summary>
internal static class FacetResults
{
    public static DataListFacetResult Create(DataListFacetRequest facet, IEnumerable<KeyValuePair<string, int>> counts, int missing, int maxBuckets)
    {
        // a facet scoped to values counts all of them: the cap bounds an open facet, and the values are few and asked for
        if (facet.Values is { Count: > 0 } values) maxBuckets = Math.Max(maxBuckets, values.Count);
        // the cap comes first: the options kept are the most counted ones, and the search and the window apply among them
        var ranked = counts.OrderByDescending(o => o.Value).ThenBy(o => o.Key, StringComparer.Ordinal).ToList();
        var truncated = ranked.Count > maxBuckets;
        IEnumerable<KeyValuePair<string, int>> options = truncated ? ranked.Take(maxBuckets) : ranked;
        if (!string.IsNullOrWhiteSpace(facet.Search))
        {
            var needle = facet.Search.Trim();
            options = options.Where(o => TextFold.Contains(o.Key, needle));
        }
        var all = options.ToList();
        IEnumerable<KeyValuePair<string, int>> window = all.Skip(facet.Skip);
        if (facet.Limit is { } limit) window = window.Take(limit);
        return new DataListFacetResult(
            facet.Key,
            [.. window.Select(o => new DataListFacetOption(o.Key, o.Value))],
            facet.IncludeMissing ? missing : null,
            all.Count)
        {
            Truncated = truncated,
        };
    }
}
