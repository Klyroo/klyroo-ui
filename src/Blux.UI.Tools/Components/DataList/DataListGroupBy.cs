using System.Globalization;
using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tools;

/// <summary>
/// One visual group of rows produced by <see cref="DataListGroupBy{TItem}"/> over the loaded rows of a data list.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataListGroup<TItem>
{
    internal DataListGroup(
        string key,
        object? value,
        bool isEmptyValue,
        string label,
        string? summary,
        IReadOnlyList<TItem> items,
        IReadOnlyList<string> rowKeys,
        bool defaultCollapsed)
    {
        Key = key;
        Value = value;
        IsEmptyValue = isEmptyValue;
        Label = label;
        Summary = summary;
        Items = items;
        RowKeys = rowKeys;
        DefaultCollapsed = defaultCollapsed;
    }

    /// <summary>Stable identifier of the group (<c>"~empty"</c> when the group value is null or blank).</summary>
    public string Key { get; }

    /// <summary>The raw group value (<see langword="null"/> when empty).</summary>
    public object? Value { get; }

    /// <summary>Whether the group holds rows whose group field is <see langword="null"/> or blank.</summary>
    public bool IsEmptyValue { get; }

    /// <summary>The display label of the group header.</summary>
    public string Label { get; internal set; }

    /// <summary>The summary text shown beside the group label (the row count by default).</summary>
    public string? Summary { get; internal set; }

    /// <summary>The rows belonging to this group, in the active sort order.</summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <summary>The string keys of <see cref="Items"/> when the schema defines a <c>RowKey</c>; empty otherwise.</summary>
    public IReadOnlyList<string> RowKeys { get; }

    /// <summary>Whether the group starts folded before any user toggle.</summary>
    public bool DefaultCollapsed { get; internal set; }
}

