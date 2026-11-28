using System.Diagnostics.CodeAnalysis;

namespace Klyroo.UI.Maui;

/// <summary>How a dialog opened with <see cref="KlyOverlayHost.ShowAsync{TResult}"/> ended.</summary>
/// <typeparam name="TResult">The type of the value the dialog completes with.</typeparam>
public sealed record KlyOverlayResult<TResult>
{
    internal KlyOverlayResult(bool canceled, TResult? value)
    {
        Canceled = canceled;
        Value = value;
    }

    /// <summary>True when the dialog was dismissed (a tap outside, the back button, the close button, <see cref="KlyOverlayContext{TResult}.Cancel"/> or a cancellation token) instead of completed.</summary>
    public bool Canceled { get; }

    /// <summary>The value passed to <see cref="KlyOverlayContext{TResult}.Close"/>, or <c>default</c> when the dialog was canceled.</summary>
    public TResult? Value { get; }

    /// <summary>Gets the value when the dialog completed with one (it was not canceled).</summary>
    public bool TryGetValue([MaybeNullWhen(false)] out TResult value)
    {
        if (!Canceled)
        {
            value = Value!;
            return true;
        }
        value = default;
        return false;
    }
}
