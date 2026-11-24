using Microsoft.AspNetCore.Components;

namespace Lux.UI;

public sealed record LxAccordionItem(string Label)
{
    /// <summary>Identifies the item in <see cref="LxAccordion.Value"/>. Defaults to its index.</summary>
    public string? Value { get; init; }
    public RenderFragment? Icon { get; init; }
    public RenderFragment? Content { get; init; }
    public bool Disabled { get; init; }
}
