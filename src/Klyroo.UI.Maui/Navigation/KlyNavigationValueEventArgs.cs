namespace Klyroo.UI.Maui;

/// <summary>The value of a navigation control after it changed: the selected tab, the open accordion items, the selected tree node, the sizes of a splitter.</summary>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed class KlyNavigationValueEventArgs<T> : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    /// <param name="value">The new value.</param>
    /// <param name="oldValue">The value before the change.</param>
    public KlyNavigationValueEventArgs(T value, T oldValue)
    {
        Value = value;
        OldValue = oldValue;
    }

    /// <summary>The new value.</summary>
    public T Value { get; }

    /// <summary>The value before the change.</summary>
    public T OldValue { get; }
}
