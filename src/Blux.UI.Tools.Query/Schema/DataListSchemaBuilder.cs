using System.Linq.Expressions;

namespace Blux.UI.Tools.Query;

/// <summary>
/// The fluent description of a <see cref="DataListSchema{T}"/>, passed to <see cref="DataListSchema.For{T}"/>:
/// <code>
/// DataListSchema.For&lt;Employee&gt;(s =&gt; s
///     .RowKey(e =&gt; e.Id).RowLabel(e =&gt; e.Name)
///     .Search(e =&gt; e.Name, e =&gt; e.Email)
///     .Field(e =&gt; e.Age, f =&gt; f.Sortable().Filter(DataListFilter.Number(min: 0, step: 1)))
///     .Field(e =&gt; e.Department.Name, f =&gt; f.Filter(DataListFilter.Option()))
///     .QueryFilter("archived", DataListFilter.Boolean(false))
///     .DefaultSort(e =&gt; e.Name)
///     .MaxPageSize(500));
/// </code>
/// </summary>
/// <typeparam name="T">The row type.</typeparam>
public sealed class DataListSchemaBuilder<T>
{
    private readonly List<FieldDefinition> _fields = [];
    private readonly List<(string Key, DataListFilter Filter)> _queryFilters = [];
    private readonly List<ValueSource> _search = [];
    private Func<T, string>? _rowKey;
    private FieldAccessor? _rowKeyAccessor;
    private Func<T, string?>? _rowLabel;
    private string? _defaultSortKey;
    private bool _defaultSortDescending;
    private FieldAccessor? _defaultSortAccessor;
    private int _maxPageSize = 500;
    private int _maxFilterRules = 100;
    private int _maxListValues = 500;
    private int _maxFacetBuckets = 1000;
    private int _defaultPageSize = 50;
    private DataListLayout _layout = DataListLayout.Table;

    internal DataListSchemaBuilder() { }

    /// <summary>The stable identity of a row, used for selection and as the cursor tie-break. Converted to text in the invariant culture.</summary>
    public DataListSchemaBuilder<T> RowKey<TKey>(Expression<Func<T, TKey>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var read = selector.Compile();
        _rowKey = item => TextConvert.Format(read(item)) ?? "";
        _rowKeyAccessor = FieldAccessor.Create(selector);
        return this;
    }

    /// <summary>The label that names a row for assistive technology ("Select Alice").</summary>
    public DataListSchemaBuilder<T> RowLabel(Func<T, string?> label)
    {
        ArgumentNullException.ThrowIfNull(label);
        _rowLabel = label;
        return this;
    }

    /// <summary>The string fields the search box looks in (case-insensitive substring, any field matches). The expressions are compiled for in-memory evaluation and passed as written to a database provider.</summary>
    public DataListSchemaBuilder<T> Search(params Expression<Func<T, string?>>[] fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        foreach (var field in fields) _search.Add(ValueSource.Scalar(FieldAccessor.Create(field)));
        return this;
    }

    /// <summary>Registers a field. Its key is the member path in camelCase (<c>department.name</c>) unless <see cref="DataListFieldBuilder{T, TProp}.Key"/> says otherwise.</summary>
    public DataListSchemaBuilder<T> Field<TProp>(Expression<Func<T, TProp>> selector, Action<DataListFieldBuilder<T, TProp>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var accessor = FieldAccessor.Create(selector);
        var definition = new FieldDefinition(ValueSource.Scalar(accessor));
        configure?.Invoke(new DataListFieldBuilder<T, TProp>(definition));
        _fields.Add(definition);
        return this;
    }

