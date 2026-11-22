using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Blux.UI.Tools.Query;

/// <summary>
/// The two queries of a request, for the caller to run: <see cref="Matching"/> counts the rows (<c>CountAsync()</c>) and <see cref="Page"/> loads the page
/// (<c>ToListAsync()</c>). Neither runs anything: they are plain <see cref="IQueryable{T}"/> built from <see cref="Queryable"/> operators.
/// </summary>
/// <typeparam name="T">The row type.</typeparam>
public sealed class DataListQuery<T>
{
    internal DataListQuery(IQueryable<T> matching, IQueryable<T> page, IQueryable<DataListRow<T>> rows, bool wantsTotal)
    {
        Matching = matching;
        Page = page;
        Rows = rows;
        WantsTotal = wantsTotal;
    }

    /// <summary>The rows that match the filter and the search, unordered and unpaged: count them to get the total.</summary>
    public IQueryable<T> Matching { get; }

    /// <summary>
    /// The page: <see cref="Matching"/> sorted (nulls last in both directions, then by row key) and cut by the paging. With cursor paging it holds one
    /// row more than the page size, which <see cref="QueryableDataList"/>.ToResult uses to know that a next page exists and removes.
    /// </summary>
    public IQueryable<T> Page { get; }

    /// <summary>
    /// <see cref="Page"/> with each row's sort values and key read by the database next to it (<see cref="DataListRow{T}"/>). Load this one for cursor paging: the
    /// next cursor is made from these values, not from the loaded entity, so a sort on <c>e.Department.Name</c> needs no <c>Include</c> and a projected row type needs
    /// nothing special. For offset paging or no paging, <see cref="Page"/> is enough.
    /// </summary>
    public IQueryable<DataListRow<T>> Rows { get; }

    /// <summary>Whether the result carries a total: always for offset and no paging, for a cursor only when the request asked (<c>Count</c>). Count <see cref="Matching"/> only then.</summary>
    public bool WantsTotal { get; }

    /// <summary>Splits the query into its two parts: <c>var (matching, page) = schema.Apply(...)</c>.</summary>
    public void Deconstruct(out IQueryable<T> matching, out IQueryable<T> page)
    {
        matching = Matching;
        page = Page;
    }
}

/// <summary>A row of a page together with what the database computed for its cursor: the value of each sort field, in the order of the sort, and the row key.</summary>
/// <typeparam name="T">The row type.</typeparam>
public sealed class DataListRow<T>
{
    /// <summary>The row.</summary>
    public T Item { get; set; } = default!;

    /// <summary>The sort values, boxed, one per sort rule (a null is a null in the database).</summary>
    public object?[] SortValues { get; set; } = [];

    /// <summary>The row key, boxed.</summary>
    public object? Key { get; set; }
}

/// <summary>One option of a facet as read from the database: the value in its own type, and the number of rows that have it.</summary>
public sealed class DataListFacetBucket
{
    /// <summary>The value of the field, boxed, never null or an empty string.</summary>
    public object? Value { get; set; }

    /// <summary>The number of rows that have the value (a row counts once per distinct value, whatever the number of elements that carry it).</summary>
    public int Count { get; set; }
}

/// <summary>The queries that count one facet, for the caller to run, and the code that turns their rows into a <see cref="DataListFacetResult"/>.</summary>
/// <typeparam name="T">The row type.</typeparam>
public sealed class DataListFacetQuery<T>
{
    private readonly DataListFacetRequest _request;
    private readonly int _maxBuckets;

    internal DataListFacetQuery(DataListFacetRequest request, IQueryable<DataListFacetBucket> buckets, IQueryable<T>? missing, int maxBuckets)
    {
        _request = request;
        _maxBuckets = maxBuckets;
        Buckets = buckets;
        Missing = missing;
    }

    /// <summary>The key of the facet.</summary>
    public string Key => _request.Key;

    /// <summary>A group-by query: one row per distinct non-empty value and its count, the most counted first (ties by value), at most <see cref="DataListSchema.MaxFacetBuckets"/> plus one (the extra row tells <see cref="ToResult"/> that the facet is truncated). Load it with <c>ToListAsync()</c>; the search among the options and the window are applied by <see cref="ToResult"/>.</summary>
    public IQueryable<DataListFacetBucket> Buckets { get; }

    /// <summary>The rows where the field is empty, or <see langword="null"/> when the request did not ask for <see cref="DataListFacetRequest.IncludeMissing"/>: count them with <c>CountAsync()</c>.</summary>
    public IQueryable<T>? Missing { get; }

