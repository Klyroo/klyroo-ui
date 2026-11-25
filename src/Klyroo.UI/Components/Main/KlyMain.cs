using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Klyroo.UI;

/// <summary>
/// The main landmark of a page. Its minimum height is the viewport minus the header (<c>--klyroo-header-height</c>, 4rem by
/// default), so a short page still pushes the footer to the bottom of the screen.
/// </summary>
public sealed class KlyMain : KlyComponentBase
{
    private SlotSet _slots = default!;

    /// <summary>Content of the main area.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The element to render, "main" by default. A page has only one main landmark: use "div" when the layout already has one.</summary>
    [Parameter] public string As { get; set; } = "main";
    /// <summary>Per-instance class overrides of the slots of the main area, see <see cref="MainUi"/>.</summary>
    [Parameter] public MainUi? Ui { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() =>
        _slots = ResolveSlots(Options.Main, new Dictionary<string, string?>(), Ui, "base");

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, As);
        builder.AddAttribute(1, "class", _slots["base"]);
        // Marks the block that owns the bottom safe area inset when no footer follows (a dashboard inside leaves it to it).
        builder.AddAttribute(2, "data-kly-main", "");
        builder.AddMultipleAttributes(3, AdditionalAttributes);
        builder.AddContent(4, ChildContent);
        builder.CloseElement();
    }
}
