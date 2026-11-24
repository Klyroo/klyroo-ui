using Lux.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Lux.UI;

public sealed record BxToggleItem(string Value, string Label)
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
