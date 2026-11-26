using Klyroo.UI.Internal;
using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Klyroo.UI;

/// <summary>Centered wrapper with a maximum width and responsive horizontal padding.</summary>
public sealed class KlyContainer : KlyComponentBase
{
    private SlotSet _slots = default!;

    /// <summary>Content of the container.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The element to render, <see cref="KlyElement.Div"/> by default: <see cref="KlyElement.Main"/>, <see cref="KlyElement.Section"/>, <see cref="KlyElement.Header"/>…</summary>
    [Parameter] public KlyElement As { get; set; } = KlyElement.Div;

    /// <summary>Maximum width: Xs 42rem, Sm 56rem, Md 80rem (default), Lg the 2xl breakpoint, Xl none.</summary>
    [Parameter] public KlySize Size { get; set; } = KlySize.Md;
    /// <summary>Per-instance class overrides of the slots of the container, see <see cref="ContainerUi"/>.</summary>
    [Parameter] public ContainerUi? Ui { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() =>
        _slots = ResolveSlots(Options.Container, new Dictionary<string, string?> { ["size"] = VariantValue.Of(Size) }, Ui, "base");

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Wire.Of(As));
        builder.AddAttribute(1, "class", _slots["base"]);
        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}
