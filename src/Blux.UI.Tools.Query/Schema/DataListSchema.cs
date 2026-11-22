using System.Diagnostics.CodeAnalysis;

namespace Blux.UI.Tools.Query;

/// <summary>
/// Describes what a data list can do with a row type: its fields, which are sortable or filterable, the search fields, the query-only filters,
/// the defaults and the limits. Built in C# with expressions (no reflection over string paths, so it is trim-safe) through
/// <see cref="For{T}"/>. The schema decides what a request may ask: <see cref="Sanitize(DataListRequest)"/> drops anything it does not know.
/// </summary>
public abstract class DataListSchema
{
    private readonly IReadOnlyDictionary<string, DataListFilter> _filters;
    private readonly HashSet<string> _queryOnly;
    private readonly HashSet<string> _facetKeys;
    private readonly HashSet<string> _sortKeys;
    private readonly bool _hasSearch;

    private protected DataListSchema(
        IReadOnlyDictionary<string, DataListFilter> filters,
        IEnumerable<string> queryOnlyKeys,
        IEnumerable<string> sortKeys,
        IEnumerable<string> facetKeys,
        bool hasSearch,
        IReadOnlyList<DataListSort> defaultSort,
        int defaultPageSize,
        int maxPageSize,
        DataListLayout defaultLayout)
    {
        _filters = filters;
        _queryOnly = [.. queryOnlyKeys];
        _sortKeys = [.. sortKeys];
        _facetKeys = [.. facetKeys];
        _hasSearch = hasSearch;
        DefaultSort = defaultSort;
        MaxPageSize = maxPageSize;
        DefaultPageSize = Math.Min(defaultPageSize, maxPageSize);
        DefaultLayout = defaultLayout;
        FilterKeys = [.. filters.Keys];
        SortKeys = [.. sortKeys];
        var rules = filters
            .Select(f => f.Value.DefaultRule(f.Key))
            .Where(r => r is not null)
            .Select(r => r!)
            .ToArray();
        DefaultState = new DataListState("", rules, defaultSort, 1, DefaultPageSize, defaultLayout);
    }

    /// <summary>Starts the schema of a row type. See <see cref="DataListSchemaBuilder{T}"/>.</summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="configure">Describes the schema.</param>
    public static DataListSchema<T> For<T>(Action<DataListSchemaBuilder<T>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new DataListSchemaBuilder<T>();
        configure(builder);
        return builder.Build();
    }

    /// <summary>The keys of every filter (fields and query-only filters), in declaration order.</summary>
    public IReadOnlyList<string> FilterKeys { get; }

    /// <summary>The keys of the sortable fields.</summary>
    public IReadOnlyList<string> SortKeys { get; }

    /// <summary>The sort applied when a request has none; omitted from the URL.</summary>
    public IReadOnlyList<DataListSort> DefaultSort { get; }

    /// <summary>The page size when none is given (50 unless set, never above <see cref="MaxPageSize"/>).</summary>
    public int DefaultPageSize { get; }

    /// <summary>The largest page a request may ask for (500 unless set).</summary>
    public int MaxPageSize { get; }

    /// <summary>The layout when none is given.</summary>
    public DataListLayout DefaultLayout { get; }

    /// <summary>The state with every default applied: the default filter values, sort, page 1, <see cref="DefaultPageSize"/> and <see cref="DefaultLayout"/>. The URL representation omits whatever equals it.</summary>
    public DataListState DefaultState { get; }

    /// <summary>The most filter rules <see cref="Sanitize(DataListRequest)"/> keeps (100 unless set); later ones are dropped. Bounds the work a hostile request can ask for.</summary>
    public int MaxFilterRules { get; internal set; } = 100;

    /// <summary>The most values <see cref="Sanitize(DataListRequest)"/> keeps in the list of one rule (500 unless set); later ones are dropped.</summary>
    public int MaxListValues { get; internal set; } = 500;

    /// <summary>
    /// The most distinct options a facet keeps (1000 unless set): the ones with the highest counts, ties by value. A facet over more values is cut and marked
    /// <see cref="DataListFacetResult.Truncated"/>; the search among the options and the window (<c>Skip</c>, <c>Limit</c>) apply to the kept ones. In a database
    /// the cap is a <c>TAKE</c> on the group-by, so a column with millions of values never transfers them all.
    /// </summary>
    public int MaxFacetBuckets { get; internal set; } = 1000;

    /// <summary>The deepest nesting of filter groups kept by <see cref="Sanitize(DataListRequest)"/>; deeper groups are dropped.</summary>
    public const int MaxFilterDepth = 16;

    /// <summary>Whether the schema has search fields.</summary>
    public bool HasSearch => _hasSearch;

    /// <summary>Gets the definition of a filter by key.</summary>
    public bool TryGetFilter(string key, [NotNullWhen(true)] out DataListFilter? filter) => _filters.TryGetValue(key, out filter);

    /// <summary>Whether <paramref name="key"/> is a query-only filter: part of the request for the provider, never matched against rows by the in-memory executor.</summary>
    public bool IsQueryFilter(string key) => _queryOnly.Contains(key);

