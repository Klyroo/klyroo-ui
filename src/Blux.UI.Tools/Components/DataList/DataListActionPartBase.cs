using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Blux.UI.Tools;

/// <summary>
/// What the parts that show actions share (<see cref="BxDataListSelectionActions{TItem}"/>, <see cref="BxDataListActionsDropdown{TItem}"/>): which actions are visible, disabled or running,
/// the confirmation dialog and the run itself. An action never starts twice at once, and a failure keeps the selection.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class DataListActionPartBase<TItem> : DataListPartBase<TItem>
{
    private readonly HashSet<string> _running = new(StringComparer.Ordinal);
    private bool _disposed;

    /// <summary>The actions, in the order they are shown.</summary>
    [Parameter] public IReadOnlyList<DataListAction<TItem>> Actions { get; set; } = [];

    /// <summary>Raised when an action throws; the part keeps the selection, does not refresh the list and does not rethrow. Without a handler the failure is logged.</summary>
    [Parameter] public EventCallback<DataListActionFailure<TItem>> OnError { get; set; }

    /// <summary>The action waiting for the answer of its confirmation dialog.</summary>
    protected DataListAction<TItem>? Pending { get; private set; }

    /// <summary>Whether the confirmation dialog is open.</summary>
    protected bool ConfirmOpen { get; set; }

    /// <summary>The context the predicates of the actions see: the ticked rows, and the bulk scope.</summary>
    protected DataListActionContext<TItem> Describe() => Context.CreateActionContext(Context.BulkScope, resolveAll: false, Context.LifetimeToken);

    /// <summary>Whether an action is shown for a context.</summary>
    protected static bool IsVisible(DataListAction<TItem> action, DataListActionContext<TItem> context) => action.Visible?.Invoke(context) ?? true;

    /// <summary>Whether an action is shown but cannot start: it says so, or it is running.</summary>
    protected bool IsDisabled(DataListAction<TItem> action, DataListActionContext<TItem> context) => IsRunning(action) || (action.Disabled?.Invoke(context) ?? false);

    /// <summary>Whether an action is running.</summary>
    protected bool IsRunning(DataListAction<TItem> action) => _running.Contains(action.Key);

    /// <summary>The confirmation of the pending action as text: its title and its description for the current rows.</summary>
    protected (string Title, string? Description, string Label, BxColor Color) ConfirmText()
    {
        var action = Pending!;
        var confirm = action.Confirm!;
        return (confirm.Title, confirm.Description?.Invoke(Describe()), confirm.ConfirmLabel ?? ToolsMessages.ActionConfirm, confirm.Color ?? action.Color);
    }

    /// <summary>A click on an action: it runs, or asks first when it has a confirmation.</summary>
    protected async Task RequestAsync(DataListAction<TItem> action)
    {
        var context = Describe();
        if (!IsVisible(action, context) || IsDisabled(action, context)) return;
        if (action.Confirm is not null)
        {
            Pending = action;
            ConfirmOpen = true;
            return;
        }
        await RunAsync(action);
    }

    /// <summary>The answer yes of the confirmation dialog.</summary>
    protected async Task ConfirmAsync()
    {
        ConfirmOpen = false;
        if (Pending is { } action)
        {
            // Pending stays: the dialog is still on screen while it closes, and it reads its text from it.
            var context = Describe();
            // The rows may have changed while the dialog was open (a refresh, a search): the action must still apply.
            if (IsVisible(action, context) && !IsDisabled(action, context)) await RunAsync(action);
        }
    }

    // The modal closes by itself (data-bx-close): its own notice only has to keep the state of the part in step. The pending action stays until the confirm click read it.
    /// <summary>The dialog opened or closed without the confirm button (Escape, Cancel).</summary>
    protected void OnConfirmOpenChanged(bool open) => ConfirmOpen = open;

    private async Task RunAsync(DataListAction<TItem> action)
    {
        if (!_running.Add(action.Key)) return;
        // "Every result" of a list in memory is resolved now: the rows the query matches at the moment of the click.
        var context = Context.CreateActionContext(Context.BulkScope, resolveAll: true, Context.LifetimeToken);
        var failed = false;
        // The rows the action targets are the ones ticked now: rows ticked while it runs are not its business and stay ticked.
        var targeted = Context.SelectedKeys;
        try
        {
            if (action.ClearSelection == DataListSelectionClear.Trigger) Context.ClearSelection();
            StateHasChanged();
            if (action.OnExecute is { } run) await run(context);
        }
        catch (Exception exception) when (!(exception is OperationCanceledException && Context.LifetimeToken.IsCancellationRequested))
        {
            failed = true;
            Context.Logger?.LogError(exception, "The data list action '{Action}' failed.", action.Key);
            if (OnError.HasDelegate) await OnError.InvokeAsync(new DataListActionFailure<TItem>(action, exception));
        }
        finally
        {
            _running.Remove(action.Key);
        }
        if (_disposed) return;
        if (!failed)
        {
            if (action.ClearSelection == DataListSelectionClear.Success) Context.ClearSelection(targeted);
            if (action.RefreshOnSuccess) await Context.RefreshAsync();
        }
        if (!_disposed)
        {
            await InvokeAsync(StateHasChanged);
            if (!failed && action.ClearSelection != DataListSelectionClear.Never) await AfterSelectionClearedAsync();
        }
    }

    /// <summary>Called once an action that clears the selection finished and the part rendered again: the part that had the focus gives it to a control that is still there.</summary>
    protected virtual Task AfterSelectionClearedAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public override void Dispose()
    {
        _disposed = true;
        base.Dispose();
    }
}
