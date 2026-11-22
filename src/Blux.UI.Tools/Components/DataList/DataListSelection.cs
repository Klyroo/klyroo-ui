namespace Blux.UI.Tools;

/// <summary>
/// The rows ticked in a data list, by row key. The rows themselves are kept too (a bulk action wants them, and a row ticked on another page is no longer in the list's rows); a key the
/// parent gave through <c>SelectedKeys</c> has no row until a load finds it. A plain class so the rules are tested without a renderer.
/// </summary>
internal sealed class DataListSelection<TItem>
{
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    private readonly record struct Entry(TItem? Item, bool Known);

    public int Count => _entries.Count;

    public bool Contains(string key) => _entries.ContainsKey(key);

    public IReadOnlyCollection<string> Keys => _entries.Keys;

    /// <summary>The rows that are known, in the order they were ticked.</summary>
    public IReadOnlyList<TItem> Items() => [.. _entries.Values.Where(e => e.Known).Select(e => e.Item!)];

    /// <summary>Ticks or unticks one row; whether anything changed.</summary>
    public bool Set(string key, TItem item, bool selected)
    {
        if (!selected) return _entries.Remove(key);
        if (_entries.TryGetValue(key, out var existing) && existing.Known)
        {
            _entries[key] = new Entry(item, true);
            return false;
        }
        _entries[key] = new Entry(item, true);
        return true;
    }

    /// <summary>Ticks or unticks many rows; whether anything changed.</summary>
    public bool SetMany(IEnumerable<KeyValuePair<string, TItem>> rows, bool selected)
    {
        var changed = false;
        foreach (var (key, item) in rows) changed |= Set(key, item, selected);
        return changed;
    }

    public bool Remove(string key) => _entries.Remove(key);

    public bool Clear()
    {
        if (_entries.Count == 0) return false;
        _entries.Clear();
        return true;
    }

    /// <summary>
    /// Makes the selection exactly <paramref name="keys"/>: rows already known stay known, the others wait for a load to find them. Whether the set of keys changed.
    /// </summary>
    public bool Replace(IEnumerable<string> keys)
    {
        var wanted = new HashSet<string>(keys, StringComparer.Ordinal);
        if (wanted.SetEquals(_entries.Keys)) return false;
        foreach (var key in _entries.Keys.Where(k => !wanted.Contains(k)).ToList()) _entries.Remove(key);
        foreach (var key in wanted)
        {
            if (!_entries.ContainsKey(key)) _entries[key] = new Entry(default, false);
        }
        return true;
    }

    /// <summary>
    /// Unticks every row that is not in <paramref name="valid"/> (the rows the selection may hold now) and refreshes the rows that are, so a bulk action sees the current data.
    /// Whether a key left.
    /// </summary>
    public bool Retain(IReadOnlyDictionary<string, TItem> valid)
    {
        var removed = false;
        foreach (var key in _entries.Keys.ToList())
        {
            if (valid.TryGetValue(key, out var item)) _entries[key] = new Entry(item, true);
            else
            {
                _entries.Remove(key);
                removed = true;
            }
        }
        return removed;
    }

    /// <summary>The keys as a set that no later change touches.</summary>
    public IReadOnlySet<string> Snapshot() => new HashSet<string>(_entries.Keys, StringComparer.Ordinal);
}
