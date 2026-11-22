using System.Globalization;
using Blux.UI.Tools.Query;
using Microsoft.Extensions.Logging;

namespace Blux.UI.Tools;

/// <summary>How a filter editor applies what the user changes.</summary>
public enum DataListApplyMode
{
    /// <summary>Each change applies at once (a text filter after a pause in the typing). The history gets an entry per change.</summary>
    Live,

    /// <summary>Changes wait in the editor until the Apply button; Cancel, Escape or a click outside drop them.</summary>
    Submit,
}

/// <summary>An option an option filter editor lists: a value, what to show for it and, when the list asked for them, how many rows have it.</summary>
/// <param name="Value">The value a rule carries.</param>
/// <param name="Label">The text shown.</param>
/// <param name="Count">The number of rows that have the value, or <see langword="null"/> when the list has no count for this filter.</param>
internal readonly record struct FilterChoice(string Value, string Label, int? Count);

public sealed partial class DataListContext<TItem>
{
    // Every kind has an editor: text, option, boolean, number and date, except a date filter exact to the second (its tag shows, and the filter applies, but a minute-precise picker cannot write it).
    internal static bool HasEditor(DataListFilter filter) =>
        filter.Kind is FilterKind.Text or FilterKind.Option or FilterKind.Boolean or FilterKind.Number || filter is { Kind: FilterKind.Date, Granularity: not DateGranularity.Second };

    /// <summary>The time zone of the user: the one of the browser once the list is interactive, UTC before that and in static rendering. The days of the date filters are in it.</summary>
    internal TimeZoneInfo Zone => DataListDates.Zone(_owner.TimeZoneId);

    /// <summary>The calendar day it is for the user, which the presets of a date editor count from.</summary>
    internal DateOnly Today => DataListDates.Today(_owner.Now, Zone);

    /// <summary>The numbers of a filter tag with the group separator of the culture and no trailing zeros, unless the filter has a format of its own.</summary>
    internal const string DefaultNumberFormat = "#,0.############################";

    /// <summary>The key of every filter of the schema that has an editor, in the order the schema declares them.</summary>
    internal IReadOnlyList<string> EditableFilterKeys =>
        Schema is null ? [] : [.. Schema.FilterKeys.Where(k => Schema.TryGetFilter(k, out var filter) && HasEditor(filter))];

    /// <summary>Raised when something asks for the focus to go to the filter tags (the clear filters button, which is gone once it worked).</summary>
    internal event Action? FilterFocusRequested;

    internal void RequestFilterFocus() => FilterFocusRequested?.Invoke();

    /// <summary>The rules of the state that belong to a filter of the schema, in the order of the state.</summary>
    internal IReadOnlyList<FilterRule> ActiveFilters =>
        Schema is null ? [] : [.. State.Filters.Where(r => Schema.TryGetFilter(r.Key, out _))];

    internal FilterRule? RuleFor(string key) => State.Filters.FirstOrDefault(r => r.Key == key);

    internal DataListFilter? FilterOf(string key) => Schema is not null && Schema.TryGetFilter(key, out var filter) ? filter : null;

    /// <summary>The name of a filter: its <see cref="DataListFilter.Label"/>, else the title of the column with that key, else the key written as words.</summary>
    internal string FilterLabel(string key)
    {
        if (FilterOf(key)?.Label is { Length: > 0 } label) return label;
        var column = Columns.FirstOrDefault(c => c.EffectiveKey == key);
        return column?.EffectiveTitle ?? DataListText.Humanize(key);
    }

    /// <summary>The counts the list loaded for a filter (<see cref="DataListFilter.WithFacets"/>), or <see langword="null"/> when it has none (yet).</summary>
    internal DataListFacetResult? FacetOf(string key) => Result?.Facets?.FirstOrDefault(f => f.Key == key);

