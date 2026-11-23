namespace Blux.UI.Tools;

/// <summary>What a row action receives when it runs: the row, its key and place, and a way to report a failure without throwing.</summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataListRowActionContext<TItem>
{
    internal DataListRowActionContext(DataListContext<TItem> list, TItem item, string key, int position, CancellationToken cancellationToken)
    {
        List = list;
        Item = item;
        Key = key;
        Position = position;
        CancellationToken = cancellationToken;
    }

    /// <summary>The list, to read its state or to load it again (<see cref="DataListContext{TItem}.RefreshAsync"/>).</summary>
    public DataListContext<TItem> List { get; }

    /// <summary>The row.</summary>
    public TItem Item { get; }

    /// <summary>The row key (<c>RowKey</c> of the schema, else the identity of the row).</summary>
    public string Key { get; }

    /// <summary>The place of the row on the page, from 1; 0 when the part was placed outside a table row.</summary>
    public int Position { get; }

    /// <summary>Cancelled when the list goes away.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>The message given to <see cref="Fail"/>.</summary>
    public string? FailureMessage { get; private set; }

    /// <summary>Whether <see cref="Fail"/> was called.</summary>
    public bool Failed { get; private set; }

    /// <summary>
    /// Reports that the action did not do what it was asked (a refusal of the server, a conflict) without throwing: the list is not refreshed and <c>OnError</c> of the part is raised.
    /// Call it, then return.
    /// </summary>
    /// <param name="message">What went wrong, for <c>OnError</c>.</param>
    public void Fail(string? message = null)
    {
        Failed = true;
        FailureMessage = message;
    }
}

/// <summary>The question a row action asks before it runs, in a dialog. The text can name the row.</summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataListRowActionConfirm<TItem>
{
    /// <summary>The title of the dialog.</summary>
    public required string Title { get; init; }

    /// <summary>The text under the title, built from the context (<c>c =&gt; $"{c.Item.Name} will be removed."</c>).</summary>
    public Func<DataListRowActionContext<TItem>, string?>? Description { get; init; }

    /// <summary>The label of the confirm button. Without it the localized "Confirm".</summary>
    public string? ConfirmLabel { get; init; }

    /// <summary>The color of the confirm button; <see cref="BxColor.Error"/> for a destructive action. Without it the color of the action.</summary>
    public BxColor? Color { get; init; }
}

/// <summary>
/// An action on one row of a data list (<see cref="BxDataListRowActions{TItem}"/>): a button of the row, an entry of its menu or of its sheet on a phone. Actions with the same
/// <see cref="Group"/> are kept together, separated from the others by a divider; an action with <see cref="Children"/> opens a submenu.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataListRowAction<TItem>
{
    /// <summary>Identifies the action; unique among the actions of the part, children included.</summary>
    public required string Key { get; init; }

    /// <summary>The text of the button or of the menu entry.</summary>
    public required string Label { get; init; }

    /// <summary>The name of an icon shown before the label (<c>copy</c>), one of the icons of Blux.UI.</summary>
    public string? Icon { get; init; }

    /// <summary>The color; <see cref="BxColor.Error"/> for a destructive action.</summary>
    public BxColor Color { get; init; } = BxColor.Neutral;

    /// <summary>The group the action belongs to. Groups are shown in the order of their first visible action, with a divider between them; actions without a group form one of their own.</summary>
    public string? Group { get; init; }

    /// <summary>The entries of a submenu. An action with children opens them instead of running, and only shows when one of them does. It always sits in the menu, never in a button of the row.</summary>
    public IReadOnlyList<DataListRowAction<TItem>>? Children { get; init; }

    /// <summary>
    /// Makes the action a link to the address it returns (<see langword="null"/>: no link for this row). A link works in a statically rendered list too, where it is the only kind of row action
    /// that is shown. <see cref="OnExecute"/> is not called for it.
    /// </summary>
    public Func<DataListRowActionContext<TItem>, string?>? Href { get; init; }

    /// <summary>Hides the action when it returns <see langword="false"/> (an action that does not apply to this row); the children of a hidden action are hidden with it. It runs on every render: keep it cheap.</summary>
    public Func<DataListRowActionContext<TItem>, bool>? Visible { get; init; }

    /// <summary>Disables the action when it returns <see langword="true"/>; the children of a disabled action cannot run either. It runs on every render: keep it cheap.</summary>
    public Func<DataListRowActionContext<TItem>, bool>? Disabled { get; init; }

    /// <summary>
    /// Why the action is disabled: shown under its label in the sheet of a phone, and on a wide screen as the title (the tooltip) and the accessible description of its button or menu entry.
    /// The reason of an action with <see cref="Children"/> is the one its children show while it is disabled.
    /// </summary>
    public Func<DataListRowActionContext<TItem>, string?>? DisabledReason { get; init; }

    /// <summary>Shows a spinner and blocks the action while it returns <see langword="true"/> (work started elsewhere). The action also shows one while it runs.</summary>
    public Func<DataListRowActionContext<TItem>, bool>? Loading { get; init; }

    /// <summary>Asks before the action runs. Without it the action runs on the click.</summary>
    public DataListRowActionConfirm<TItem>? Confirm { get; init; }

    /// <summary>Loads the list again once the action succeeded (the row it changed or removed). Off by default; an action can also call <see cref="DataListContext{TItem}.RefreshAsync"/> itself.</summary>
    public bool RefreshOnSuccess { get; init; }

    /// <summary>
    /// What the action does. An exception, or a call to <see cref="DataListRowActionContext{TItem}.Fail"/>, marks the run as failed: the list is not refreshed, <c>OnError</c> of the part is raised
    /// and nothing is thrown to the page.
    /// </summary>
    public Func<DataListRowActionContext<TItem>, Task>? OnExecute { get; init; }
}

