using System.Diagnostics.CodeAnalysis;

namespace Klyroo.UI;

/// <summary>
/// Date field with a calendar popup, with localized formatting. It binds to a <c>DateOnly</c>, a <c>DateTime</c> or a <c>DateTimeOffset</c>, or to the nullable form of one of them.
/// Picking a day keeps the time of day of the current value (and the kind of a <c>DateTime</c>, the offset of a <c>DateTimeOffset</c>); a value that had none (null before, or the default) starts at midnight, a <c>DateTimeOffset</c> in the offset of the server's local time zone on that day.
/// </summary>
/// <typeparam name="TValue">The bound type: <c>DateOnly</c>, <c>DateTime</c> or <c>DateTimeOffset</c>, or the nullable form of one of them. Any other type throws when the component is first set up.</typeparam>
// InputDate<TValue> requires this annotation to stay trim-safe.
public partial class KlyInputDate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>;
