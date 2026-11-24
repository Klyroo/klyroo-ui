using System.Diagnostics.CodeAnalysis;
using Lux.UI.Internal;
using Microsoft.AspNetCore.Components;

namespace Lux.UI.Theming;

/// <summary>
/// Base of form controls whose value is a list of strings kept by a custom element: the element reports changes
/// with <c>bx-change</c> (the values joined by a separator), the component renders the list back in
/// <c>data-values</c>. Unbound, no handler is attached and the browser keeps the state.
/// </summary>
public abstract class BxListFieldBase : BxFieldBase<IReadOnlyList<string>>
{
    private int _sequence;

    private protected int? SequenceAttribute => ValueChanged.HasDelegate ? _sequence : null;

    private protected string DataValues => ListValue.Join(Value);

    private protected bool IsSelected(string value) => Value?.Contains(value) == true;

    private protected EventCallback<BxChangeEventArgs> ChangeHandler =>
        ValueChanged.HasDelegate ? EventCallback.Factory.Create<BxChangeEventArgs>(this, OnChange) : default;

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(BxChangeEventArgs))]
    private async Task OnChange(BxChangeEventArgs e)
    {
        _sequence = e.Sequence;
        var values = ListValue.Split(e.Value);
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
