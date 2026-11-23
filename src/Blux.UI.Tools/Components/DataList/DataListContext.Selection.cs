namespace Blux.UI.Tools;

public sealed partial class DataListContext<TItem>
{
    /// <summary>Raised when the ticked rows, or what a bulk action targets, change. Cheaper than <see cref="Changed"/> for a part that only draws a checkbox.</summary>
    public event Action? SelectionChanged
    {
        add => _owner.SelectionChanged += value;
        remove => _owner.SelectionChanged -= value;
    }

    /// <summary>The list has a selection: <c>Selectable</c> is on, or it has a <see cref="BxDataListSelectionActions{TItem}"/> bar, or <c>SelectedKeys</c> is bound.</summary>
    public bool SelectionEnabled => _owner.SelectionEnabled;

    /// <summary>The selection is drawn: it is enabled and the renderer can run handlers (or will, once a prerendered page turns interactive). A static render has no checkboxes.</summary>
    public bool SelectionVisible => _owner.SelectionVisible;

    /// <summary>The boxes cannot be used yet: the render is the prerender of a page that will be interactive.</summary>
    public bool SelectionDisabled => !IsInteractive;

    /// <summary>The keys of the ticked rows (<c>RowKey</c> of the schema). A set that later changes do not touch.</summary>
    public IReadOnlySet<string> SelectedKeys => _owner.SelectedKeySet;

    /// <summary>How many rows are ticked.</summary>
    public int SelectedCount => _owner.SelectedCount;

    /// <summary>The ticked rows that the list holds: those on screen and, for a list of rows in memory, those ticked on other pages.</summary>
    public IReadOnlyList<TItem> SelectedItems => _owner.SelectedRows;

    /// <summary>Whether a row is ticked.</summary>
    public bool IsSelected(TItem item) => _owner.IsKeySelected(_owner.KeyOf(item));

    /// <summary>The row key of a row, as text. Throws when the schema has no <c>RowKey</c>.</summary>
    public string RowKey(TItem item) => _owner.KeyOf(item);

    /// <summary>What a row action receives. A list without a row key identifies the row by its place on the page.</summary>
    internal DataListRowActionContext<TItem> CreateRowActionContext(TItem item, int position) =>
        new(this, item, Schema is { HasRowKey: true } ? _owner.KeyOf(item) : "#" + position, position, LifetimeToken);

    /// <summary>Whether a key is ticked.</summary>
    public bool IsKeySelected(string key) => _owner.IsKeySelected(key);

    /// <summary>
    /// The state of the select-all box for the rows on screen: <see langword="true"/> when all are ticked, <see langword="false"/> when none is (or there are no rows),
    /// <see langword="null"/> when only some are (the mixed state).
    /// </summary>
    public bool? PageSelection
    {
        get
        {
            var rows = _owner.SelectablePageRows();
            if (rows.Count == 0) return false;
            var ticked = 0;
            foreach (var row in rows)
            {
                if (_owner.IsKeySelected(_owner.KeyOf(row))) ticked++;
            }
            return ticked == 0 ? false : ticked == rows.Count ? true : null;
        }
    }

    /// <summary>
    /// Ticks or unticks a row. With <paramref name="range"/> (a click with Shift held) every row on screen between the row ticked last and this one gets the same state.
    /// Nothing happens in a renderer that is not interactive.
    /// </summary>
    public void SetSelected(TItem item, bool selected, bool range = false)
    {
        if (IsInteractive) _owner.SetRowSelected(item, selected, range);
    }

    /// <summary>Ticks or unticks the rows on screen.</summary>
    public void SetPageSelected(bool selected)
    {
        if (IsInteractive) _owner.SetPageSelected(selected);
    }

    /// <summary>
    /// The selection state of a visual row group: <see langword="true"/> when all rows in <paramref name="group"/> are ticked,
    /// <see langword="false"/> when none is (or the group is empty), and <see langword="null"/> when only some are ticked (indeterminate).
    /// </summary>
    public bool? GetGroupSelection(DataListGroup<TItem> group) =>
        _owner.GetGroupSelection(group.Items);

    /// <summary>Ticks or unticks every row in <paramref name="group"/>.</summary>
    public void SetGroupSelected(DataListGroup<TItem> group, bool selected)
    {
        if (IsInteractive) _owner.SetGroupSelected(group.Items, selected);
    }

    /// <summary>Unticks every row, and goes back to acting on the selection when the bulk scope was every result.</summary>
    public void ClearSelection()
    {
        if (IsInteractive) _owner.ClearSelection();
    }

    /// <summary>What a bulk action targets: the ticked rows, or every result of the query on screen.</summary>
    public DataListActionScope BulkScope => _owner.BulkScope;

    /// <summary>How many rows the search and the filters match, when the source says; <see langword="null"/> when it does not.</summary>
    public long? MatchingCount => _owner.MatchingCount;

    /// <summary>Whether the bulk scope can switch to every result: the source knows how many results there are and there are more than the ticked rows.</summary>
    public bool CanSelectAllResults => _owner.CanSelectAllResults;

    /// <summary>Sets what a bulk action targets. <see cref="DataListActionScope.All"/> needs <see cref="CanSelectAllResults"/>; it is dropped when the rows or the query change.</summary>
    public void SetBulkScope(DataListActionScope scope)
    {
        if (IsInteractive) _owner.SetBulkScope(scope);
    }

    internal void ClearSelection(IEnumerable<string> keys)
    {
        if (IsInteractive) _owner.ClearSelection(keys);
    }

    internal void AddSelectionPart() => _owner.AddSelectionPart();

    internal void RemoveSelectionPart() => _owner.RemoveSelectionPart();

    internal DataListActionContext<TItem> CreateActionContext(DataListActionScope scope, bool resolveAll, CancellationToken cancellationToken) =>
        _owner.CreateActionContext(scope, resolveAll, cancellationToken);
}
