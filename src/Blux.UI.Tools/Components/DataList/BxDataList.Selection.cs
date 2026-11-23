using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tools;

// Row selection: the ticked rows, which of them survive a new result, and what a bulk action targets. The rules (table.md 2.14) are:
// a list over a provider or a queryable keeps the ticked rows of the page on screen; a list of rows in memory keeps them across pages while the search and the filters still match them.
public partial class BxDataList<TItem>
{
    private readonly DataListSelection<TItem> _selection = new();
    private IReadOnlySet<string>? _lastSelectedKeysParam;
    private string? _anchorKey;
    private DataListActionScope _bulkScope;
    private DataListRequest? _bulkRequest;
    private DataListRequest? _reconciledRequest;
    private bool _selectionDirty;
    private int _reconciledVersion = -1;
    private int _selectionParts;

    /// <summary>
    /// Shows a checkbox column (a header box that selects the rows of the page, a box per row). <see langword="null"/>, the default, shows it when the list has a
    /// <see cref="BxDataListSelectionActions{TItem}"/> bar or <see cref="SelectedKeys"/> is bound; <see langword="false"/> turns selection off whatever else is there.
    /// Selection needs a <c>RowKey</c> in the schema and is interactive only: a statically rendered list has no checkbox column (a prerender of an interactive page shows the boxes disabled until the page is interactive).
    /// </summary>
    [Parameter] public bool? Selectable { get; set; }

    /// <summary>Whether the ticked rows stay ticked on other pages: see <see cref="DataListSelectionScope"/>.</summary>
    [Parameter] public DataListSelectionScope SelectionScope { get; set; }

    /// <summary>
    /// The keys (<c>RowKey</c> of the schema, as text) of the ticked rows. Bind it with <c>@bind-SelectedKeys</c> to read or set the selection; give a new set when you change it (the list compares references).
    /// Keys the list cannot keep (rows that are not on the page of a list over a provider, rows the search no longer matches) are dropped and reported through <see cref="SelectedKeysChanged"/>.
    /// </summary>
    [Parameter] public IReadOnlySet<string>? SelectedKeys { get; set; }

    /// <summary>Raised with a new set when the user ticks or unticks rows, or when rows leave the selection because a new result no longer holds them.</summary>
    [Parameter] public EventCallback<IReadOnlySet<string>> SelectedKeysChanged { get; set; }

    /// <summary>Raised on the renderer's thread when the ticked rows, or what a bulk action targets, change. The checkboxes of the rows listen to it instead of to every change of the list.</summary>
    internal event Action? SelectionChanged;

    internal bool SelectionEnabled => Selectable ?? (_selectionParts > 0 || SelectedKeys is not null || SelectedKeysChanged.HasDelegate);

    // Only a renderer that can run handlers selects. A prerender of a component with a render mode will be interactive: it keeps the column (disabled boxes) so that the page does not shift.
    internal bool SelectionWillBeInteractive => !RendererInfo.IsInteractive && AssignedRenderMode is not null;

    internal bool SelectionVisible => SelectionEnabled && (RendererInfo.IsInteractive || SelectionWillBeInteractive);

    internal bool SelectionAcrossPages => Items is not null && SelectionScope != DataListSelectionScope.Page;

    internal int SelectedCount => _selection.Count;

    internal IReadOnlySet<string> SelectedKeySet => _selection.Snapshot();

    internal IReadOnlyList<TItem> SelectedRows => _selection.Items();

    internal bool IsKeySelected(string key) => _selection.Contains(key);

    internal DataListActionScope BulkScope => _bulkScope;

    internal long? MatchingCount => Controller?.Result?.TotalCount;

    internal bool CanSelectAllResults => MatchingCount is { } total && total > _selection.Count;

    internal void AddSelectionPart()
    {
        _selectionParts++;
        _context.NotifyChanged();
    }

    internal void RemoveSelectionPart() => _selectionParts--;

    internal string KeyOf(TItem item)
    {
        if (_schema is not { HasRowKey: true })
        {
            throw new InvalidOperationException($"{nameof(BxDataList<TItem>)} selection needs a RowKey in the schema (RowKey(e => e.Id)): the keys of the selected rows come from it.");
        }
        return _schema.GetRowKey(item);
    }

    internal void SetRowSelected(TItem item, bool selected, bool range)
    {
        var key = KeyOf(item);
        bool changed;
        if (range && _anchorKey is not null && _anchorKey != key && RowSpan(_anchorKey, key) is { Count: > 0 } span)
        {
            changed = _selection.SetMany(span, selected);
        }
        else
        {
            changed = _selection.Set(key, item, selected);
        }
        _anchorKey = key;
        AfterUserChange(changed);
    }

    internal IReadOnlyList<TItem> SelectablePageRows()
    {
        if (_tableTree is not null && _context.EffectiveLayout == DataListLayout.Table)
        {
            var visibleNodes = VisibleTreeNodes();
            return _tableTree.Selectable == DataListTreeSelectionMode.Roots
                ? [.. visibleNodes.Where(n => n.Depth == 0).Select(n => n.Item)]
                : [.. visibleNodes.Select(n => n.Item)];
        }
        return Controller?.Rows ?? [];
    }

