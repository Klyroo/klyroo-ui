namespace Klyroo.UI.Maui;

/// <summary>The values of a <see cref="KlyAuthForm"/> when it is submitted.</summary>
public sealed class KlyAuthFormSubmitEventArgs : EventArgs
{
    /// <summary>Creates the arguments of a submit.</summary>
    /// <param name="values">The values by field name.</param>
    public KlyAuthFormSubmitEventArgs(IReadOnlyDictionary<string, string?> values) => Values = values;

    /// <summary>The values by field name. A checkbox is <c>"true"</c> or <c>"false"</c>.</summary>
    public IReadOnlyDictionary<string, string?> Values { get; }
}