    /// <summary>
    /// How many rows have <paramref name="value"/> in the facet of <paramref name="key"/>, or <see langword="null"/> when there is no count for it: the filter has no facet, or the facet holds only
    /// some values (the most counted ones, cut by the cap of buckets or by a limit) and this one is not among them. A value a complete facet lacks has no rows: 0. The options a remote editor
    /// loaded are counted one by one (<see cref="LoadCountsAsync"/>), so each page has its counts whatever the size of the facet.
    /// </summary>
    internal int? CountOf(string key, string value)
    {
        if (FilterOf(key)?.RemoteOptions is not null && IsInteractive)
        {
            if (CountsHold && _valueCounts.TryGetValue(key, out var counted) && counted.TryGetValue(value, out var count)) return count;
            return FacetOptionCount(key, value);
        }
        if (FacetOf(key) is not { } facet) return null;
        foreach (var option in facet.Options)
        {
            if (string.Equals(option.Value, value, StringComparison.Ordinal)) return option.Count;
        }
        return facet.Truncated || facet.Total > facet.Options.Count ? null : 0;
    }

    private int? FacetOptionCount(string key, string value) =>
        FacetOf(key)?.Options.FirstOrDefault(o => string.Equals(o.Value, value, StringComparison.Ordinal))?.Count;

    // The counts of the values a remote editor loaded: valid for the filters and search of the result they were counted under.
    private readonly Dictionary<string, Dictionary<string, int>> _valueCounts = new(StringComparer.Ordinal);
    private readonly HashSet<(string Key, string Value)> _countAttempts = [];
    private DataListRequest? _valueCountsFor;

    private bool CountsHold => _valueCountsFor is { } scope && _owner.Controller?.LoadedRequest is { } loaded && SameScope(scope, loaded);

    private static bool SameScope(DataListRequest a, DataListRequest b) => a.Filter == b.Filter && a.Search == b.Search && a.TimeZone == b.TimeZone;

