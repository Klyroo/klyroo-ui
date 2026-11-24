using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Lux.UI.Internal;

/// <summary>Renders h1…h6 from a number.</summary>
public sealed class Heading : ComponentBase
{
    [Parameter] public int Level { get; set; } = 2;
    [Parameter] public string? Class { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "h" + Math.Clamp(Level, 1, 6));
        builder.AddAttribute(1, "class", Class);
        builder.AddContent(2, ChildContent);
        builder.CloseElement();
    }
}
