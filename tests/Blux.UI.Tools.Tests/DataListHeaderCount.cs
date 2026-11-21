using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI.Tools.Tests;

/// <summary>A part of a list that shows how many rows the page has, to prove that a header template renders inside the list.</summary>
public sealed class DataListHeaderCount : DataListPartBase<Person>
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-testid", "header-count");
        builder.AddContent(2, $"{Context.Rows.Count} rows");
        builder.CloseElement();
    }
}
