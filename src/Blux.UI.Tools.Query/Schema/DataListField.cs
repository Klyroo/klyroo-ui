using System.Linq.Expressions;

namespace Blux.UI.Tools.Query;

/// <summary>A registered field of the row type.</summary>
internal sealed class DataListField<T>
{
    private Func<T, object?>? _getter;

    public DataListField(string key, FieldAccessor accessor, bool sortable, DataListFilter? filter)
    {
        Key = key;
        Accessor = accessor;
        Sortable = sortable;
        Filter = filter;
    }

    public string Key { get; }

    public FieldAccessor Accessor { get; }

    public bool Sortable { get; }

    public DataListFilter? Filter { get; }

    /// <summary>The value of the field for a row, boxed and null-safe along the chain. Compiled once.</summary>
    public Func<T, object?> Getter => _getter ??= Compile();

    private Func<T, object?> Compile()
    {
        var p = Expression.Parameter(typeof(T), "e");
        var body = Expression.Convert(Accessor.Bind(p, nullSafe: true), typeof(object));
        return Expression.Lambda<Func<T, object?>>(body, p).Compile();
    }
}
