using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tools;

/// <summary>
/// Base of the parts of a <see cref="BxDataList{TItem}"/>: takes the cascaded <see cref="DataListContext{TItem}"/> and renders again when it changes.
/// Derive from it to write a part of your own.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
public abstract class DataListPartBase<TItem> : BxToolsComponentBase, IDisposable
{
    private DataListContext<TItem>? _subscribed;

    /// <summary>The list this part belongs to.</summary>
    [CascadingParameter] protected DataListContext<TItem> Context { get; set; } = default!;

    /// <summary>Per-instance classes for each slot.</summary>
    [Parameter] public DataListUi? Ui { get; set; }

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException($"{GetType().Name} must be placed inside a {nameof(BxDataList<TItem>)}.");
        }
        _subscribed = Context;
        Context.Changed += OnContextChanged;
    }

    private int _renderQueued;

    // Notifications that arrive before the render runs (the controller announces the start and the end of a load, the list a changed state) make one render.
    private void OnContextChanged()
    {
        if (Interlocked.Exchange(ref _renderQueued, 1) == 1) return;
        try
        {
            _ = InvokeAsync(() =>
            {
                Interlocked.Exchange(ref _renderQueued, 0);
                OnContextUpdated();
                StateHasChanged();
            });
        }
        catch (ObjectDisposedException)
        {
            // The renderer went away with the page.
        }
    }

    /// <summary>Called on the renderer's thread when the list announced a change, just before this part renders again. Override it to read the new state of the list (a part that keeps a draft of it).</summary>
    protected virtual void OnContextUpdated()
    {
    }

    /// <summary>Unsubscribes from the list.</summary>
    public virtual void Dispose()
    {
        if (_subscribed is not null) _subscribed.Changed -= OnContextChanged;
    }
}