    /// <summary>
    /// The facet as the in-memory executor would answer it: options as text, searched, ordered by count then value (ordinal), then cut by <c>Skip</c> and
    /// <c>Limit</c>.
    /// </summary>
    /// <param name="buckets">The rows of <see cref="Buckets"/>.</param>
    /// <param name="missing">The count of <see cref="Missing"/>, when it was requested.</param>
    public DataListFacetResult ToResult(IEnumerable<DataListFacetBucket> buckets, int? missing = null)
    {
        ArgumentNullException.ThrowIfNull(buckets);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var bucket in buckets)
        {
            // two values that format the same (a database whose collation merges 'a' and 'A' returns one of them) are one option
            if (TextConvert.Format(bucket.Value) is { } text) counts[text] = counts.GetValueOrDefault(text) + bucket.Count;
        }
        return FacetResults.Create(_request, counts, missing ?? 0, _maxBuckets);
    }
}

/// <summary>
/// Applies a <see cref="DataListRequest"/> to an <see cref="IQueryable{T}"/> of any provider (EF Core, for one) without referencing it: the filter tree, the
/// search, the sort and the paging become <c>Queryable</c> operators the provider translates to SQL. The caller runs the queries (<c>CountAsync</c>,
/// <c>ToListAsync</c>), so the package needs no EF reference.
/// <para>
/// The expressions are the raw flavour of the filter definitions: values travel as parameters, never as literals (an <c>IN</c> for an option list, plain
/// <c>Contains</c> for text, no <c>ToLower</c>), nothing is client-evaluated, and a null along a path (<c>e.Department.Name</c>) is SQL null. The results are
/// those of <see cref="InMemoryDataList.ExecuteInMemory{T}"/>, with these differences: <b>text case and accents follow the database</b> (the collation of
/// the column, and how the provider translates <c>Contains</c>: SQLite's is case sensitive whatever the collation), and sorting text uses the database's
/// order instead of the natural order. The providers must be able to compare the types of the fields: SQLite needs a value converter for
/// <see cref="DateTimeOffset"/> (UTC ticks) and <see cref="decimal"/> (double); see the package README.
/// </para>
/// </summary>
public static class QueryableDataList
{
    /// <summary>
    /// Builds the queries of a request. The request is first passed through <see cref="DataListSchema.Sanitize(DataListRequest)"/>, and query-only filters
    /// are skipped (read them with <see cref="DataListSchema.QueryFilterRules"/> and apply them to <paramref name="source"/> before this call).
    /// Rows are ordered by the sort (the schema default when the request has none), nulls last in both directions, then by the row key when the schema has
    /// one, so that pages never repeat or skip a row. Cursor paging is a keyset <c>WHERE</c> over the sort values and the row key.
    /// </summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="schema">The schema of the rows.</param>
    /// <param name="source">The rows: a provider query, not an in-memory sequence.</param>
    /// <param name="request">What to compute.</param>
    /// <exception cref="InvalidOperationException"><paramref name="source"/> is an in-memory <c>AsQueryable()</c> (the check recognizes the LINQ to Objects provider only: a test double that wraps one, or any other provider that evaluates in memory, is not caught and must use <c>ExecuteInMemory</c> too; use <c>ExecuteInMemory</c>: it compares text ignoring case and guards nulls, which a provider expression does not); cursor paging on a schema without a row key, on a key or sort field a cursor cannot carry, or on a <see cref="Guid"/> (databases order them in their own way).</exception>
    public static DataListQuery<T> Apply<T>(this DataListSchema<T> schema, IQueryable<T> source, DataListRequest request)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);
        RejectInMemory(source);

        var clean = schema.Sanitize(request);
        var zone = TimeZones.Resolve(clean.TimeZone) ?? TimeZoneInfo.Utc;
        var matching = Filtered(schema, source, clean, excludeKey: null, zone);

        var sort = clean.Sort.Count > 0 ? clean.Sort : schema.DefaultSort;
        var sortFields = sort.Select(s => (Field: ResolveSort(schema, s.Key), Rule: s)).ToList();

        switch (clean.Paging)
        {
            case DataListPaging.Offset offset:
            {
                var page = Order(schema, matching, sortFields);
                page = Skip(page, offset.Skip);
                page = Take(page, offset.Take);
                return new DataListQuery<T>(matching, page, ToRows(schema, page, sortFields), wantsTotal: true);
            }
            case DataListPaging.Cursor cursor:
                return ApplyCursor(schema, matching, sortFields, cursor);
            default:
            {
                var all = Order(schema, matching, sortFields);
                return new DataListQuery<T>(matching, all, ToRows(schema, all, sortFields), wantsTotal: true);
            }
        }
    }

    /// <summary>
    /// The count queries of the requested facets (the sanitized ones: option and boolean fields), one per facet, for the caller to run. A facet counts the
    /// rows that match the search and the filters, leaving out the facet's own filter in the default <see cref="FacetMode.ExcludeSelf"/> mode.
    /// </summary>
    /// <exception cref="InvalidOperationException">A facet on a collection field in a schema without a row key (a row is counted once per value, which needs the key).</exception>
    public static IReadOnlyList<DataListFacetQuery<T>> FacetQueries<T>(this DataListSchema<T> schema, IQueryable<T> source, DataListRequest request)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);
        RejectInMemory(source);

        var clean = schema.Sanitize(request);
        var zone = TimeZones.Resolve(clean.TimeZone) ?? TimeZoneInfo.Utc;
        var queries = new List<DataListFacetQuery<T>>();
        foreach (var facet in clean.Facets)
        {
            schema.TryGetField(facet.Key, out var field);
            var rows = Filtered(schema, source, clean, facet.Mode == FacetMode.ExcludeSelf ? facet.Key : null, zone);
            IQueryable<T>? missing = null;
            if (facet.IncludeMissing)
            {
                var p = Expression.Parameter(typeof(T), "e");
                missing = rows.Where(Expression.Lambda<Func<T, bool>>(FilterExpressions.EmptyOf(field!.Source, p, inMemory: false), p));
            }
            queries.Add(new DataListFacetQuery<T>(facet, QueryableBuilder.Buckets(schema, field!, rows, facet.Values), missing, schema.MaxFacetBuckets));
        }
        return queries;
    }

    /// <summary>
    /// The <see cref="DataListResult{T}"/> of an offset or unpaged request from what the caller loaded: the rows of <see cref="DataListQuery{T}.Page"/>, the count of
    /// <see cref="DataListQuery{T}.Matching"/> and the facets (<see cref="DataListFacetQuery{T}.ToResult"/>).
    /// </summary>
    /// <exception cref="InvalidOperationException">The request pages by cursor: load <see cref="DataListQuery{T}.Rows"/> and use the overload that takes them, because the cursor needs the sort values the database computed.</exception>
    public static DataListResult<T> ToResult<T>(
        this DataListSchema<T> schema,
        DataListRequest request,
        IReadOnlyList<T> pageRows,
        int? totalCount = null,
        IReadOnlyList<DataListFacetResult>? facets = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(pageRows);

        var clean = schema.Sanitize(request);
        return clean.Paging switch
        {
            DataListPaging.Cursor => throw new InvalidOperationException(
                "A cursor is made from the sort values of the last row, which the database computes: load DataListQuery.Rows and pass those rows to ToResult."),
            DataListPaging.Offset when totalCount is null => throw MissingTotal(),
            DataListPaging.Offset => new DataListResult<T>(pageRows, totalCount, null, facets),
            _ => new DataListResult<T>(pageRows, totalCount ?? pageRows.Count, null, facets),
        };
    }

    /// <summary>
    /// The <see cref="DataListResult{T}"/> of any request from the rows of <see cref="DataListQuery{T}.Rows"/>. With cursor paging the extra row is removed and
    /// <see cref="DataListResult{T}.NextCursor"/> is made from the sort values and the key that the database returned for the last row of the page.
    /// </summary>
    /// <param name="schema">The schema of the rows.</param>
    /// <param name="request">The request that was applied.</param>
    /// <param name="rows">The rows loaded from <see cref="DataListQuery{T}.Rows"/>.</param>
    /// <param name="totalCount">The count of <see cref="DataListQuery{T}.Matching"/>, when <see cref="DataListQuery{T}.WantsTotal"/>.</param>
    /// <param name="facets">The facet results, or <see langword="null"/> when none were requested.</param>
    public static DataListResult<T> ToResult<T>(
        this DataListSchema<T> schema,
        DataListRequest request,
        IReadOnlyList<DataListRow<T>> rows,
        int? totalCount = null,
        IReadOnlyList<DataListFacetResult>? facets = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(rows);

        var clean = schema.Sanitize(request);
        if (clean.Paging is not DataListPaging.Cursor cursor)
        {
            var items = rows.Select(r => r.Item).ToList();
            if (clean.Paging is DataListPaging.Offset && totalCount is null) throw MissingTotal();
            return clean.Paging is DataListPaging.Offset
                ? new DataListResult<T>(items, totalCount, null, facets)
                : new DataListResult<T>(items, totalCount ?? items.Count, null, facets);
        }

        var sort = clean.Sort.Count > 0 ? clean.Sort : schema.DefaultSort;
        var total = cursor.Count ? totalCount : null;
        if (rows.Count <= cursor.Take) return new DataListResult<T>([.. rows.Select(r => r.Item)], total, null, facets);
        var page = rows.Take(cursor.Take).ToList();
        var last = page[^1];
        var next = DataListCursor.Encode(
            CursorPaging.Signature(sort),
            TextConvert.Format(last.Key) ?? "",
            [.. last.SortValues.Select(CursorValue.Format)]);
        return new DataListResult<T>([.. page.Select(r => r.Item)], total, next, facets);
    }

    // ---- internals

    private static InvalidOperationException MissingTotal() =>
        new("An offset result carries the total count: count DataListQuery.Matching (DataListQuery.WantsTotal is true) and pass it as totalCount.");

    private static void RejectInMemory<T>(IQueryable<T> source)
    {
        if (source.Provider is EnumerableQuery)
            throw new InvalidOperationException(
                "Apply and FacetQueries are for a database provider (EF Core). This is an in-memory IQueryable (AsQueryable()): its expressions would throw on a null along a path and compare text case-sensitively. Use schema.ExecuteInMemory(items, request) instead.");
    }

    private static IQueryable<T> Filtered<T>(DataListSchema<T> schema, IQueryable<T> source, DataListRequest clean, string? excludeKey, TimeZoneInfo zone)
    {
        var predicate = FilterExpressions.Rows(schema, clean.Filter, clean.Search, excludeKey, inMemory: false, zone);
        return predicate is null ? source : source.Where(predicate);
    }

    private static DataListField<T> ResolveSort<T>(DataListSchema<T> schema, string key) =>
        schema.TryGetField(key, out var field) ? field : throw new InvalidOperationException($"The sort key '{key}' is not a field of the schema.");

    private static IQueryable<T> Order<T>(DataListSchema<T> schema, IQueryable<T> rows, List<(DataListField<T> Field, DataListSort Rule)> sort)
    {
        var ordered = rows;
        var first = true;
        foreach (var (field, rule) in sort)
        {
            var p = Expression.Parameter(typeof(T), "e");
            var value = field.Accessor.Bind(p, nullSafe: false);
            // nulls last whatever the direction: a leading 0 or 1 sorts them after every value, then the direction applies to the values only
            if (NeedsNullFlag(field.Accessor))
            {
                var flag = Expression.Condition(
                    Expression.Equal(value, Expression.Constant(null, value.Type)), Expression.Constant(1), Expression.Constant(0));
                ordered = QueryableBuilder.AddOrder(ordered, QueryableBuilder.Lambda(flag, p), descending: false, first);
                first = false;
            }
            ordered = QueryableBuilder.AddOrder(ordered, QueryableBuilder.Lambda(value, p), rule.Descending, first);
            first = false;
        }
        if (schema.RowKeyAccessor is { } key)
        {
            var p = Expression.Parameter(typeof(T), "e");
            ordered = QueryableBuilder.AddOrder(ordered, QueryableBuilder.Lambda(key.Bind(p, nullSafe: false), p), descending: false, first);
        }
        return ordered;
    }

    private static bool NeedsNullFlag(FieldAccessor accessor) =>
        !accessor.Type.IsValueType || Nullable.GetUnderlyingType(accessor.Type) is not null || accessor.Chain is not { Length: 1 };

    /// <summary>The page with the sort values and the key computed by the database: <c>Select(e =&gt; new DataListRow { Item = e, SortValues = new object[] { (object)e.Sort1, ... }, Key = (object)e.Key })</c>.</summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "An array of object, a type every runtime has.")]
    private static IQueryable<DataListRow<T>> ToRows<T>(DataListSchema<T> schema, IQueryable<T> page, List<(DataListField<T> Field, DataListSort Rule)> sort)
    {
        var p = Expression.Parameter(typeof(T), "e");
        var values = sort.Select(s => (Expression)Expression.Convert(s.Field.Accessor.Bind(p, nullSafe: false), typeof(object)));
        var init = Expression.MemberInit(
            Expression.New(typeof(DataListRow<T>)),
            Expression.Bind(typeof(DataListRow<T>).GetProperty(nameof(DataListRow<T>.Item))!, p),
            Expression.Bind(typeof(DataListRow<T>).GetProperty(nameof(DataListRow<T>.SortValues))!, Expression.NewArrayInit(typeof(object), values)),
            Expression.Bind(
                typeof(DataListRow<T>).GetProperty(nameof(DataListRow<T>.Key))!,
                schema.RowKeyAccessor is { } key ? Expression.Convert(key.Bind(p, nullSafe: false), typeof(object)) : Expression.Constant(null, typeof(object))));
        return page.Select(Expression.Lambda<Func<T, DataListRow<T>>>(init, p));
    }

    private static IQueryable<T> Skip<T>(IQueryable<T> source, int count) =>
        count <= 0 ? source : QueryableBuilder.Window(source, QueryableBuilder.SkipMethod, count);

    private static IQueryable<T> Take<T>(IQueryable<T> source, int count) => QueryableBuilder.Window(source, QueryableBuilder.TakeMethod, count);

    private static DataListQuery<T> ApplyCursor<T>(
        DataListSchema<T> schema,
        IQueryable<T> matching,
        List<(DataListField<T> Field, DataListSort Rule)> sort,
        DataListPaging.Cursor cursor)
    {
        var keyAccessor = schema.RowKeyAccessor ?? throw new InvalidOperationException("Cursor paging needs a row key: declare RowKey(e => e.Id) in the schema.");
        CursorPaging.Validate(sort.Select(s => s.Field).ToList(), keyAccessor);

        var signature = CursorPaging.Signature(sort.Select(s => s.Rule).ToList());
        var rows = matching;
        if (!string.IsNullOrEmpty(cursor.After))
        {
            // A cursor that does not decode, or was made for another sort, ends the walk (an empty page), like the in-memory executor.
            if (!DataListCursor.TryDecode(cursor.After, signature, sort.Count, out var afterKeyText, out var afterValues)
                || !CursorPaging.TryParse(sort.Select(s => s.Field).ToList(), afterValues, out var parsed)
                || !TextConvert.TryParse(afterKeyText, keyAccessor.UnderlyingType, out var afterKey))
            {
                var none = Take(matching, 0);
                return new DataListQuery<T>(matching, none, ToRows(schema, none, sort), cursor.Count);
            }

            var p = Expression.Parameter(typeof(T), "e");
            rows = rows.Where(Expression.Lambda<Func<T, bool>>(QueryableBuilder.After(p, sort, parsed, keyAccessor, afterKey), p));
        }
        var page = Take(Order(schema, rows, sort), cursor.Take + 1);
        return new DataListQuery<T>(matching, page, ToRows(schema, page, sort), cursor.Count);
    }
}

