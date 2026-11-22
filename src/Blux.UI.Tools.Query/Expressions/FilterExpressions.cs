using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Blux.UI.Tools.Query;

/// <summary>A value carried in a closure-like holder, so that a database provider sees a member access (and emits a parameter) rather than a literal.</summary>
internal sealed class ValueBox(object? value)
{
    public readonly object? Value = value;

    public static readonly FieldInfo ValueField = typeof(ValueBox).GetField(nameof(Value))!;
}

/// <summary>Case and accent insensitive substring search, in the invariant culture, for in-memory evaluation.</summary>
internal static class TextFold
{
    public const CompareOptions Options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    public static bool Contains(string haystack, string needle) =>
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, Options) >= 0;
}

/// <summary>
/// Builds <c>Expression&lt;Func&lt;T, bool&gt;&gt;</c> predicates from a filter tree and a search term. Two flavours of the same definition:
/// <list type="bullet">
/// <item><description><b>In memory</b> (<c>inMemory: true</c>): null guards along every path, and text <c>contains</c> and search that ignore case and accents
/// (<c>CompareInfo.IndexOf</c> with <c>IgnoreCase | IgnoreNonSpace</c>, invariant culture).</description></item>
/// <item><description><b>Raw</b> (<c>inMemory: false</c>), for the IQueryable applier of a later step: the expression as written, <c>string.Contains</c> without any
/// <c>ToLower</c> (the database collation decides about case and accents), no null guards, values read through <see cref="ValueBox"/> members so that
/// providers emit parameters, and <c>array.Contains(x)</c> for lists (an <c>IN</c> clause). Used by the IQueryable applier (<see cref="QueryableDataList"/>); proven against
/// EF Core and SQLite in <c>Blux.UI.Tools.Query.EfTests</c>, with no EF dependency in this package.</description></item>
/// </list>
/// Dates are half-open ranges <c>[start, next)</c> computed in the request's time zone.
/// </summary>
internal static class FilterExpressions
{
    private static readonly MethodInfo StringContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
    private static readonly MethodInfo IndexOf = typeof(CompareInfo).GetMethod(nameof(CompareInfo.IndexOf), [typeof(string), typeof(string), typeof(CompareOptions)])!;
    private static readonly MethodInfo ContainsDefinition = ((Func<IEnumerable<int>, int, bool>)Enumerable.Contains).Method.GetGenericMethodDefinition();

    /// <summary>
    /// The predicate for the search and the filter tree, or <see langword="null"/> when nothing restricts the rows. Query-only rules, unknown keys,
    /// invalid rules and the rules of <paramref name="excludeKey"/> are skipped.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(DateOnly))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(TimeOnly))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(DateTime))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(DateTimeOffset))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(TimeSpan))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(decimal))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(Guid))]
    public static Expression<Func<T, bool>>? Rows<T>(
        DataListSchema<T> schema, DataListFilterGroup filter, DataListSearch? search, string? excludeKey, bool inMemory, TimeZoneInfo? zone = null)
    {
        var p = Expression.Parameter(typeof(T), "e");
        var body = And(Search(schema, search, p, inMemory), Group(schema, filter, p, excludeKey, inMemory, zone ?? TimeZoneInfo.Utc));
        return body is null ? null : Expression.Lambda<Func<T, bool>>(body, p);
    }

    private static Expression? Search<T>(DataListSchema<T> schema, DataListSearch? search, ParameterExpression p, bool inMemory)
    {
        var term = search?.Term?.Trim();
        if (string.IsNullOrEmpty(term)) return null;
        Expression? any = null;
        foreach (var source in schema.SearchSources)
            any = Or(any, source.Exists(p, inMemory, v => ContainsText(v, term, inMemory)));
        return any;
    }

