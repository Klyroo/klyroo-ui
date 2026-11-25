using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

public sealed record KlyToggleItem(string Value, string Label)
{
    public bool Disabled { get; init; }
    public RenderFragment? Icon { get; init; }
}

public sealed record ToggleGroupUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Item { get; init; }
    public string? Icon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "item" => Item,
        "icon" => Icon,
        _ => null,
    };
}
