namespace Blux.UI.Tools.Query;

/// <summary>
/// What a <see cref="DataListOptionsProvider"/> is asked for: one page of options for a search. Both kinds of paging are carried: a source that pages by position reads
/// <see cref="Skip"/> and <see cref="Take"/>, one that pages with an opaque token reads <see cref="Cursor"/>. The editor fills all of them.
/// </summary>
/// <param name="Search">The text typed in the search field of the editor, trimmed. Empty when nothing is typed: list the options unfiltered.</param>
/// <param name="Skip">How many options the earlier pages of this search returned (0 for the first page).</param>
/// <param name="Take">The number of options wanted.</param>
/// <param name="Cursor">The <see cref="DataListOptionsPage.NextCursor"/> of the previous page; <see langword="null"/> for the first page and for a source that does not use cursors.</param>
public sealed record DataListOptionsRequest(string Search, int Skip, int Take, string? Cursor);

/// <summary>One page of options loaded from a remote source.</summary>
/// <param name="Items">The options of this page, in the order to list them. A value that an earlier page of the same search already listed is skipped.</param>
/// <param name="HasMore">Whether another page exists. A non-null <see cref="NextCursor"/> also means there is one.</param>
/// <param name="NextCursor">The cursor that asks for the page after this one; <see langword="null"/> when the source pages by <c>Skip</c> and <c>Take</c> or this is the last page.</param>
public sealed record DataListOptionsPage(IReadOnlyList<DataListFilterOption> Items, bool HasMore = false, string? NextCursor = null);

/// <summary>
/// Loads the options of an option filter from a remote source, one page at a time. The token is cancelled when a newer search replaces the request (a late answer is ignored anyway).
/// An exception shows an error with a retry button in the editor.
/// </summary>
/// <param name="request">The search and the page wanted.</param>
/// <param name="cancellationToken">Cancelled when the request is stale.</param>
public delegate ValueTask<DataListOptionsPage> DataListOptionsProvider(DataListOptionsRequest request, CancellationToken cancellationToken);

/// <summary>
/// Returns the options of values that no loaded page holds (a value restored from the address, whose page was never loaded), so that a tag and an editor can show their labels. Values it cannot find
/// may be left out: they show as they are.
/// </summary>
/// <param name="values">The values to label.</param>
/// <param name="cancellationToken">Cancelled when the list goes away.</param>
public delegate ValueTask<IReadOnlyList<DataListFilterOption>> DataListOptionsResolver(IReadOnlyList<string> values, CancellationToken cancellationToken);

/// <summary>The remote source of the options of an option filter. See <see cref="DataListFilter.WithRemoteOptions"/>.</summary>
public sealed class DataListRemoteOptions
{
    /// <summary>The page size when none is given.</summary>
    public const int DefaultPageSize = 25;

    internal DataListRemoteOptions(DataListOptionsProvider provider, DataListOptionsResolver? resolver, int pageSize, int minLength)
    {
        Provider = provider;
        Resolver = resolver;
        PageSize = pageSize;
        MinLength = minLength;
    }

    /// <summary>Loads a page of options.</summary>
    public DataListOptionsProvider Provider { get; }

    /// <summary>Labels the values that no loaded page holds, or <see langword="null"/> when values show as they are.</summary>
    public DataListOptionsResolver? Resolver { get; }

    /// <summary>How many options each page asks for.</summary>
    public int PageSize { get; }

    /// <summary>The shortest search that is sent.</summary>
    public int MinLength { get; }
}