    /// <summary>The query-only rules of a request, for the provider to interpret. Looks through nested groups.</summary>
    public IReadOnlyList<FilterRule> QueryFilterRules(DataListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var found = new List<FilterRule>();
        Collect(request.Filter);
        return found;

        void Collect(DataListFilterGroup group)
        {
            foreach (var child in group.Children)
            {
                if (child is FilterRule rule && IsQueryFilter(rule.Key) && _filters[rule.Key].IsValid(rule)) found.Add(rule);
                else if (child is DataListFilterGroup nested) Collect(nested);
            }
        }
    }

    /// <summary>
    /// Returns a request a provider can trust: filter rules with an unknown key, a disallowed operator or a value of the wrong shape are dropped (empty
    /// nested groups with them), sort keys that are unknown, not sortable or repeated are dropped, facets that are not option or boolean field
    /// filters are dropped, the search is dropped when the schema has no search field, a page size is clamped to 1..<see cref="MaxPageSize"/> (zero or
    /// less means <see cref="DefaultPageSize"/>), a negative skip becomes 0 and a facet limit is clamped to 1..1000. Hostile input is bounded: rules without
    /// a key and null nodes are dropped, a list keeps at most <see cref="MaxListValues"/> distinct non-null values, a request keeps at most
    /// <see cref="MaxFilterRules"/> rules and groups nest at most <see cref="MaxFilterDepth"/> deep. A time zone id the server does not know is cleared (UTC).
    /// </summary>
    public DataListRequest Sanitize(DataListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        DataListPaging paging = request.Paging switch
        {
            DataListPaging.Offset o => new DataListPaging.Offset(Math.Max(0, o.Skip), ClampTake(o.Take)),
            DataListPaging.Cursor c => c with { Take = ClampTake(c.Take) },
            var other => other,
        };

        var sort = new List<DataListSort>();
        foreach (var rule in request.Sort)
        {
            if (rule is null || !_sortKeys.Contains(rule.Key) || sort.Any(s => s.Key == rule.Key)) continue;
            sort.Add(rule);
        }

        var facets = new List<DataListFacetRequest>();
        foreach (var facet in request.Facets)
        {
            if (facet is null || !_facetKeys.Contains(facet.Key) || facets.Any(f => f.Key == facet.Key)) continue;
            facets.Add(facet with
            {
                Limit = facet.Limit is { } l ? Math.Clamp(l, 1, MaxFacetLimit) : null,
                Skip = Math.Max(0, facet.Skip),
                Values = facet.Values is null ? null : [.. facet.Values.Where(v => !string.IsNullOrEmpty(v)).Distinct(StringComparer.Ordinal).Take(MaxListValues)],
            });
        }

        var search = _hasSearch && request.Search is { Term: { } term } && !string.IsNullOrWhiteSpace(term) ? request.Search : null;
        var budget = MaxFilterRules;
        var zone = TimeZones.Resolve(request.TimeZone) is null ? null : request.TimeZone;
        return new DataListRequest(paging, sort, SanitizeGroup(request.Filter, ref budget, 0), search, facets, zone);
    }

    internal const int MaxFacetLimit = 1000;

    internal bool IsFacetKey(string key) => _facetKeys.Contains(key);

    internal bool IsSortKey(string key) => _sortKeys.Contains(key);

    private int ClampTake(int take) => take <= 0 ? DefaultPageSize : Math.Min(take, MaxPageSize);

    private DataListFilterGroup SanitizeGroup(DataListFilterGroup? group, ref int budget, int depth)
    {
        var children = new List<DataListFilterNode>();
        if (group is null) return new DataListFilterGroup(FilterCombinator.And, children);
        foreach (var child in group.Children ?? [])
        {
            switch (child)
            {
                case FilterRule { Key: { Length: > 0 } key } rule when budget > 0 && _filters.TryGetValue(key, out var filter):
                {
                    var clean = Normalize(rule);
                    if (!filter.IsValid(clean)) break;
                    budget--;
                    children.Add(clean);
                    break;
                }
                case DataListFilterGroup nested when depth < MaxFilterDepth:
                    var cleanGroup = SanitizeGroup(nested, ref budget, depth + 1);
                    if (cleanGroup.Children.Count > 0) children.Add(cleanGroup);
                    break;
            }
        }
        return new DataListFilterGroup(group.Combinator, children);
    }

    private FilterRule Normalize(FilterRule rule)
    {
        if (rule.Operator is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty)
            return rule.IncludeEmpty ? rule with { IncludeEmpty = false } : rule;
        if (rule.Value is FilterValue.List { Values: { } values })
        {
            var clean = values.Where(v => v is not null).Distinct().Take(MaxListValues).ToList();
            if (clean.Count != values.Count) return rule with { Value = new FilterValue.List(clean) };
        }
        return rule;
    }
}

