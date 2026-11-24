using Lux.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Lux.UI;

/// <summary>Centered wrapper with a maximum width and responsive horizontal padding.</summary>
public sealed class LxContainer : LxComponentBase
{
    private SlotSet _slots = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The element to render, "div" by default: "main", "section", "header"…</summary>
    [Parameter] public string As { get; set; } = "div";

    /// <summary>Maximum width: Xs 42rem, Sm 56rem, Md 80rem (default), Lg the 2xl breakpoint, Xl none.</summary>
    [Parameter] public LxSize Size { get; set; } = LxSize.Md;
    [Parameter] public ContainerUi? Ui { get; set; }

    protected override void OnParametersSet() =>
        _slots = ResolveSlots(Options.Container, new Dictionary<string, string?> { ["size"] = VariantValue.Of(Size) }, Ui, "base");

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, As);
        builder.AddAttribute(1, "class", _slots["base"]);
        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}