/// <summary>What the in-memory executor and the applier share about cursors.</summary>
internal static class CursorPaging
{
    public static string Signature(IReadOnlyList<DataListSort> sort) =>
        string.Join('|', sort.Select(s => s.Descending ? "-" + s.Key : s.Key));

    public static void Validate<T>(List<DataListField<T>> sortFields, FieldAccessor key)
    {
        foreach (var field in sortFields)
        {
            var type = field.Accessor.UnderlyingType;
            if (!TextConvert.IsSupported(type))
                throw new InvalidOperationException($"The sort field '{field.Key}' ({type.Name}) cannot be carried by a cursor.");
            if (type == typeof(Guid))
                throw new InvalidOperationException($"The sort field '{field.Key}' is a Guid: databases order Guids in their own way, so a keyset cursor cannot be built on it. Sort by another field.");
        }
        if (!TextConvert.IsSupported(key.UnderlyingType))
            throw new InvalidOperationException($"The row key ({key.UnderlyingType.Name}) cannot be carried by a cursor.");
        if (key.UnderlyingType == typeof(Guid))
            throw new InvalidOperationException("The row key is a Guid: databases order Guids in their own way, so a keyset cursor cannot use it as the tie-break. Use an integer or string key for the schema.");
    }

    public static bool TryParse<T>(List<DataListField<T>> sortFields, string?[] texts, out object?[] values)
    {
        values = new object?[texts.Length];
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i] is null) continue;
            if (!CursorValue.TryParse(texts[i]!, sortFields[i].Accessor.UnderlyingType, out values[i])) return false;
        }
        return true;
    }
}

