namespace Blux.UI.Tools.Query;

/// <summary>
/// The definition of a filter: its kind, the operators it allows, its default operator and default value, and the limits an editor shows.
/// Immutable; the <c>With...</c> methods return a changed copy. Create one with <see cref="Text"/>, <see cref="Option"/>, <see cref="Boolean"/>,
/// <see cref="Number"/> or <see cref="Date"/> and attach it to a field (<see cref="DataListFieldBuilder{T, TProp}.Filter"/>) or register it as a
/// query-only filter (<see cref="DataListSchemaBuilder{T}.QueryFilter"/>).
/// </summary>
public sealed class DataListFilter
{
    private static readonly FilterOperator[] TextOperators = [FilterOperator.Contains, FilterOperator.Is, FilterOperator.IsNot];
    private static readonly FilterOperator[] OptionOperators = [FilterOperator.IsAnyOf, FilterOperator.Is, FilterOperator.IsNot];
    private static readonly FilterOperator[] BooleanOperators = [FilterOperator.Is, FilterOperator.IsNot];
    private static readonly FilterOperator[] NumberOperators =
        [FilterOperator.Is, FilterOperator.IsNot, FilterOperator.Gt, FilterOperator.Gte, FilterOperator.Lt, FilterOperator.Lte, FilterOperator.Between];
    private static readonly FilterOperator[] DateOperators =
        [FilterOperator.Is, FilterOperator.IsNot, FilterOperator.Before, FilterOperator.After, FilterOperator.Between];

    private DataListFilter(FilterKind kind, IReadOnlyList<FilterOperator> operators, FilterOperator defaultOperator)
    {
        Kind = kind;
        AllowedOperators = operators;
        DefaultOperator = defaultOperator;
        DefaultValueOperator = defaultOperator;
    }

    /// <summary>The kind of filter.</summary>
    public FilterKind Kind { get; private init; }

    /// <summary>The operators a rule of this filter may use. The per-kind set of the spec, or the subset given to <see cref="WithOperators"/>.</summary>
    public IReadOnlyList<FilterOperator> AllowedOperators { get; private init; }

    /// <summary>The operator a rule gets when none is given, and the one a URL key without an operator suffix means.</summary>
    public FilterOperator DefaultOperator { get; private init; }

    /// <summary>The value effective when the URL has no override; omitted from the URL and not counted as active. <see langword="null"/> for none.</summary>
    public FilterValue? DefaultValue { get; private init; }

    /// <summary>The operator of the default rule (<see cref="DefaultOperator"/> unless <see cref="WithDefault"/> set another).</summary>
    public FilterOperator DefaultValueOperator { get; private init; }

    /// <summary>Whether the default rule also includes empty rows.</summary>
    public bool DefaultIncludeEmpty { get; private init; }

    /// <summary>The smallest value an editor offers (number filters).</summary>
    public decimal? Min { get; private init; }

    /// <summary>The largest value an editor offers (number filters).</summary>
    public decimal? Max { get; private init; }

    /// <summary>The step of a number editor.</summary>
    public decimal? Step { get; private init; }

    /// <summary>The precision of a date editor; decides whether values are <see cref="FilterValue.Date"/> or <see cref="FilterValue.Moment"/>.</summary>
    public DateGranularity Granularity { get; private init; }

    /// <summary>Whether the user's last value is remembered between visits (browser storage), restored only when the URL has no value.</summary>
    public bool Persist { get; private init; }

    /// <summary>A text filter: <c>contains</c> (default), <c>is</c>, <c>isNot</c>.</summary>
    public static DataListFilter Text() => new(FilterKind.Text, TextOperators, FilterOperator.Contains);

    /// <summary>An option filter: <c>isAnyOf</c> (default), <c>is</c>, <c>isNot</c>. Works on strings, enums, numbers, booleans, Guids and dates.</summary>
    public static DataListFilter Option() => new(FilterKind.Option, OptionOperators, FilterOperator.IsAnyOf);

    /// <summary>A boolean filter: <c>is</c> (default), <c>isNot</c>.</summary>
    /// <param name="defaultValue">The value effective without a URL override, or <see langword="null"/> for none.</param>
    public static DataListFilter Boolean(bool? defaultValue = null)
    {
        var filter = new DataListFilter(FilterKind.Boolean, BooleanOperators, FilterOperator.Is);
        return defaultValue is { } v ? filter.WithDefault(new FilterValue.Boolean(v)) : filter;
    }

    /// <summary>A number filter: <c>is</c> (default), <c>isNot</c>, <c>gt</c>, <c>gte</c>, <c>lt</c>, <c>lte</c>, <c>between</c>.</summary>
    /// <param name="min">The smallest value an editor offers.</param>
    /// <param name="step">The step of the editor.</param>
    /// <param name="max">The largest value an editor offers.</param>
    public static DataListFilter Number(decimal? min = null, decimal? step = null, decimal? max = null) =>
        new(FilterKind.Number, NumberOperators, FilterOperator.Is) { Min = min, Max = max, Step = step };

    /// <summary>A date filter: <c>is</c> (default), <c>isNot</c>, <c>before</c>, <c>after</c>, <c>between</c>.</summary>
    /// <param name="granularity">The precision; <see cref="DateGranularity.Day"/> edits <see cref="FilterValue.Date"/> values.</param>
    public static DataListFilter Date(DateGranularity granularity = DateGranularity.Day) =>
        new(FilterKind.Date, DateOperators, FilterOperator.Is) { Granularity = granularity };

