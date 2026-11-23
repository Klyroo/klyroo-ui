using System.Globalization;
using Blux.UI.Tools.Query;

namespace Blux.UI.Tools;

/// <summary>Built-in column aggregation kinds for the summary footer (<c>&lt;tfoot&gt;</c>) of <see cref="BxDataListTable{TItem}"/>.</summary>
public enum DataListSummaryKind
{
    /// <summary>No summary value in this column.</summary>
    None,

    /// <summary>Sum of numeric values in the column across the summary scope.</summary>
    Sum,

    /// <summary>Arithmetic mean of numeric values in the column across the summary scope.</summary>
    Average,

    /// <summary>Count of rows with a non-empty value in the column across the summary scope.</summary>
    Count,

    /// <summary>Minimum value in the column across the summary scope.</summary>
    Min,

    /// <summary>Maximum value in the column across the summary scope.</summary>
    Max,
}

/// <summary>Which set of rows a summary footer row aggregates.</summary>
public enum DataListSummaryScope
{
    /// <summary>Every row matching the active search and filters (or the loaded rows on screen for remote sources without <see cref="DataListSummaries{TItem}.ResolveAsync"/>).</summary>
    Filtered,

    /// <summary>The rows currently loaded on screen (<see cref="DataListContext{TItem}.Rows"/>).</summary>
    Page,

    /// <summary>The currently selected rows (<see cref="DataListContext{TItem}.SelectedItems"/>).</summary>
    Selection,
}

/// <summary>Context passed to <see cref="BxDataListColumn{TItem}.SummaryTemplate"/> when rendering a summary footer cell.</summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed record DataListSummaryCellContext<TItem>(
    DataListContext<TItem> List,
    string? ColumnKey,
    DataListSummaryScope Scope,
    IReadOnlyList<TItem> Rows,
    object? Value,
    string? FormattedValue,
    bool IsLoading);

/// <summary>Context passed to <see cref="DataListSummaries{TItem}.ResolveAsync"/> when resolving summary values asynchronously.</summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed record DataListSummaryResolveContext<TItem>(
    DataListContext<TItem> List,
    DataListSummaryScope Scope,
    IReadOnlyList<TItem> Rows,
    DataListRequest? Request,
    CancellationToken CancellationToken);

/// <summary>
/// Footer summary configuration (<c>&lt;tfoot&gt;</c>) for <see cref="BxDataListTable{TItem}"/>.
/// When omitted on <see cref="BxDataListTable{TItem}"/>, a single summary row with <see cref="DataListSummaryScope.Filtered"/>
/// is still rendered automatically whenever at least one <see cref="BxDataListColumn{TItem}"/> sets <see cref="BxDataListColumn{TItem}.Summary"/>,
/// <see cref="BxDataListColumn{TItem}.SummaryValue"/> or <see cref="BxDataListColumn{TItem}.SummaryTemplate"/>.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataListSummaries<TItem>
{
    /// <summary>The default scope aggregated when <see cref="Scopes"/> is not set (<see cref="DataListSummaryScope.Filtered"/> by default).</summary>
    public DataListSummaryScope Scope { get; init; } = DataListSummaryScope.Filtered;

    /// <summary>
    /// Optional list of scopes to render as separate summary rows in <c>&lt;tfoot&gt;</c> (for example <c>[DataListSummaryScope.Page, DataListSummaryScope.Filtered]</c>).
    /// When <see langword="null"/> or empty, renders one row for <see cref="Scope"/>.
    /// </summary>
    public IReadOnlyList<DataListSummaryScope>? Scopes { get; init; }

    /// <summary>
    /// Label of the summary row (defaults to the localized <see cref="BxToolsMessages.SummaryTotal"/> "Total" when a single scope is shown,
    /// or the localized scope name when multiple <see cref="Scopes"/> are shown).
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Optional asynchronous resolver returning a map of column key to aggregated value for a scope (for remote sources or custom async totals).
    /// Columns not present in the returned dictionary fall back to their declared <see cref="BxDataListColumn{TItem}.Summary"/> or <see cref="BxDataListColumn{TItem}.SummaryValue"/>.
    /// </summary>
    public Func<DataListSummaryResolveContext<TItem>, ValueTask<IReadOnlyDictionary<string, object?>>>? ResolveAsync { get; init; }

    internal IReadOnlyList<DataListSummaryScope> EffectiveScopes =>
        Scopes is { Count: > 0 } ? Scopes.Distinct().ToArray() : [Scope];
}

internal static class DataListSummaryCalculator
{
    public static object? Compute<TItem>(
        DataListSummaryKind kind,
        IReadOnlyList<TItem> rows,
        Func<TItem, object?> selector)
    {
        if (kind == DataListSummaryKind.None) return null;
        if (kind == DataListSummaryKind.Count)
        {
            var count = 0;
            foreach (var row in rows)
            {
                var v = selector(row);
                if (v is not null && (v is not string s || !string.IsNullOrWhiteSpace(s))) count++;
            }
            return count;
        }

        if (rows.Count == 0) return kind == DataListSummaryKind.Sum ? 0m : null;

        if (kind is DataListSummaryKind.Sum or DataListSummaryKind.Average)
        {
            var sum = 0m;
            var count = 0;
            foreach (var row in rows)
            {
                if (TryToDecimal(selector(row), out var d))
                {
                    sum += d;
                    count++;
                }
            }
            if (count == 0) return kind == DataListSummaryKind.Sum ? 0m : null;
            return kind == DataListSummaryKind.Sum ? sum : sum / count;
        }

        object? best = null;
        decimal? bestDecimal = null;
        foreach (var row in rows)
        {
            var val = selector(row);
            if (val is null || (val is string s && string.IsNullOrWhiteSpace(s))) continue;
            if (TryToDecimal(val, out var dec))
            {
                if (bestDecimal is null
                    || (kind == DataListSummaryKind.Min && dec < bestDecimal.Value)
                    || (kind == DataListSummaryKind.Max && dec > bestDecimal.Value))
                {
                    bestDecimal = dec;
                    best = val;
                }
            }
            else if (best is null)
            {
                best = val;
            }
            else if (val is IComparable cmp && best.GetType() == val.GetType())
            {
                var order = cmp.CompareTo(best);
                if ((kind == DataListSummaryKind.Min && order < 0) || (kind == DataListSummaryKind.Max && order > 0))
                {
                    best = val;
                }
            }
        }
        return best;
    }

    private static bool TryToDecimal(object? value, out decimal result)
    {
        switch (value)
        {
            case null:
                result = 0m;
                return false;
            case decimal d:
                result = d;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case short s:
                result = s;
                return true;
            case byte b:
                result = b;
                return true;
            case double db when !double.IsNaN(db) && !double.IsInfinity(db):
                result = (decimal)db;
                return true;
            case float fl when !float.IsNaN(fl) && !float.IsInfinity(fl):
                result = (decimal)fl;
                return true;
            case IConvertible conv:
                try
                {
                    result = conv.ToDecimal(CultureInfo.InvariantCulture);
                    return true;
                }
                catch
                {
                    result = 0m;
                    return false;
                }
            default:
                result = 0m;
                return false;
        }
    }
}