/// <summary>The generic plumbing of the applier: <c>Queryable</c> calls whose key types are only known at runtime (the type of a field), built as expression trees.</summary>
internal static class QueryableBuilder
{
    private static readonly MethodInfo OrderByDef = Def((Func<IQueryable<int>, Expression<Func<int, int>>, IOrderedQueryable<int>>)Queryable.OrderBy);
    private static readonly MethodInfo OrderByDescendingDef = Def((Func<IQueryable<int>, Expression<Func<int, int>>, IOrderedQueryable<int>>)Queryable.OrderByDescending);
    private static readonly MethodInfo ThenByDef = Def((Func<IOrderedQueryable<int>, Expression<Func<int, int>>, IOrderedQueryable<int>>)Queryable.ThenBy);
    private static readonly MethodInfo ThenByDescendingDef = Def((Func<IOrderedQueryable<int>, Expression<Func<int, int>>, IOrderedQueryable<int>>)Queryable.ThenByDescending);
    public static readonly MethodInfo SkipMethod = Def((Func<IQueryable<int>, int, IQueryable<int>>)Queryable.Skip);
    public static readonly MethodInfo TakeMethod = Def((Func<IQueryable<int>, int, IQueryable<int>>)Queryable.Take);
    private static readonly MethodInfo GroupByDef = Def((Func<IQueryable<int>, Expression<Func<int, int>>, IQueryable<IGrouping<int, int>>>)Queryable.GroupBy);
    private static readonly MethodInfo SelectDef = Def((Func<IQueryable<int>, Expression<Func<int, int>>, IQueryable<int>>)Queryable.Select);
    private static readonly MethodInfo SelectManyWithResultDef = Def((Func<IQueryable<int>, Expression<Func<int, IEnumerable<int>>>, Expression<Func<int, int, int>>, IQueryable<int>>)Queryable.SelectMany);
    private static readonly MethodInfo EnumerableSelectDef = Def((Func<IEnumerable<int>, Func<int, int>, IEnumerable<int>>)Enumerable.Select);
    private static readonly MethodInfo EnumerableCountDef = Def((Func<IEnumerable<int>, int>)Enumerable.Count);
    private static readonly MethodInfo EnumerableDistinctDef = Def((Func<IEnumerable<int>, IEnumerable<int>>)Enumerable.Distinct);
    private static readonly MethodInfo StringCompare = typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)])!;

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The delegate type is inferred from the field types of the application's own lambdas; see the README on runtimes.")]
    public static LambdaExpression Lambda(Expression body, params ParameterExpression[] parameters) => Expression.Lambda(body, parameters);

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The delegate type is built from the row and element types of the application's own lambdas; see the README on runtimes.")]
    private static LambdaExpression LambdaOf(Type delegateType, Expression body, params ParameterExpression[] parameters) => Expression.Lambda(delegateType, body, parameters);

    private static MethodInfo Def(Delegate method) => method.Method.GetGenericMethodDefinition();

    private const string Justification = "The key and element types are those of fields the application declared in its own lambdas, so the instantiations exist for its expressions already. Expression trees and IQueryable providers need a runtime that builds generic instantiations (CoreCLR, Mono); documented in the README.";

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = Justification)]
    [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = Justification)]
    private static IQueryable CallAny(IQueryable source, MethodInfo definition, Type[] typeArguments, params Expression[] arguments)
    {
        var call = Expression.Call(definition.MakeGenericMethod(typeArguments), [source.Expression, .. arguments]);
        return source.Provider.CreateQuery(call);
    }

    private static IQueryable<T> Call<T>(IQueryable<T> source, MethodInfo definition, Type[] typeArguments, params Expression[] arguments) =>
        (IQueryable<T>)CallAny(source, definition, typeArguments, arguments);

    public static IQueryable<T> AddOrder<T>(IQueryable<T> source, LambdaExpression key, bool descending, bool first)
    {
        var definition = first
            ? (descending ? OrderByDescendingDef : OrderByDef)
            : (descending ? ThenByDescendingDef : ThenByDef);
        return Call(source, definition, [typeof(T), key.ReturnType], Expression.Quote(key));
    }

    /// <summary>Skip or take with the count in a holder, so the provider sees a member access and emits a parameter (a literal would give each page its own query plan).</summary>
    public static IQueryable<T> Window<T>(IQueryable<T> source, MethodInfo definition, int count) =>
        Call(source, definition, [typeof(T)], Expression.Convert(Expression.Field(Expression.Constant(new ValueBox(count)), ValueBox.ValueField), typeof(int)));

    // ---- keyset

    /// <summary>
    /// The rows that sort strictly after the cursor: for some sort field i, all the fields before it equal the cursor's and field i comes after (nulls
    /// last, so a null is after every value and nothing is after a null), or all fields are equal and the row key is greater.
    /// </summary>
    public static Expression After<T>(
        ParameterExpression p,
        List<(DataListField<T> Field, DataListSort Rule)> sort,
        object?[] values,
        FieldAccessor key,
        object? keyValue)
    {
        Expression? result = null;
        Expression? equalSoFar = null;
        for (var i = 0; i < sort.Count; i++)
        {
            var accessor = sort[i].Field.Accessor;
            var value = accessor.Bind(p, nullSafe: false);
            var cursorValue = values[i];
            if (cursorValue is not null)
            {
                var after = Expression.OrElse(
                    Expression.Equal(value, Expression.Constant(null, value.Type)),
                    Relational(value, cursorValue, accessor, greater: !sort[i].Rule.Descending));
                result = FilterExpressions.Or(result, FilterExpressions.And(equalSoFar, after));
            }
            var equal = cursorValue is null
                ? Expression.Equal(value, Expression.Constant(null, value.Type))
                : Expression.Equal(value, FilterExpressions.Param(cursorValue, accessor.LiftedType));
            equalSoFar = FilterExpressions.And(equalSoFar, equal);
        }
        var keyExpression = key.Bind(p, nullSafe: false);
        var keyAfter = Relational(keyExpression, keyValue!, key, greater: true);
        return FilterExpressions.Or(result, FilterExpressions.And(equalSoFar, keyAfter))!;
    }

    /// <summary><c>value &gt; bound</c> or <c>value &lt; bound</c> for the type of the field; operators the type lacks (text, bool, enum) are spelled in a way providers translate.</summary>
    private static Expression Relational(Expression value, object bound, FieldAccessor accessor, bool greater)
    {
        var type = accessor.UnderlyingType;
        if (type == typeof(string))
        {
            var compare = Expression.Call(StringCompare, value, FilterExpressions.Param(bound, typeof(string)));
            return greater ? Expression.GreaterThan(compare, Expression.Constant(0)) : Expression.LessThan(compare, Expression.Constant(0));
        }
        if (type == typeof(bool))
        {
            var b = (bool)bound;
            // false < true: after false is true, after true is nothing; before true is false, before false is nothing
            if (greater) return b ? Expression.Constant(false) : Expression.Equal(value, FilterExpressions.Param(true, typeof(bool?)));
            return b ? Expression.Equal(value, FilterExpressions.Param(false, typeof(bool?))) : Expression.Constant(false);
        }
        Expression left = value;
        Expression right;
        if (type.IsEnum)
        {
            var underlying = Enum.GetUnderlyingType(type);
            var lifted = FieldAccessor.Lift(underlying);
            left = Expression.Convert(value, lifted);
            right = FilterExpressions.Param(Convert.ChangeType(bound, underlying, System.Globalization.CultureInfo.InvariantCulture), lifted);
        }
        else
        {
            right = FilterExpressions.Param(bound, accessor.LiftedType);
        }
        return greater ? Expression.GreaterThan(left, right) : Expression.LessThan(left, right);
    }

    // ---- facets

    /// <summary>The group-by of a facet: every distinct non-empty value of the field of <paramref name="rows"/> and the number of rows that have it.</summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = Justification)]
    [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = Justification)]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = Justification)]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = Justification)]
    public static IQueryable<DataListFacetBucket> Buckets<T>(DataListSchema<T> schema, DataListField<T> field, IQueryable<T> rows, IReadOnlyList<string>? values = null)
    {
        var accessor = field.Accessor;
        var p = Expression.Parameter(typeof(T), "e");
        var bucketType = typeof(DataListFacetBucket);
        // a facet scoped to values counts all of them, whatever the cap of an open facet
        var cap = values is { Count: > 0 } ? Math.Max(schema.MaxFacetBuckets, values.Count) : schema.MaxFacetBuckets;

        if (!field.IsCollection)
        {
            var value = accessor.Bind(p, nullSafe: false);
            var keyType = value.Type;
            Expression keep = Expression.Not(FilterExpressions.IsEmpty(value, accessor));
            // Only some values: in the query, so the cap of buckets applies to them alone.
            if (values is { Count: > 0 }) keep = Expression.AndAlso(keep, FilterExpressions.ValuesRule(value, accessor, values, inMemory: false));
            var filtered = rows.Where(Expression.Lambda<Func<T, bool>>(keep, p));
            var grouped = CallAny(filtered, GroupByDef, [typeof(T), keyType], Expression.Quote(QueryableBuilder.Lambda(value, p)));
            return Project(grouped, typeof(IGrouping<,>).MakeGenericType(keyType, typeof(T)), typeof(T), countOf: null, cap);
        }

        // A collection: one (row key, value) pair per element, then the distinct rows per value. A row is counted once per value. The result-selector form
        // of SelectMany is what providers translate to a join; a correlated Select inside the collection selector needs APPLY, which SQLite lacks.
        var rowKey = schema.RowKeyAccessor
            ?? throw new InvalidOperationException($"The facet '{field.Key}' is on a collection: a row is counted once per value, which needs a row key. Declare RowKey(e => e.Id) in the schema.");
        var source = field.Source;
        var keyValue = rowKey.Bind(p, nullSafe: false);
        var pairType = typeof(FacetPair<,>).MakeGenericType(keyValue.Type, accessor.LiftedType);
        var x = Expression.Parameter(source.ElementType!, "x");
        var elementValue = accessor.Bind(x, nullSafe: false);
        var pair = Expression.MemberInit(
            Expression.New(pairType),
            Expression.Bind(pairType.GetProperty(nameof(FacetPair<int, int>.Row))!, keyValue),
            Expression.Bind(pairType.GetProperty(nameof(FacetPair<int, int>.Value))!, elementValue));
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(source.ElementType!);
        var collection = source.Collection!.Bind(p, nullSafe: false);
        // typed as Func<T, IEnumerable<TElem>> with the property as the body (a list is reference-assignable), without a Convert node a provider cannot translate
        var collectionSelector = LambdaOf(typeof(Func<,>).MakeGenericType(typeof(T), enumerableType), collection, p);
        var pairs = CallAny(rows, SelectManyWithResultDef, [typeof(T), source.ElementType!, pairType], Expression.Quote(collectionSelector), Expression.Quote(Lambda(pair, p, x)));

        var q = Expression.Parameter(pairType, "q");
        var pairValue = Member(q, nameof(FacetPair<int, int>.Value));
        Expression keepPair = Expression.Not(FilterExpressions.IsEmpty(pairValue, accessor));
        if (values is { Count: > 0 }) keepPair = Expression.AndAlso(keepPair, FilterExpressions.ValuesRule(pairValue, accessor, values, inMemory: false));
        var nonEmpty = QueryableBuilder.Lambda(keepPair, q);
        var whereMethod = ((Func<IQueryable<int>, Expression<Func<int, bool>>, IQueryable<int>>)Queryable.Where).Method.GetGenericMethodDefinition();
        var nonEmptyPairs = CallAny(pairs, whereMethod, [pairType], Expression.Quote(nonEmpty));
        var groupedPairs = CallAny(nonEmptyPairs, GroupByDef, [pairType, accessor.LiftedType], Expression.Quote(QueryableBuilder.Lambda(pairValue, q)));
        return Project(groupedPairs, typeof(IGrouping<,>).MakeGenericType(accessor.LiftedType, pairType), pairType, countOf: nameof(FacetPair<int, int>.Row), cap);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = Justification)]
    private static MemberExpression Member(Expression instance, string name) =>
        Expression.MakeMemberAccess(instance, instance.Type.GetProperty(name)!);

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = Justification)]
    [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = Justification)]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = Justification)]
    private static IQueryable<DataListFacetBucket> Project(IQueryable grouped, Type groupingType, Type elementType, string? countOf, int maxBuckets)
    {
        var g = Expression.Parameter(groupingType, "g");
        Expression count;
        if (countOf is null)
        {
            count = Expression.Call(EnumerableCountDef.MakeGenericMethod(elementType), g);
        }
        else
        {
            // COUNT(DISTINCT row key): a row with the same value on two elements counts once
            var e = Expression.Parameter(elementType, "r");
            var rowOf = Expression.Call(
                EnumerableSelectDef.MakeGenericMethod(elementType, elementType.GetProperty(countOf)!.PropertyType),
                g,
                QueryableBuilder.Lambda(Member(e, countOf), e));
            var distinct = Expression.Call(EnumerableDistinctDef.MakeGenericMethod(elementType.GetProperty(countOf)!.PropertyType), rowOf);
            count = Expression.Call(EnumerableCountDef.MakeGenericMethod(elementType.GetProperty(countOf)!.PropertyType), distinct);
        }
        // the most counted values first, ties by value, and one more than the cap: a result longer than the cap says that the facet is truncated
        var keyMember = Member(g, "Key");
        grouped = CallAny(grouped, OrderByDescendingDef, [groupingType, typeof(int)], Expression.Quote(QueryableBuilder.Lambda(count, g)));
        grouped = CallAny(grouped, ThenByDef, [groupingType, keyMember.Type], Expression.Quote(QueryableBuilder.Lambda(keyMember, g)));
        grouped = CallAny(grouped, TakeMethod, [groupingType], Expression.Convert(Expression.Field(Expression.Constant(new ValueBox(maxBuckets + 1)), ValueBox.ValueField), typeof(int)));
        var bucket = Expression.MemberInit(
            Expression.New(typeof(DataListFacetBucket)),
            Expression.Bind(typeof(DataListFacetBucket).GetProperty(nameof(DataListFacetBucket.Value))!, Expression.Convert(Member(g, "Key"), typeof(object))),
            Expression.Bind(typeof(DataListFacetBucket).GetProperty(nameof(DataListFacetBucket.Count))!, count));
        var select = SelectDef.MakeGenericMethod(groupingType, typeof(DataListFacetBucket));
        var call = Expression.Call(select, grouped.Expression, Expression.Quote(QueryableBuilder.Lambda(bucket, g)));
        return grouped.Provider.CreateQuery<DataListFacetBucket>(call);
    }
}

/// <summary>A (row key, value) pair read from one element of a collection field, so a facet can count distinct rows per value.</summary>
internal sealed class FacetPair<TRow, TValue>
{
    public TRow? Row { get; set; }

    public TValue? Value { get; set; }
}