/// <summary>A row action that failed: it threw, or called <see cref="DataListRowActionContext{TItem}.Fail"/>.</summary>
/// <param name="Action">The action.</param>
/// <param name="Item">The row.</param>
/// <param name="Exception">What it threw; <see langword="null"/> when it reported the failure itself.</param>
/// <param name="Message">What <see cref="DataListRowActionContext{TItem}.Fail"/> was given.</param>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed record DataListRowActionFailure<TItem>(DataListRowAction<TItem> Action, TItem Item, Exception? Exception, string? Message);

/// <summary>What a row action shows for a row right now.</summary>
internal readonly record struct RowActionState(bool Visible, bool Disabled, string? Reason, bool Loading, string? Href);

internal enum RowActionOutcome
{
    /// <summary>It did not start: hidden, disabled, loading, running, a link or a submenu.</summary>
    Ignored,
    Succeeded,
    Failed,
}

/// <summary>
/// The run loop of the row actions of a row: which are running (per row and action), what each shows and the run itself. An action that is hidden, disabled, loading or running is ignored;
/// a failure, thrown or reported, does not throw.
/// </summary>
internal sealed class DataListRowActionRunner<TItem>
{
    private readonly HashSet<(string Row, string Action)> _running = [];

    public bool IsRunning(string row, string action) => _running.Contains((row, action));

    /// <summary>
    /// What an action shows for a row. <paramref name="ancestors"/> are the actions it sits under, outermost first: a hidden ancestor hides it and a disabled one disables it (with the reason of the ancestor),
    /// so a child of a disabled or hidden action never runs and is never offered as runnable.
    /// </summary>
    public RowActionState StateOf(string row, DataListRowAction<TItem> action, DataListRowActionContext<TItem> context, IReadOnlyList<DataListRowAction<TItem>>? ancestors = null)
    {
        var inheritedDisabled = false;
        string? inheritedReason = null;
        if (ancestors is not null)
        {
            foreach (var ancestor in ancestors)
            {
                if (ancestor.Visible?.Invoke(context) == false) return default;
                if (inheritedDisabled || ancestor.Disabled?.Invoke(context) != true) continue;
                inheritedDisabled = true;
                inheritedReason = ancestor.DisabledReason?.Invoke(context);
            }
        }
        if (action.Visible?.Invoke(context) == false) return default;
        var children = action.Children;
        if (children is { Count: > 0 })
        {
            var below = ancestors is null ? [action] : (IReadOnlyList<DataListRowAction<TItem>>)[.. ancestors, action];
            var visible = children.Any(c => StateOf(row, c, context, below).Visible);
            var ownDisabled = action.Disabled?.Invoke(context) ?? false;
            var disabled = inheritedDisabled || ownDisabled;
            return new RowActionState(visible, disabled, disabled ? inheritedReason ?? action.DisabledReason?.Invoke(context) : null, false, null);
        }
        var href = action.Href?.Invoke(context);
        if (action.Href is not null && href is null) return default;
        var running = IsRunning(row, action.Key);
        var blocked = inheritedDisabled || running || (action.Disabled?.Invoke(context) ?? false);
        var loading = running || (action.Loading?.Invoke(context) ?? false);
        return new RowActionState(true, blocked, blocked ? inheritedReason ?? action.DisabledReason?.Invoke(context) : null, loading, href);
    }

