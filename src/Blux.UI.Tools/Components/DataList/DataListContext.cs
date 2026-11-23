using Blux.UI.Tools.Query;

namespace Blux.UI.Tools;

/// <summary>How a data list pages its rows.</summary>
public enum DataListPagingMode
{
    /// <summary>Numbered pages (<c>p</c>, <c>ps</c>) with <see cref="BxDataListPagination{TItem}"/>.</summary>
    Offset,

    /// <summary>Keyset cursor paging (<c>c</c>): pages append in place with <see cref="BxDataListInfiniteLoader{TItem}"/>, and degrade to a link in static rendering.</summary>
    Cursor,

    /// <summary>No paging: the source returns every matching row at once. Pairs with <see cref="BxDataListTable{TItem}.Virtualize"/>.</summary>
    None,
}

/// <summary>How tight the rows of a data list are.</summary>
public enum DataListDensity
{
    /// <summary>Normal rows.</summary>
    Default,

    /// <summary>Little padding, for long lists.</summary>
    Compact,

    /// <summary>Generous padding.</summary>
    Comfortable,
}

/// <summary>Horizontal alignment of a column.</summary>
public enum DataListAlign
{
    /// <summary>At the start of the line: left in a left-to-right language.</summary>
    Start,

    /// <summary>Centered.</summary>
    Center,

    /// <summary>At the end of the line, for numbers.</summary>
    End,
}

