namespace Klyroo.UI.Maui;

/// <summary>The arguments of the <c>ValueChanged</c> event of the input controls of Klyroo.UI.Maui: the value before and after.</summary>
/// <typeparam name="T">Type of the value.</typeparam>
public sealed class KlyInputChangedEventArgs<T> : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    /// <param name="oldValue">The value before the change.</param>
    /// <param name="newValue">The value after the change.</param>
    public KlyInputChangedEventArgs(T? oldValue, T? newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>The value before the change.</summary>
    public T? OldValue { get; }

    /// <summary>The value after the change.</summary>
    public T? NewValue { get; }
}