    /// <summary>
    /// Registers a field that reads one value from each element of a collection of the row:
    /// <c>.Collection(e =&gt; e.Tags, t =&gt; t.Name, f =&gt; f.Filter(DataListFilter.Option()))</c>. The key joins both paths in camelCase
    /// (<c>tags.name</c>). A row matches a rule when at least one element does; <c>isNot</c> is the negation of <c>is</c>/<c>isAnyOf</c> (no element is any
    /// of the values, so a row without elements matches); <c>isEmpty</c> means no non-empty element (an empty or null collection included). The predicate
    /// becomes <c>collection.Any(x =&gt; ...)</c>. Collection fields cannot be sorted.
    /// </summary>
    /// <typeparam name="TElem">The element type.</typeparam>
    /// <typeparam name="TProp">The type of the value read from an element.</typeparam>
    /// <param name="collection">The collection of the row (a property of a type that is an <see cref="IEnumerable{T}"/> of <typeparamref name="TElem"/>).</param>
    /// <param name="element">The value read from an element.</param>
    /// <param name="configure">The key and the filter.</param>
    public DataListSchemaBuilder<T> Collection<TElem, TProp>(
        Expression<Func<T, IEnumerable<TElem>?>> collection,
        Expression<Func<TElem, TProp>> element,
        Action<DataListCollectionFieldBuilder<T, TProp>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(element);
        var source = ValueSource.OfCollection<TElem>(FieldAccessor.Create(collection), FieldAccessor.Create(element, guardRoot: true));
        var definition = new FieldDefinition(source);
        configure?.Invoke(new DataListCollectionFieldBuilder<T, TProp>(definition));
        _fields.Add(definition);
        return this;
    }

