using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

public sealed record KlyTabItem(string Label)
{
    /// <summary>Identifies the tab in <see cref="KlyTabs.Value"/>. Defaults to its index.</summary>
    public string? Value { get; init; }
    public RenderFragment? Icon { get; init; }
    public RenderFragment? Content { get; init; }
    public bool Disabled { get; init; }
}
