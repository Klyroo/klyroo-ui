using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI.Tools.Tests;

/// <summary>A part written the way an app writes one: it lists the columns the list registered, visible or not.</summary>
public sealed class DataListColumnsProbe : DataListPartBase<Person>
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-testid", "columns");
        builder.AddContent(2, string.Join(",", Context.Columns.Select(c => $"{c.EffectiveKey}{(c.Visible ? "" : "!")}")));
        builder.CloseElement();
    }
}
