namespace Klyroo.UI;

/// <summary>
/// A highlighted number with a title, unit, delta badge, sparkline and caption. <typeparamref name="TValue"/> is a built-in number type
/// (<see cref="sbyte"/> to <see cref="ulong"/>, <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/>) or its nullable form, which shows a dash when it has no value:
/// Blazor infers it from <see cref="Value"/> or <see cref="Delta"/> (write the number as an expression, <c>Value="@amount"</c>). Another type throws an <see cref="InvalidOperationException"/> that lists the supported ones.
/// </summary>
/// <typeparam name="TValue">The number type of the value and of its change.</typeparam>
public partial class KlyStat<TValue>;
