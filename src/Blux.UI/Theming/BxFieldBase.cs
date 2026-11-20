using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Blux.UI.Theming;

/// <summary>
/// Base of form controls. Inside an <c>EditForm</c> with <c>@bind-Value</c>, a control takes part in validation and
/// posts under the bound field's name; elsewhere it is a plain bindable input named by <see cref="Name"/>.
/// Inside a <see cref="BxFormField"/>, it takes the field's id and description.
/// </summary>
public abstract class BxFieldBase<TValue> : BxComponentBase
{
    private string? _generatedId;

    [CascadingParameter] private protected EditContext? EditContext { get; set; }
    [CascadingParameter] private protected FormFieldContext? Field { get; set; }

    [Parameter] public TValue? Value { get; set; }
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }
    [Parameter] public Expression<Func<TValue?>>? ValueExpression { get; set; }

    /// <summary>Form field name outside an <c>EditForm</c>. Inside one, the name comes from the bound expression.</summary>
    [Parameter] public string? Name { get; set; }
    [Parameter] public string? Id { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public BxColor Color { get => _colorParameter ?? DefaultColor(BxColor.Primary); set => _colorParameter = value; }
    private BxColor? _colorParameter;

    /// <summary>Defaults to the size of the enclosing <see cref="BxFormField"/>, or medium.</summary>
    [Parameter] public BxSize? Size { get; set; }

    private protected bool UseEditContext => EditContext is not null && ValueExpression is not null;

    private protected string ControlId => Id ?? Field?.InputId ?? (_generatedId ??= NewId(GetType().Name.Split('`')[0].ToLowerInvariant()));

    private protected BxSize EffectiveSize => Size ?? Field?.Size ?? DefaultSize(BxSize.Md);

    private protected Task SetValue(TValue? value) => ValueChanged.InvokeAsync(value);

    /// <summary>Attributes of the native control: id, state, description, then unmatched attributes.</summary>
    private protected Dictionary<string, object> ControlAttributes(string controlClass, string? ownDescriptionId = null)
    {
        var attributes = new Dictionary<string, object>
        {
            ["id"] = ControlId,
            ["class"] = controlClass,
            ["data-slot"] = "control",
        };
        if (Disabled) attributes["disabled"] = true;
        if (Required || Field?.Required == true) attributes["required"] = true;
        var describedBy = FormFieldContext.Join(ownDescriptionId, Field?.DescribedBy);
        if (describedBy is not null) attributes["aria-describedby"] = describedBy;
        // Inside an EditForm, InputBase sets aria-invalid from validation; elsewhere the form field decides.
        if (!UseEditContext && Field?.Invalid == true) attributes["aria-invalid"] = "true";
        if (AdditionalAttributes is not null)
        {
            foreach (var (key, value) in AdditionalAttributes)
            {
                attributes[key] = value;
            }
        }
        return attributes;
    }

    /// <summary>Tells the edit context a custom control changed its field, so validation runs as for built-in inputs.</summary>
    private protected void NotifyFieldChanged()
    {
        if (UseEditContext)
        {
            EditContext!.NotifyFieldChanged(FieldIdentifier.Create(ValueExpression!));
        }
    }
}
