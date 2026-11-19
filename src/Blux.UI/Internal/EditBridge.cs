using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI.Internal;

/// <summary>Infrastructure of Blux form controls; not meant to be used directly. What a custom control needs from an <c>EditForm</c> field: its form name and its validation state.</summary>
public readonly record struct EditBinding(string? Name, bool Invalid);

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
        builder.AddContent(0, ChildContent(new EditBinding(NameAttributeValue, EditContext.GetValidationMessages(FieldIdentifier).Any())));

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
        builder.AddContent(0, ChildContent(new EditBinding(NameAttributeValue, EditContext.GetValidationMessages(FieldIdentifier).Any())));

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