    internal void SetPageSelected(bool selected)
    {
        var rows = SelectablePageRows();
        var changed = _selection.SetMany(rows.Select(row => new KeyValuePair<string, TItem>(KeyOf(row), row)), selected);
        _anchorKey = null;
        AfterUserChange(changed);
    }

    internal bool? GetGroupSelection(IReadOnlyList<TItem> items)
    {
        if (items.Count == 0) return false;
        var ticked = 0;
        foreach (var row in items)
        {
            if (_selection.Contains(KeyOf(row))) ticked++;
        }
        return ticked == 0 ? false : ticked == items.Count ? true : null;
    }

    internal void SetGroupSelected(IReadOnlyList<TItem> items, bool selected)
    {
        var changed = _selection.SetMany(items.Select(row => new KeyValuePair<string, TItem>(KeyOf(row), row)), selected);
        _anchorKey = null;
        AfterUserChange(changed);
    }

    // Unticks only these keys: the rows an action targeted, not the ones ticked while it ran.
    internal void ClearSelection(IEnumerable<string> keys)
    {
        var changed = false;
        foreach (var key in keys) changed |= _selection.Remove(key);
        if (_anchorKey is not null && !_selection.Contains(_anchorKey)) _anchorKey = null;
        if (_selection.Count == 0) _bulkScope = DataListActionScope.Selection;
        AfterUserChange(changed);
    }

    internal void ClearSelection()
    {
        var changed = _selection.Clear();
        _anchorKey = null;
        AfterUserChange(changed);
    }

    internal void SetBulkScope(DataListActionScope scope)
    {
        if (scope == DataListActionScope.All && !CanSelectAllResults) scope = DataListActionScope.Selection;
        if (scope == _bulkScope) return;
        _bulkScope = scope;
        _bulkRequest = scope == DataListActionScope.All ? Controller?.LoadedRequest : null;
        SelectionChanged?.Invoke();
        _context.NotifyChanged();
    }

    // The keys in the order of the visible rows on screen (respecting group order and skipping collapsed groups), from the anchor of a shift-click to the row clicked.
    private List<KeyValuePair<string, TItem>>? RowSpan(string from, string to)
    {
        var rows = VisibleRowsForSelection();
        if (rows is null || rows.Count == 0) return null;
        var entries = rows.Select(row => new KeyValuePair<string, TItem>(KeyOf(row), row)).ToList();
        var a = entries.FindIndex(e => e.Key == from);
        var b = entries.FindIndex(e => e.Key == to);
        if (a < 0 || b < 0) return null;
        return entries.GetRange(Math.Min(a, b), Math.Abs(a - b) + 1);
    }

    private IReadOnlyList<TItem>? VisibleRowsForSelection()
    {
        var rows = Controller?.Rows;
        if (rows is null || rows.Count == 0) return rows;
        if (_tableTree is not null && _context.EffectiveLayout == DataListLayout.Table)
        {
            return SelectablePageRows();
        }
        if (ActiveGroupBy is null) return rows;
        var groups = BuildActiveGroups();
        var visible = new List<TItem>(rows.Count);
        foreach (var group in groups)
        {
            if (!IsGroupCollapsed(group.Key, group.DefaultCollapsed))
            {
                visible.AddRange(group.Items);
            }
        }
        return visible;
    }

    private void AfterUserChange(bool changed)
    {
        if (!changed) return;
        // What the user ticks is a deliberate choice of rows: "every result" no longer describes it.
        _bulkScope = DataListActionScope.Selection;
        _bulkRequest = null;
        if (_selection.Count == 0) _anchorKey = null;
        PublishSelection();
    }

    private void PublishSelection()
    {
        SelectionChanged?.Invoke();
        _context.NotifyChanged();
        if (SelectedKeysChanged.HasDelegate) _ = SelectedKeysChanged.InvokeAsync(_selection.Snapshot());
    }

    // Called with the parameters: a new set from the parent replaces the selection (its rows are found by the next load, or at once when the list already has a result).
    private void ApplySelectedKeysParameter()
    {
        if (SelectedKeys is null || ReferenceEquals(SelectedKeys, _lastSelectedKeysParam)) return;
        _lastSelectedKeysParam = SelectedKeys;
        if (!_selection.Replace(SelectedKeys)) return;
        _anchorKey = null;
        _bulkScope = DataListActionScope.Selection;
        _bulkRequest = null;
        _selectionDirty = true;
        ReconcileSelection();
        SelectionChanged?.Invoke();
        _context.NotifyChanged();
    }

