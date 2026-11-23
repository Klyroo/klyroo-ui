using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Blux.UI.Tools;

// Column preferences: visibility, order, pinning and pixel widths. With a PreferencesKey they live in a cookie (a localStorage entry in a Blazor Hybrid web view), so a static render or a
// prerender reads them from the request and paints the user's columns at once; an interactive render takes them from the prerender (persistent state) or from the browser.
public partial class BxDataList<TItem>
{
    /// <summary>The width a pinned column gets when nobody gave it one: the offsets of the pinned columns need every width.</summary>
    internal const int DefaultPinnedWidth = 150;

    private DataListColumnPreferences _columnPrefs = new();
    private int _columnsVersion;
    private bool _columnPrefsResolved;
    private PersistentComponentState? _columnsPersistent;
    private PersistingComponentStateSubscription? _columnsPersistSubscription;
    private string? _claimedPreferencesKey;
    private bool _columnsOverflowLogged;
    private bool _hasRowSummary;
    private bool _rowSummaryDefaultActive;
    private bool _rowSummaryAutoPhone = true;
    private bool? _phoneSummaryDetected;
    private DataListGroupBy<TItem>? _tableGroupBy;
    private DataListGroupBy<TItem>? _gridGroupBy;
    private DataListTree<TItem>? _tableTree;
    private bool? _treeBaselineOverride;
    private readonly Dictionary<string, bool> _treeExpandedOverrides = new(StringComparer.Ordinal);

    /// <summary>
    /// Name under which column visibility, order, pinning and widths are saved in the grouped cookie <c>blux-dl-cols</c> (scoped to the path base of the app like the other Blux cookies, SameSite=Lax, a year,
    /// capped at 3,000 characters across up to 32 lists with least-recently-updated eviction, and 600 characters per list), or a localStorage entry of the same name in a Blazor Hybrid web view (MAUI),
    /// where there is no server to read a cookie. Statically rendered and prerendered lists read it from the request when the server calls <c>AddBluxServer()</c> (package <c>Blux.UI.Server</c>), so the first
    /// paint already has the user's columns; an interactive list takes it from the prerender or from the browser. Only letters, digits, <c>_</c> and <c>-</c> are kept (64 characters at most). Without a key
    /// the preferences stay in memory for the life of the component. Never part of the URL.
    /// </summary>
    [Parameter] public string? PreferencesKey { get; set; }

    internal string? NormalizedPreferencesKey => DataListColumnPreferences.NormalizeKey(PreferencesKey);

    internal bool WillBeInteractive => !RendererInfo.IsInteractive && AssignedRenderMode is not null;

    internal int ColumnsVersion => _columnsVersion;

    /// <summary>The last change made through the column panel, as a sentence for a live region; empty before any.</summary>
    internal string ColumnAnnouncement { get; private set; } = "";

    /// <summary>The last width set by resizing a column, as a sentence for a live region of the table.</summary>
    internal string WidthAnnouncement { get; private set; } = "";

    private string? ColumnsPersistKey => NormalizedPreferencesKey is { } key ? "bx-dl-cols:" + key : null;

