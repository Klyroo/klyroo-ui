using System.Diagnostics.CodeAnalysis;

namespace Klyroo.UI;

// InputRadioGroup<TValue> requires this annotation to stay trim-safe.
public partial class KlyRadioGroup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>;
