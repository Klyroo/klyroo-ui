using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>An item of <see cref="KlyAccordion"/>: a header label and a content that opens and closes.</summary>
/// <param name="Label">Text of the header button of the item.</param>
public sealed record KlyAccordionItem(string Label)
{
    /// <summary>Identifies the item in <see cref="KlyAccordion.Value"/>. Defaults to its index.</summary>
    public string? Value { get; init; }
    /// <summary>Icon shown before the label, as markup such as a <see cref="KlyIcon"/>.</summary>
    public RenderFragment? Icon { get; init; }
    /// <summary>Content of the panel that opens under the header.</summary>
    public RenderFragment? Content { get; init; }
    /// <summary>Prevents the user from opening or closing the item.</summary>
    public bool Disabled { get; init; }
}
