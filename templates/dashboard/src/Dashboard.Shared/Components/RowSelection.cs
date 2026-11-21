namespace Dashboard.Shared.Components;

/// <summary>
/// The rows ticked in a table, by id. It lives in the page, not in the table: the data list shows one page at a time, and a selection made on one page has to
/// survive a move to the next. <see cref="Changed"/> is what the page listens to, to render its toolbar and the checkboxes again.
/// </summary>
public sealed class RowSelection
{
    private readonly HashSet<int> _ids = [];

    public int Count => _ids.Count;

    public IReadOnlyCollection<int> Ids => _ids;

    public event Action? Changed;

    public bool Contains(int id) => _ids.Contains(id);

    /// <summary>Ticks or unticks one row.</summary>
    public void Set(int id, bool selected)
    {
        if (selected ? _ids.Add(id) : _ids.Remove(id)) Changed?.Invoke();
    }

    /// <summary>Ticks or unticks many rows (the rows of a page) with one notification.</summary>
    public void Set(IEnumerable<int> ids, bool selected)
    {
        var changed = false;
        foreach (var id in ids) changed |= selected ? _ids.Add(id) : _ids.Remove(id);
        if (changed) Changed?.Invoke();
    }

    public void Clear()
    {
        if (_ids.Count == 0) return;
        _ids.Clear();
        Changed?.Invoke();
    }
}
