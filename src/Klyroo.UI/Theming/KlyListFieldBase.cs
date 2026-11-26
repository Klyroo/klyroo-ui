using System.Diagnostics.CodeAnalysis;
using Klyroo.UI.Internal;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI.Theming;

/// <summary>
/// Base of form controls whose value is a list kept by a custom element: the element reports changes
/// with <c>kly-change</c> (the invariant text of the values joined by a separator), the component renders the list back in
/// <c>data-values</c>. Unbound, no handler is attached and the browser keeps the state.
/// </summary>
/// <typeparam name="TValue">Type of one value of the list.</typeparam>
public abstract class KlyListFieldBase<TValue> : KlyFieldBase<IReadOnlyList<TValue>>
{
    private int _sequence;

    private protected int? SequenceAttribute => ValueChanged.HasDelegate ? _sequence : null;

    private protected string DataValues => ListValue.Join(Value?.Select(v => OptionValue.Key(v)).Distinct(StringComparer.Ordinal));

    private protected bool IsSelected(TValue value) => SelectedKeys.Contains(OptionValue.Key(value));

    private IReadOnlyList<TValue>? _keysOf;
    private int _keysCount = -1;
    private HashSet<string> _keys = new(StringComparer.Ordinal);

    /// <summary>The text of the selected values, kept while the list stays the same, so that a long list of options is not compared with every value of a long selection.</summary>
    private HashSet<string> SelectedKeys
    {
        get
        {
            if (!ReferenceEquals(_keysOf, Value) || _keysCount != (Value?.Count ?? 0))
            {
                _keysOf = Value;
                _keysCount = Value?.Count ?? 0;
                _keys = Value is null ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(Value.Select(v => OptionValue.Key(v)), StringComparer.Ordinal);
            }
            return _keys;
        }
    }

    /// <summary>Whether an option, listed from the items or from a remote source, is one of the selected values.</summary>
    private protected bool IsSelected(IKlySelectItem item) => SelectedKeys.Contains(OptionValue.Key(item.Value));

    private protected EventCallback<KlyChangeEventArgs> ChangeHandler =>
        ValueChanged.HasDelegate ? EventCallback.Factory.Create<KlyChangeEventArgs>(this, OnChange) : default;

    /// <summary>The value the browser reported as text: an option's value, or for strings the text itself. False when no value has that text (it is then left out of the list).</summary>
    private protected abstract bool TryParseKey(string key, out TValue value);

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(KlyChangeEventArgs))]
    private async Task OnChange(KlyChangeEventArgs e)
    {
        _sequence = e.Sequence;
        var values = new List<TValue>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in ListValue.Split(e.Value))
        {
            // One entry per option, however many times the browser names it.
            if (seen.Add(key) && TryParseKey(key, out var parsed)) values.Add(parsed);
        }
        if (ListValue.Same(values, Value))
        {
            return;
        }
        Value = values;
        await ValueChanged.InvokeAsync(values);
        NotifyFieldChanged();
    }

    /// <summary>Attributes of a focusable part of the control (trigger, field): id, description, validity, extra attributes.</summary>
    private protected Dictionary<string, object> PartAttributes(string controlClass, EditBinding binding, string? role = null)
    {
        var attributes = ControlAttributes(controlClass);
        attributes.Remove("required");
        attributes.Remove("disabled");
        if (Disabled) attributes["disabled"] = true;
        if (Required || Field?.Required == true) attributes["aria-required"] = "true";
        if (binding.Invalid) attributes["aria-invalid"] = "true";
        attributes.AddFieldClass(binding);
        if (role is not null) attributes["role"] = role;
        return attributes;
    }
}

/// <summary>
/// Base of form controls whose value is a list of strings kept by a custom element: the element reports changes
/// with <c>kly-change</c> (the values joined by a separator), the component renders the list back in
/// <c>data-values</c>. Unbound, no handler is attached and the browser keeps the state.
/// </summary>
public abstract class KlyListFieldBase : KlyListFieldBase<string>
{
    private protected override bool TryParseKey(string key, out string value)
    {
        value = key;
        return true;
    }
}
