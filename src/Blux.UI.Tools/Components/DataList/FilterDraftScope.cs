using Blux.UI.Tools.Query;

namespace Blux.UI.Tools;

/// <summary>
/// The changes the editors of a filter panel in the Submit apply mode have staged: each editor still builds its rule the way it does inside a popover (same operators, presets and parsing),
/// but hands it here instead of to the list, and the panel applies all of them in one state change (one history entry) when Apply is pressed.
/// </summary>
internal sealed class FilterDraftScope
{
    private readonly List<KeyValuePair<string, FilterRule?>> _pending = [];

    /// <summary>The editors hand their changes to <see cref="Stage"/> instead of applying them (the Submit apply mode).</summary>
    public bool Deferred { get; set; }

    /// <summary>The editors are in a form of the panel (static rendering): they leave out their own form and fill it with a field per filter.</summary>
    public bool Embedded { get; set; }

    /// <summary>Raised when something was staged or dropped.</summary>
    public event Action? Changed;

    public bool HasPending => _pending.Count > 0;

    /// <summary>Stages the rule of a filter (<see langword="null"/> takes the filter off) until the panel applies or drops it.</summary>
    public void Stage(string key, FilterRule? rule)
    {
        var index = _pending.FindIndex(p => p.Key == key);
        if (index >= 0) _pending[index] = new(key, rule);
        else _pending.Add(new(key, rule));
        Changed?.Invoke();
    }

    /// <summary>The rule staged for a filter (<see langword="null"/> when the staged change takes the filter off), if one is staged.</summary>
    public bool TryGetStaged(string key, out FilterRule? rule)
    {
        var index = _pending.FindIndex(p => p.Key == key);
        rule = index >= 0 ? _pending[index].Value : null;
        return index >= 0;
    }

    /// <summary>Drops what was staged for a filter: the editor shows the list again, or the staged rule is what the list already has.</summary>
    public void Unstage(string key)
    {
        if (_pending.RemoveAll(p => p.Key == key) > 0) Changed?.Invoke();
    }

    /// <summary>Drops everything that was staged.</summary>
    public void Clear()
    {
        if (_pending.Count == 0) return;
        _pending.Clear();
        Changed?.Invoke();
    }

    /// <summary>The staged changes, in the order the filters were first touched, and nothing staged any more.</summary>
    public IReadOnlyList<KeyValuePair<string, FilterRule?>> Take()
    {
        var all = _pending.ToList();
        Clear();
        return all;
    }
}
