namespace Klyroo.UI.Maui;

/// <summary>Handed to the view built by <see cref="KlyOverlayHost.ShowAsync{TResult}"/>, so it can end its own dialog.</summary>
/// <typeparam name="TResult">The type of the value the dialog completes with.</typeparam>
public sealed class KlyOverlayContext<TResult>
{
    private readonly Action<TResult> _close;
    private readonly Action _cancel;
    private readonly Func<bool> _isClosed;

    internal KlyOverlayContext(Action<TResult> close, Action cancel, Func<bool> isClosed)
    {
        _close = close;
        _cancel = cancel;
        _isClosed = isClosed;
    }

    /// <summary>Completes the dialog: closes it and hands <paramref name="value"/> to the caller of <c>ShowAsync</c>. Does nothing once the dialog has ended.</summary>
    public void Close(TResult value) => _close(value);

    /// <summary>Dismisses the dialog: closes it and gives the caller a canceled result. Does nothing once the dialog has ended.</summary>
    public void Cancel() => _cancel();

    /// <summary>True once the dialog has been completed or canceled.</summary>
    public bool IsClosed => _isClosed();
}
