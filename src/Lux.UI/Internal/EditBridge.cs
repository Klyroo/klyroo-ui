using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Lux.UI.Internal;

/// <summary>Infrastructure of Blux form controls; not meant to be used directly. What a custom control needs from an <c>EditForm</c> field: its form name and its validation state.</summary>
public readonly record struct EditBinding(string? Name, bool Invalid, string? CssClass = null)
{
    /// <summary><paramref name="classes"/> followed by the classes of the field's <c>FieldCssClassProvider</c> (valid, modified, invalid, or the app's own), as <c>InputText</c> adds them to its input.</summary>
    public string? WithFieldClass(string? classes) =>
        string.IsNullOrEmpty(CssClass) ? classes : string.IsNullOrEmpty(classes) ? CssClass : $"{classes} {CssClass}";

    internal static string? Of(EditContext context, FieldIdentifier field) => context.FieldCssClass(field);
}

internal static class EditBindingExtensions
{
    /// <summary>Appends the field's validation classes to the <c>class</c> of the element that carries <c>aria-invalid</c>.</summary>
    internal static void AddFieldClass(this Dictionary<string, object> attributes, EditBinding binding)
    {
        if (string.IsNullOrEmpty(binding.CssClass)) return;
        attributes["class"] = binding.WithFieldClass(attributes.TryGetValue("class", out var existing) ? existing?.ToString() : null)!;
    }
}

/// <summary>
/// Infrastructure of Blux form controls; not meant to be used directly.
/// Lets a control with custom markup take part in an <c>EditForm</c> like a built-in input: <see cref="InputBase{TValue}"/>
/// computes the posted field name and re-renders on validation changes; the control renders its own markup.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class EditBridge : InputBase<string?>
{
    [Parameter, EditorRequired] public RenderFragment<EditBinding> ChildContent { get; set; } = default!;

    protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        builder.AddContent(0, ChildContent(new EditBinding(NameAttributeValue, EditContext.GetValidationMessages(FieldIdentifier).Any(), EditBinding.Of(EditContext, FieldIdentifier))));

    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}

/// <summary>
/// Infrastructure of Blux form controls; not meant to be used directly. Same as <see cref="EditBridge"/> for controls
/// whose bound value is not a string (dates, times).
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class FieldBridge<TValue> : InputBase<TValue>
{
    [Parameter, EditorRequired] public RenderFragment<EditBinding> ChildContent { get; set; } = default!;

    protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        builder.AddContent(0, ChildContent(new EditBinding(NameAttributeValue, EditContext.GetValidationMessages(FieldIdentifier).Any(), EditBinding.Of(EditContext, FieldIdentifier))));

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage) =>
        throw new NotSupportedException();
}

/// <summary>Renders its content after its earlier siblings, so child components can register first (table columns).</summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class Defer : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
}
