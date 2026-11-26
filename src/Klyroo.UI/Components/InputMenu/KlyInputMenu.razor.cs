namespace Klyroo.UI;

/// <summary>Text input with a list of suggestions that filters as the user types, bound to the typed text (a string) or, with <c>Strict</c> or any other type, to the chosen option's value (a number, an enum, a <see cref="Guid"/>...). The options can be static or fetched from .NET.</summary>
/// <typeparam name="TValue">Type of the bound value. Only <see cref="string"/> can be typed free; any other type behaves as <c>Strict</c>.</typeparam>
public partial class KlyInputMenu<TValue>;
