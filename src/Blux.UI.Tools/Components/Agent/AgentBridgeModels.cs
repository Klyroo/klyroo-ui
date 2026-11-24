namespace Blux.UI.Tools;

/// <summary>Source of a component state change reported to <see cref="AgentBridgeService.Observe"/>.</summary>
public enum AgentChangeSource
{
    Keyboard,
    Mouse,
    Touch,
    Pen,
    Virtual,
    Programmatic,
    Agent,
}

/// <summary>A single notification emitted by the unified <c>observe</c> change stream.</summary>
public sealed record ObservedChange(
    string Component,
    string Ref,
    string Label,
    string Key,
    object? Previous,
    object? Value,
    string Reason,
    AgentChangeSource Source,
    string? Row = null);

/// <summary>Snapshot entry describing one component on the page and the actions it currently supports.</summary>
public sealed record AgentComponentSnapshot(
    string Ref,
    string Component,
    string Label,
    object? Value,
    IReadOnlyList<string> Actions,
    bool Disabled = false,
    bool OffPage = false,
    bool ConfirmRequired = false);

/// <summary>One choice item returned by <see cref="AgentBridgeService.Query"/> for a select, filter, or list component.</summary>
public sealed record AgentChoiceItem(
    string Value,
    string Label,
    bool Selected = false,
    bool Disabled = false);

/// <summary>Result entry returned by <see cref="AgentBridgeService.Query"/>.</summary>
public sealed record AgentQueryMatch(
    string Ref,
    string Component,
    string Label,
    IReadOnlyList<AgentChoiceItem> Items,
    int More = 0);

/// <summary>Query parameters for <see cref="AgentBridgeService.Query"/>.</summary>
public sealed class AgentQueryRequest
{
    public string? Within { get; init; }
    public string? Text { get; init; }
    public string? Kind { get; init; }
}

/// <summary>One action step in an <see cref="AgentBridgeService.ActAsync"/> call.</summary>
public sealed record AgentActStep(
    string Ref,
    string Action,
    IReadOnlyList<object?>? Args = null);

/// <summary>Outcome of a single step in <see cref="AgentActResult"/>.</summary>
public sealed record AgentActStepOutcome(
    string Ref,
    string Action,
    bool Ok,
    object? Value);

/// <summary>Result of an <see cref="AgentBridgeService.ActAsync"/> invocation.</summary>
public sealed record AgentActResult(
    bool Ok,
    IReadOnlyList<AgentActStepOutcome> Steps,
    IReadOnlyDictionary<string, object?> Values,
    string? Error = null);

/// <summary>Tool metadata and hints following the <c>ui-sdk.dev</c> Agent / WebMCP specification.</summary>
public sealed record AgentToolDefinition(
    string Name,
    string Description,
    bool ReadOnlyHint,
    bool ConsequentialHint,
    bool UntrustedContentHint);

/// <summary>Generic undo/redo snapshot stack inspired by <c>createUndoStack</c> from <c>ui-sdk.dev</c>.</summary>
public sealed class UndoStack<T>
{
    private readonly List<T> _past = [];
    private readonly List<T> _future = [];
    private readonly int _limit;

    public UndoStack(T initial, int limit = 50)
    {
        Current = initial;
        _limit = Math.Max(1, limit);
    }

    public T Current { get; private set; }
    public bool CanUndo => _past.Count > 0;
    public bool CanRedo => _future.Count > 0;

    public T Record(T next)
    {
        if (EqualityComparer<T>.Default.Equals(Current, next)) return Current;
        _past.Add(Current);
        if (_past.Count > _limit) _past.RemoveAt(0);
        Current = next;
        _future.Clear();
        return Current;
    }

    public T Undo()
    {
        if (_past.Count == 0) return Current;
        _future.Add(Current);
        Current = _past[^1];
        _past.RemoveAt(_past.Count - 1);
        return Current;
    }

    public T Redo()
    {
        if (_future.Count == 0) return Current;
        _past.Add(Current);
        Current = _future[^1];
        _future.RemoveAt(_future.Count - 1);
        return Current;
    }

    public void Clear()
    {
        _past.Clear();
        _future.Clear();
    }
}
