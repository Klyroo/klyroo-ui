namespace Blux.UI.Tools.Query;

/// <summary>How a number filter editor shows its value.</summary>
public enum DataListNumberDisplay
{
    /// <summary>A number field (the default).</summary>
    Input,

    /// <summary>A slider between <see cref="DataListFilter.Min"/> and <see cref="DataListFilter.Max"/>.</summary>
    Slider,

    /// <summary>A number field and a slider.</summary>
    InputAndSlider,
}

/// <summary>How an option filter orders the options it lists.</summary>
public enum DataListOptionSort
{
    /// <summary>By count, the most counted first, then by label. Needs the counts (<see cref="DataListFilter.WithFacets"/>); an option without a count comes last.</summary>
    Count,

    /// <summary>By label, ignoring case and accents.</summary>
    Alpha,
}

/// <summary>
/// The definition of a filter: its kind, the operators it allows, its default operator and default value, and the limits an editor shows.
/// Immutable; the <c>With...</c> methods return a changed copy. Create one with <see cref="Text"/>, <see cref="Option()"/>, <see cref="Boolean"/>,
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
    public FilterKind Kind { get; private set; }

    /// <summary>The operators a rule of this filter may use. The per-kind set of the spec, or the subset given to <see cref="WithOperators"/>.</summary>
    public IReadOnlyList<FilterOperator> AllowedOperators { get; private set; }

    /// <summary>The operator a rule gets when none is given, and the one a URL key without an operator suffix means.</summary>
    public FilterOperator DefaultOperator { get; private set; }

    /// <summary>The value effective when the URL has no override; omitted from the URL and not counted as active. <see langword="null"/> for none.</summary>
    public FilterValue? DefaultValue { get; private set; }

    /// <summary>The operator of the default rule (<see cref="DefaultOperator"/> unless <see cref="WithDefault"/> set another).</summary>
    public FilterOperator DefaultValueOperator { get; private set; }

    /// <summary>Whether the default rule also includes empty rows.</summary>
    public bool DefaultIncludeEmpty { get; private set; }

    /// <summary>The smallest value an editor offers (number filters).</summary>
    public decimal? Min { get; private set; }

    /// <summary>The largest value an editor offers (number filters).</summary>
    public decimal? Max { get; private set; }

    /// <summary>The step of a number editor.</summary>
    public decimal? Step { get; private set; }

    /// <summary>The precision of a date editor; decides whether values are <see cref="FilterValue.Date"/> or <see cref="FilterValue.Moment"/>.</summary>
    public DateGranularity Granularity { get; private set; }

    /// <summary>How a number editor shows its value: a field, a slider or both. Only number filters; a slider needs <see cref="Min"/> and <see cref="Max"/>, without them the editor shows the field.</summary>
    public DataListNumberDisplay NumberDisplay { get; private set; }

    /// <summary>The .NET numeric format string the filter tag writes a number with (<c>N0</c>, <c>C2</c>, <c>0.0 'kg'</c>), in the culture of the user. It formats the tag only, not the field of the editor. <see langword="null"/> writes the number with its digits and the group separator of the culture.</summary>
    public string? NumberFormat { get; private set; }

    /// <summary>The most days a range of a date editor may cover, both ends included. <see langword="null"/> for no limit. This limits the editor only (the calendar, the presets and the typed ends): a range in the URL, a bound state or a request is not checked against it, and neither is it on the server.</summary>
    public int? MaxRangeDays { get; private set; }

    /// <summary>Whether the user's last value is remembered between visits (browser storage), restored only when the URL has no value.</summary>
    public bool Persist { get; private set; }

    /// <summary>The name of the filter in the interface (the tag, the add filter menu). <see langword="null"/> falls back to the title of the column with the same key, then to the key written as words.</summary>
    public string? Label { get; private set; }

    /// <summary>The options an option filter offers, in order; empty for the other kinds. Without them the editor lists the values of the facet counts (<see cref="Facets"/>) and the values already selected.</summary>
    public IReadOnlyList<DataListFilterOption> Options { get; private set; } = [];

    /// <summary>Whether the data list asks for the counts of this filter (the rows that have each option, or each yes and no) and shows them in the editor. Only for option and boolean filters.</summary>
    public bool Facets { get; private set; }

    /// <summary>Whether the counts leave out this filter's own rule (<see cref="FacetMode.ExcludeSelf"/>, the default: the other options stay visible) or keep it (<see cref="FacetMode.IncludeSelf"/>).</summary>
    public FacetMode FacetMode { get; private set; }

    /// <summary>How the editor orders the options. <see langword="null"/> keeps the declared options in their order and lists the options that come from counts by count (the order of the facet). Not used for remote options, which keep the order of their source.</summary>
    public DataListOptionSort? OptionSort { get; private set; }

    /// <summary>How many options the editor lists before a "show more" button, or <see langword="null"/> to list them all. The selected options always show. A statically rendered editor lists them all.</summary>
    public int? OptionLimit { get; private set; }

    /// <summary>Where the options come from when they are loaded from a remote source, page by page, with a search. <see langword="null"/> for declared options and for options taken from the counts.</summary>
    public DataListRemoteOptions? RemoteOptions { get; private set; }

    /// <summary>A text filter: <c>contains</c> (default), <c>is</c>, <c>isNot</c>.</summary>
    public static DataListFilter Text() => new(FilterKind.Text, TextOperators, FilterOperator.Contains);

    /// <summary>An option filter: <c>isAnyOf</c> (default), <c>is</c>, <c>isNot</c>. Works on strings, enums, numbers, booleans, Guids and dates.</summary>
    public static DataListFilter Option() => new(FilterKind.Option, OptionOperators, FilterOperator.IsAnyOf);

    /// <summary>An option filter with the options its editor lists (value and label).</summary>
    /// <param name="options">The options, in the order the editor shows them.</param>
    public static DataListFilter Option(params DataListFilterOption[] options) => Option().WithOptions(options);

    /// <summary>An option filter that lists <paramref name="values"/>, each labelled by itself.</summary>
    /// <param name="values">The values, in the order the editor shows them.</param>
    public static DataListFilter Option(params string[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Option().WithOptions([.. values.Select(v => new DataListFilterOption(v, v))]);
    }

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
        return copy.With(c => { c.DefaultValue = value; c.DefaultValueOperator = effective; c.DefaultIncludeEmpty = includeEmpty; });
    }

    /// <summary>Remembers the last value of this filter between visits.</summary>
    public DataListFilter WithPersist(bool persist = true) => With(c => c.Persist = persist);

    /// <summary>Names the filter in the interface.</summary>
    /// <param name="label">The label of the tag and of the add filter menu.</param>
    public DataListFilter WithLabel(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return Clone(label, Options, Facets);
    }

    /// <summary>Sets the options the editor lists. Only for option filters.</summary>
    /// <param name="options">The options, in order; their values must be distinct.</param>
    public DataListFilter WithOptions(params DataListFilterOption[] options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (Kind != FilterKind.Option) throw new InvalidOperationException("Only an option filter has options.");
        if (RemoteOptions is not null && options.Length > 0) throw new InvalidOperationException("A filter takes declared options or remote ones, not both.");
        var distinct = options.DistinctBy(o => o.Value, StringComparer.Ordinal).ToArray();
        if (distinct.Length != options.Length) throw new ArgumentException("The values of the options must be distinct.", nameof(options));
        return Clone(Label, distinct, Facets);
    }

    /// <summary>
    /// Asks the data list for the counts of this filter and shows them next to the options (or next to Yes and No). By default the counts leave out this filter's own rule, so the other options stay visible
    /// (see <see cref="WithFacetMode"/>). The counts are asked for when the filters or the search change, and kept while the user pages or sorts.
    /// </summary>
    /// <param name="facets">Whether to ask for the counts.</param>
    public DataListFilter WithFacets(bool facets = true)
    {
        if (Kind is not (FilterKind.Option or FilterKind.Boolean)) throw new InvalidOperationException("Only an option or a boolean filter has facets.");
        return Clone(Label, Options, facets);
    }

    /// <summary>Chooses whether the counts of this filter keep its own rule (<see cref="FacetMode.IncludeSelf"/>) or leave it out (<see cref="FacetMode.ExcludeSelf"/>, the default).</summary>
    /// <param name="mode">The mode.</param>
    public DataListFilter WithFacetMode(FacetMode mode)
    {
        if (Kind is not (FilterKind.Option or FilterKind.Boolean)) throw new InvalidOperationException("Only an option or a boolean filter has facets.");
        return With(c => c.FacetMode = mode);
    }

    /// <summary>Orders the declared options and the counted options of an option filter by count or by label (<see cref="OptionSort"/>). Remote options keep the order of their source: the sort is ignored for them.</summary>
    /// <param name="sort">The order.</param>
    public DataListFilter WithOptionSort(DataListOptionSort sort)
    {
        if (Kind != FilterKind.Option) throw new InvalidOperationException("Only an option filter has options to order.");
        return With(c => c.OptionSort = sort);
    }

    /// <summary>Lists only the first options of an option filter, with a "show more" button for the others. The selected options always show.</summary>
    /// <param name="limit">How many options to list first; at least 1.</param>
    public DataListFilter WithOptionLimit(int limit)
    {
        if (Kind != FilterKind.Option) throw new InvalidOperationException("Only an option filter has options to limit.");
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        return With(c => c.OptionLimit = limit);
    }

    /// <summary>
    /// Loads the options from a remote source instead of declaring them: the editor asks for a page when it is shown (a tag's popover that opens, a panel section that opens, the add filter menu
    /// when the filter is picked), searches while the user types (after a pause), and loads the next page as the list scrolls. The values of a rule that the loaded pages do not contain (restored from the
    /// address) get their labels from <paramref name="resolveSelected"/>, asked for while the rows load. Only interactive renders load pages: a statically rendered editor lists the selected options and, when the
    /// filter has counts (<see cref="WithFacets"/>), the most counted ones (at most <see cref="OptionLimit"/>, else the page size), all labelled in one call to the resolver; a counted option the resolver does not
    /// know is left out. The counts of the options that are listed are asked for by value (<see cref="DataListFacetRequest.Values"/>), so every page has its counts whatever the number of distinct values.
    /// <see cref="OptionSort"/> does not apply: the options keep the order of the source.
    /// </summary>
    /// <param name="provider">Loads one page of options for a search.</param>
    /// <param name="resolveSelected">Returns the options of values that no loaded page holds; values it leaves out show as they are.</param>
    /// <param name="pageSize">How many options to ask for per page.</param>
    /// <param name="minLength">The shortest search that is sent; a shorter one (not empty) lists nothing.</param>
    public DataListFilter WithRemoteOptions(DataListOptionsProvider provider, DataListOptionsResolver? resolveSelected = null, int pageSize = DataListRemoteOptions.DefaultPageSize, int minLength = 0)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (Kind != FilterKind.Option) throw new InvalidOperationException("Only an option filter has options.");
        if (Options.Count > 0) throw new InvalidOperationException("A filter takes declared options or remote ones, not both.");
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(minLength, 0);
        return With(c => c.RemoteOptions = new DataListRemoteOptions(provider, resolveSelected, pageSize, minLength));
    }

    private DataListFilter Clone(string? label, IReadOnlyList<DataListFilterOption> options, bool facets) =>
        With(c => { c.Label = label; c.Options = options; c.Facets = facets; });

    /// <summary>Chooses how a number editor shows its value. A slider needs <see cref="Min"/> and <see cref="Max"/>; without them the editor shows the field.</summary>
    /// <param name="display">The display.</param>
    public DataListFilter WithNumberDisplay(DataListNumberDisplay display)
    {
        if (Kind != FilterKind.Number) throw new InvalidOperationException("Only a number filter has a number display.");
        return With(c => c.NumberDisplay = display);
    }

    /// <summary>Sets the format the filter tag writes a number with (the tag only, not the editor). Only number filters.</summary>
    /// <param name="format">A .NET numeric format string such as <c>N0</c> or <c>C2</c>.</param>
    public DataListFilter WithNumberFormat(string format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        if (Kind != FilterKind.Number) throw new InvalidOperationException("Only a number filter has a format.");
        try
        {
            _ = 0m.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (FormatException e)
        {
            throw new ArgumentException($"'{format}' is not a numeric format string.", nameof(format), e);
        }
        return With(c => c.NumberFormat = format);
    }

    /// <summary>Limits the days a range of a date editor may cover (both ends included). Only date filters. An editor limit only: the URL and the server do not enforce it.</summary>
    /// <param name="days">At least 1.</param>
    public DataListFilter WithMaxRangeDays(int days)
    {
        if (Kind != FilterKind.Date) throw new InvalidOperationException("Only a date filter has a range limit.");
        ArgumentOutOfRangeException.ThrowIfLessThan(days, 1);
        return With(c => c.MaxRangeDays = days);
    }

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
        With(c => { c.AllowedOperators = operators; c.DefaultOperator = defaultOp; c.DefaultValueOperator = valueOp; });

    // A copy with one change: the definition is immutable, so every With... method goes through here and a new property needs no change in the others.
    private DataListFilter With(Action<DataListFilter> change)
    {
        var copy = (DataListFilter)MemberwiseClone();
        change(copy);
        return copy;
    }
}

/// <summary>One option of an option filter: the value that goes into the rule and the URL, and what the interface shows.</summary>
/// <param name="Value">The value as text, the way a <see cref="FilterValue.List"/> carries it (an enum name, a number, <c>true</c>).</param>
/// <param name="Label">The text shown to the user.</param>
public sealed record DataListFilterOption(string Value, string Label);
