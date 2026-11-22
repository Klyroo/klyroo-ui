using System.Text.Json.Serialization;

namespace Blux.UI.Tools.Query;

/// <summary>How a request pages: <see cref="Offset"/>, <see cref="Cursor"/> or <see cref="None"/>.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Offset), "offset")]
[JsonDerivedType(typeof(Cursor), "cursor")]
[JsonDerivedType(typeof(None), "none")]
public abstract record DataListPaging
{
    private protected DataListPaging() { }

    /// <summary>Skip a number of rows, then take a page.</summary>
    /// <param name="Skip">Rows to skip.</param>
    /// <param name="Take">Page size.</param>
    public sealed record Offset(int Skip, int Take) : DataListPaging
    {
        /// <summary>The 1-based page number: <c>Skip / Take + 1</c>.</summary>
        [JsonIgnore]
        public int Number => Take > 0 ? Skip / Take + 1 : 1;

        /// <summary>The offset of a 1-based page.</summary>
        public static Offset ForPage(int page, int pageSize)
        {
            var size = Math.Max(1, pageSize);
            var skip = (long)(Math.Max(1, page) - 1) * size;
            return new Offset((int)Math.Min(skip, int.MaxValue), size);
        }
    }

    /// <summary>Keyset paging: the page after an opaque cursor.</summary>
    /// <param name="After">The cursor of the previous page (<see cref="DataListResult{T}.NextCursor"/>), or <see langword="null"/> for the first page.</param>
    /// <param name="Take">Page size.</param>
    /// <param name="Count">Also compute <see cref="DataListResult{T}.TotalCount"/>.</param>
    public sealed record Cursor(string? After, int Take, bool Count = false) : DataListPaging;

    /// <summary>No paging: the whole result.</summary>
    public sealed record None : DataListPaging
    {
        /// <summary>The shared instance.</summary>
        public static None Instance { get; } = new();
    }
}

/// <summary>One sort rule.</summary>
/// <param name="Key">The key of a sortable field.</param>
/// <param name="Descending">Sort from the largest to the smallest. Nulls always sort last.</param>
public sealed record DataListSort(string Key, bool Descending = false);

/// <summary>The search box: a substring searched, ignoring case, in every search field of the schema.</summary>
/// <param name="Term">The text; blank matches everything.</param>
public sealed record DataListSearch(string Term);

/// <summary>Asks for the option counts of one filter.</summary>
/// <param name="Key">The key of a field with an option or boolean filter.</param>
/// <param name="Mode">Whether the facet's own filter applies.</param>
/// <param name="Limit">Maximum options returned; <see langword="null"/> for all.</param>
/// <param name="Skip">Options to skip (for loading more).</param>
/// <param name="Search">Keeps only the options whose value contains this text, ignoring case.</param>
/// <param name="IncludeMissing">Also count the rows where the field is empty.</param>
/// <param name="Values">
/// Counts only these values (as text, the way a rule carries them), whatever <see cref="DataListSchema.MaxFacetBuckets"/> says: a remote option filter asks for the values of the pages it has
/// loaded. A value that no row has is not in the result (a reader takes it as 0). <see langword="null"/> or empty counts every value.
/// </param>
public sealed record DataListFacetRequest(
    string Key,
    FacetMode Mode = FacetMode.ExcludeSelf,
    int? Limit = null,
    int Skip = 0,
    string? Search = null,
    bool IncludeMissing = false,
    IReadOnlyList<string>? Values = null)
{
    /// <summary>Value equality: the values are compared one by one, like the other lists of the model.</summary>
    public bool Equals(DataListFacetRequest? other) =>
        other is not null && Key == other.Key && Mode == other.Mode && Limit == other.Limit && Skip == other.Skip && Search == other.Search && IncludeMissing == other.IncludeMissing
        && (Values is null ? other.Values is null : other.Values is not null && SequenceEquality.Equal(Values, other.Values));

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Key, Mode, Limit, Skip, Search, IncludeMissing, Values is null ? 0 : SequenceEquality.Hash(Values));
}

/// <summary>What a data list asks a provider for: a page of rows under a sort, a filter tree and a search, plus facet counts.</summary>
public sealed record DataListRequest
{
    /// <summary>Creates a request. Null lists are read as empty and a null <paramref name="paging"/> as <see cref="DataListPaging.None"/>. <paramref name="timeZone"/> is the IANA id of the user's time zone, see <see cref="TimeZone"/>.</summary>
    public DataListRequest(
        DataListPaging paging,
        IReadOnlyList<DataListSort> sort,
        DataListFilterGroup filter,
        DataListSearch? search = null,
        IReadOnlyList<DataListFacetRequest>? facets = null,
        string? timeZone = null)
    {
        TimeZone = timeZone;
        Paging = paging ?? DataListPaging.None.Instance;
        Sort = sort ?? [];
        Filter = filter ?? DataListFilterGroup.Empty;
        Search = search;
        Facets = facets ?? [];
    }

    /// <summary>How to page.</summary>
    public DataListPaging Paging { get; init; }

    /// <summary>The sort rules, most significant first; empty uses the schema default.</summary>
    public IReadOnlyList<DataListSort> Sort { get; init; }

    /// <summary>The filter tree (rules and query-only rules).</summary>
    public DataListFilterGroup Filter { get; init; }

    /// <summary>The search, if any.</summary>
    public DataListSearch? Search { get; init; }

    /// <summary>The facets to count.</summary>
    public IReadOnlyList<DataListFacetRequest> Facets { get; init; }

    /// <summary>
    /// The IANA id of the user's time zone (<c>Europe/Paris</c>), sent by the client with each request; never part of the URL state. A <see cref="FilterValue.Date"/>
    /// on a <see cref="DateTimeOffset"/> or <see cref="DateTime"/> field means that day in this zone, and hour, minute and second units are cut in it.
    /// <see langword="null"/>, or an id the server does not know, means UTC (<see cref="DataListSchema.Sanitize(DataListRequest)"/> clears unknown ids).
    /// </summary>
    public string? TimeZone { get; init; }

    /// <summary>A request for everything: no paging, sort, filter, search or facets.</summary>
    public static DataListRequest Empty { get; } = new(DataListPaging.None.Instance, [], DataListFilterGroup.Empty);

    /// <inheritdoc />
    public bool Equals(DataListRequest? other) =>
        other is not null
        && Paging == other.Paging
        && SequenceEquality.Equal(Sort, other.Sort)
        && Filter == other.Filter
        && Search == other.Search
        && SequenceEquality.Equal(Facets, other.Facets)
        && TimeZone == other.TimeZone;

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Paging, SequenceEquality.Hash(Sort), Filter, Search, SequenceEquality.Hash(Facets), TimeZone);
}