    /// <summary>The search also looks at a string of each element of a collection (any element matches).</summary>
    public DataListSchemaBuilder<T> SearchAny<TElem>(Expression<Func<T, IEnumerable<TElem>?>> collection, Expression<Func<TElem, string?>> element)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(element);
        _search.Add(ValueSource.OfCollection<TElem>(FieldAccessor.Create(collection), FieldAccessor.Create(element, guardRoot: true)));
        return this;
    }

    /// <summary>A filter that is not a field of the row: its rules reach the provider (<see cref="DataListSchema.QueryFilterRules"/>) but the in-memory executor never matches rows against them. Use it for server-side switches such as <c>archived</c>.</summary>
    public DataListSchemaBuilder<T> QueryFilter(string key, DataListFilter filter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(filter);
        _queryFilters.Add((key, filter));
        return this;
    }

    /// <summary>The sort used when a request has none. The field is made sortable (and registered when missing).</summary>
    public DataListSchemaBuilder<T> DefaultSort<TProp>(Expression<Func<T, TProp>> selector, bool descending = false)
    {
        ArgumentNullException.ThrowIfNull(selector);
        _defaultSortAccessor = FieldAccessor.Create(selector);
        _defaultSortDescending = descending;
        _defaultSortKey = null;
        return this;
    }

    /// <summary>The sort used when a request has none, by the key of a sortable field.</summary>
    public DataListSchemaBuilder<T> DefaultSort(string key, bool descending = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _defaultSortKey = key;
        _defaultSortDescending = descending;
        _defaultSortAccessor = null;
        return this;
    }

    /// <summary>The largest page a request may ask for (500 by default).</summary>
    public DataListSchemaBuilder<T> MaxPageSize(int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        _maxPageSize = size;
        return this;
    }

    /// <summary>The most filter rules a sanitized request keeps (100 by default).</summary>
    public DataListSchemaBuilder<T> MaxFilterRules(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        _maxFilterRules = count;
        return this;
    }

    /// <summary>The most values a sanitized request keeps in the list of one rule (500 by default).</summary>
    public DataListSchemaBuilder<T> MaxListValues(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        _maxListValues = count;
        return this;
    }

    /// <summary>The most distinct options a facet keeps (1000 by default); more are cut and the facet is marked truncated. See <see cref="DataListSchema.MaxFacetBuckets"/>.</summary>
    public DataListSchemaBuilder<T> MaxFacetBuckets(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        _maxFacetBuckets = count;
        return this;
    }

    /// <summary>The page size when none is given (50 by default, never above the maximum).</summary>
    public DataListSchemaBuilder<T> DefaultPageSize(int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        _defaultPageSize = size;
        return this;
    }

    /// <summary>The layout when none is given (table by default).</summary>
    public DataListSchemaBuilder<T> DefaultLayout(DataListLayout layout)
    {
        _layout = layout;
        return this;
    }

    internal DataListSchema<T> Build()
    {
        var defaultSort = new List<DataListSort>();
        if (_defaultSortAccessor is { } accessor)
        {
            var existing = accessor.Path is null ? null : _fields.FirstOrDefault(f => !f.Source.IsCollection && f.Source.Path == accessor.Path);
            if (existing is null)
            {
                existing = new FieldDefinition(ValueSource.Scalar(accessor));
                _fields.Add(existing);
            }
            existing.IsSortable = true;
            defaultSort.Add(new DataListSort(existing.ResolveKey(), _defaultSortDescending));
        }

        var fields = new List<DataListField<T>>();
        var filters = new Dictionary<string, DataListFilter>(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var def in _fields)
        {
            var key = def.ResolveKey();
            ValidateKey(key);
            if (!used.Add(key)) throw new InvalidOperationException($"The key '{key}' is used by two fields. Give one a distinct key with f.Key(\"...\").");
            if (def.Filter is { } filter)
            {
                CheckKind(key, filter.Kind, def.Source.Value.UnderlyingType);
                filters[key] = filter;
            }
            if (def.StoresLocalTime && def.Source.Value.UnderlyingType != typeof(DateTime))
                throw new InvalidOperationException($"The field '{key}' is a {def.Source.Value.UnderlyingType.Name}: StoresLocalTime is for DateTime fields.");
            fields.Add(new DataListField<T>(key, def.Source, def.IsSortable, def.Filter, def.StoresLocalTime));
        }

        foreach (var (key, filter) in _queryFilters)
        {
            ValidateKey(key);
            if (!used.Add(key)) throw new InvalidOperationException($"The key '{key}' is used by a field and a query filter, or by two query filters.");
            filters[key] = filter;
        }

        if (_defaultSortKey is { } sortKey)
        {
            if (fields.Any(f => f.Key == sortKey && f.IsCollection))
                throw new InvalidOperationException($"DefaultSort(\"{sortKey}\"): a collection field cannot be sorted.");
            if (!fields.Any(f => f.Key == sortKey && f.Sortable))
                throw new InvalidOperationException($"DefaultSort(\"{sortKey}\"): no sortable field has this key.");
            defaultSort.Add(new DataListSort(sortKey, _defaultSortDescending));
        }

        var schema = new DataListSchema<T>(
            fields,
            filters,
            _queryFilters.Select(q => q.Key),
            _search,
            _rowKey,
            _rowKeyAccessor,
            _rowLabel,
            defaultSort,
            _defaultPageSize,
            _maxPageSize,
            _layout);
        schema.MaxFilterRules = _maxFilterRules;
        schema.MaxListValues = _maxListValues;
        schema.MaxFacetBuckets = _maxFacetBuckets;
        return schema;
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Contains('|') || key.Contains('=') || key.Contains('&') || key.StartsWith('-') || key.Any(char.IsWhiteSpace))
            throw new InvalidOperationException($"'{key}' cannot be a key: it must be non-empty, without spaces, '|', '=' or '&', and not start with '-'.");
    }

    private static void CheckKind(string key, FilterKind kind, Type type)
    {
        var ok = kind switch
        {
            FilterKind.Text => type == typeof(string),
            FilterKind.Boolean => type == typeof(bool),
            FilterKind.Number => TextConvert.IsNumeric(type),
            FilterKind.Date => TextConvert.IsDate(type),
            _ => TextConvert.IsSupported(type),
        };
        if (!ok) throw new InvalidOperationException($"The field '{key}' is a {type.Name}, which a {kind} filter cannot use.");
    }

    /// <summary>A field being declared, shared between the builders.</summary>
    internal sealed class FieldDefinition(ValueSource source)
    {
        public ValueSource Source { get; } = source;
        public string? ExplicitKey { get; set; }
        public bool IsSortable { get; set; }
        public bool StoresLocalTime { get; set; }
        public DataListFilter? Filter { get; set; }

        public string ResolveKey() =>
            ExplicitKey ?? Source.DefaultKey
            ?? throw new InvalidOperationException("The field is not a plain member access (e => e.A.B); give it a key with f.Key(\"...\").");
    }
}