    // The result changed (a page, a sort, a search, a filter, a refresh, new rows): the selection keeps only the rows it may still hold.
    private void ReconcileSelection()
    {
        var controller = Controller;
        // Mid-load the result on screen is the old one; the new one reconciles when it arrives.
        if (controller is not { Result: { } result, IsLoading: false }) return;
        var version = controller.ResultVersion;
        if (version == _reconciledVersion && !_selectionDirty) return;
        var request = controller.LoadedRequest;
        _reconciledVersion = version;
        if (_selection.Count == 0)
        {
            _selectionDirty = false;
            _reconciledRequest = request;
            if (_bulkScope != DataListActionScope.Selection)
            {
                _bulkScope = DataListActionScope.Selection;
                _bulkRequest = null;
            }
            return;
        }
        // A bulk action on "every result" means the results of the query it was chosen under.
        if (_bulkScope == DataListActionScope.All && (request is null || !SameQuery(request, _bulkRequest)))
        {
            _bulkScope = DataListActionScope.Selection;
            _bulkRequest = null;
        }

        bool removed;
        if (SelectionAcrossPages && request is not null)
        {
            if (!_selectionDirty && SameQuery(request, _reconciledRequest))
            {
                // Another page or sort of the same search and filters: the rows ticked elsewhere still match.
                removed = false;
            }
            else
            {
                var matching = ExecuteAll(request);
                removed = _selection.Retain(matching);
            }
        }
        else
        {
            var onPage = new Dictionary<string, TItem>(StringComparer.Ordinal);
            AddSelectableRowsToMap(result.Items, onPage);
            removed = _selection.Retain(onPage);
        }
        _reconciledRequest = request;
        _selectionDirty = false;
        if (removed)
        {
            if (_anchorKey is not null && !_selection.Contains(_anchorKey)) _anchorKey = null;
            if (_selection.Count == 0) _bulkScope = DataListActionScope.Selection;
            PublishSelectionAfterReconcile();
        }
    }

    // Reconciling runs inside a notification of the controller, which notifies the parts itself afterwards: only the listeners of the selection and the parent are told here.
    private void PublishSelectionAfterReconcile()
    {
        SelectionChanged?.Invoke();
        if (SelectedKeysChanged.HasDelegate) _ = SelectedKeysChanged.InvokeAsync(_selection.Snapshot());
    }

    private static bool SameQuery(DataListRequest a, DataListRequest? b) =>
        b is not null && a.Filter == b.Filter && a.Search == b.Search && a.TimeZone == b.TimeZone;

    // Every row a query matches in a list of rows in memory, by key; the page and the facets are not part of "matching".
    private Dictionary<string, TItem> ExecuteAll(DataListRequest request)
    {
        var all = _schema!.ExecuteInMemory(Items!, request with { Paging = DataListPaging.None.Instance, Facets = [] });
        var map = new Dictionary<string, TItem>(StringComparer.Ordinal);
        AddSelectableRowsToMap(all.Items, map);
        return map;
    }

    private void AddSelectableRowsToMap(IReadOnlyList<TItem> rootRows, Dictionary<string, TItem> map)
    {
        if (_tableTree is not null)
        {
            var (_, allNodes) = _tableTree.BuildTree(rootRows, _schema);
            foreach (var node in allNodes)
            {
                if (_tableTree.Selectable == DataListTreeSelectionMode.Roots && node.Depth > 0) continue;
                map[node.Key] = node.Item;
            }
            return;
        }
        foreach (var row in rootRows) map[KeyOf(row)] = row;
    }

    /// <summary>The request for every row the list matches: the one on screen without paging and facets.</summary>
    internal DataListRequest? AllResultsRequest => Controller?.LoadedRequest is { } request ? request with { Paging = DataListPaging.None.Instance, Facets = [] } : null;

    internal IReadOnlyList<TItem> FilteredRowsForSummary()
    {
        if (Items is not null && _schema is not null && Controller?.LoadedRequest is { } request)
        {
            return _schema.ExecuteInMemory(Items, request with { Paging = DataListPaging.None.Instance, Facets = [] }).Items;
        }
        if (Items is not null)
        {
            return [.. Items];
        }
        return Controller?.Rows ?? [];
    }

    // The rows an action receives: the ticked ones, or for "every result" of a list in memory the rows the query matches (a provider's rows never come to the client).
    internal DataListActionContext<TItem> CreateActionContext(DataListActionScope scope, bool resolveAll, CancellationToken cancellationToken)
    {
        IReadOnlyList<TItem> items;
        IReadOnlyList<string> keys;
        if (scope == DataListActionScope.All)
        {
            if (resolveAll && Items is not null && Controller?.LoadedRequest is { } request)
            {
                var matching = ExecuteAll(request);
                items = [.. matching.Values];
                keys = [.. matching.Keys];
            }
            else if (resolveAll)
            {
                items = [];
                keys = [];
            }
            else
            {
                items = _selection.Items();
                keys = [.. _selection.Keys];
            }
        }
        else
        {
            items = _selection.Items();
            keys = [.. _selection.Keys];
        }
        var target = scope == DataListActionScope.All && MatchingCount is { } total ? total : _selection.Count;
        return new DataListActionContext<TItem>(_context, scope, items, keys, MatchingCount, target, AllResultsRequest, cancellationToken);
    }

    // After the load of a refresh the rows may be other instances or gone: the selection checks them once more.
    internal void MarkSelectionStale() => _selectionDirty = true;

    private void OnControllerChanged()
    {
        ReconcileSelection();
        _context.NotifyChanged();
    }
}
