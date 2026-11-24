using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Lux.UI.Internal;

/// <summary>
/// Infrastructure of <see cref="LxForm"/>; not meant to be used directly.
/// Lists every validation message of an edit context and re-renders when validation changes.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class MessageSummary : ComponentBase, IDisposable
{
    private EditContext? _subscribed;

    [Parameter, EditorRequired] public EditContext For { get; set; } = default!;
    [Parameter] public string? Class { get; set; }

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribed, For))
        {
            return;
        }
        if (_subscribed is not null) _subscribed.OnValidationStateChanged -= OnValidationStateChanged;
        _subscribed = For;
        _subscribed.OnValidationStateChanged += OnValidationStateChanged;
    }

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) => InvokeAsync(StateHasChanged);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var messages = For.GetValidationMessages().Distinct().ToList();
        if (messages.Count == 0)
        {
            return;
        }
        builder.OpenElement(0, "ul");
        builder.AddAttribute(1, "class", Class);
        foreach (var message in messages)
        {
            builder.OpenElement(2, "li");
            builder.AddContent(3, message);
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    public void Dispose()
    {
        if (_subscribed is not null) _subscribed.OnValidationStateChanged -= OnValidationStateChanged;
    }
}
