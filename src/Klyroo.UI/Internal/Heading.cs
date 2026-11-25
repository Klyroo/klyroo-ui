using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Klyroo.UI.Internal;

/// <summary>Renders h1…h6 from a number.</summary>
public sealed class Heading : ComponentBase
{
    /// <summary>Heading level, 1 to 6. Defaults to 2.</summary>
    [Parameter] public int Level { get; set; } = 2;
    /// <summary>Classes of the heading element.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Content of the heading.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "h" + Math.Clamp(Level, 1, 6));
        builder.AddAttribute(1, "class", Class);
        builder.AddContent(2, ChildContent);
        builder.CloseElement();
    }
}
