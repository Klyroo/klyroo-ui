namespace Klyroo.UI;

/// <summary>
/// What a list control reads of an option, whatever the type of its value: <see cref="KlySelectItem{TValue}"/> is the usual implementation.
/// The remote options of a menu (see <see cref="KlyOptionsResult"/>) are given as this interface, so one provider serves a menu of any value type.
/// </summary>
public interface IKlySelectItem
{
    /// <summary>Value the control takes when the option is chosen, as an object: its type is the one the control is bound to.</summary>
    object? Value { get; }

    /// <summary>Text of the option.</summary>
    string Label { get; }

    /// <summary>The option is listed but cannot be chosen.</summary>
    bool Disabled { get; }

    /// <summary>A number shown at the end of the row (matches, stock, unread): formatted for the locale, and not searched or typed ahead.</summary>
    int? Count { get; }

    /// <summary>An icon drawn before the label, by name like every icon of Klyroo (a Lucide name, or any Iconify name the build bundles).</summary>
    string? Icon { get; }

    /// <summary>Lists the item under a heading; items without a group are listed first, with no heading.</summary>
    string? Group { get; }
}

/// <summary>
/// An option with a typed value. The interface is covariant in the value, so a list of <see cref="KlySelectItem"/> (strings) is accepted by a control
/// bound to a <c>string?</c> property as well as one bound to a <c>string</c>.
/// </summary>
/// <typeparam name="TValue">Type of the value the control takes when the option is chosen.</typeparam>
public interface IKlySelectItem<out TValue> : IKlySelectItem
{
    /// <summary>Value the control takes when the option is chosen.</summary>
    new TValue Value { get; }
}

/// <summary>An option of <see cref="KlySelect{TValue}"/>, <see cref="KlySelectMenu{TValue}"/>, <see cref="KlyInputMenu{TValue}"/> and <see cref="KlyListbox{TValue}"/>.</summary>
/// <typeparam name="TValue">Type of the value the control takes: <see cref="string"/>, a number, an enum, a <see cref="Guid"/>, a date, or the nullable form of a value type.</typeparam>
/// <param name="Value">Value the control takes when the option is chosen.</param>
/// <param name="Label">Text of the option.</param>
public record KlySelectItem<TValue>(TValue Value, string Label) : IKlySelectItem<TValue>
{
    object? IKlySelectItem.Value => Value;

    /// <summary>The option is listed but cannot be chosen.</summary>
    public bool Disabled { get; init; }

    /// <summary>A number shown at the end of the row (matches, stock, unread): formatted for the locale, and not searched or typed ahead.</summary>
    public int? Count { get; init; }

    /// <summary>An icon drawn before the label, by name like every icon of Klyroo (a Lucide name, or any Iconify name the build bundles).</summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Lists the item under a heading. The items without a group are listed first, in their order and with no heading, then
    /// each group in the order of its first item, with all its items, wherever they are in the list (so an item without a group
    /// never sits between two groups, where it would look like part of one). The heading is not an option: the arrow keys and the typeahead skip it,
    /// and a filter hides it when none of its items matches. The list has role group, named by the heading.
    /// </summary>
    public string? Group { get; init; }
}

/// <summary>
/// An option whose value is a string, so <c>new KlySelectItem("fr", "France")</c> keeps working. It is a <see cref="KlySelectItem{TValue}"/> of
/// <see cref="string"/>: lists of it are accepted wherever a list of <c>KlySelectItem&lt;string&gt;</c> is.
/// </summary>
/// <param name="Value">Value the control takes when the option is chosen.</param>
/// <param name="Label">Text of the option.</param>
public sealed record KlySelectItem(string Value, string Label) : KlySelectItem<string>(Value, Label);
