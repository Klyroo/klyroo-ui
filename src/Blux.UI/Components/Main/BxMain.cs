using Blux.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI;

/// <summary>
/// The main landmark of a page. Its minimum height is the viewport minus the header (<c>--bx-header-height</c>, 4rem by
/// default), so a short page still pushes the footer to the bottom of the screen.
/// </summary>
public sealed class BxMain : BxComponentBase
{
    private SlotSet _slots = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The element to render, "main" by default. A page has only one main landmark: use "div" when the layout already has one.</summary>
    [Parameter] public string As { get; set; } = "main";
    [Parameter] public MainUi? Ui { get; set; }

    protected override void OnParametersSet() =>
        _slots = ResolveSlots(Options.Main, new Dictionary<string, string?>(), Ui, "base");

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, As);
        builder.AddAttribute(1, "class", _slots["base"]);
        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}
