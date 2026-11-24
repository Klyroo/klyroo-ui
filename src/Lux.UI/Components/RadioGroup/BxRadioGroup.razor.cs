using System.Diagnostics.CodeAnalysis;

namespace Lux.UI;

// InputRadioGroup<TValue> requires this annotation to stay trim-safe.
public partial class BxRadioGroup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>;
