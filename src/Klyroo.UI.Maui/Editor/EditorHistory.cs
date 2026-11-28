namespace Klyroo.UI.Maui;

/// <summary>
/// The undo and redo stacks of a <see cref="KlyEditor"/>: a native <see cref="Editor"/> keeps none the app can reach. Typing in a run
/// is one entry; an action of the toolbar, an undo or a pause in the typing starts a new one.
/// </summary>
internal sealed class EditorHistory
{
    private const int Limit = 200;
    private static readonly TimeSpan Pause = TimeSpan.FromSeconds(1);

    private readonly List<EditorEdit> _undo = [];
    private readonly List<EditorEdit> _redo = [];
    private DateTimeOffset _lastTyping;
    private bool _boundary = true;

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    /// <summary>Keeps <paramref name="before"/>, the state a change started from. Typing that follows typing within a second adds to the same entry.</summary>
    public void Record(EditorEdit before, DateTimeOffset now, bool typing)
    {
        if (!typing || _boundary || now - _lastTyping > Pause)
        {
            _undo.Add(before);
            if (_undo.Count > Limit) _undo.RemoveAt(0);
        }
        _redo.Clear();
        _boundary = !typing;
        _lastTyping = now;
    }

    /// <summary>The state to go back to, or null; <paramref name="current"/> becomes what a redo returns.</summary>
    public EditorEdit? Undo(EditorEdit current)
    {
        if (_undo.Count == 0) return null;
        var state = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(current);
        _boundary = true;
        return state;
    }

    /// <summary>The state that was undone, or null; <paramref name="current"/> becomes what an undo returns.</summary>
    public EditorEdit? Redo(EditorEdit current)
    {
        if (_redo.Count == 0) return null;
        var state = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(current);
        _boundary = true;
        return state;
    }
}
