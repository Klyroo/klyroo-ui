using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>A tab of <see cref="KlyTabs"/>.</summary>
/// <param name="Label">Text of the tab button.</param>
public sealed record KlyTabItem(string Label)
{
    /// <summary>Identifies the tab in <see cref="KlyTabs.Value"/>. Defaults to its index.</summary>
    public string? Value { get; init; }
    /// <summary>Icon shown before the label, as markup such as a <see cref="KlyIcon"/>.</summary>
    public RenderFragment? Icon { get; init; }
    /// <summary>Content of the panel the tab shows.</summary>
    public RenderFragment? Content { get; init; }
    /// <summary>Disables the tab.</summary>
    public bool Disabled { get; init; }
}