    /// <summary>
    /// Counts the given values of a remote option filter that has counts, under the filters and search on screen: one call to the source with a facet scoped to them
    /// (<see cref="DataListFacetRequest.Values"/>), for the values nobody counted yet. A value no row has counts 0. A failure leaves the options without a count and is logged.
    /// </summary>
    internal async Task LoadCountsAsync(string key, IReadOnlyCollection<string> values, CancellationToken cancellationToken, ILogger? logger = null)
    {
        if (_owner.Controller is not { LoadedRequest: { } loaded } controller || FilterOf(key) is not { Facets: true, RemoteOptions: not null }) return;
        if (!CountsHold)
        {
            _valueCounts.Clear();
            _countAttempts.Clear();
            _valueCountsFor = loaded;
        }
        var missing = values.Distinct(StringComparer.Ordinal).Where(v => !(_valueCounts.TryGetValue(key, out var m) && m.ContainsKey(v)) && _countAttempts.Add((key, v))).ToList();
        if (missing.Count == 0) return;
        try
        {
            var facet = await controller.LoadFacetValuesAsync(key, missing, cancellationToken).ConfigureAwait(true);
            if (facet is null || !CountsHold)
            {
                foreach (var v in missing) _countAttempts.Remove((key, v));
                return;
            }
            if (!_valueCounts.TryGetValue(key, out var counts)) _valueCounts[key] = counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var v in missing) counts[v] = facet.Options.FirstOrDefault(o => string.Equals(o.Value, v, StringComparison.Ordinal))?.Count ?? 0;
            NotifyChanged();
        }
        catch (Exception e)
        {
            foreach (var v in missing) _countAttempts.Remove((key, v));
            if (cancellationToken.IsCancellationRequested) return;
            logger?.LogWarning(e, "The counts of the options of the filter '{Key}' of a data list could not be loaded; the options show without a count.", key);
        }
    }

    /// <summary>The number of rows where the field of an option filter is empty, when the list counted them.</summary>
    internal int? MissingCountOf(string key) => FacetOf(key)?.Missing;

    // The labels that remote options gave to values: what an editor loaded, picked or resolved, so a tag can write "Owner is Ann Lee" and not "Owner is 7".
    private readonly Dictionary<string, Dictionary<string, string>> _remoteLabels = new(StringComparer.Ordinal);
    private readonly HashSet<(string Key, string Value)> _labelAttempts = [];
    private readonly HashSet<(string Key, string Value)> _labelFailures = [];

    /// <summary>The label of an option value: the one the filter declares, else the one its remote source gave, else the value itself.</summary>
    internal string OptionLabelOf(string key, string value)
    {
        if (FilterOf(key) is { Options.Count: > 0 } filter && filter.Options.FirstOrDefault(o => o.Value == value) is { } declared) return declared.Label;
        return _remoteLabels.TryGetValue(key, out var labels) && labels.TryGetValue(value, out var label) ? label : value;
    }

    /// <summary>Remembers labels that a remote option source gave. Returns whether one is new.</summary>
    internal bool LearnLabels(string key, IEnumerable<DataListFilterOption> options)
    {
        var changed = false;
        foreach (var option in options)
        {
            if (!_remoteLabels.TryGetValue(key, out var labels)) _remoteLabels[key] = labels = new Dictionary<string, string>(StringComparer.Ordinal);
            if (labels.TryGetValue(option.Value, out var known) && known == option.Label) continue;
            labels[option.Value] = option.Label;
            changed = true;
        }
        return changed;
    }

    internal bool KnowsLabel(string key, string value) => _remoteLabels.TryGetValue(key, out var labels) && labels.ContainsKey(value);

    /// <summary>Every label a remote source gave, by filter key and value: what a prerender hands to the interactive render.</summary>
    internal IReadOnlyDictionary<string, Dictionary<string, string>> RemoteLabels => _remoteLabels;

    /// <summary>Takes the labels a prerender resolved, so the interactive render shows them at once and does not ask the resolver again.</summary>
    internal void AdoptLabels(Dictionary<string, Dictionary<string, string>> labels)
    {
        foreach (var (key, byValue) in labels)
        {
            if (FilterOf(key)?.RemoteOptions is null) continue;
            LearnLabels(key, byValue.Select(pair => new DataListFilterOption(pair.Key, pair.Value)));
        }
    }

    /// <summary>
    /// Asks the resolver of each remote option filter, in one call per filter, for the labels of the values that nobody labelled: the values of its rule in <paramref name="state"/> (restored from the
    /// address) and, with <paramref name="includeCounted"/>, the counted options that its statically rendered editor lists (at most its option limit, else its page size), so that it lists only
    /// options it can label. A value is asked once. A resolver that throws is logged as a warning and leaves the raw values; they are asked again at the next load. Never throws.
    /// </summary>
    internal async Task ResolveLabelsAsync(DataListState state, bool includeCounted, CancellationToken cancellationToken, ILogger? logger = null, bool retryFailed = false)
    {
        if (Schema is null) return;
        if (retryFailed)
        {
            // A new load: the values whose resolution failed get another try; within one load a failure is logged once, not once per caller.
            foreach (var failed in _labelFailures) _labelAttempts.Remove(failed);
            _labelFailures.Clear();
        }
        var changed = false;
        try
        {
            foreach (var key in Schema.FilterKeys)
            {
                if (FilterOf(key) is not { RemoteOptions: { Resolver: { } resolver } remote } filter) continue;
                // A static editor lists counted options with the selected ones: one call for all of them, once the counts are there.
                if (!includeCounted && !IsInteractive && filter.Facets) continue;
                var wanted = new List<string>();
                if (state.Filters.FirstOrDefault(r => r.Key == key) is { Value: { } value }) wanted.AddRange(value is FilterValue.Text t ? [t.Value] : value is FilterValue.List list ? list.Values : []);
                if (includeCounted && FacetOf(key) is { } facet) wanted.AddRange(facet.Options.Take(filter.OptionLimit ?? remote.PageSize).Select(o => o.Value));
                var missing = wanted.Distinct(StringComparer.Ordinal).Where(v => !KnowsLabel(key, v) && _labelAttempts.Add((key, v))).ToList();
                if (missing.Count == 0) continue;
                try
                {
                    var found = await resolver(missing, cancellationToken).ConfigureAwait(true);
                    changed |= LearnLabels(key, found.Where(o => missing.Contains(o.Value)));
                }
                catch (Exception e)
                {
                    // The raw values stay; the next load asks again. A list that went away (the token) says nothing worth logging.
                    foreach (var v in missing) _labelFailures.Add((key, v));
                    if (cancellationToken.IsCancellationRequested) return;
                    logger?.LogWarning(e, "The labels of {Count} values of the filter '{Key}' of a data list could not be resolved; the values show as they are.", missing.Count, key);
                }
            }
            if (changed) NotifyChanged();
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            // Notified while the list was going away: nobody is left to show the labels.
        }
    }

    /// <summary>
    /// The options an option filter lists: the ones its definition declares and, when the list asked for counts, their counts; without declared options the
    /// values of the counts (labelled by the remote source when it has given labels). A selected value that is in neither stays in the list so that it can be taken off.
    /// The filter's <see cref="DataListFilter.OptionSort"/> orders the lists that come from the definition or the counts.
    /// </summary>
    internal IReadOnlyList<FilterChoice> FilterChoices(string key, IEnumerable<string> selected, CultureInfo? culture = null)
    {
        var filter = FilterOf(key);
        var facet = FacetOf(key);
        var choices = new List<FilterChoice>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (filter is { Options.Count: > 0 })
        {
            foreach (var option in filter.Options)
            {
                if (seen.Add(option.Value)) choices.Add(new FilterChoice(option.Value, option.Label, CountOf(key, option.Value)));
            }
        }
        else if (facet is not null)
        {
            // A remote filter lists the counted options it can label (the server resolved them in one call), and no more than its limit.
            var remote = filter?.RemoteOptions;
            var counted = remote is null ? facet.Options : facet.Options.Where(o => KnowsLabel(key, o.Value)).Take(filter!.OptionLimit ?? remote.PageSize);
            foreach (var option in counted)
            {
                if (seen.Add(option.Value)) choices.Add(new FilterChoice(option.Value, OptionLabelOf(key, option.Value), option.Count));
            }
        }
        // The options of a remote source keep its order (and the count order of the facet): the sort is for declared and counted options.
        if (filter is { RemoteOptions: null, OptionSort: { } sort })
        {
            var comparer = StringComparer.Create(culture ?? CultureInfo.CurrentCulture, ignoreCase: true);
            choices = sort == DataListOptionSort.Count
                ? [.. choices.OrderByDescending(c => c.Count ?? -1).ThenBy(c => c.Label, comparer)]
                : [.. choices.OrderBy(c => c.Label, comparer)];
        }
        foreach (var value in selected)
        {
            if (seen.Add(value)) choices.Add(new FilterChoice(value, OptionLabelOf(key, value), CountOf(key, value)));
        }
        return choices;
    }

    /// <summary>The state with the rule of <paramref name="key"/> replaced by <paramref name="rule"/> (added at the end when the key has none), or taken off when it is <see langword="null"/>; back on the first page.</summary>
    internal static DataListState WithFilter(DataListState state, string key, FilterRule? rule)
    {
        var filters = new List<FilterRule>(state.Filters.Count + 1);
        var placed = false;
        foreach (var existing in state.Filters)
        {
            if (existing.Key != key) filters.Add(existing);
            else if (rule is not null && !placed)
            {
                filters.Add(rule);
                placed = true;
            }
        }
        if (rule is not null && !placed) filters.Add(rule);
        return state with { Filters = filters, Page = 1, Cursor = null };
    }

    /// <summary>The state with every filter taken off (the search, sort and page size stay), on the first page.</summary>
    internal static DataListState WithoutFilters(DataListState state) => state with { Filters = [], Page = 1, Cursor = null };

    /// <summary>Applies a rule (or takes the filter off with <see langword="null"/>) to the state: written to the URL with <paramref name="history"/> and back on page 1.</summary>
    internal void ApplyFilter(string key, FilterRule? rule, QueryHistory history = QueryHistory.Push) =>
        SetState(state => WithFilter(state, key, rule), history);

    /// <summary>The name of the field a GET form uses for the rule of a filter: <c>f.key</c> for its default operator, <c>f.key.op</c> for another one.</summary>
    internal string FilterFieldName(string key, FilterOperator op)
    {
        var filter = FilterOf(key);
        var name = filter is null || op == filter.DefaultOperator ? "f." + key : "f." + key + "." + FilterRuleText.OperatorName(op);
        return FieldName(name);
    }

    /// <summary>The names, relative to <see cref="UrlKey"/>, of every URL key a rule of <paramref name="key"/> can use: a GET form that edits the filter leaves them out of its hidden fields.</summary>
    internal string[] FilterKeyNames(string key)
    {
        var filter = FilterOf(key);
        var names = new List<string> { "f." + key };
        if (filter is not null) names.AddRange(filter.AllowedOperators.Select(op => "f." + key + "." + FilterRuleText.OperatorName(op)));
        names.AddRange(Enum.GetValues<FilterOperator>().Select(op => "f." + key + "." + FilterRuleText.OperatorName(op)));
        return [.. names.Distinct()];
    }

    /// <summary>The operators the editor of a filter offers: the ones the filter allows. An option filter does not list <c>is</c> next to <c>isAnyOf</c> (it is one value of it) unless its rule already uses it.</summary>
    internal IReadOnlyList<FilterOperator> EditorOperators(string key, FilterOperator? current)
    {
        var filter = FilterOf(key);
        if (filter is null) return [];
        var operators = filter.AllowedOperators.Where(op => op is not (FilterOperator.IsEmpty or FilterOperator.IsNotEmpty)).ToList();
        if (filter.Kind == FilterKind.Option && operators.Contains(FilterOperator.IsAnyOf) && current != FilterOperator.Is) operators.Remove(FilterOperator.Is);
        return operators;
    }

    /// <summary>The "operator value" parts of a filter tag for a rule: <c>is not</c> and <c>Active, Paused +2</c>.</summary>
    internal (string Operator, string Value) Summarize(FilterRule rule, BxToolsMessages messages, CultureInfo culture)
    {
        var filter = FilterOf(rule.Key);
        var op = messages.OperatorLabel(rule.Operator, forTag: true);
        if (rule.Operator is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty) return (op, "");

        string ValueLabel(string value) => OptionLabelOf(rule.Key, value);

        var granularity = filter?.Granularity ?? DateGranularity.Day;
        var zone = Zone;
        string Number(decimal value) => value.ToString(filter?.NumberFormat ?? DefaultNumberFormat, culture);
        string Bound(FilterValue? value) => value switch
        {
            FilterValue.Number n => Number(n.Value),
            FilterValue.Date or FilterValue.Moment => DataListDates.Format(value, granularity, culture, zone),
            _ => "",
        };

        var text = rule.Value switch
        {
            FilterValue.Text t => filter?.Kind == FilterKind.Option ? ValueLabel(t.Value) : t.Value,
            FilterValue.List list => ListText(list.Values.Select(ValueLabel).ToList(), messages, culture),
            FilterValue.Boolean b => b.Value ? messages.FilterYes : messages.FilterNo,
            FilterValue.Range r => (Bound(r.From) + " – " + Bound(r.To)).Trim(),
            FilterValue.Number or FilterValue.Date or FilterValue.Moment => Bound(rule.Value),
            _ => "",
        };
        if (rule.IncludeEmpty) text = text.Length == 0 ? messages.FilterEmpty : text + " " + messages.FilterOrEmpty;
        return (op, text);
    }

    private static string ListText(IReadOnlyList<string> labels, BxToolsMessages messages, CultureInfo culture)
    {
        const int shown = 2;
        var text = string.Join(", ", labels.Take(shown));
        return labels.Count > shown ? text + " " + string.Format(culture, messages.FilterMore, labels.Count - shown) : text;
    }
}
