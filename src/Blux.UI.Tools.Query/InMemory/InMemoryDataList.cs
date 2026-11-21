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

        var predicate = FilterExpressions.Rows(schema, clean.Filter, clean.Search, excludeKey: null, nullSafe: true)?.Compile();
        var matching = predicate is null ? rows : rows.Where(predicate).ToList();

        IReadOnlyList<DataListFacetResult>? facets = clean.Facets.Count == 0
            ? null
            : [.. clean.Facets.Select(f => CountFacet(schema, rows, clean, f))];

        var sort = clean.Sort.Count > 0 ? clean.Sort : schema.DefaultSort;
        var sortFields = sort.Select(s => schema.TryGetField(s.Key, out var f) ? (Field: f, Rule: s) : throw new InvalidOperationException()).ToList();

        switch (clean.Paging)
        {
            case DataListPaging.Offset offset:
            {
                var ordered = Order(schema, matching, sortFields, rowKeyTieBreak: false);
                var page = ordered.Skip(offset.Skip).Take(offset.Take).ToList();
                return new DataListResult<T>(page, matching.Count, null, facets);
            }
            case DataListPaging.Cursor cursor:
                return ExecuteCursor(schema, matching, sortFields, cursor, facets);
            default:
                return new DataListResult<T>(Order(schema, matching, sortFields, rowKeyTieBreak: false).ToList(), matching.Count, null, facets);
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
            ordered = ordered is null ? rows.OrderBy(schema.GetRowKey, StringComparer.Ordinal) : ordered.ThenBy(schema.GetRowKey, StringComparer.Ordinal);
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

        var signature = string.Join('|', sort.Select(s => s.Rule.Descending ? "-" + s.Rule.Key : s.Rule.Key));
        var ordered = Order(schema, matching, sort, rowKeyTieBreak: true).ToList();

        var start = 0;
        if (!string.IsNullOrEmpty(cursor.After)
            && DataListCursor.TryDecode(cursor.After, signature, sort.Count, out var afterKey, out var afterValues)
            && TryParseValues(sort, afterValues, out var parsed))
        {
            var comparers = sort.Select(s => new SortValueComparer(s.Rule.Descending)).ToArray();
            start = ordered.FindIndex(row =>
            {
                for (var i = 0; i < sort.Count; i++)
                {
                    var c = comparers[i].Compare(sort[i].Field.Getter(row), parsed[i]);
                    if (c != 0) return c > 0;
                }
                return string.CompareOrdinal(schema.GetRowKey(row), afterKey) > 0;
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
                [.. sort.Select(s => TextConvert.Format(s.Field.Getter(last)))]);
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
            if (!TextConvert.TryParse(texts[i]!, sort[i].Field.Accessor.UnderlyingType, out values[i])) return false;
        }
        return true;
    }

    private static DataListFacetResult CountFacet<T>(DataListSchema<T> schema, IReadOnlyList<T> rows, DataListRequest request, DataListFacetRequest facet)
    {
        schema.TryGetField(facet.Key, out var field);
        var exclude = facet.Mode == FacetMode.ExcludeSelf ? facet.Key : null;
        var predicate = FilterExpressions.Rows(schema, request.Filter, request.Search, exclude, nullSafe: true)?.Compile();

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var missing = 0;
        foreach (var row in rows)
        {
            if (predicate is not null && !predicate(row)) continue;
            var text = TextConvert.Format(field!.Getter(row));
            if (text is null) missing++;
            else counts[text] = counts.GetValueOrDefault(text) + 1;
        }

        IEnumerable<KeyValuePair<string, int>> options = counts;
        if (!string.IsNullOrWhiteSpace(facet.Search))
        {
            var needle = facet.Search.Trim().ToLowerInvariant();
            options = options.Where(o => o.Key.ToLowerInvariant().Contains(needle, StringComparison.Ordinal));
        }
        var all = options.OrderByDescending(o => o.Value).ThenBy(o => o.Key, StringComparer.Ordinal).ToList();
        IEnumerable<KeyValuePair<string, int>> window = all.Skip(facet.Skip);
        if (facet.Limit is { } limit) window = window.Take(limit);
        return new DataListFacetResult(
            facet.Key,
            [.. window.Select(o => new DataListFacetOption(o.Key, o.Value))],
            facet.IncludeMissing ? missing : null,
            all.Count);
    }
}
