using System.Linq.Expressions;

namespace Blux.UI.Tools.Query;

/// <summary>A registered field of the row type.</summary>
internal sealed class DataListField<T>
{
    private Func<T, object?>? _getter;

    public DataListField(string key, ValueSource source, bool sortable, DataListFilter? filter, bool storesLocalTime = false)
    {
        StoresLocalTime = storesLocalTime;
        Key = key;
        Source = source;
        Sortable = sortable;
        Filter = filter;
    }

    public string Key { get; }

    public ValueSource Source { get; }

    /// <summary>The scalar field, or the value read from one element of a collection field.</summary>
    public FieldAccessor Accessor => Source.Value;

    public bool IsCollection => Source.IsCollection;

    public bool Sortable { get; }

    /// <summary>The <see cref="DateTime"/> values are wall-clock times in the request time zone, not UTC.</summary>
    public bool StoresLocalTime { get; }

    public DataListFilter? Filter { get; }

    /// <summary>The value of the field for a row, boxed and null-safe along the chain. Compiled once.</summary>
    public Func<T, object?> Getter => IsCollection
        ? throw new InvalidOperationException("A collection field has no single value; it cannot be sorted.")
        : _getter ??= Compile();

    private Func<T, IEnumerable<object?>>? _values;

    /// <summary>Every value of the field for a row: one for a scalar, one per element for a collection. Compiled once.</summary>
    public Func<T, IEnumerable<object?>> Values => _values ??= Source.CompileValues<T>();

    private Func<T, object?> Compile()
    {
        var p = Expression.Parameter(typeof(T), "e");
        var body = Expression.Convert(Accessor.Bind(p, nullSafe: true), typeof(object));
        return Expression.Lambda<Func<T, object?>>(body, p).Compile();
    }
}