/// <summary>
/// What the parts of a <see cref="BxDataList{TItem}"/> (the table, the pagination, the result count...) read and change: the state of the list, its
/// rows and its loading flags. The list cascades one for its whole life (the value is fixed), so a part subscribes to <see cref="Changed"/> once and renders
/// again when it is raised. Write your own part by taking it as a cascading parameter.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed partial class DataListContext<TItem>
{
    private readonly BxDataList<TItem> _owner;

    internal DataListContext(BxDataList<TItem> owner) => _owner = owner;

    /// <summary>Raised when the state, the rows or a flag change. It can run on any thread: from a component, go through <c>InvokeAsync</c> before rendering.</summary>
    public event Action? Changed;

    internal void NotifyChanged() => Changed?.Invoke();

    internal Task EnsureLoadedAsync() => _owner.EnsureLoadedAsync();

    internal CancellationToken LifetimeToken => _owner.LifetimeToken;

    internal Microsoft.Extensions.Logging.ILogger? Logger => _owner.Logger;

    private readonly List<BxDataListColumn<TItem>> _columns = [];
    private int _columnIndex;

    /// <summary>
    /// Every column the list's table declares, visible or not (a columns panel lists them all), by <see cref="BxDataListColumn{TItem}.Order"/> and then in the order they first rendered:
    /// the order of the markup, except for a column that an <c>@if</c> adds later, which comes after the others unless it has an <c>Order</c>. Prefer <see cref="BxDataListColumn{TItem}.Visible"/> to an <c>@if</c>.
    /// </summary>
    public IReadOnlyList<BxDataListColumn<TItem>> Columns => _columns;

    internal string? ColumnsSignature { get; set; }

    internal int NextColumnIndex() => _columnIndex++;

    internal void AddColumn(BxDataListColumn<TItem> column)
    {
        if (!_columns.Contains(column)) _columns.Add(column);
        // Stable: equal orders keep the order of first rendering.
        _columns.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.RegistrationIndex.CompareTo(b.RegistrationIndex));
    }

    internal void RemoveColumn(BxDataListColumn<TItem> column) => _columns.Remove(column);

    /// <summary>The name of the list, for the accessible names of its table and its pagination (<c>Label</c> of the list).</summary>
    public string? Label => _owner.Label;

    /// <summary>Counts the results shown so far; it does not change when only a flag does, which lets rows skip a render.</summary>
    public int ResultVersion => _owner.Controller?.ResultVersion ?? 0;

    /// <summary>The schema, once the list has one (an explicit one, or the one its columns describe after they registered).</summary>
    public DataListSchema<TItem>? Schema => _owner.CurrentSchema;

    /// <summary>The state the list shows: search, filters, sort, page, page size. Immutable; change it with <see cref="SetState"/>.</summary>
    public DataListState State => _owner.CurrentState;

    /// <summary>The result on screen: the last one that loaded, kept while the next one loads. <see langword="null"/> before the first load.</summary>
    public DataListResult<TItem>? Result => _owner.Controller?.Result;

    /// <summary>The rows of <see cref="Result"/>; empty before the first load.</summary>
    public IReadOnlyList<TItem> Rows => _owner.Controller?.Rows ?? [];

    /// <summary>The state <see cref="Result"/> was loaded for: the page and page size of the rows on screen.</summary>
    public DataListState? LoadedState => _owner.Controller?.LoadedState;

    /// <summary>No result yet and nothing failed: show placeholders.</summary>
    public bool IsInitial => _owner.Controller?.IsInitial ?? true;

    /// <summary>A load is running.</summary>
    public bool IsLoading => _owner.Controller?.IsLoading ?? false;

    /// <summary>The rows on screen are being loaded again (refresh or retry).</summary>
    public bool IsRefreshing => _owner.Controller?.IsRefreshing ?? false;

    /// <summary>Other rows are being loaded (a new page, sort or filter) while the previous ones stay on screen.</summary>
    public bool IsReplacing => _owner.Controller?.IsReplacing ?? false;

    /// <summary>The next page of a cursor-paged list is being loaded to append to the rows on screen.</summary>
    public bool IsLoadingMore => _owner.Controller?.IsLoadingMore ?? false;

    /// <summary>Whether the source has another page after the rows loaded so far (cursor paging only).</summary>
    public bool HasMore => _owner.Controller?.HasMore ?? false;

    /// <summary>The opaque cursor for the next page (cursor paging only), or <see langword="null"/> after the last one.</summary>
    public string? NextCursor => _owner.Controller?.NextCursor;

    /// <summary>The newest load-more request failed while the previously loaded rows stayed on screen.</summary>
    public bool HasLoadMoreError => _owner.Controller?.HasLoadMoreError ?? false;

    /// <summary>Why the newest load-more request failed; <see langword="null"/> when it did not.</summary>
    public Exception? LoadMoreError => _owner.Controller?.LoadMoreError;

    /// <summary>How the list pages its rows.</summary>
    public DataListPagingMode PagingMode => _owner.PagingMode;

    /// <summary>The newest load failed. The previous rows, if any, are still in <see cref="Rows"/>.</summary>
    public bool HasError => _owner.Controller?.HasError ?? false;

    /// <summary>Why the newest load failed.</summary>
    public Exception? Error => _owner.Controller?.Error;

    /// <summary>The renderer can run handlers: controls are buttons. Otherwise (static rendering, prerendering) they are links and forms when <see cref="CanLink"/>.</summary>
    public bool IsInteractive => _owner.IsInteractive;

    /// <summary>The state lives in the URL and the renderer is not interactive: controls are links (<see cref="HrefWith"/>) and GET forms.</summary>
    public bool CanLink => _owner.CanLink;

    /// <summary>The rows have a search or filters applied, so an empty result means "nothing matches" rather than "nothing exists".</summary>
    public bool IsFiltered => _owner.IsFiltered;

    /// <summary>Whether <paramref name="state"/> has a search or filters applied: <see cref="IsFiltered"/> for the state the rows on screen were loaded for.</summary>
    public bool IsFilteredFor(DataListState state) => _owner.IsFilteredFor(state);

    /// <summary>The density of the rows.</summary>
    public DataListDensity Density => _owner.Density;

    /// <summary>The key prefix of the URL (<c>UrlKey</c>), or <see langword="null"/>.</summary>
    public string? UrlKey => _owner.NormalizedUrlKey;

    /// <summary>
    /// Changes the state: <paramref name="change"/> receives the current one and returns the new one. With URL sync the change is written to the URL
    /// (<paramref name="history"/> says whether it adds an entry: push for a page or a sort, replace for typing) and the list follows the URL; without it
    /// the list keeps the state itself and reports it through <c>StateChanged</c>. Nothing happens in a renderer that is not interactive: use <see cref="HrefWith"/>.
    /// </summary>
    public void SetState(Func<DataListState, DataListState> change, QueryHistory history = QueryHistory.Push) => _owner.SetState(change, history);

    /// <summary>
    /// Takes the search and the filters off and goes back to the first page (the sort, the page size and the layout stay), with a history entry by default.
    /// Nothing happens in a renderer that is not interactive: link to <see cref="HrefWith"/> with <see cref="ResetState"/>.
    /// </summary>
    public void Reset(QueryHistory history = QueryHistory.Push) => SetState(_owner.ResetState, history);

    /// <summary><paramref name="state"/> without its search and its filters (those of the schema's default state), on the first page: what <see cref="Reset"/> sets.</summary>
    public DataListState ResetState(DataListState state) => _owner.ResetState(state);

    /// <summary>Raised when something asks for the focus of the search field of the list (the empty state's "Clear search" action, whose own button is gone once it worked).</summary>
    internal event Action? SearchFocusRequested;

    internal void RequestSearchFocus() => SearchFocusRequested?.Invoke();

    /// <summary>The URL of the current page with the state <paramref name="change"/> returns written in it, for a link of a static render; <see langword="null"/> when the list is not synced with the URL.</summary>
    public string? HrefWith(Func<DataListState, DataListState> change) => _owner.HrefWith(change);

    /// <summary>
    /// The fields of a GET form that submits <paramref name="state"/>: every parameter of the URL that is not the list's own, then the list's own for the state, so submitting
    /// the form does not drop anything. Keys the form supplies itself (<paramref name="exclude"/>, names relative to <see cref="UrlKey"/>: <c>ps</c>, <c>p</c>) are left out.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> FormFields(DataListState state, params string[] exclude) => _owner.FormFields(state, exclude);

    /// <summary>The name of a form field for a key of the list (<c>ps</c> becomes <c>users.ps</c> with <c>UrlKey="users"</c>).</summary>
    public string FieldName(string key) => string.IsNullOrEmpty(UrlKey) ? key : UrlKey + "." + key;

    /// <summary>The path of the current page, the action of a GET form.</summary>
    public string Path => _owner.CurrentPath;

    /// <summary>Loads the rows on screen again, or the failed request again after an error.</summary>
    public Task RefreshAsync() => _owner.RefreshAsync();

    /// <summary>Appends the next page of a cursor-paged list. Does nothing outside cursor mode, while a load is running, when there is no next cursor, or when the renderer is not interactive.</summary>
    public Task LoadMoreAsync() => _owner.LoadMoreAsync();

    /// <summary>The sort that applies: the state's, or the schema's default one when the state has none.</summary>
    public IReadOnlyList<DataListSort> EffectiveSort =>
        State.Sort.Count > 0 ? State.Sort : Schema?.DefaultSort ?? [];

    /// <summary>The sort of a key as it stands: <see langword="null"/> when the list is not sorted by it, else whether it is descending.</summary>
    public bool? SortDescending(string key) =>
        EffectiveSort.Count > 0 && EffectiveSort[0].Key == key ? EffectiveSort[0].Descending : null;

    /// <summary>
    /// The state after a click on the sort control of <paramref name="key"/>: ascending, then descending, then back to the default sort. The page goes back to the first.
    /// </summary>
    public DataListState NextSort(DataListState state, string key)
    {
        var current = (state.Sort.Count > 0 ? state.Sort : Schema?.DefaultSort ?? [])
            is { Count: > 0 } sort && sort[0].Key == key ? sort[0].Descending : (bool?)null;
        IReadOnlyList<DataListSort> next = current switch
        {
            null => [new DataListSort(key)],
            false => [new DataListSort(key, true)],
            true => Schema?.DefaultSort ?? [],
        };
        return state with { Sort = next, Page = 1, Cursor = null };
    }
}