    private static Expression? Group<T>(DataListSchema<T> schema, DataListFilterGroup group, ParameterExpression p, string? excludeKey, bool inMemory, TimeZoneInfo zone)
    {
        Expression? result = null;
        foreach (var child in group.Children)
        {
            var node = child switch
            {
                FilterRule rule => Rule(schema, rule, p, excludeKey, inMemory, zone),
                DataListFilterGroup nested => Group(schema, nested, p, excludeKey, inMemory, zone),
                _ => null,
            };
            if (node is null) continue;
            result = group.Combinator == FilterCombinator.Or ? Or(result, node) : And(result, node);
        }
        return result;
    }

    private static Expression? Rule<T>(DataListSchema<T> schema, FilterRule rule, ParameterExpression p, string? excludeKey, bool inMemory, TimeZoneInfo zone)
    {
        if (rule.Key == excludeKey || schema.IsQueryFilter(rule.Key)) return null;
        if (!schema.TryGetField(rule.Key, out var field) || field.Filter is not { } filter || !filter.IsValid(rule)) return null;

        var source = field.Source;
        var accessor = source.Value;
        var op = rule.Operator;

        Expression Empty() => EmptyOf(source, p, inMemory);

        if (op == FilterOperator.IsEmpty) return Empty();
        if (op == FilterOperator.IsNotEmpty) return Expression.Not(Empty());

        // isNot is the negation of is/isAnyOf, so on a collection it means "no element is any of the values".
        var negate = op == FilterOperator.IsNot;
        var positive = negate ? FilterOperator.Is : op;
        Expression PerValue(Expression v) => filter.Kind switch
        {
            FilterKind.Text => TextRule(v, positive, (FilterValue.Text)rule.Value!, inMemory),
            FilterKind.Option => OptionRule(v, accessor, rule.Value!, inMemory),
            FilterKind.Boolean => BoolRule(v, (FilterValue.Boolean)rule.Value!),
            FilterKind.Number => NumberRule(v, accessor, positive, rule.Value!),
            _ => DateRule(v, accessor, positive, rule.Value!, filter.Granularity, zone, field.StoresLocalTime),
        };

        var body = source.Exists(p, inMemory, PerValue);
        if (negate) body = Expression.Not(body);
        return rule.IncludeEmpty ? Expression.OrElse(body, Empty()) : body;
    }

    /// <summary>"The field is empty" is "no value is non-empty": a null or "" scalar, or a collection without any non-empty element (or a null collection).</summary>
    internal static Expression EmptyOf(ValueSource source, ParameterExpression row, bool inMemory) => source.IsCollection
        ? Expression.Not(source.Exists(row, inMemory, v => Expression.Not(IsEmpty(v, source.Value))))
        : IsEmpty(source.Value.Bind(row, inMemory), source.Value);

    // ---- values as parameters

    internal static Expression Param(object? value, Type type)
    {
        Expression field = Expression.Field(Expression.Constant(new ValueBox(value)), ValueBox.ValueField);
        return type == typeof(object) ? field : Expression.Convert(field, type);
    }

    // ---- text

    private static Expression ContainsText(Expression v, string needle, bool inMemory)
    {
        var notNull = Expression.NotEqual(v, Expression.Constant(null, typeof(string)));
        Expression contains = inMemory
            ? Expression.GreaterThanOrEqual(
                Expression.Call(
                    Expression.Constant(CultureInfo.InvariantCulture.CompareInfo), IndexOf, v, Param(needle, typeof(string)), Expression.Constant(TextFold.Options)),
                Expression.Constant(0))
            : Expression.Call(v, StringContains, Param(needle, typeof(string)));
        return Expression.AndAlso(notNull, contains);
    }

    private static Expression TextRule(Expression v, FilterOperator op, FilterValue.Text value, bool inMemory) => op == FilterOperator.Contains
        ? ContainsText(v, value.Value, inMemory)
        : Expression.Equal(v, Param(value.Value, typeof(string)));

    // ---- option