    // What the preferences amount to for the columns that exist: keys of columns that were removed, or that a hand-edited cookie invented, do not count and do not travel.
    private DataListColumnPreferences PrunedPreferences()
    {
        var clone = _columnPrefs.Clone();
        if (_context.Columns.Count > 0)
        {
            var known = new HashSet<string>(StringComparer.Ordinal);
            var required = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in _context.Columns)
            {
                if (column.ResolvedKey is not { } key) continue;
                known.Add(key);
                if (column.Required) required.Add(key);
            }
            clone.Prune(known, required);
            // What equals the declared defaults is not a preference: the cookie holds only what the user changed.
            var declared = _context.Columns.Where(c => c.ResolvedKey is not null).ToList();
            if (clone.Order.SequenceEqual(declared.Select(c => c.ResolvedKey!), StringComparer.Ordinal)) clone.Order.Clear();
            foreach (var column in declared)
            {
                var key = column.ResolvedKey!;
                if (column.Visible) clone.Shown.Remove(key);
                else clone.Hidden.Remove(key);
                if (column.Pinned == DataListPin.Start) clone.PinnedStart.Remove(key);
                if (column.Pinned == DataListPin.End) clone.PinnedEnd.Remove(key);
                if (column.Pinned == DataListPin.None) clone.Unpinned.Remove(key);
                var (min, max) = WidthLimits(column);
                if (clone.Widths.TryGetValue(key, out var width) && column.Width is { } declaredWidth && Math.Clamp(declaredWidth, min, max) == Math.Clamp(width, min, max)) clone.Widths.Remove(key);
            }
        }
        else
        {
            clone.ToggledGroups.RemoveWhere(k => k.Length is 0 or > DataListColumnPreferences.MaxKeyLength);
        }
        return clone;
    }

    internal bool HasCustomColumnPreferences => PrunedPreferences().HasColumnOverrides;

    internal DataListTree<TItem>? TableTree => _tableTree;

    internal bool HasTree => _tableTree is not null;

    internal void SetTableTree(DataListTree<TItem>? tree)
    {
        if (ReferenceEquals(_tableTree, tree)) return;
        _tableTree = tree;
        if (tree is null)
        {
            _treeBaselineOverride = null;
            _treeExpandedOverrides.Clear();
        }
        _context.NotifyChanged();
    }

    internal bool IsTreeNodeExpanded(string key, int depth)
    {
        if (_tableTree is null || string.IsNullOrEmpty(key)) return false;
        if (_treeExpandedOverrides.TryGetValue(key, out var overridden)) return overridden;
        if (_treeBaselineOverride is { } baseline) return baseline;
        return _tableTree.IsDefaultExpanded(depth);
    }

    internal void ToggleTreeNodeExpanded(string key, int depth)
    {
        if (_tableTree is null || string.IsNullOrEmpty(key)) return;
        var current = IsTreeNodeExpanded(key, depth);
        _treeExpandedOverrides[key] = !current;
        _columnsVersion++;
        _context.NotifyChanged();
    }

    internal void SetTreeNodeExpanded(string key, int depth, bool expanded)
    {
        if (_tableTree is null || string.IsNullOrEmpty(key)) return;
        if (IsTreeNodeExpanded(key, depth) == expanded && _treeExpandedOverrides.TryGetValue(key, out var existing) && existing == expanded) return;
        _treeExpandedOverrides[key] = expanded;
        _columnsVersion++;
        _context.NotifyChanged();
    }

    internal void ExpandAllTreeNodes()
    {
        if (_tableTree is null) return;
        _treeBaselineOverride = true;
        _treeExpandedOverrides.Clear();
        _columnsVersion++;
        _context.NotifyChanged();
    }

    internal void CollapseAllTreeNodes()
    {
        if (_tableTree is null) return;
        _treeBaselineOverride = false;
        _treeExpandedOverrides.Clear();
        _columnsVersion++;
        _context.NotifyChanged();
    }

    internal void ResetTreeExpansion()
    {
        if (_tableTree is null) return;
        _treeBaselineOverride = null;
        _treeExpandedOverrides.Clear();
        _columnsVersion++;
        _context.NotifyChanged();
    }

    /// <summary>Expands every branch node in the table's hierarchical tree (<see cref="BxDataListTable{TItem}.Tree"/>).</summary>
    public void ExpandAll() => ExpandAllTreeNodes();

    /// <summary>Collapses every branch node in the table's hierarchical tree (<see cref="BxDataListTable{TItem}.Tree"/>).</summary>
    public void CollapseAll() => CollapseAllTreeNodes();

    /// <summary>Resets tree expansion back to the defaults declared on <see cref="BxDataListTable{TItem}.Tree"/>.</summary>
    public void ResetExpansion() => ResetTreeExpansion();

    internal (IReadOnlyList<DataListTreeNode<TItem>> Roots, IReadOnlyList<DataListTreeNode<TItem>> AllNodes) BuildTableTree()
    {
        if (_tableTree is null) return ([], []);
        var rows = Controller?.Rows ?? [];
        return _tableTree.BuildTree(rows, _schema);
    }

    internal IReadOnlyList<DataListTreeNode<TItem>> VisibleTreeNodes()
    {
        if (_tableTree is null) return [];
        var (roots, _) = BuildTableTree();
        return DataListTree<TItem>.FlattenVisible(roots, n => IsTreeNodeExpanded(n.Key, n.Depth));
    }

    internal IReadOnlySet<string> ExpandedTreeKeys()
    {
        if (_tableTree is null) return new HashSet<string>(StringComparer.Ordinal);
        var (_, allNodes) = BuildTableTree();
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in allNodes)
        {
            if (node.HasChildren && IsTreeNodeExpanded(node.Key, node.Depth)) set.Add(node.Key);
        }
        return set;
    }

    internal int TreeBranchCount()
    {
        if (_tableTree is null) return 0;
        var (_, allNodes) = BuildTableTree();
        return allNodes.Count(n => n.HasChildren);
    }

    internal bool AllTreeNodesExpanded()
    {
        if (_tableTree is null) return false;
        var (_, allNodes) = BuildTableTree();
        var branches = 0;
        foreach (var node in allNodes)
        {
            if (!node.HasChildren) continue;
            branches++;
            if (!IsTreeNodeExpanded(node.Key, node.Depth)) return false;
        }
        return branches > 0;
    }

    internal DataListGroupBy<TItem>? ActiveGroupBy =>
        _context.EffectiveLayout == DataListLayout.Grid ? _gridGroupBy : (_tableTree is null ? _tableGroupBy : null);

    internal void SetTableGroupBy(DataListGroupBy<TItem>? groupBy)
    {
        if (ReferenceEquals(_tableGroupBy, groupBy)) return;
        _tableGroupBy = groupBy;
        _context.NotifyChanged();
    }

    internal void SetGridGroupBy(DataListGroupBy<TItem>? groupBy)
    {
        if (ReferenceEquals(_gridGroupBy, groupBy)) return;
        _gridGroupBy = groupBy;
        _context.NotifyChanged();
    }

    internal IReadOnlyList<DataListGroup<TItem>> BuildActiveGroups()
    {
        if (ActiveGroupBy is not { } groupBy) return [];
        var rows = Controller?.Rows ?? [];
        return groupBy.BuildGroups(rows, _schema, ToolsMessages, CurrentLocale.Culture);
    }

    internal bool IsGroupCollapsed(string groupKey, bool defaultCollapsed)
    {
        if (string.IsNullOrEmpty(groupKey)) return defaultCollapsed;
        return _columnPrefs.ToggledGroups.Contains(groupKey) ? !defaultCollapsed : defaultCollapsed;
    }

    internal void ToggleGroup(string groupKey)
    {
        if (string.IsNullOrEmpty(groupKey) || groupKey.Length > DataListColumnPreferences.MaxKeyLength) return;
        if (!_columnPrefs.ToggledGroups.Remove(groupKey))
        {
            _columnPrefs.ToggledGroups.Add(groupKey);
        }
        PersistColumnPreferences();
        _context.NotifyChanged();
    }

    internal void SetGroupCollapsed(string groupKey, bool defaultCollapsed, bool collapsed)
    {
        if (string.IsNullOrEmpty(groupKey) || groupKey.Length > DataListColumnPreferences.MaxKeyLength) return;
        var shouldBeToggled = collapsed != defaultCollapsed;
        var changed = shouldBeToggled
            ? _columnPrefs.ToggledGroups.Add(groupKey)
            : _columnPrefs.ToggledGroups.Remove(groupKey);
        if (!changed) return;
        PersistColumnPreferences();
        _context.NotifyChanged();
    }

    internal bool HasRowSummary => _hasRowSummary && _tableTree is null;

    internal bool SummaryViewActive =>
        _hasRowSummary && (_columnPrefs.SummaryView ?? (_rowSummaryAutoPhone && _phoneSummaryDetected is { } detected ? detected : _rowSummaryDefaultActive));

    internal void SetRowSummary(bool enabled, bool defaultActive, bool autoPhone)
    {
        if (_hasRowSummary == enabled && _rowSummaryDefaultActive == defaultActive && _rowSummaryAutoPhone == autoPhone) return;
        _hasRowSummary = enabled;
        _rowSummaryDefaultActive = defaultActive;
        _rowSummaryAutoPhone = autoPhone;
        if (!enabled) _phoneSummaryDetected = null;
        _context.NotifyChanged();
    }

    internal void SetDetectedPhoneSummary(bool isPhone)
    {
        if (_phoneSummaryDetected == isPhone) return;
        _phoneSummaryDetected = isPhone;
        _context.NotifyChanged();
    }

    internal void SetSummaryView(bool active)
    {
        if (!_hasRowSummary || SummaryViewActive == active && _columnPrefs.SummaryView == active) return;
        _columnPrefs.SummaryView = active;
        PersistColumnPreferences();
        _context.NotifyChanged();
    }

    // OnInitialized: a static render and a prerender read the request; an interactive render takes what the prerender handed over.
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only a string goes through the reflection-based serializer of PersistentComponentState, and string has a built-in converter.")]
    private void InitializeColumnPreferences()
    {
        if (NormalizedPreferencesKey is not { } normalized) return;
        _claimedPreferencesKey = ServerColumnPreferences.Claim(Services, normalized);
        _columnsPersistent = Services.GetService(typeof(PersistentComponentState)) as PersistentComponentState;
        if (RendererInfo.IsInteractive)
        {
            if (_columnsPersistent is not null && ColumnsPersistKey is { } key && _columnsPersistent.TryTakeFromJson<string>(key, out var handedOver))
            {
                _columnPrefs = DataListColumnPreferences.Decode(handedOver);
                _columnPrefsResolved = true;
            }
            return;
        }
        var (prefs, fromServer) = ServerColumnPreferences.Read(Services, normalized, interactive: false);
        _columnPrefs = prefs;
        _columnPrefsResolved = fromServer;
        if (fromServer && !prefs.IsEmpty && _columnsPersistent is not null && AssignedRenderMode is not null)
        {
            // Prerendering a component of an interactive mode: the interactive render has no request, so the cookie travels with the page.
            _columnsPersistSubscription = _columnsPersistent.RegisterOnPersisting(PersistColumnPreferencesAsync, AssignedRenderMode);
        }
    }

    private void DisposeColumnPreferences()
    {
        _columnsPersistSubscription?.Dispose();
        ServerColumnPreferences.Release(Services, _claimedPreferencesKey);
        _claimedPreferencesKey = null;
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only a string goes through the reflection-based serializer of PersistentComponentState, and string has a built-in converter.")]
    private Task PersistColumnPreferencesAsync()
    {
        var encoded = PrunedPreferences().Encode();
        if (string.IsNullOrEmpty(encoded) || ColumnsPersistKey is not { } key || _columnsPersistent is null) return Task.CompletedTask;
        // The client sends the whole persisted state back when a circuit starts: the preferences count in the budget of the page like a result does, and a refused one is read from the browser instead.
        if (!DataListPersistBudget.Reserve(Services, encoded.Length + key.Length + 16, ToolsOptions.PersistMaxBytes, ToolsOptions.PersistPageMaxBytes)) return Task.CompletedTask;
        _columnsPersistent.PersistAsJson(key, encoded);
        return Task.CompletedTask;
    }

    // The interactive render of a list the prerender did not hand its preferences to (WebAssembly, a Hybrid web view, a refused budget): the browser has them.
    private async Task LoadBrowserColumnPreferencesAsync()
    {
        if (_columnPrefsResolved || !RendererInfo.IsInteractive || NormalizedPreferencesKey is not { } key) return;
        _columnPrefsResolved = true;
        try
        {
            var raw = await JS.InvokeAsync<string?>("Blux.tools.readColumnPrefs", key);
            var decoded = DataListColumnPreferences.Decode(raw);
            if (!decoded.IsEmpty && _columnPrefs.IsEmpty)
            {
                _columnPrefs = decoded;
                _columnsVersion++;
                _context.NotifyChanged();
            }
        }
        catch (Exception e) when (e is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
            // No browser side (a test, a circuit that ended).
        }
    }

    private void PersistColumnPreferences()
    {
        _columnsVersion++;
        if (!RendererInfo.IsInteractive || NormalizedPreferencesKey is not { } key) return;
        var pruned = PrunedPreferences();
        var encoded = pruned.Encode();
        if (encoded is null)
        {
            // Too many overrides for a cookie: they stay in memory and the cookie keeps its last value.
            if (!_columnsOverflowLogged)
            {
                _columnsOverflowLogged = true;
                Logger?.LogWarning("The column preferences '{Key}' are longer than {Max} characters, too long for a cookie: they apply until the page is reloaded.", key, DataListColumnPreferences.MaxEncodedLength);
            }
            return;
        }
        _ = WriteColumnCookieAsync(key, encoded);
    }

    private async Task WriteColumnCookieAsync(string key, string encoded)
    {
        try
        {
            await JS.InvokeVoidAsync("Blux.tools.writeColumnPrefs", key, encoded);
        }
        catch (Exception e) when (e is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
            // No browser side.
        }
    }

    internal bool EffectiveColumnVisible(BxDataListColumn<TItem> column)
    {
        if (column.Required) return true;
        var key = column.ResolvedKey;
        if (key is null) return column.Visible;
        if (_columnPrefs.Hidden.Contains(key)) return false;
        if (_columnPrefs.Shown.Contains(key)) return true;
        return column.Visible;
    }

    internal DataListPin EffectiveColumnPin(BxDataListColumn<TItem> column)
    {
        var key = column.ResolvedKey;
        if (key is null) return column.Pinned;
        if (_columnPrefs.PinnedStart.Contains(key)) return DataListPin.Start;
        if (_columnPrefs.PinnedEnd.Contains(key)) return DataListPin.End;
        if (_columnPrefs.Unpinned.Contains(key)) return DataListPin.None;
        return column.Pinned;
    }

    private static (int Min, int Max) WidthLimits(BxDataListColumn<TItem> column)
    {
        var min = Math.Clamp(column.MinWidth, DataListColumnPreferences.MinAllowedWidth, DataListColumnPreferences.MaxAllowedWidth);
        var max = Math.Clamp(column.MaxWidth, min, DataListColumnPreferences.MaxAllowedWidth);
        return (min, max);
    }

    internal int? EffectiveColumnWidth(BxDataListColumn<TItem> column)
    {
        var (min, max) = WidthLimits(column);
        var key = column.ResolvedKey;
        if (key is not null && _columnPrefs.Widths.TryGetValue(key, out var custom))
            return Math.Clamp(custom, min, max);
        return column.Width is { } width ? Math.Clamp(width, min, max) : null;
    }

    /// <summary>Columns in user order (before grouping pinned columns to table edges): used by the column panel.</summary>
    internal List<BxDataListColumn<TItem>> OrderedColumns()
    {
        var declared = _context.Columns;
        if (_columnPrefs.Order.Count == 0) return [.. declared];
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < _columnPrefs.Order.Count; i++) index[_columnPrefs.Order[i]] = i;
        return [.. declared
            .Select((col, pos) => (col, pos, rank: col.ResolvedKey is { } k && index.TryGetValue(k, out var r) ? r : int.MaxValue))
            .OrderBy(x => x.rank)
            .ThenBy(x => x.pos)
            .Select(x => x.col)];
    }

    /// <summary>Columns for the table: Start-pinned first, then unpinned in user order, then End-pinned. A list never shows no column: when the preferences hid every one, the first stays.</summary>
    internal List<BxDataListColumn<TItem>> TableColumns()
    {
        var ordered = OrderedColumns();
        var start = new List<BxDataListColumn<TItem>>();
        var middle = new List<BxDataListColumn<TItem>>();
        var end = new List<BxDataListColumn<TItem>>();
        foreach (var col in ordered)
        {
            if (!EffectiveColumnVisible(col)) continue;
            switch (EffectiveColumnPin(col))
            {
                case DataListPin.Start: start.Add(col); break;
                case DataListPin.End: end.Add(col); break;
                default: middle.Add(col); break;
            }
        }
        start.AddRange(middle);
        start.AddRange(end);
        if (start.Count == 0 && ordered.Count > 0) start.Add(ordered[0]);
        return start;
    }

    internal IReadOnlyList<DataListColumnState> ColumnStates()
    {
        var ordered = OrderedColumns();
        var shown = TableColumns();
        var result = new List<DataListColumnState>(ordered.Count);
        foreach (var col in ordered)
        {
            if (col.ResolvedKey is not { } key) continue;
            var (min, max) = WidthLimits(col);
            result.Add(new DataListColumnState(
                key,
                col.ResolvedTitle,
                shown.Contains(col),
                col.Required,
                EffectiveColumnPin(col),
                EffectiveColumnWidth(col),
                min,
                max,
                col.Resizable));
        }
        return result;
    }

    private BxDataListColumn<TItem>? FindColumn(string key) =>
        _context.Columns.FirstOrDefault(c => string.Equals(c.ResolvedKey, key, StringComparison.Ordinal));

    private void Announce(string message)
    {
        ColumnAnnouncement = message;
    }

    private string Format(string template, params object[] args) => string.Format(CurrentLocale.Culture, template, args);

    // Hiding the last column of the table would leave nothing to look at: the last visible column stays.
    internal bool CanHideColumn(BxDataListColumn<TItem> column) =>
        !column.Required && column.ResolvedKey is not null && TableColumns().Any(c => !ReferenceEquals(c, column));

    private void WriteVisibility(BxDataListColumn<TItem> col, string key, bool visible)
    {
        _columnPrefs.Hidden.Remove(key);
        _columnPrefs.Shown.Remove(key);
        if (visible != col.Visible)
        {
            if (visible) _columnPrefs.Shown.Add(key);
            else _columnPrefs.Hidden.Add(key);
        }
    }

    internal void SetColumnVisible(string key, bool visible)
    {
        if (FindColumn(key) is not { } col || col.Required) return;
        if (EffectiveColumnVisible(col) == visible) return;
        if (!visible && !CanHideColumn(col)) return;
        WriteVisibility(col, key, visible);
        Announce(Format(visible ? ToolsMessages.ColumnShown : ToolsMessages.ColumnHidden, col.ResolvedTitle));
        PersistColumnPreferences();
        _context.NotifyChanged();
    }

    /// <summary>Shows every column, or hides every column that is not Required (the first one stays when none is).</summary>
    internal void SetAllColumnsVisible(bool visible)
    {
        var changed = false;
        var keep = visible ? null : OrderedColumns().FirstOrDefault(c => c.Required && c.ResolvedKey is not null) ?? OrderedColumns().FirstOrDefault(c => c.ResolvedKey is not null);
        foreach (var col in OrderedColumns())
        {
            if (col.Required || col.ResolvedKey is not { } key) continue;
            var target = visible || ReferenceEquals(col, keep);
            if (EffectiveColumnVisible(col) == target) continue;
            WriteVisibility(col, key, target);
            changed = true;
        }
        if (!changed) return;
        Announce(visible ? ToolsMessages.ColumnsAllShown : ToolsMessages.ColumnsAllHidden);
        PersistColumnPreferences();
        _context.NotifyChanged();
    }

    internal void SetColumnPinned(string key, DataListPin pin)
    {
        if (FindColumn(key) is not { } col) return;
        if (EffectiveColumnPin(col) == pin) return;
        _columnPrefs.PinnedStart.Remove(key);
        _columnPrefs.PinnedEnd.Remove(key);
        _columnPrefs.Unpinned.Remove(key);
        if (pin != col.Pinned)
        {
            switch (pin)
            {
                case DataListPin.Start: _columnPrefs.PinnedStart.Add(key); break;
                case DataListPin.End: _columnPrefs.PinnedEnd.Add(key); break;
                case DataListPin.None: _columnPrefs.Unpinned.Add(key); break;
            }
        }
        Announce(Format(pin switch
        {
            DataListPin.Start => ToolsMessages.ColumnPinnedToStart,
            DataListPin.End => ToolsMessages.ColumnPinnedToEnd,
            _ => ToolsMessages.ColumnUnpinned,
        }, col.ResolvedTitle));
        PersistColumnPreferences();
        _context.NotifyChanged();
    }

    private void ApplyOrder(List<string> keys, string movedKey)
    {
        var defaultKeys = _context.Columns.Select(c => c.ResolvedKey).Where(k => k is not null).Cast<string>().ToList();
        _columnPrefs.Order.Clear();
        if (!keys.SequenceEqual(defaultKeys, StringComparer.Ordinal)) _columnPrefs.Order.AddRange(keys);
        var position = keys.IndexOf(movedKey) + 1;
        Announce(Format(ToolsMessages.ColumnMoved, FindColumn(movedKey)?.ResolvedTitle ?? movedKey, position, keys.Count));
        PersistColumnPreferences();
        _context.NotifyChanged();
    }

    private List<string> OrderedKeys() => OrderedColumns().Select(c => c.ResolvedKey).Where(k => k is not null).Cast<string>().ToList();

    internal void MoveColumn(string key, int delta)
    {
        if (delta == 0 || FindColumn(key) is not { Required: false }) return;
        var configurable = OrderedColumns().Where(c => !c.Required && c.ResolvedKey is not null).Select(c => c.ResolvedKey!).ToList();
        var from = configurable.FindIndex(k => string.Equals(k, key, StringComparison.Ordinal));
        if (from < 0) return;
        var to = Math.Clamp(from + delta, 0, configurable.Count - 1);
        if (to == from) return;
        MoveColumnNextTo(key, configurable[to], after: to > from);
    }

    /// <summary>Puts <paramref name="key"/> right before or right after <paramref name="targetKey"/>: what dropping a dragged column on another does.</summary>
    internal void MoveColumnNextTo(string key, string targetKey, bool after)
    {
        if (string.Equals(key, targetKey, StringComparison.Ordinal)) return;
        if (FindColumn(key) is not { Required: false } || FindColumn(targetKey) is not { Required: false }) return;
        var keys = OrderedKeys();
        if (!keys.Contains(key, StringComparer.Ordinal) || !keys.Contains(targetKey, StringComparer.Ordinal)) return;
        var before = keys.ToList();
        keys.Remove(key);
        var at = keys.FindIndex(k => string.Equals(k, targetKey, StringComparison.Ordinal));
        keys.Insert(after ? at + 1 : at, key);
        if (keys.SequenceEqual(before, StringComparer.Ordinal)) return;
        ApplyOrder(keys, key);
    }

    internal void SetColumnWidth(string key, int? width)
    {
        if (FindColumn(key) is not { } col || !col.Resizable) return;
        var (min, max) = WidthLimits(col);
        int? clamped = width is { } px ? Math.Clamp(px, min, max) : null;
        int? defaultWidth = col.Width is { } dw ? Math.Clamp(dw, min, max) : null;
        var changed = false;
        if (clamped is null || clamped == defaultWidth)
        {
            changed = _columnPrefs.Widths.Remove(key);
        }
        else if (!_columnPrefs.Widths.TryGetValue(key, out var existing) || existing != clamped)
        {
            _columnPrefs.Widths[key] = clamped.Value;
            changed = true;
        }
        // Said even when nothing was stored: the browser already showed the new width, and a reset to a width that was never stored is still a reset.
        WidthAnnouncement = clamped is { } now && clamped != defaultWidth
            ? Format(ToolsMessages.ColumnWidthChanged, col.ResolvedTitle, now)
            : Format(ToolsMessages.ColumnWidthReset, col.ResolvedTitle);
        if (changed) PersistColumnPreferences();
        _context.NotifyChanged();
    }

    internal void ResetColumns()
    {
        if (!_columnPrefs.HasColumnOverrides) return;
        _columnPrefs.ClearColumns();
        Announce(ToolsMessages.ColumnsWereReset);
        PersistColumnPreferences();
        _context.NotifyChanged();
    }
}
