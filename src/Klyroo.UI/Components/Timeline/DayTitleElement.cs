using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Klyroo.UI.Internal;

/// <summary>The title of a day in the schedule layout of <see cref="KlyTimeline"/>: a heading of the given level, or a paragraph without one.</summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class DayTitleElement : ComponentBase
{
    /// <summary>Heading level, 1 to 6, or null for a paragraph.</summary>
    [Parameter] public int? Level { get; set; }
    /// <summary>Classes of the element.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>The <c>data-slot</c> of the element.</summary>
    [Parameter] public string? Slot { get; set; }
    /// <summary>Content of the title.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Level is { } level ? "h" + Math.Clamp(level, 1, 6) : "p");
        builder.AddAttribute(1, "class", Class);
        builder.AddAttribute(2, "data-slot", Slot);
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}