    private static Expression OptionRule(Expression v, FieldAccessor accessor, FilterValue value, bool inMemory)
    {
        var candidates = new List<object?>();
        void Add(string text)
        {
            if (TextConvert.TryParse(text, accessor.UnderlyingType, out var converted) && !candidates.Contains(converted)) candidates.Add(converted);
        }
        switch (value)
        {
            case FilterValue.Text t: Add(t.Value); break;
            case FilterValue.List l: foreach (var text in l.Values) Add(text); break;
            case FilterValue.Boolean b: Add(b.Value ? "true" : "false"); break;
            case FilterValue.Number n:
                // exact conversion for numeric fields; a normalized text ("1.0" and "1" are the same number) for the others
                if (TextConvert.IsNumeric(accessor.UnderlyingType))
                {
                    if (TextConvert.TryConvertExact(n.Value, accessor.UnderlyingType, out var exact) && !candidates.Contains(exact)) candidates.Add(exact);
                }
                else
                {
                    Add(Normalize(n.Value).ToString(CultureInfo.InvariantCulture));
                }
                break;
        }
        return candidates.Count switch
        {
            0 => Expression.Constant(false),
            1 => Expression.Equal(v, Param(candidates[0], accessor.LiftedType)),
            // In memory the list is a chain of equalities: no array of a type built at runtime, which NativeAOT cannot do for a type the app never names.
            // A provider gets array.Contains(x), which it translates to IN.
            _ when inMemory => candidates.Select(c => (Expression)Expression.Equal(v, Param(c, accessor.LiftedType))).Aggregate(Expression.OrElse),
            _ => InList(v, accessor.LiftedType, candidates),
        };
    }

    /// <summary>"The value is one of these texts" for a facet restricted to some values: the same expression as an option rule with a list, so it translates the same way.</summary>
    internal static Expression ValuesRule(Expression v, FieldAccessor accessor, IReadOnlyList<string> values, bool inMemory) =>
        OptionRule(v, accessor, new FilterValue.List(values), inMemory);

