namespace Blux.UI.Tools.Query;

/// <summary>
/// The state of a data list as the user left it: search, filters, sort, page, page size, layout and, for cursor paging, the cursor.
/// Immutable. <see cref="DataListStateQuery"/> converts it to and from URL query keys.
/// </summary>
public sealed record DataListState
{
    /// <summary>The highest page number read from a URL.</summary>
    public const int MaxPage = 1_000_000;

    /// <summary>Creates a state. Null lists are read as empty.</summary>
    public DataListState(
        string search,
        IReadOnlyList<FilterRule> filters,
        IReadOnlyList<DataListSort> sort,
        int page,
        int pageSize,
        DataListLayout layout,
        string? cursor = null)
    {
        Search = search ?? "";
        Filters = filters ?? [];
        Sort = sort ?? [];
        Page = page;
        PageSize = pageSize;
        Layout = layout;
        Cursor = cursor;
    }

    /// <summary>The search text (<c>q</c>).</summary>
    public string Search { get; init; }

    /// <summary>The filter rules, combined with AND (<c>f.&lt;key&gt;</c>).</summary>
    public IReadOnlyList<FilterRule> Filters { get; init; }

    /// <summary>The sort rules (<c>s</c>); the UI edits one.</summary>
    public IReadOnlyList<DataListSort> Sort { get; init; }

    /// <summary>The 1-based page (<c>p</c>).</summary>
    public int Page { get; init; }

    /// <summary>The page size (<c>ps</c>).</summary>
    public int PageSize { get; init; }

    /// <summary>The layout (<c>l</c>).</summary>
    public DataListLayout Layout { get; init; }

    /// <summary>The cursor of the current page in cursor mode (<c>c</c>), <see langword="null"/> on the first page.</summary>
    public string? Cursor { get; init; }

    /// <summary>The state with nothing applied: empty search, no filters or sort, page 1, 50 rows, table layout.</summary>
    public static DataListState Initial { get; } = new("", [], [], 1, 50, DataListLayout.Table);

    /// <summary>Builds the request for this state: offset paging at <see cref="Page"/>, or cursor paging with <see cref="Cursor"/> when <paramref name="cursorPaging"/> is set. <paramref name="timeZone"/> is the IANA id of the browser's time zone (<see cref="DataListRequest.TimeZone"/>); it comes from the client at request time, not from the state.</summary>
    public DataListRequest ToRequest(bool cursorPaging = false, IReadOnlyList<DataListFacetRequest>? facets = null, string? timeZone = null)
    {
        DataListPaging paging = cursorPaging
            ? new DataListPaging.Cursor(Cursor, Math.Max(1, PageSize), Count: true)
            : DataListPaging.Offset.ForPage(Page, PageSize);
        return new DataListRequest(
            paging,
            Sort,
            new DataListFilterGroup(FilterCombinator.And, [.. Filters]),
            string.IsNullOrWhiteSpace(Search) ? null : new DataListSearch(Search),
            facets,
            timeZone);
    }

    /// <inheritdoc />
    public bool Equals(DataListState? other) =>
        other is not null
        && Search == other.Search && Page == other.Page && PageSize == other.PageSize
        && Layout == other.Layout && Cursor == other.Cursor
        && SequenceEquality.Equal(Filters, other.Filters) && SequenceEquality.Equal(Sort, other.Sort);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Search, SequenceEquality.Hash(Filters), SequenceEquality.Hash(Sort), Page, PageSize, Layout, Cursor);
}
