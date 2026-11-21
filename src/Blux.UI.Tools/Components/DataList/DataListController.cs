using Blux.UI.Tools.Query;

namespace Blux.UI.Tools;

/// <summary>
/// Loads the rows of a data list: turns a <see cref="DataListState"/> into a request, runs it against the source, cancels the request a newer one
/// replaces and keeps the rows of the previous result while the next one loads. A plain class, so it is tested without a renderer; the
/// <see cref="BxDataList{TItem}"/> component owns one and publishes its flags to the parts through <see cref="DataListContext{TItem}"/>.
/// </summary>
/// <remarks>
/// Exactly one source feeds a list: <c>Items</c> (run in memory by <see cref="InMemoryDataList"/>) or a <see cref="DataListProvider{T}"/>.
/// <c>Queryable</c> is not a source yet: the applier that turns a request into a query on an <c>IQueryable</c> is being built in
/// <c>Blux.UI.Tools.Query</c>, and running a query through the in-memory path would pull the whole table into memory.
/// </remarks>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataListController<TItem> : IDisposable
{
    private CancellationTokenSource? _cts;
    private long _version;
    private bool _refreshing;
    private bool _disposed;

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

    /// <summary>The newest load failed.</summary>
    public bool HasError => Error is not null;

    /// <summary>
    /// The one source of a list from what the component was given.
    /// </summary>
    /// <param name="schema">The schema, used to run <paramref name="items"/>.</param>
    /// <param name="items">Rows in memory.</param>
    /// <param name="provider">A provider.</param>
    /// <exception cref="InvalidOperationException">None or both were given.</exception>
    public static DataListProvider<TItem> CreateProvider(DataListSchema<TItem> schema, IEnumerable<TItem>? items, DataListProvider<TItem>? provider)
    {
        ArgumentNullException.ThrowIfNull(schema);
        if (items is not null && provider is not null)
            throw new InvalidOperationException("A data list takes one source: give it Items or a Provider, not both.");
        if (provider is not null) return provider;
        // TODO(Queryable): when the IQueryable applier of Blux.UI.Tools.Query lands, a third source runs the request on the query and the provider here goes through it.
        if (items is not null) return schema.InMemoryProvider(items);
        throw new InvalidOperationException("A data list needs a source: give it Items or a Provider.");
    }

    /// <summary>Brings a state into the range the schema allows: a page of at least 1 and a page size from 1 to <see cref="DataListSchema.MaxPageSize"/> (the default one for zero or less).</summary>
    public DataListState Normalize(DataListState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var size = state.PageSize <= 0 ? Schema.DefaultPageSize : Math.Min(state.PageSize, Schema.MaxPageSize);
        var page = Math.Clamp(state.Page, 1, DataListState.MaxPage);
        return size == state.PageSize && page == state.Page ? state : state with { Page = page, PageSize = size };
    }

    /// <summary>The request for a state: sanitized by the schema, carrying the default sort when the state has none and the time zone.</summary>
    public DataListRequest BuildRequest(DataListState state)
    {
        var request = Schema.Sanitize(Normalize(state).ToRequest(timeZone: TimeZone));
        return request.Sort.Count == 0 && Schema.DefaultSort.Count > 0 ? request with { Sort = Schema.DefaultSort } : request;
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
        LoadedState = loaded;
        LoadedRequest = request;
        RequestedState = state;
        Error = null;
        IsLoading = false;
        _refreshing = false;
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
        Error = null;
        Changed?.Invoke();

        try
        {
            var normalized = Normalize(state);
            var request = BuildRequest(normalized);
            var result = await Provider(request, cts.Token).ConfigureAwait(true);
            if (version != Volatile.Read(ref _version)) return;

            if (request.Paging is DataListPaging.Offset { Skip: > 0 } offset && result.TotalCount is { } total && offset.Skip >= total)
            {
                // A page past the end (a typed URL, rows deleted since): the last page, in the same load.
                normalized = normalized with { Page = Math.Max(1, (total + offset.Take - 1) / offset.Take) };
                request = BuildRequest(normalized);
                result = await Provider(request, cts.Token).ConfigureAwait(true);
                if (version != Volatile.Read(ref _version)) return;
            }

            Result = result;
            LoadedState = normalized;
            LoadedRequest = request;
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

    /// <summary>Loads the state of the newest load again: the retry after an error and the refresh button.</summary>
    public Task RefreshAsync() =>
        RequestedState is { } state ? LoadAsync(state, refresh: Result is not null) : Task.CompletedTask;

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
        IsLoading = false;
        _refreshing = false;
    }

    /// <summary>Cancels the running load. The controller cannot load afterwards.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Cancel();
    }
}
