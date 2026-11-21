using System.Globalization;
using System.Linq.Expressions;

namespace Blux.UI.Tools.Query;

/// <summary>
/// Builds <c>Expression&lt;Func&lt;T, bool&gt;&gt;</c> predicates from a filter tree and a search term. The in-memory executor compiles them with
/// <c>nullSafe: true</c>; the IQueryable applier (a later step) will pass the same expression to <c>Where</c> with <c>nullSafe: false</c>, so both
/// paths share one definition of every operator. Text is compared with <c>ToLowerInvariant().Contains(..)</c> and <c>==</c> so that database
/// providers can translate it; in memory this is ordinal and culture independent.
/// </summary>
internal static class FilterExpressions
{
    private static readonly System.Reflection.MethodInfo ToLowerInvariant = typeof(string).GetMethod(nameof(string.ToLowerInvariant), Type.EmptyTypes)!;
    private static readonly System.Reflection.MethodInfo StringContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    /// <summary>
    /// The predicate for the search and the filter tree, or <see langword="null"/> when nothing restricts the rows. Query-only rules, unknown keys,
    /// invalid rules and the rules of <paramref name="excludeKey"/> are skipped.
    /// </summary>
    public static Expression<Func<T, bool>>? Rows<T>(DataListSchema<T> schema, DataListFilterGroup filter, DataListSearch? search, string? excludeKey, bool nullSafe)
    {
        var p = Expression.Parameter(typeof(T), "e");
        var body = And(Search(schema, search, p, nullSafe), Group(schema, filter, p, excludeKey, nullSafe));
        return body is null ? null : Expression.Lambda<Func<T, bool>>(body, p);
    }

    private static Expression? Search<T>(DataListSchema<T> schema, DataListSearch? search, ParameterExpression p, bool nullSafe)
    {
        var term = search?.Term?.Trim();
        if (string.IsNullOrEmpty(term)) return null;
        Expression? any = null;
        foreach (var source in schema.SearchSources)
            any = Or(any, source.Exists(p, nullSafe, v => ContainsIgnoreCase(v, term)));
        return any;
    }

    private static Expression? Group<T>(DataListSchema<T> schema, DataListFilterGroup group, ParameterExpression p, string? excludeKey, bool nullSafe)
    {
        Expression? result = null;
        foreach (var child in group.Children)
        {
            var node = child switch
            {
                FilterRule rule => Rule(schema, rule, p, excludeKey, nullSafe),
                DataListFilterGroup nested => Group(schema, nested, p, excludeKey, nullSafe),
                _ => null,
            };
            if (node is null) continue;
            result = group.Combinator == FilterCombinator.Or ? Or(result, node) : And(result, node);
        }
        return result;
    }

    private static Expression? Rule<T>(DataListSchema<T> schema, FilterRule rule, ParameterExpression p, string? excludeKey, bool nullSafe)
    {
        if (rule.Key == excludeKey || schema.IsQueryFilter(rule.Key)) return null;
        if (!schema.TryGetField(rule.Key, out var field) || field.Filter is not { } filter || !filter.IsValid(rule)) return null;

        var source = field.Source;
        var accessor = source.Value;
        var op = rule.Operator;

        // "The field is empty" is "no value is non-empty": a null or "" scalar, or a collection without any non-empty element (or a null collection).
        Expression Empty() => source.IsCollection
            ? Expression.Not(source.Exists(p, nullSafe, v => Expression.Not(IsEmpty(v, accessor))))
            : IsEmpty(accessor.Bind(p, nullSafe), accessor);

        if (op == FilterOperator.IsEmpty) return Empty();
        if (op == FilterOperator.IsNotEmpty) return Expression.Not(Empty());

        // isNot is the negation of is/isAnyOf, so on a collection it means "no element is any of the values".
        var negate = op == FilterOperator.IsNot;
        var positive = negate ? FilterOperator.Is : op;
        Expression PerValue(Expression v) => filter.Kind switch
        {
            FilterKind.Text => TextRule(v, positive, (FilterValue.Text)rule.Value!),
            FilterKind.Option => OptionRule(v, accessor, positive, rule.Value!),
            FilterKind.Boolean => BoolRule(v, positive, (FilterValue.Bool)rule.Value!),
            FilterKind.Number => NumberRule(v, accessor, positive, rule.Value!),
            _ => DateRule(v, accessor, positive, rule.Value!),
        };

        var body = source.Exists(p, nullSafe, PerValue);
        if (negate) body = Expression.Not(body);
        return rule.IncludeEmpty ? Expression.OrElse(body, Empty()) : body;
    }

    private static Expression TextRule(Expression v, FilterOperator op, FilterValue.Text value) => op switch
    {
        FilterOperator.Contains => ContainsIgnoreCase(v, value.Value),
        FilterOperator.Is => Expression.Equal(v, Expression.Constant(value.Value, typeof(string))),
        _ => Expression.Not(Expression.Equal(v, Expression.Constant(value.Value, typeof(string)))),
    };

    private static Expression OptionRule(Expression v, FieldAccessor accessor, FilterOperator op, FilterValue value)
    {
        IEnumerable<string> texts = value switch
        {
            FilterValue.Text t => [t.Value],
            FilterValue.List l => l.Values,
            FilterValue.Number n => [n.Value.ToString(CultureInfo.InvariantCulture)],
            FilterValue.Bool b => [b.Value ? "true" : "false"],
            _ => [],
        };
        Expression? any = null;
        foreach (var text in texts)
        {
            if (!TextConvert.TryParse(text, accessor.UnderlyingType, out var converted)) continue;
            any = Or(any, Expression.Equal(v, Expression.Constant(converted, accessor.LiftedType)));
        }
        any ??= Expression.Constant(false);
        return op == FilterOperator.IsNot ? Expression.Not(any) : any;
    }

