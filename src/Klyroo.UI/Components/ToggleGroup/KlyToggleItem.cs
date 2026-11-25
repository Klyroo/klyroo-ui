using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>A button of a <see cref="KlyToggleGroup"/>.</summary>
/// <param name="Value">Value of the item in the selection of the group.</param>
/// <param name="Label">Text of the button.</param>
public sealed record KlyToggleItem(string Value, string Label)
{
    /// <summary>Disables the button.</summary>
    public bool Disabled { get; init; }
    /// <summary>Icon of the button, as markup such as a <see cref="KlyIcon"/>.</summary>
    public RenderFragment? Icon { get; init; }
}

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyToggleGroup"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record ToggleGroupUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyToggleGroup"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyToggleGroup"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyToggleGroup"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "item" => Item,
        "icon" => Icon,
        _ => null,
    };
}