    /// <summary>The action with this key among <paramref name="roots"/> and the actions it sits under (outermost first), or <see langword="null"/>.</summary>
    public static (DataListRowAction<TItem> Action, IReadOnlyList<DataListRowAction<TItem>> Ancestors)? Locate(IEnumerable<DataListRowAction<TItem>> roots, string? key)
    {
        if (key is null) return null;
        return Search(roots, key, []);
    }

    private static (DataListRowAction<TItem>, IReadOnlyList<DataListRowAction<TItem>>)? Search(IEnumerable<DataListRowAction<TItem>> actions, string key, List<DataListRowAction<TItem>> path)
    {
        foreach (var action in actions)
        {
            if (action.Key == key) return (action, [.. path]);
            if (action.Children is not { Count: > 0 }) continue;
            path.Add(action);
            var found = Search(action.Children, key, path);
            path.RemoveAt(path.Count - 1);
            if (found is not null) return found;
        }
        return null;
    }

    /// <summary>Runs an action. <paramref name="changed"/> is called when it starts and when it ends, so that the row shows its spinner.</summary>
    public async Task<RowActionOutcome> RunAsync(string row, DataListRowAction<TItem> action, DataListRowActionContext<TItem> context, Action changed, Func<Exception?, string?, Task> onFailure, CancellationToken lifetime,
        IReadOnlyList<DataListRowAction<TItem>>? ancestors = null)
    {
        var state = StateOf(row, action, context, ancestors);
        if (!state.Visible || state.Disabled || state.Loading || state.Href is not null || action.Children is { Count: > 0 }) return RowActionOutcome.Ignored;
        if (!_running.Add((row, action.Key))) return RowActionOutcome.Ignored;
        Exception? thrown = null;
        try
        {
            changed();
            if (action.OnExecute is { } run) await run(context);
        }
        catch (Exception exception) when (!(exception is OperationCanceledException && lifetime.IsCancellationRequested))
        {
            thrown = exception;
        }
        finally
        {
            _running.Remove((row, action.Key));
        }
        if (thrown is not null || context.Failed)
        {
            await onFailure(thrown, context.FailureMessage);
            return RowActionOutcome.Failed;
        }
        return RowActionOutcome.Succeeded;
    }

    /// <summary>The visible actions grouped for a menu: groups in the order of their first visible action, actions without a group together.</summary>
    public static List<List<DataListRowAction<TItem>>> Group(IEnumerable<DataListRowAction<TItem>> visible)
    {
        var groups = new List<List<DataListRowAction<TItem>>>();
        var index = new Dictionary<string, List<DataListRowAction<TItem>>>(StringComparer.Ordinal);
        List<DataListRowAction<TItem>>? ungrouped = null;
        foreach (var action in visible)
        {
            List<DataListRowAction<TItem>> group;
            if (action.Group is null)
            {
                if (ungrouped is null) groups.Add(ungrouped = []);
                group = ungrouped;
            }
            else if (!index.TryGetValue(action.Group, out group!))
            {
                groups.Add(group = []);
                index[action.Group] = group;
            }
            group.Add(action);
        }
        return groups;
    }
}