/// <summary>The schema of a row type <typeparamref name="T"/>: a <see cref="DataListSchema"/> that can also read a row's key and label and run requests in memory.</summary>
/// <typeparam name="T">The row type.</typeparam>
public sealed class DataListSchema<T> : DataListSchema
{
    private readonly IReadOnlyDictionary<string, DataListField<T>> _fields;
    private readonly Func<T, string>? _rowKey;
    private readonly Func<T, string?>? _rowLabel;

    internal DataListSchema(
        IReadOnlyList<DataListField<T>> fields,
        IReadOnlyDictionary<string, DataListFilter> filters,
        IEnumerable<string> queryOnlyKeys,
        IReadOnlyList<ValueSource> search,
        Func<T, string>? rowKey,
        FieldAccessor? rowKeyAccessor,
        Func<T, string?>? rowLabel,
        IReadOnlyList<DataListSort> defaultSort,
        int defaultPageSize,
        int maxPageSize,
        DataListLayout defaultLayout)
        : base(
            filters,
            queryOnlyKeys,
            fields.Where(f => f.Sortable).Select(f => f.Key),
            fields.Where(f => f.Filter is { Kind: FilterKind.Option or FilterKind.Boolean }).Select(f => f.Key),
            search.Count > 0,
            defaultSort,
            defaultPageSize,
            maxPageSize,
            defaultLayout)
    {
        Fields = fields;
        _fields = fields.ToDictionary(f => f.Key, StringComparer.Ordinal);
        SearchSources = search;
        _rowKey = rowKey;
        RowKeyAccessor = rowKeyAccessor;
        _rowLabel = rowLabel;
    }

    /// <summary>The row key as an expression (for the IQueryable applier and the typed tie-break), or <see langword="null"/> without a row key.</summary>
    internal FieldAccessor? RowKeyAccessor { get; }

    private Func<T, object?>? _rowKeyValue;

    /// <summary>The row key as a boxed value of its own type (not text), compiled once.</summary>
    internal Func<T, object?> RowKeyValue => _rowKeyValue ??= CompileRowKey();

    private Func<T, object?> CompileRowKey()
    {
        var accessor = RowKeyAccessor ?? throw new InvalidOperationException("The schema has no RowKey; declare one with RowKey(e => e.Id).");
        var p = System.Linq.Expressions.Expression.Parameter(typeof(T), "e");
        var body = System.Linq.Expressions.Expression.Convert(accessor.Bind(p, nullSafe: true), typeof(object));
        return System.Linq.Expressions.Expression.Lambda<Func<T, object?>>(body, p).Compile();
    }

    /// <summary>
    /// Orders two rows by their key, by the type of the key (so 2 comes before 10), strings ordinal: the tie-break of every sort, in memory and in the
    /// IQueryable applier. A key is never null in practice; a null one sorts last.
    /// </summary>
    internal int CompareRowKeys(T a, T b) => RowKeyComparer.Instance.Compare(RowKeyValue(a), RowKeyValue(b));

    internal IReadOnlyList<DataListField<T>> Fields { get; }

    internal IReadOnlyList<ValueSource> SearchSources { get; }

    private readonly System.Collections.Concurrent.ConcurrentDictionary<(DataListFilterGroup Filter, string? Term, string? Exclude, string Zone), Func<T, bool>?> _predicates = new();

    /// <summary>
    /// The compiled row predicate for a sanitized filter, search term and excluded key (<see langword="null"/> when nothing restricts the rows). Compiling an
    /// expression costs about a millisecond, and a list asks for the same predicate again for every page and facet. At most 64 are kept: when the cache is full it is emptied (no LRU), which is enough because a list reuses its few current predicates. Filter values compare by value, so two requests share an entry only when they would build the same expression.
    /// </summary>
    internal Func<T, bool>? GetPredicate(DataListFilterGroup filter, DataListSearch? search, string? excludeKey, TimeZoneInfo? zone = null)
    {
        var term = string.IsNullOrWhiteSpace(search?.Term) ? null : search!.Term.Trim();
        zone ??= TimeZoneInfo.Utc;
        var key = (filter, term, excludeKey, zone.Id);
        if (_predicates.TryGetValue(key, out var cached)) return cached;
        var predicate = FilterExpressions.Rows(this, filter, term is null ? null : new DataListSearch(term), excludeKey, inMemory: true, zone)?.Compile();
        if (_predicates.Count >= 64) _predicates.Clear();
        _predicates[key] = predicate;
        return predicate;
    }

    internal bool TryGetField(string key, [NotNullWhen(true)] out DataListField<T>? field) => _fields.TryGetValue(key, out field);

    /// <summary>Whether the schema has a <c>RowKey</c>. Cursor paging needs one.</summary>
    public bool HasRowKey => _rowKey is not null;

    /// <summary>The stable key of a row, as text (invariant culture).</summary>
    /// <exception cref="InvalidOperationException">The schema has no row key.</exception>
    public string GetRowKey(T item) =>
        _rowKey?.Invoke(item) ?? throw new InvalidOperationException("The schema has no RowKey; declare one with RowKey(e => e.Id).");

    /// <summary>The label that names a row for accessibility ("Select Alice"), or <see langword="null"/>.</summary>
    public string? GetRowLabel(T item) => _rowLabel?.Invoke(item);
}
