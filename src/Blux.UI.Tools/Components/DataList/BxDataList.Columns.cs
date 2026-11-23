using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blux.UI.Tools;

// Column preferences: visibility, order, pinning and pixel widths, backed by a cookie when PreferencesKey is set so the first SSR / prerender paint matches.
public partial class BxDataList<TItem>
{
    private DataListColumnPreferences _columnPrefs = new();
    private int _columnsVersion;
    private bool _columnPrefsFromServer;
    private bool _columnPrefsFromBrowser;

    /// <summary>
    /// Cookie key under which column visibility, order, pinning and widths are saved (<c>blux-dl-cols-&lt;key&gt;</c>, scoped to the app's path base).
    /// When set, preferences survive reloads and are read on the server during SSR and prerendering via <c>AddBluxServer()</c>.
    /// Without it, column preferences stay in memory for the current interactive session only.
    /// </summary>
    [Parameter] public string? PreferencesKey { get; set; }

    internal string? NormalizedPreferencesKey => DataListColumnPreferences.NormalizeKey(PreferencesKey);

    internal bool WillBeInteractive => !RendererInfo.IsInteractive && AssignedRenderMode is not null;

    internal bool HasCustomColumnPreferences => !_columnPrefs.IsEmpty;

    internal int ColumnsVersion => _columnsVersion;

    private void InitializeColumnPreferences()
    {
        var (prefs, fromServer) = ServerColumnPreferences.Read(Services, NormalizedPreferencesKey);
        _columnPrefs = prefs;
        _columnPrefsFromServer = fromServer;
    }

    private async Task LoadBrowserColumnPreferencesAsync()
    {
        if (_columnPrefsFromServer || _columnPrefsFromBrowser || !RendererInfo.IsInteractive) return;
        _columnPrefsFromBrowser = true;
        var key = NormalizedPreferencesKey;
        if (key is null) return;
        try
        {
            var raw = await JS.InvokeAsync<string?>("Blux.tools.readColumnPrefs", key);
            var decoded = DataListColumnPreferences.Decode(raw);
            if (!decoded.IsEmpty)
            {
                _columnPrefs = decoded;
                _columnsVersion++;
                _context.NotifyChanged();
                StateHasChanged();
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
        if (!RendererInfo.IsInteractive) return;
        var key = NormalizedPreferencesKey;
        if (key is null) return;
        var encoded = _columnPrefs.Encode();
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

    internal int? EffectiveColumnWidth(BxDataListColumn<TItem> column)
    {
        var min = Math.Max(DataListColumnPreferences.MinAllowedWidth, column.MinWidth);
        var max = Math.Clamp(column.MaxWidth, min, DataListColumnPreferences.MaxAllowedWidth);
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

    /// <summary>Columns for the table: Start-pinned first, then unpinned in user order, then End-pinned.</summary>
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
        return start;
    }

    internal IReadOnlyList<DataListColumnState> ColumnStates()
    {
        var ordered = OrderedColumns();
        var result = new List<DataListColumnState>(ordered.Count);
        foreach (var col in ordered)
        {
            if (col.ResolvedKey is not { } key) continue;
            var min = Math.Max(DataListColumnPreferences.MinAllowedWidth, col.MinWidth);
            var max = Math.Clamp(col.MaxWidth, min, DataListColumnPreferences.MaxAllowedWidth);
            result.Add(new DataListColumnState(
                key,
                col.ResolvedTitle,
                EffectiveColumnVisible(col),
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

    internal void SetColumnVisible(string key, bool visible)
    {
        if (FindColumn(key) is not { } col || col.Required) return;
        if (EffectiveColumnVisible(col) == visible) return;
        _columnPrefs.Hidden.Remove(key);
        _columnPrefs.Shown.Remove(key);
        if (visible != col.Visible)
        {
            if (visible) _columnPrefs.Shown.Add(key);
            else _columnPrefs.Hidden.Add(key);
        }
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
        PersistColumnPreferences();
        _context.NotifyChanged();
    }

    internal void MoveColumn(string key, int delta)
    {
        if (delta == 0) return;
        var keys = OrderedColumns().Select(c => c.ResolvedKey).Where(k => k is not null).Cast<string>().ToList();
        var from = keys.FindIndex(k => string.Equals(k, key, StringComparison.Ordinal));
        if (from < 0) return;
        var to = Math.Clamp(from + delta, 0, keys.Count - 1);
        if (to == from) return;
        var moved = keys[from];
        keys.RemoveAt(from);
        keys.Insert(to, moved);

        var defaultKeys = _context.Columns.Select(c => c.ResolvedKey).Where(k => k is not null).Cast<string>().ToList();
        _columnPrefs.Order.Clear();
        if (!keys.SequenceEqual(defaultKeys, StringComparer.Ordinal)) _columnPrefs.Order.AddRange(keys);
        PersistColumnPreferences();
        _context.NotifyChanged();
    }

    internal void SetColumnWidth(string key, int? width)
    {
        if (FindColumn(key) is not { } col || !col.Resizable) return;
        var min = Math.Max(DataListColumnPreferences.MinAllowedWidth, col.MinWidth);
        var max = Math.Clamp(col.MaxWidth, min, DataListColumnPreferences.MaxAllowedWidth);
        int? clamped = width is { } px ? Math.Clamp(px, min, max) : null;
        int? defaultWidth = col.Width is { } dw ? Math.Clamp(dw, min, max) : null;
        if (clamped == defaultWidth)
        {
            if (!_columnPrefs.Widths.Remove(key)) return;
        }
        else if (clamped is { } value)
        {
            if (_columnPrefs.Widths.TryGetValue(key, out var existing) && existing == value) return;
            _columnPrefs.Widths[key] = value;
        }
        else
        {
            if (!_columnPrefs.Widths.Remove(key)) return;
        }
        PersistColumnPreferences();
        _context.NotifyChanged();
    }

    internal void ResetColumns()
    {
        if (_columnPrefs.IsEmpty) return;
        _columnPrefs.Clear();
        PersistColumnPreferences();
        _context.NotifyChanged();
    }
}