/// <summary>
/// Visual grouping configuration for <see cref="BxDataListTable{TItem}"/> and <see cref="BxDataListGrid{TItem}"/>.
/// Groups are visual only: filtering, sorting, pagination and actions continue to operate on the flat rows, while rows on screen
/// are partitioned under collapsible <see cref="BxDataListGroupHeader{TItem}"/> headers with optional per-group selection checkboxes.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataListGroupBy<TItem>
{
    /// <summary>The stable key used for the group of rows whose value is <see langword="null"/> or empty.</summary>
    public const string EmptyGroupKey = "~empty";

    /// <summary>
    /// Key of the field in the schema to group rows by when <see cref="Key"/> is not provided.
    /// </summary>
    public string? Field { get; init; }

    /// <summary>
    /// Extracts the group value for a row. When <see langword="null"/>, reads <see cref="Field"/> from the schema.
    /// </summary>
    public Func<TItem, object?>? Key { get; init; }

    /// <summary>
    /// Explicit group values placed first, in this order. Remaining non-empty groups follow in ascending value order, and the empty group (<see langword="null"/> or blank) comes last.
    /// </summary>
    public IReadOnlyList<object?>? Order { get; init; }

    /// <summary>
    /// Formats the heading label of a group. When <see langword="null"/>, formats <see cref="DataListGroup{TItem}.Value"/> or uses the localized "Other" label (<see cref="BxToolsMessages.GroupEmpty"/>) for empty values.
    /// </summary>
    public Func<DataListGroup<TItem>, string?>? Label { get; init; }

    /// <summary>
    /// Custom template for the group heading label inside <see cref="BxDataListGroupHeader{TItem}"/>.
    /// </summary>
    public RenderFragment<DataListGroup<TItem>>? LabelTemplate { get; init; }

    /// <summary>
    /// Formats the summary badge shown beside the group label. When <see langword="null"/>, shows the number of rows in the group. Return an empty string to hide the badge.
    /// </summary>
    public Func<DataListGroup<TItem>, string?>? Summary { get; init; }

    /// <summary>
    /// Custom template for the summary badge shown beside the group label inside <see cref="BxDataListGroupHeader{TItem}"/>.
    /// </summary>
    public RenderFragment<DataListGroup<TItem>>? SummaryTemplate { get; init; }

    /// <summary>
    /// Whether groups start collapsed (folded) by default (<see langword="false"/> by default). Overridden by <see cref="DefaultCollapsed"/> when set.
    /// </summary>
    public bool Collapsed { get; init; }

    /// <summary>
    /// Optional per-group predicate determining whether a group starts collapsed (folded) by default.
    /// </summary>
    public Func<DataListGroup<TItem>, bool>? DefaultCollapsed { get; init; }

    internal IReadOnlyList<DataListGroup<TItem>> BuildGroups(
        IReadOnlyList<TItem> rows,
        DataListSchema<TItem>? schema,
        BxToolsMessages messages,
        CultureInfo culture)
    {
        if (rows.Count == 0) return [];

        var buckets = new Dictionary<(bool IsEmpty, string Token), Bucket>();
        var discoveryOrder = 0;

        foreach (var row in rows)
        {
            var raw = ExtractValue(row, schema);
            var (isEmpty, normalizedValue, token) = NormalizeValue(raw);
            var bucketKey = (isEmpty, token);
            if (!buckets.TryGetValue(bucketKey, out var bucket))
            {
                var groupKey = isEmpty ? EmptyGroupKey : (token == EmptyGroupKey ? "~val:" + token : token);
                bucket = new Bucket(groupKey, normalizedValue, isEmpty, discoveryOrder++);
                buckets[bucketKey] = bucket;
            }
            bucket.Items.Add(row);
            if (schema is { HasRowKey: true })
            {
                bucket.RowKeys.Add(schema.GetRowKey(row));
            }
        }

        var orderRanks = BuildOrderRanks();
        var sortedBuckets = buckets.Values.ToList();
        sortedBuckets.Sort((a, b) => CompareBuckets(a, b, orderRanks, culture));

        var result = new List<DataListGroup<TItem>>(sortedBuckets.Count);
        foreach (var bucket in sortedBuckets)
        {
            var defaultLabel = bucket.IsEmpty
                ? messages.GroupEmpty
                : FormatValue(bucket.Value, culture);
            var defaultSummary = bucket.Items.Count.ToString(culture);
            var group = new DataListGroup<TItem>(
                bucket.GroupKey,
                bucket.Value,
                bucket.IsEmpty,
                defaultLabel,
                defaultSummary,
                bucket.Items,
                bucket.RowKeys,
                Collapsed);

            if (Label is not null)
            {
                var customLabel = Label(group);
                if (!string.IsNullOrWhiteSpace(customLabel)) group.Label = customLabel;
            }
            if (Summary is not null)
            {
                group.Summary = Summary(group);
            }
            if (DefaultCollapsed is not null)
            {
                group.DefaultCollapsed = DefaultCollapsed(group);
            }
            result.Add(group);
        }

        return result;
    }

    private object? ExtractValue(TItem item, DataListSchema<TItem>? schema)
    {
        if (Key is not null) return Key(item);
        if (!string.IsNullOrWhiteSpace(Field) && schema is not null) return schema.ReadField(item, Field);
        return null;
    }

    private static (bool IsEmpty, object? Value, string Token) NormalizeValue(object? raw)
    {
        if (raw is null) return (true, null, "");
        if (raw is string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return (true, null, "");
            var trimmed = s.Trim();
            return (false, trimmed, trimmed);
        }
        var token = raw is IFormattable f
            ? f.ToString(null, CultureInfo.InvariantCulture)
            : raw.ToString() ?? "";
        return string.IsNullOrWhiteSpace(token)
            ? (true, null, "")
            : (false, raw, token);
    }

    private static string FormatValue(object? value, CultureInfo culture) =>
        value switch
        {
            null => "",
            string s => s,
            IFormattable f => f.ToString(null, culture),
            _ => value.ToString() ?? "",
        };

    private Dictionary<(bool IsEmpty, string Token), int>? BuildOrderRanks()
    {
        if (Order is not { Count: > 0 }) return null;
        var ranks = new Dictionary<(bool IsEmpty, string Token), int>();
        for (var i = 0; i < Order.Count; i++)
        {
            var (isEmpty, _, token) = NormalizeValue(Order[i]);
            ranks.TryAdd((isEmpty, token), i);
        }
        return ranks;
    }

    private static int CompareBuckets(
        Bucket a,
        Bucket b,
        Dictionary<(bool IsEmpty, string Token), int>? orderRanks,
        CultureInfo culture)
    {
        var keyA = (a.IsEmpty, a.IsEmpty ? "" : (a.Value is IFormattable fa ? fa.ToString(null, CultureInfo.InvariantCulture) : a.Value?.ToString() ?? ""));
        var keyB = (b.IsEmpty, b.IsEmpty ? "" : (b.Value is IFormattable fb ? fb.ToString(null, CultureInfo.InvariantCulture) : b.Value?.ToString() ?? ""));

        int rankA = 0, rankB = 0;
        var hasRankA = orderRanks is not null && orderRanks.TryGetValue(keyA, out rankA);
        var hasRankB = orderRanks is not null && orderRanks.TryGetValue(keyB, out rankB);
        if (hasRankA && hasRankB) return rankA.CompareTo(rankB);
        if (hasRankA) return -1;
        if (hasRankB) return 1;

        // Empty values always come last unless explicitly ordered above.
        if (a.IsEmpty != b.IsEmpty) return a.IsEmpty ? 1 : -1;
        if (a.IsEmpty) return 0;

        if (a.Value is not null && b.Value is not null && a.Value.GetType() == b.Value.GetType() && a.Value is IComparable comparable)
        {
            if (a.Value is not string)
            {
                var cmp = comparable.CompareTo(b.Value);
                if (cmp != 0) return cmp;
            }
        }

        var strA = FormatValue(a.Value, culture);
        var strB = FormatValue(b.Value, culture);
        var textCmp = culture.CompareInfo.Compare(strA, strB, CompareOptions.IgnoreCase | CompareOptions.NumericOrdering);
        return textCmp != 0 ? textCmp : a.DiscoveryIndex.CompareTo(b.DiscoveryIndex);
    }

    private sealed class Bucket(string groupKey, object? value, bool isEmpty, int discoveryIndex)
    {
        public string GroupKey { get; } = groupKey;
        public object? Value { get; } = value;
        public bool IsEmpty { get; } = isEmpty;
        public int DiscoveryIndex { get; } = discoveryIndex;
        public List<TItem> Items { get; } = [];
        public List<string> RowKeys { get; } = [];
    }
}
