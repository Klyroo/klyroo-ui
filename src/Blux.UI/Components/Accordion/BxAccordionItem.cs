using Microsoft.AspNetCore.Components;

namespace Blux.UI;

public sealed record BxAccordionItem(string Label)
{
    /// <summary>Identifies the item in <see cref="BxAccordion.Value"/>. Defaults to its index.</summary>
    public string? Value { get; init; }
    public RenderFragment? Icon { get; init; }
    public RenderFragment? Content { get; init; }
    public bool Disabled { get; init; }
}