    private static Expression BoolRule(Expression v, FilterOperator op, FilterValue.Bool value)
    {
        var eq = Expression.Equal(v, Expression.Constant(value.Value, typeof(bool?)));
        return op == FilterOperator.IsNot ? Expression.Not(eq) : eq;
    }

    private static Expression NumberRule(Expression v, FieldAccessor accessor, FilterOperator op, FilterValue value)
    {
        if (op == FilterOperator.Between)
        {
            var range = (FilterValue.Range)value;
            Expression? both = null;
            if (range.From is FilterValue.Number from) both = And(both, NumberCompare(v, accessor, FilterOperator.Gte, from.Value));
            if (range.To is FilterValue.Number to) both = And(both, NumberCompare(v, accessor, FilterOperator.Lte, to.Value));
            return both ?? Expression.Constant(true);
        }
        var number = ((FilterValue.Number)value).Value;
        return op == FilterOperator.IsNot
            ? Expression.Not(NumberCompare(v, accessor, FilterOperator.Is, number))
            : NumberCompare(v, accessor, op, number);
    }

    private static Expression NumberCompare(Expression v, FieldAccessor accessor, FilterOperator op, decimal number)
    {
        Expression left, right;
        if (TextConvert.TryConvertExact(number, accessor.UnderlyingType, out var converted))
        {
            left = v;
            right = Expression.Constant(converted, accessor.LiftedType);
        }
        else
        {
            // A fraction against an integer field: compare as decimal.
            left = Expression.Convert(v, typeof(decimal?));
            right = Expression.Constant(number, typeof(decimal?));
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

    private static Expression DateRule(Expression v, FieldAccessor accessor, FilterOperator op, FilterValue value)
    {
        switch (op)
        {
            case FilterOperator.Between:
            {
                var range = (FilterValue.Range)value;
                Expression? both = null;
                if (range.From is { } from) both = And(both, Expression.GreaterThanOrEqual(v, DateBound(accessor, from, upper: false)));
                if (range.To is { } to) both = And(both, Expression.LessThanOrEqual(v, DateBound(accessor, to, upper: true)));
                return both ?? Expression.Constant(true);
            }
            case FilterOperator.Before:
                return Expression.LessThan(v, DateBound(accessor, value, upper: false));
            case FilterOperator.After:
                return Expression.GreaterThan(v, DateBound(accessor, value, upper: true));
            default:
            {
                var is_ = Expression.AndAlso(
                    Expression.GreaterThanOrEqual(v, DateBound(accessor, value, upper: false)),
                    Expression.LessThanOrEqual(v, DateBound(accessor, value, upper: true)));
                return op == FilterOperator.IsNot ? Expression.Not(is_) : is_;
            }
        }
    }

    /// <summary>
    /// The start (<paramref name="upper"/> false) or the inclusive end of a date value, as a constant of the field type. A day covers
    /// 00:00:00 to 23:59:59.9999999 UTC; an instant is its own start and end. Dates are read as UTC for <see cref="DateTime"/> and
    /// <see cref="DateTimeOffset"/> fields.
    /// </summary>
    private static Expression DateBound(FieldAccessor accessor, FilterValue value, bool upper)
    {
        var type = accessor.UnderlyingType;
        object bound;
        switch (value)
        {
            case FilterValue.Date day:
            {
                var time = upper ? TimeOnly.MaxValue : TimeOnly.MinValue;
                if (type == typeof(DateOnly)) bound = day.Value;
                else if (type == typeof(DateTime)) bound = day.Value.ToDateTime(time);
                else bound = new DateTimeOffset(day.Value.ToDateTime(time), TimeSpan.Zero);
                break;
            }
            case FilterValue.Moment moment:
                if (type == typeof(DateOnly)) bound = DateOnly.FromDateTime(moment.Value.UtcDateTime);
                else if (type == typeof(DateTime)) bound = moment.Value.UtcDateTime;
                else bound = moment.Value;
                break;
            default:
                throw new InvalidOperationException("A date filter value must be a Date or a Moment.");
        }
        return Expression.Constant(bound, accessor.LiftedType);
    }

    private static Expression IsEmpty(Expression v, FieldAccessor accessor) =>
        accessor.UnderlyingType == typeof(string)
            ? Expression.OrElse(
                Expression.Equal(v, Expression.Constant(null, typeof(string))),
                Expression.Equal(v, Expression.Constant("", typeof(string))))
            : Expression.Equal(v, Expression.Constant(null, v.Type));

    private static Expression ContainsIgnoreCase(Expression v, string needle) =>
        Expression.AndAlso(
            Expression.NotEqual(v, Expression.Constant(null, typeof(string))),
            Expression.Call(
                Expression.Call(v, ToLowerInvariant),
                StringContains,
                Expression.Constant(needle.ToLowerInvariant())));

    private static Expression? And(Expression? a, Expression? b) =>
        a is null ? b : b is null ? a : Expression.AndAlso(a, b);

    private static Expression? Or(Expression? a, Expression? b) =>
        a is null ? b : b is null ? a : Expression.OrElse(a, b);
}
