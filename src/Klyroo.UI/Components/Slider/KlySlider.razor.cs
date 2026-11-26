namespace Klyroo.UI;

/// <summary>
/// Slider with one thumb, bound to a number of any built-in numeric type (<c>double</c>, <c>int</c>, <c>decimal</c>, <c>float</c>...) or its nullable form.
/// A nullable slider with no value rests on its minimum; moving it sets a value.
/// </summary>
/// <typeparam name="TNumber">
/// The bound type: <c>sbyte</c>, <c>byte</c>, <c>short</c>, <c>ushort</c>, <c>int</c>, <c>uint</c>, <c>long</c>, <c>ulong</c>, <c>float</c>, <c>double</c>
/// or <c>decimal</c>, or the nullable form of one of them. Any other type throws when the component is first set up.
/// </typeparam>
public partial class KlySlider<TNumber>;