    /// <summary>The decimal without trailing zeros, so that equal numbers format the same.</summary>
    internal static decimal Normalize(decimal value) => value / 1.000000000000000000000000000000000m;

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The array and generic Enumerable.Contains instantiation use the value type of a field the application declared in its own lambda; that instantiation is what the application's expressions already need. Documented in the README: expression-based schemas need a runtime that can build generic instantiations (CoreCLR, Mono JIT or interpreter), not NativeAOT-only.")]
    [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "Same instantiation over a type of the application's own lambda, kept by that lambda.")]
    private static Expression InList(Expression v, Type liftedType, List<object?> values)
    {
        var array = Array.CreateInstance(liftedType, values.Count);
        for (var i = 0; i < values.Count; i++) array.SetValue(values[i], i);
        var contains = ContainsDefinition.MakeGenericMethod(liftedType);
        return Expression.Call(contains, Param(array, liftedType.MakeArrayType()), v);
    }

    // ---- boolean

    private static Expression BoolRule(Expression v, FilterValue.Boolean value) =>
        Expression.Equal(v, Param(value.Value, typeof(bool?)));

    // ---- number

    private static Expression NumberRule(Expression v, FieldAccessor accessor, FilterOperator op, FilterValue value)
    {
        if (op != FilterOperator.Between) return NumberCompare(v, accessor, op, ((FilterValue.Number)value).Value);
        var range = (FilterValue.Range)value;
        Expression? both = null;
        if (range.From is FilterValue.Number from) both = And(both, NumberCompare(v, accessor, FilterOperator.Gte, from.Value));
        if (range.To is FilterValue.Number to) both = And(both, NumberCompare(v, accessor, FilterOperator.Lte, to.Value));
        return both ?? Expression.Constant(true);
    }

    private static Expression NumberCompare(Expression v, FieldAccessor accessor, FilterOperator op, decimal number)
    {
        Expression left, right;
        if (TextConvert.TryConvertExact(number, accessor.UnderlyingType, out var converted))
        {
            left = v;
            right = Param(converted, accessor.LiftedType);
        }
        else
        {
            // A fraction against an integer field: compare as decimal.
            left = Expression.Convert(v, typeof(decimal?));
            right = Param(number, typeof(decimal?));
        }
        return op switch
        {
            FilterOperator.Gt => Expression.GreaterThan(left, right),
            FilterOperator.Gte => Expression.GreaterThanOrEqual(left, right),
            FilterOperator.Lt => Expression.LessThan(left, right),
            FilterOperator.Lte => Expression.LessThanOrEqual(left, right),
            _ => Expression.Equal(left, right),
        };
    }

    // ---- date

    private static Expression DateRule(Expression v, FieldAccessor accessor, FilterOperator op, FilterValue value, DateGranularity granularity, TimeZoneInfo zone, bool localTime)
    {
        switch (op)
        {
            case FilterOperator.Between:
            {
                var range = (FilterValue.Range)value;
                Expression? both = null;
                if (range.From is { } from) both = And(both, Expression.GreaterThanOrEqual(v, Start(accessor, from, granularity, zone, localTime)));
                if (range.To is { } to && Next(accessor, to, granularity, zone, localTime) is { } next) both = And(both, Expression.LessThan(v, next));
                return both ?? Expression.Constant(true);
            }
            case FilterOperator.Before:
                return Expression.LessThan(v, Start(accessor, value, granularity, zone, localTime));
            case FilterOperator.After:
                return Next(accessor, value, granularity, zone, localTime) is { } after ? Expression.GreaterThanOrEqual(v, after) : Expression.Constant(false);
            default:
            {
                var atLeast = Expression.GreaterThanOrEqual(v, Start(accessor, value, granularity, zone, localTime));
                return Next(accessor, value, granularity, zone, localTime) is { } next ? Expression.AndAlso(atLeast, Expression.LessThan(v, next)) : atLeast;
            }
        }
    }

    private static Expression Start(FieldAccessor accessor, FilterValue value, DateGranularity granularity, TimeZoneInfo zone, bool localTime) =>
        DateBound(accessor, DateUnit.Of(value, granularity, zone), upper: false, localTime)!;

    /// <summary>The exclusive end of the unit, or <see langword="null"/> when the unit reaches the end of time (nothing is after it).</summary>
    private static Expression? Next(FieldAccessor accessor, FilterValue value, DateGranularity granularity, TimeZoneInfo zone, bool localTime) =>
        DateBound(accessor, DateUnit.Of(value, granularity, zone), upper: true, localTime);

    private static Expression? DateBound(FieldAccessor accessor, DateUnit unit, bool upper, bool localTime)
    {
        var type = accessor.UnderlyingType;
        object? bound;
        if (type == typeof(DateOnly))
        {
            bound = upper ? (object?)unit.NextDay : unit.StartDay;
            if (bound is null) return null;
        }
        else
        {
            var unitBound = upper ? unit.NextUtc : unit.StartUtc;
            if (type == typeof(DateTime) && localTime)
            {
                // wall-clock storage: the bound is the local time of the unit (the date and time part of the offset-carrying bound), kind unspecified
                bound = DateTime.SpecifyKind(unitBound.DateTime, DateTimeKind.Unspecified);
            }
            else
            {
                var instant = unitBound.ToUniversalTime();
                // DateTime fields are read as UTC (the Kind is ignored by comparisons, and databases store no Kind).
                bound = type == typeof(DateTime) ? (object)instant.UtcDateTime : instant;
            }
        }
        return Param(bound, accessor.LiftedType);
    }

    // ---- shared

    internal static Expression IsEmpty(Expression v, FieldAccessor accessor) =>
        accessor.UnderlyingType == typeof(string)
            ? Expression.OrElse(
                Expression.Equal(v, Expression.Constant(null, typeof(string))),
                Expression.Equal(v, Expression.Constant("", typeof(string))))
            : Expression.Equal(v, Expression.Constant(null, v.Type));

    internal static Expression? And(Expression? a, Expression? b) =>
        a is null ? b : b is null ? a : Expression.AndAlso(a, b);

    internal static Expression? Or(Expression? a, Expression? b) =>
        a is null ? b : b is null ? a : Expression.OrElse(a, b);
}
