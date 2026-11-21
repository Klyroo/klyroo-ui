namespace Blux.UI.Tools.Query;

/// <summary>One option of a facet and the number of rows that have it.</summary>
/// <param name="Value">The value as text: invariant culture, <c>true</c>/<c>false</c>, enum name, ISO dates.</param>
/// <param name="Count">The row count.</param>
public sealed record DataListFacetOption(string Value, int Count);

/// <summary>The counts of one facet.</summary>
public sealed record DataListFacetResult
{
    /// <summary>Creates a facet result.</summary>
    public DataListFacetResult(string key, IReadOnlyList<DataListFacetOption> options, int? missing = null, int total = 0)
    {
        Key = key;
        Options = options ?? [];
        Missing = missing;
        Total = total;
    }

    /// <summary>The key of the filter.</summary>
    public string Key { get; init; }

    /// <summary>The options, by count descending then value (ordinal), after <c>Search</c>, <c>Skip</c> and <c>Limit</c>.</summary>
    public IReadOnlyList<DataListFacetOption> Options { get; init; }

    /// <summary>Rows where the field is empty, when requested.</summary>
    public int? Missing { get; init; }

    /// <summary>The number of distinct options before <c>Skip</c> and <c>Limit</c> (after <c>Search</c>); more are available when <c>Skip + Options.Count &lt; Total</c>.</summary>
    public int Total { get; init; }

    /// <summary>The facet has more distinct values than <see cref="DataListSchema.MaxFacetBuckets"/>: the options are the ones with the highest counts, and the others are not listed or counted in <see cref="Total"/>. A UI can say "more values".</summary>
    public bool Truncated { get; init; }

    /// <inheritdoc />
    public bool Equals(DataListFacetResult? other) =>
        other is not null && Key == other.Key && Missing == other.Missing && Total == other.Total && Truncated == other.Truncated
        && SequenceEquality.Equal(Options, other.Options);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Key, Missing, Total, Truncated, SequenceEquality.Hash(Options));
}

/// <summary>A page of rows and what goes with it.</summary>
/// <typeparam name="T">The row type.</typeparam>
/// <param name="Items">The rows of the page.</param>
/// <param name="TotalCount">Rows matching the filter and search across all pages; <see langword="null"/> when not computed.</param>
/// <param name="NextCursor">The cursor of the next page, or <see langword="null"/> after the last one (cursor paging only).</param>
/// <param name="Facets">The requested facet counts.</param>
public sealed record DataListResult<T>(
    IReadOnlyList<T> Items,
    int? TotalCount = null,
    string? NextCursor = null,
    IReadOnlyList<DataListFacetResult>? Facets = null)
{
    /// <summary>Value equality: the items are compared one by one with their own equality, like the other lists of the model.</summary>
    public bool Equals(DataListResult<T>? other) =>
        other is not null && TotalCount == other.TotalCount && NextCursor == other.NextCursor
        && SequenceEquality.Equal(Items, other.Items)
        && (Facets is null ? other.Facets is null : other.Facets is not null && SequenceEquality.Equal(Facets, other.Facets));

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(TotalCount, NextCursor, SequenceEquality.Hash(Items), Facets is null ? 0 : SequenceEquality.Hash(Facets));
}

/// <summary>Loads a page of rows for a request. The same shape for an in-memory list, an EF query or an HTTP call.</summary>
/// <typeparam name="T">The row type.</typeparam>
/// <param name="request">What to load.</param>
/// <param name="cancellationToken">Cancels a stale request.</param>
public delegate ValueTask<DataListResult<T>> DataListProvider<T>(DataListRequest request, CancellationToken cancellationToken);