/// <summary>The options of one field, passed to <see cref="DataListSchemaBuilder{T}.Field{TProp}"/>.</summary>
/// <typeparam name="T">The row type.</typeparam>
/// <typeparam name="TProp">The type of the field.</typeparam>
public sealed class DataListFieldBuilder<T, TProp>
{
    private readonly DataListSchemaBuilder<T>.FieldDefinition _definition;

    internal DataListFieldBuilder(DataListSchemaBuilder<T>.FieldDefinition definition) => _definition = definition;

    /// <summary>Overrides the stable key. Needed for fields that are not a plain member path.</summary>
    public DataListFieldBuilder<T, TProp> Key(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _definition.ExplicitKey = key;
        return this;
    }

    /// <summary>Lets requests sort by this field.</summary>
    public DataListFieldBuilder<T, TProp> Sortable(bool sortable = true)
    {
        _definition.IsSortable = sortable;
        return this;
    }

    /// <summary>
    /// The <see cref="DateTime"/> values of this field are wall-clock times in the time zone of the request (a column such as PostgreSQL <c>timestamp without time zone</c>,
    /// or a SQL Server <c>datetime2</c> that holds local times), not UTC. The bounds of a date filter are then local wall-clock times with kind <see cref="DateTimeKind.Unspecified"/>
    /// (a day is the local midnight to the next local midnight, an hour is the local hour) instead of UTC instants, so a provider that refuses a UTC <see cref="DateTime"/> for such a
    /// column accepts them. Without this option a <see cref="DateTime"/> field is read as UTC. Only for <see cref="DateTime"/> fields.
    /// </summary>
    public DataListFieldBuilder<T, TProp> StoresLocalTime(bool localTime = true)
    {
        _definition.StoresLocalTime = localTime;
        return this;
    }

    /// <summary>Gives the field a filter. The filter kind must suit the type of the field (text on a string, number on a numeric type, and so on).</summary>
    public DataListFieldBuilder<T, TProp> Filter(DataListFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        _definition.Filter = filter;
        return this;
    }
}

/// <summary>The options of a collection field, passed to <see cref="DataListSchemaBuilder{T}.Collection{TElem, TProp}"/>. There is no <c>Sortable</c>: a collection has no single value to sort by.</summary>
/// <typeparam name="T">The row type.</typeparam>
/// <typeparam name="TProp">The type of the value read from an element.</typeparam>
public sealed class DataListCollectionFieldBuilder<T, TProp>
{
    private readonly DataListSchemaBuilder<T>.FieldDefinition _definition;

    internal DataListCollectionFieldBuilder(DataListSchemaBuilder<T>.FieldDefinition definition) => _definition = definition;

    /// <summary>Overrides the stable key (<c>tags.name</c> by default).</summary>
    public DataListCollectionFieldBuilder<T, TProp> Key(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _definition.ExplicitKey = key;
        return this;
    }

    /// <summary>The <see cref="DateTime"/> values read from the elements are wall-clock times in the request's time zone, not UTC. See <see cref="DataListFieldBuilder{T, TProp}.StoresLocalTime"/>.</summary>
    public DataListCollectionFieldBuilder<T, TProp> StoresLocalTime(bool localTime = true)
    {
        _definition.StoresLocalTime = localTime;
        return this;
    }

    /// <summary>Gives the field a filter; the kind must suit the element value type.</summary>
    public DataListCollectionFieldBuilder<T, TProp> Filter(DataListFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        _definition.Filter = filter;
        return this;
    }
}
