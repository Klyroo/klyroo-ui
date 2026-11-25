using System.Diagnostics.CodeAnalysis;

namespace Klyroo.UI;

// InputRadioGroup<TValue> requires this annotation to stay trim-safe.
/// <summary>Group of radio buttons bound to one value.</summary>
public partial class KlyRadioGroup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>;
