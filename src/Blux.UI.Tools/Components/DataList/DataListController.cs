using Blux.UI.Tools.Query;

namespace Blux.UI.Tools;

/// <summary>
/// Loads the rows of a data list: turns a <see cref="DataListState"/> into a request, runs it against the source, cancels the request a newer one
/// replaces and keeps the rows of the previous result while the next one loads. A plain class, so it is tested without a renderer; the
/// <see cref="BxDataList{TItem}"/> component owns one and publishes its flags to the parts through <see cref="DataListContext{TItem}"/>.
/// </summary>
/// <remarks>
/// Exactly one source feeds a list: <c>Items</c> (run in memory by <see cref="InMemoryDataList"/>), a <c>Queryable</c> (a database query, translated by
/// <see cref="QueryableDataList"/> and run by an <see cref="IDataListQueryExecutor"/>) or a <see cref="DataListProvider{T}"/>.
/// </remarks>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataListController<TItem> : IDisposable
{
    private CancellationTokenSource? _cts;
    private long _version;
    private bool _refreshing;
    private bool _dropFacets;
    private bool _disposed;
    private int _pagesLoaded = 1;

    /// <summary>Creates a controller.</summary>
    /// <param name="schema">What a request may ask for; also names the sortable fields.</param>
    /// <param name="provider">Where the rows come from, see <see cref="CreateProvider"/>.</param>
    public DataListController(DataListSchema<TItem> schema, DataListProvider<TItem> provider)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(provider);
        Schema = schema;
        Provider = provider;
    }

    /// <summary>The schema of the rows.</summary>
    public DataListSchema<TItem> Schema { get; }

    /// <summary>Where the rows come from. Replace it when the source of the list changes; the next load uses it.</summary>
    public DataListProvider<TItem> Provider { get; set; }

    /// <summary>The IANA id of the time zone of the user (<c>Europe/Paris</c>), sent with each request (<see cref="DataListRequest.TimeZone"/>). <see langword="null"/> means UTC.</summary>
    public string? TimeZone { get; set; }

    /// <summary>How requests page: numbered pages (<see cref="DataListPagingMode.Offset"/>), keyset cursors (<see cref="DataListPagingMode.Cursor"/>) or all matching rows (<see cref="DataListPagingMode.None"/>).</summary>
    public DataListPagingMode PagingMode { get; set; }

    /// <summary>Raised on whatever thread finished a load or started one, whenever a flag or the result changes. A component calls <c>InvokeAsync</c> before it renders.</summary>
    public event Action? Changed;

    /// <summary>The last result that loaded, kept while the next one loads. <see langword="null"/> until the first one.</summary>
    public DataListResult<TItem>? Result { get; private set; }

    /// <summary>The state the last result was loaded for: the requested one with the page and page size brought into range.</summary>
    public DataListState? LoadedState { get; private set; }

    /// <summary>The request the last result was loaded with.</summary>
    public DataListRequest? LoadedRequest { get; private set; }

    /// <summary>The state of the newest load, finished or not.</summary>
    public DataListState? RequestedState { get; private set; }

    /// <summary>Why the newest load failed; <see langword="null"/> when it did not. The rows of the previous result stay in <see cref="Rows"/>.</summary>
    public Exception? Error { get; private set; }

    /// <summary>Counts the results shown so far: it changes when a load or an adoption replaces <see cref="Result"/>, not when only a flag changes.</summary>
    public int ResultVersion { get; private set; }

    /// <summary>The rows of <see cref="Result"/>, empty before the first load.</summary>
    public IReadOnlyList<TItem> Rows => Result?.Items ?? [];

    /// <summary>A load is running.</summary>
    public bool IsLoading { get; private set; }

    /// <summary>No result yet and nothing failed: the first load is running or has not started. A table shows placeholder rows.</summary>
    public bool IsInitial => Result is null && Error is null;

    /// <summary>A load of the request that is already on screen is running (<see cref="RefreshAsync"/>).</summary>
    public bool IsRefreshing => IsLoading && _refreshing;

    /// <summary>A load of another request than the one on screen is running, with the previous rows still shown.</summary>
    public bool IsReplacing => IsLoading && !_refreshing && Result is not null;

    /// <summary>The next page of a cursor-paged list is being loaded (<see cref="LoadMoreAsync"/>) to append to <see cref="Rows"/>.</summary>
    public bool IsLoadingMore { get; private set; }

    /// <summary>Why the newest <see cref="LoadMoreAsync"/> failed; <see langword="null"/> when it did not. The previously loaded rows and <see cref="NextCursor"/> stay intact so retrying works.</summary>
    public Exception? LoadMoreError { get; private set; }

    /// <summary>The newest <see cref="LoadMoreAsync"/> failed.</summary>
    public bool HasLoadMoreError => LoadMoreError is not null;

    /// <summary>The opaque cursor of the next page (cursor paging only), or <see langword="null"/> after the last one.</summary>
    public string? NextCursor => PagingMode == DataListPagingMode.Cursor ? Result?.NextCursor : null;

    /// <summary>Whether there is another page to append in cursor mode.</summary>
    public bool HasMore => !string.IsNullOrEmpty(NextCursor);

    /// <summary>The newest load failed.</summary>
    public bool HasError => Error is not null;

    /// <summary>
    /// How many cursor pages <see cref="Rows"/> holds right now: 1 after a load, one more after each <see cref="LoadMoreAsync"/> that appended a page (cursor paging only).
    /// </summary>
    public int PagesLoaded => PagingMode == DataListPagingMode.Cursor ? _pagesLoaded : 1;

    /// <summary>
    /// The most cursor pages a <see cref="RefreshAsync"/> loads again (20 by default, at least 1). A refresh of a cursor-paged list asks for the pages that are on screen one after the other, starting from the first cursor, and replaces
    /// the rows with what they return, so the list stays where it was; when more pages than this were loaded, the first <see cref="MaxRefreshPages"/> come back and the list goes on from the cursor after them. This bounds the requests a refresh makes and the memory it
    /// holds (the old rows and the new ones for the time it runs).
    /// </summary>
    public int MaxRefreshPages { get; set; } = 20;

    /// <summary>
    /// The one source of a list from what the component was given.
    /// </summary>
    /// <param name="schema">The schema, used to run <paramref name="items"/>.</param>
    /// <param name="items">Rows in memory.</param>
    /// <param name="provider">A provider.</param>
    /// <exception cref="InvalidOperationException">None or both were given.</exception>
    /// <param name="queryable">A query of a database provider, run through <see cref="QueryableDataList"/> with <paramref name="executor"/>.</param>
    /// <param name="executor">Runs the queries of <paramref name="queryable"/>; the synchronous one when <see langword="null"/>.</param>
    public static DataListProvider<TItem> CreateProvider(
        DataListSchema<TItem> schema,
        IEnumerable<TItem>? items,
        DataListProvider<TItem>? provider,
        IQueryable<TItem>? queryable = null,
        IDataListQueryExecutor? executor = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        if ((items is not null ? 1 : 0) + (provider is not null ? 1 : 0) + (queryable is not null ? 1 : 0) > 1)
            throw new InvalidOperationException("A data list takes one source: give it Items, a Queryable or a Provider, not several.");
        if (provider is not null) return provider;
        if (queryable is not null) return DataListQueryableProvider.Create(schema, queryable, executor ?? SynchronousDataListQueryExecutor.Instance);
        if (items is not null) return schema.InMemoryProvider(items);
        throw new InvalidOperationException("A data list needs a source: give it Items, a Queryable or a Provider.");
    }

    /// <summary>
    /// Brings a state into the range the schema allows: a page of at least 1, a page size from 1 to <see cref="DataListSchema.MaxPageSize"/> (the default one for zero or less), a search without the spaces around it
    /// and no cursor unless the list pages by cursor (a stray <c>c</c> in the address of a list with numbered pages, or with no paging, is dropped).
    /// </summary>
    public DataListState Normalize(DataListState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var size = state.PageSize <= 0 ? Schema.DefaultPageSize : Math.Min(state.PageSize, Schema.MaxPageSize);
        var page = Math.Clamp(state.Page, 1, DataListState.MaxPage);
        var search = state.Search.Trim();
        var cursor = PagingMode == DataListPagingMode.Cursor ? state.Cursor : null;
        return size == state.PageSize && page == state.Page && search == state.Search && cursor == state.Cursor
            ? state
            : state with { Page = page, PageSize = size, Search = search, Cursor = cursor };
    }

    /// <summary>The request for a state: sanitized by the schema, carrying the default sort when the state has none and the time zone.</summary>
    public DataListRequest BuildRequest(DataListState state) => BuildRequest(state, afterCursor: null, includeCount: null);

    private DataListRequest BuildRequest(DataListState state, string? afterCursor, bool? includeCount)
    {
        var normalized = Normalize(state);
        var facets = FacetRequests();
        var raw = PagingMode switch
        {
            DataListPagingMode.None => normalized.ToRequest(cursorPaging: false, facets: facets, timeZone: TimeZone) with { Paging = DataListPaging.None.Instance },
            DataListPagingMode.Cursor => normalized.ToRequest(cursorPaging: true, facets: facets, timeZone: TimeZone) with
            {
                Paging = new DataListPaging.Cursor(afterCursor ?? normalized.Cursor, Math.Max(1, normalized.PageSize), Count: includeCount ?? true),
            },
            _ => normalized.ToRequest(cursorPaging: false, facets: facets, timeZone: TimeZone),
        };
        var request = Schema.Sanitize(raw);
        return request.Sort.Count == 0 && Schema.DefaultSort.Count > 0 ? request with { Sort = Schema.DefaultSort } : request;
    }

    // The option and boolean filters that ask for their counts (WithFacets): one facet request each, in the mode of the filter (without its own rule by default, so the other options stay visible).
    // Only an option filter counts the rows where the field is empty: its editor has an "include empty values" choice, a yes / no / any one has none.
    private IReadOnlyList<DataListFacetRequest>? FacetRequests()
    {
        List<DataListFacetRequest>? facets = null;
        foreach (var key in Schema.FilterKeys)
        {
            if (Schema.TryGetFilter(key, out var filter) && filter is { Kind: FilterKind.Option or FilterKind.Boolean, Facets: true } && !Schema.IsQueryFilter(key))
            {
                // A remote option filter lists a few counted options in a static render (and counts the values of the pages it loads through LoadFacetValuesAsync): not the whole facet.
                int? limit = filter.RemoteOptions is { } remote ? filter.OptionLimit ?? remote.PageSize : null;
                (facets ??= []).Add(new DataListFacetRequest(key, filter.FacetMode, limit, IncludeMissing: filter.Kind == FilterKind.Option));
            }
        }
        return facets;
    }

    // The facets of the result on screen when they still hold for a request: same filters, same search, same zone and the same facets asked for (a page or a sort does not change what a facet counts),
    // and the request is not a refresh. The provider is then not asked for them, so a database or an API does not count again on every page.
    private IReadOnlyList<DataListFacetResult>? ReusableFacets(DataListRequest request, bool refresh)
    {
        if (refresh || _dropFacets || request.Facets.Count == 0 || Result?.Facets is not { } facets || LoadedRequest is not { } loaded) return null;
        return loaded.Filter == request.Filter && loaded.Search == request.Search && loaded.TimeZone == request.TimeZone && SequenceEquality.Equal(loaded.Facets, request.Facets)
            ? facets
            : null;
    }

    /// <summary>
    /// Sets a result that was loaded elsewhere (the prerender), without calling the provider.
    /// </summary>
    /// <param name="state">The state it was requested for.</param>
    /// <param name="loaded">The state it was loaded for (the requested one with the page brought into range).</param>
    /// <param name="request">The request it was loaded with.</param>
    /// <param name="result">The result.</param>
    public void Adopt(DataListState state, DataListState loaded, DataListRequest request, DataListResult<TItem> result)
    {
        Interlocked.Increment(ref _version);
        Result = result;
        ResultVersion++;
        LoadedState = loaded;
        LoadedRequest = request;
        RequestedState = state;
        _dropFacets = false;
        Error = null;
        LoadMoreError = null;
        IsLoading = false;
        IsLoadingMore = false;
        _refreshing = false;
        _pagesLoaded = 1;
    }

    /// <summary>
    /// Updates the state when only <see cref="DataListState.Layout"/> changed: the rows on screen are the same in Table and Grid views, so the source is not queried again.
    /// </summary>
    internal bool TryAdoptLayout(DataListState state)
    {
        var normalized = Normalize(state);
        if (IsLoading || HasError || Result is null || LoadedState is not { } loaded) return false;
        if ((normalized with { Layout = loaded.Layout }) != loaded) return false;
        RequestedState = state;
        LoadedState = normalized;
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Loads the rows for a state. A load that is still running is cancelled and its result, should it arrive, is ignored. The rows of the previous
    /// result stay until this one replaces them; a failure keeps them and sets <see cref="Error"/>. A page past the last one is replaced by the
    /// last page when the total is known (read it back as <see cref="LoadedState"/>).
    /// </summary>
    /// <param name="state">The state to load.</param>
    /// <param name="refresh">Whether this is a reload of what is on screen (<see cref="IsRefreshing"/>).</param>
    public async Task LoadAsync(DataListState state, bool refresh = false)
    {
        ArgumentNullException.ThrowIfNull(state);
        ObjectDisposedException.ThrowIf(_disposed, this);
        var version = Interlocked.Increment(ref _version);
        var previous = _cts;
        var cts = _cts = new CancellationTokenSource();
        if (previous is not null)
        {
            try
            {
                previous.Cancel();
            }
            finally
            {
                previous.Dispose();
            }
        }

        RequestedState = state;
        _refreshing = refresh;
        IsLoading = true;
        IsLoadingMore = false;
        Error = null;
        LoadMoreError = null;
        Changed?.Invoke();

        try
        {
            var normalized = Normalize(state);
            var request = BuildRequest(normalized);
            var cached = ReusableFacets(request, refresh);
            var pages = 1;
            DataListResult<TItem> result;
            if (refresh && PagingMode == DataListPagingMode.Cursor && _pagesLoaded > 1 && Result is not null)
            {
                // A refresh of a list that grew keeps what is on screen: the pages are asked for again, one after the other.
                (result, pages) = await FetchLoadedPages(normalized, request, cts.Token).ConfigureAwait(true);
            }
            else
            {
                result = await Fetch(request, cached, cts.Token).ConfigureAwait(true);
            }
            if (version != Volatile.Read(ref _version)) return;

            if (request.Paging is DataListPaging.Offset { Skip: > 0 } offset && result.TotalCount is { } total && offset.Skip >= total)
            {
                // A page past the end (a typed URL, rows deleted since): the last page, in the same load.
                normalized = normalized with { Page = Math.Max(1, (total + offset.Take - 1) / offset.Take) };
                // The same filters and search: the counts of the first answer hold for the page that replaces it.
                var first = cached ?? (result.Facets is { Count: > 0 } && request.Facets.Count > 0 ? result.Facets : null);
                request = BuildRequest(normalized);
                result = await Fetch(request, first, cts.Token).ConfigureAwait(true);
                if (version != Volatile.Read(ref _version)) return;
            }

            Result = result;
            ResultVersion++;
            LoadedState = normalized;
            LoadedRequest = request;
            _pagesLoaded = pages;
            _dropFacets = false;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested || version != Volatile.Read(ref _version))
        {
            // Replaced by a newer load, or the list went away: the newer one reports.
            return;
        }
        catch (Exception exception)
        {
            if (version != Volatile.Read(ref _version)) return;
            Error = exception;
        }

        IsLoading = false;
        _refreshing = false;
        Changed?.Invoke();
    }

    // Asks the provider, without the facets when the ones on screen still hold (<paramref name="cached"/>), and gives them to the result. A request that went out without facets has
    // the cached ones in its result whatever the provider put in Facets (nothing, or an empty list: it was not asked to count).
    private async Task<DataListResult<TItem>> Fetch(DataListRequest request, IReadOnlyList<DataListFacetResult>? cached, CancellationToken cancellationToken)
    {
        var result = await Provider(cached is null ? request : request with { Facets = [] }, cancellationToken).ConfigureAwait(true);
        return cached is not null ? result with { Facets = cached } : result;
    }

    // The pages of a cursor-paged list that are on screen, asked for again from the first cursor (that of the state, when the list was entered mid-way): the first one the way a load asks for it (count and facets), the
    // others by the cursor each answer gives, up to MaxRefreshPages. A page that fails after the first one is not an error of the list: it goes back to the first page, which is a fresh start the user can scroll on from.
    private async Task<(DataListResult<TItem> Result, int Pages)> FetchLoadedPages(DataListState state, DataListRequest first, CancellationToken cancellationToken)
    {
        var wanted = Math.Clamp(_pagesLoaded, 1, Math.Max(1, MaxRefreshPages));
        var head = await Fetch(first, null, cancellationToken).ConfigureAwait(true);
        if (wanted == 1 || string.IsNullOrEmpty(head.NextCursor)) return (head, 1);
        var items = new List<TItem>(head.Items.Count * wanted);
        var keys = KeysOf(items);
        AppendDistinct(items, keys, head.Items);
        var last = head;
        var pages = 1;
        try
        {
            while (pages < wanted && !string.IsNullOrEmpty(last.NextCursor))
            {
                var page = await Provider(BuildRequest(state, afterCursor: last.NextCursor, includeCount: false) with { Facets = [] }, cancellationToken).ConfigureAwait(true);
                AppendDistinct(items, keys, page.Items);
                last = page;
                pages++;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return (head, 1);
        }
        return (new DataListResult<TItem>(items, head.TotalCount, last.NextCursor, head.Facets), pages);
    }

    // The keys of the rows already held (the row key of the schema), or null when the schema has none: rows then cannot be told apart and none is dropped.
    private HashSet<string>? KeysOf(IReadOnlyList<TItem> rows)
    {
        if (!Schema.HasRowKey) return null;
        var keys = new HashSet<string>(rows.Count, StringComparer.Ordinal);
        foreach (var row in rows) keys.Add(Schema.GetRowKey(row));
        return keys;
    }

    // Appends the rows of a page that are not on screen yet: a row that shows up on two pages (it moved between the requests) is kept where it first appeared.
    private void AppendDistinct(List<TItem> target, HashSet<string>? keys, IReadOnlyList<TItem> page)
    {
        if (keys is null)
        {
            target.AddRange(page);
            return;
        }
        foreach (var row in page)
        {
            if (keys.Add(Schema.GetRowKey(row))) target.Add(row);
        }
    }

    /// <summary>
    /// The counts of some values of one filter under the filters and search of the result on screen, for the options a remote editor has loaded. It asks the source for a one-row page of the
    /// current state with a facet scoped to <paramref name="values"/> (<see cref="DataListFacetRequest.Values"/>), and returns that facet; <see langword="null"/> when nothing loaded yet or the
    /// source returned no facet for the key.
    /// </summary>
    internal async Task<DataListFacetResult?> LoadFacetValuesAsync(string key, IReadOnlyList<string> values, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (LoadedRequest is null || LoadedState is not { } state || !Schema.TryGetFilter(key, out var filter)) return null;
        var mode = filter.FacetMode;
        var request = BuildRequest(state) with { Paging = new DataListPaging.Offset(0, 1), Facets = [new DataListFacetRequest(key, mode, Values: values)] };
        var result = await Provider(request, cancellationToken).ConfigureAwait(true);
        return result.Facets?.FirstOrDefault(f => f.Key == key);
    }

    /// <summary>Forgets the counts of the result on screen, so the next load asks for them again: the rows the list reads changed (new items, a new query), and the counts of the old ones no longer hold.</summary>
    internal void DropFacets() => _dropFacets = true;

    /// <summary>Sets the time zone of the request an adopted result stands for, once the browser told it (the prerender ran in UTC).</summary>
    public void SetLoadedTimeZone(string? timeZone)
    {
        if (LoadedRequest is { } request) LoadedRequest = request with { TimeZone = timeZone };
    }

    /// <summary>Loads the state of the newest load again: the retry after an error and the refresh button.</summary>
    public Task RefreshAsync() =>
        RequestedState is { } state ? LoadAsync(state, refresh: Result is not null) : Task.CompletedTask;

    /// <summary>
    /// Replaces a single row in the loaded <see cref="Result"/> in place by matching its schema <c>RowKey</c>, without querying the provider again.
    /// Returns <see langword="true"/> when a matching row was found and replaced.
    /// </summary>
    public bool UpdateRow(TItem item) => item is not null && UpdateRows([item]) > 0;

    /// <summary>
    /// Replaces matching rows in the loaded <see cref="Result"/> in place by their schema <c>RowKey</c>, without querying the provider again.
    /// Returns the number of rows replaced.
    /// </summary>
    public int UpdateRows(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (!Schema.HasRowKey)
            throw new InvalidOperationException($"{nameof(UpdateRows)} requires a RowKey in the schema.");
        if (Result is not { Items.Count: > 0 } current) return 0;

        var replacements = new Dictionary<string, TItem>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (item is null) continue;
            var key = Schema.GetRowKey(item);
            if (!string.IsNullOrEmpty(key)) replacements[key] = item;
        }
        if (replacements.Count == 0) return 0;

        var copy = new List<TItem>(current.Items.Count);
        var updated = 0;
        foreach (var existing in current.Items)
        {
            var key = Schema.GetRowKey(existing);
            if (replacements.TryGetValue(key, out var replacement))
            {
                copy.Add(replacement);
                updated++;
            }
            else
            {
                copy.Add(existing);
            }
        }
        if (updated == 0) return 0;
        Result = current with { Items = copy };
        ResultVersion++;
        Changed?.Invoke();
        return updated;
    }

    /// <summary>
    /// Appends the next page of a cursor-paged list using <see cref="NextCursor"/>. Does nothing outside <see cref="DataListPagingMode.Cursor"/>, while a load is running or when there
    /// is no next cursor. A newer <see cref="LoadAsync"/> cancels this load and ignores its result; a failure keeps the rows loaded so far and <see cref="NextCursor"/>, sets
    /// <see cref="LoadMoreError"/> and lets the caller retry.
    /// </summary>
    public async Task LoadMoreAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (PagingMode != DataListPagingMode.Cursor || IsLoading || IsLoadingMore || Result is not { } previous || LoadedState is not { } state || NextCursor is not { } cursor || cursor.Length == 0)
            return;

        var version = Interlocked.Increment(ref _version);
        var previousCts = _cts;
        var cts = _cts = new CancellationTokenSource();
        previousCts?.Dispose();

        IsLoadingMore = true;
        LoadMoreError = null;
        Changed?.Invoke();

        try
        {
            var request = BuildRequest(state, afterCursor: cursor, includeCount: false);
            // Facets already belong to the filter and search of the loaded result: appending a page does not ask the provider to recount them.
            var page = await Provider(request with { Facets = [] }, cts.Token).ConfigureAwait(true);
            if (version != Volatile.Read(ref _version)) return;

            // A row the source repeats on a later page (it moved while the user was scrolling, or the cursor overlaps) stays where it first appeared.
            var combined = new List<TItem>(previous.Items.Count + page.Items.Count);
            combined.AddRange(previous.Items);
            AppendDistinct(combined, KeysOf(previous.Items), page.Items);
            Result = new DataListResult<TItem>(combined, page.TotalCount ?? previous.TotalCount, page.NextCursor, previous.Facets ?? page.Facets);
            ResultVersion++;
            _pagesLoaded++;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested || version != Volatile.Read(ref _version))
        {
            return;
        }
        catch (Exception exception)
        {
            if (version != Volatile.Read(ref _version)) return;
            LoadMoreError = exception;
        }

        IsLoadingMore = false;
        Changed?.Invoke();
    }

    /// <summary>Cancels the load that is running, if any. Its result is ignored.</summary>
    public void Cancel()
    {
        Interlocked.Increment(ref _version);
        var cts = _cts;
        _cts = null;
        if (cts is null) return;
        try
        {
            cts.Cancel();
        }
        finally
        {
            cts.Dispose();
        }
        var wasBusy = IsLoading || IsLoadingMore;
        IsLoading = false;
        IsLoadingMore = false;
        _refreshing = false;
        if (wasBusy) Changed?.Invoke();
    }

    /// <summary>Cancels the running load. The controller cannot load afterwards.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Cancel();
    }
}
