namespace Klyroo.UI;

/// <summary>
/// Number field with increment and decrement buttons, limits and localized formatting. It binds to any built-in numeric type
/// (<c>int</c>, <c>long</c>, <c>decimal</c>, <c>double</c>, <c>byte</c>...) and to its nullable form; only a nullable type can be empty,
/// clearing the field of a non-nullable one sets zero.
/// </summary>
/// <typeparam name="TNumber">
/// The bound type: <c>sbyte</c>, <c>byte</c>, <c>short</c>, <c>ushort</c>, <c>int</c>, <c>uint</c>, <c>long</c>, <c>ulong</c>, <c>float</c>, <c>double</c>
/// or <c>decimal</c>, or the nullable form of one of them. Any other type throws when the component is first set up.
/// </typeparam>
public partial class KlyInputNumber<TNumber>;