    /// <summary>Restricts the allowed operators. Each must belong to the kind's set, except <see cref="FilterOperator.IsEmpty"/> and <see cref="FilterOperator.IsNotEmpty"/>, which any kind may add. Resets the default operator to the first one when the old default is no longer allowed.</summary>
    public DataListFilter WithOperators(params FilterOperator[] operators)
    {
        ArgumentNullException.ThrowIfNull(operators);
        if (operators.Length == 0) throw new ArgumentException("At least one operator is required.", nameof(operators));
        var baseSet = BaseOperators(Kind);
        foreach (var op in operators)
        {
            if (op is not (FilterOperator.IsEmpty or FilterOperator.IsNotEmpty) && Array.IndexOf(baseSet, op) < 0)
                throw new ArgumentException($"The operator {op} is not allowed on a {Kind} filter.", nameof(operators));
        }
        var distinct = operators.Distinct().ToArray();
        var defaultOp = Array.IndexOf(distinct, DefaultOperator) >= 0 ? DefaultOperator : distinct[0];
        var valueOp = Array.IndexOf(distinct, DefaultValueOperator) >= 0 ? DefaultValueOperator : defaultOp;
        return Copy(distinct, defaultOp, valueOp);
    }

    /// <summary>Sets the operator used when none is given. It must be allowed.</summary>
    public DataListFilter WithDefaultOperator(FilterOperator op)
    {
        if (!AllowedOperators.Contains(op)) throw new ArgumentException($"The operator {op} is not allowed on this filter.", nameof(op));
        return Copy(AllowedOperators, op, DefaultValue is null ? op : DefaultValueOperator);
    }

    /// <summary>Sets the default rule: effective without a URL override, omitted from the URL.</summary>
    /// <param name="value">The value.</param>
    /// <param name="op">The operator of the default rule; <see cref="DefaultOperator"/> when <see langword="null"/>.</param>
    /// <param name="includeEmpty">Whether the default rule includes empty rows.</param>
    public DataListFilter WithDefault(FilterValue value, FilterOperator? op = null, bool includeEmpty = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        var effective = op ?? DefaultOperator;
        if (!AllowedOperators.Contains(effective)) throw new ArgumentException($"The operator {effective} is not allowed on this filter.", nameof(op));
        var copy = Copy(AllowedOperators, DefaultOperator, effective);
        return new DataListFilter(copy.Kind, copy.AllowedOperators, copy.DefaultOperator)
        {
            DefaultValue = value, DefaultValueOperator = effective, DefaultIncludeEmpty = includeEmpty,
            Min = Min, Max = Max, Step = Step, Granularity = Granularity, Persist = Persist,
        };
    }

    /// <summary>Remembers the last value of this filter between visits.</summary>
    public DataListFilter WithPersist(bool persist = true) =>
        new(Kind, AllowedOperators, DefaultOperator)
        {
            DefaultValue = DefaultValue, DefaultValueOperator = DefaultValueOperator, DefaultIncludeEmpty = DefaultIncludeEmpty,
            Min = Min, Max = Max, Step = Step, Granularity = Granularity, Persist = persist,
        };

    /// <summary>The default rule of this filter under <paramref name="key"/>, or <see langword="null"/> when there is no default value.</summary>
    public FilterRule? DefaultRule(string key) =>
        DefaultValue is null ? null : new FilterRule(key, DefaultValueOperator, DefaultValue, DefaultIncludeEmpty);

    /// <summary>Whether <paramref name="rule"/> is well formed for this filter: an allowed operator and a value of the right shape. Rules that fail are dropped by <see cref="DataListSchema.Sanitize(DataListRequest)"/>.</summary>
    public bool IsValid(FilterRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var op = rule.Operator;
        if (!AllowedOperators.Contains(op)) return false;
        var value = rule.Value;
        if (op is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty) return value is null;
        if (value is null) return false;
        switch (Kind)
        {
            case FilterKind.Text:
                return value is FilterValue.Text { Value.Length: > 0 };
            case FilterKind.Option:
                return value switch
                {
                    FilterValue.Text { Value.Length: > 0 } => true,
                    FilterValue.List list => list.Values.Count > 0 || rule.IncludeEmpty,
                    FilterValue.Number or FilterValue.Boolean => true,
                    _ => false,
                };
            case FilterKind.Boolean:
                return value is FilterValue.Boolean;
            case FilterKind.Number:
                return op == FilterOperator.Between
                    ? IsValidRange(value, static v => v is FilterValue.Number)
                    : value is FilterValue.Number;
            case FilterKind.Date:
                return op == FilterOperator.Between
                    ? IsValidRange(value, static v => v is FilterValue.Date or FilterValue.Moment)
                    : value is FilterValue.Date or FilterValue.Moment;
            default:
                return false;
        }
    }

    private static bool IsValidRange(FilterValue value, Func<FilterValue, bool> bound) =>
        value is FilterValue.Range { From: var from, To: var to }
        && (from is not null || to is not null)
        && (from is null || bound(from))
        && (to is null || bound(to));

    internal static FilterOperator[] BaseOperators(FilterKind kind) => kind switch
    {
        FilterKind.Text => TextOperators,
        FilterKind.Option => OptionOperators,
        FilterKind.Boolean => BooleanOperators,
        FilterKind.Number => NumberOperators,
        _ => DateOperators,
    };

    private DataListFilter Copy(IReadOnlyList<FilterOperator> operators, FilterOperator defaultOp, FilterOperator valueOp) =>
        new(Kind, operators, defaultOp)
        {
            DefaultValue = DefaultValue, DefaultValueOperator = valueOp, DefaultIncludeEmpty = DefaultIncludeEmpty,
            Min = Min, Max = Max, Step = Step, Granularity = Granularity, Persist = Persist,
        };
}
