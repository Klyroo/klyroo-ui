using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Lux.UI.Internal;

/// <summary>
/// Infrastructure of Blux form controls; not meant to be used directly.
/// <see cref="EditBridge"/> for values that are not strings (lists, numbers, booleans).
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class ValueBridge<TValue> : InputBase<TValue>
{
    [Parameter, EditorRequired] public RenderFragment<EditBinding> ChildContent { get; set; } = default!;

    protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        builder.AddContent(0, ChildContent(new EditBinding(NameAttributeValue, EditContext.GetValidationMessages(FieldIdentifier).Any(), EditBinding.Of(EditContext, FieldIdentifier))));

    protected override bool TryParseValueFromString(string? value, out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = default!;
        validationErrorMessage = "This control does not parse text.";
        return false;
    }
}

/// <summary>Lists of strings travel through browser events and attributes as one string.</summary>
internal static class ListValue
{
    /// <summary>Same separator as <c>form2-shared.js</c>.</summary>
    public const char Separator = '\u001f';

    public static string Join(IEnumerable<string>? values) => values is null ? "" : string.Join(Separator, values);

    public static IReadOnlyList<string> Split(string? text) => string.IsNullOrEmpty(text) ? [] : text.Split(Separator);

    public static bool Same(IReadOnlyList<string>? a, IReadOnlyList<string>? b) =>
        (a ?? []).SequenceEqual(b ?? []);
}
