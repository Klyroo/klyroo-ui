using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Blux.UI.Tools.Query;

/// <summary>
/// Where a filter, search or facet reads its values: one scalar value per row, or the elements of a collection of the row (<c>e.Tags</c> then
/// <c>t =&gt; t.Name</c>). Collection sources have "at least one" semantics and are built as <c>Enumerable.Any(collection, x =&gt; predicate)</c>, the
/// shape EF Core translates for a collection navigation. The <c>Any</c> method is bound where the element type is a compile-time type parameter, so no
/// reflection over runtime types is needed.
/// </summary>
internal sealed class ValueSource
{
    private readonly Func<Expression, Expression, ParameterExpression, Expression>? _any;

    private ValueSource(FieldAccessor value, FieldAccessor? collection, Type? elementType, Func<Expression, Expression, ParameterExpression, Expression>? any)
    {
        Value = value;
        Collection = collection;
        ElementType = elementType;
        _any = any;
    }

    /// <summary>The scalar field, or the value read from one element (its lambda parameter is the element).</summary>
    public FieldAccessor Value { get; }

    /// <summary>The collection of the row, or <see langword="null"/> for a scalar source.</summary>
    public FieldAccessor? Collection { get; }

    public Type? ElementType { get; }

    public bool IsCollection => Collection is not null;

    public string? DefaultKey => Collection is null
        ? Value.DefaultKey
        : Collection.DefaultKey is { } c && Value.DefaultKey is { } v ? c + "." + v : null;

    public string? Path => Collection is null
        ? Value.Path
        : Collection.Path is { } c && Value.Path is { } v ? c + "." + v : null;

    public static ValueSource Scalar(FieldAccessor value) => new(value, null, null, null);

    public static ValueSource OfCollection<TElem>(FieldAccessor collection, FieldAccessor element) =>
        new(element, collection, typeof(TElem), AnyCall<TElem>);

    private static Expression AnyCall<TElem>(Expression collection, Expression body, ParameterExpression element) =>
        Expression.Call(AnyMethod<TElem>.Info, collection, Expression.Lambda<Func<TElem, bool>>(body, element));

    private static class AnyMethod<TElem>
    {
        public static readonly MethodInfo Info = ((Func<IEnumerable<TElem>, Func<TElem, bool>, bool>)Enumerable.Any).Method;
    }

    /// <summary>
    /// The condition "some value satisfies <paramref name="perValue"/>": for a scalar source the condition on the value, for a collection
    /// <c>collection != null &amp;&amp; collection.Any(x =&gt; condition(x.value))</c> (without the null check when <paramref name="nullSafe"/> is false).
    /// </summary>
    public Expression Exists(Expression row, bool nullSafe, Func<Expression, Expression> perValue)
    {
        if (Collection is null) return perValue(Value.Bind(row, nullSafe));
        var x = Expression.Parameter(ElementType!, "x");
        var any = _any!(Collection.Bind(row, nullSafe), perValue(Value.Bind(x, nullSafe)), x);
        if (!nullSafe) return any;
        var collection = Collection.Bind(row, nullSafe);
        return Expression.AndAlso(Expression.NotEqual(collection, Expression.Constant(null, collection.Type)), any);
    }

    /// <summary>The values of a row, boxed, null-safe: one for a scalar source, one per element for a collection (none for a null collection).</summary>
    public Func<TRow, IEnumerable<object?>> CompileValues<TRow>()
    {
        var p = Expression.Parameter(typeof(TRow), "e");
        if (Collection is null)
        {
            var single = Expression.Lambda<Func<TRow, object?>>(Expression.Convert(Value.Bind(p, true), typeof(object)), p).Compile();
            return row => [single(row)];
        }
        var bound = Collection.Bind(p, true);
        var sequence = Expression.Lambda<Func<TRow, IEnumerable?>>(Expression.Convert(bound, typeof(IEnumerable)), p).Compile();
        var o = Expression.Parameter(typeof(object), "o");
        var element = Expression.Lambda<Func<object?, object?>>(
            Expression.Convert(Value.Bind(Expression.Convert(o, ElementType!), true), typeof(object)), o).Compile();
        return row => Enumerate(sequence(row), element);
    }

    private static IEnumerable<object?> Enumerate(IEnumerable? sequence, Func<object?, object?> element)
    {
        if (sequence is null) yield break;
        foreach (var item in sequence) yield return element(item);
    }
}
