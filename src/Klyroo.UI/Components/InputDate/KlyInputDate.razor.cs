using System.Diagnostics.CodeAnalysis;

namespace Klyroo.UI;

/// <summary>
/// Date field with a calendar popup, with localized formatting. It binds to a <c>DateOnly</c>, a <c>DateTime</c> or a <c>DateTimeOffset</c>, or to the nullable form of one of them.
/// Picking a day sets the value to that day at midnight; the time of day of a <c>DateTime</c> or <c>DateTimeOffset</c> is not kept.
/// </summary>
/// <typeparam name="TValue">The bound type: <c>DateOnly</c>, <c>DateTime</c> or <c>DateTimeOffset</c>, or the nullable form of one of them. Any other type throws when the component is first set up.</typeparam>
// InputDate<TValue> requires this annotation to stay trim-safe.
public partial class KlyInputDate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>;
