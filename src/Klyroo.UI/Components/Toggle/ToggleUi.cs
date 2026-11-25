using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Look of a <see cref="KlyToggle"/> and of the buttons of a <see cref="KlyToggleGroup"/>.</summary>
public enum ToggleVariant
{
    /// <summary>A border around the button.</summary>
    Outline,
    /// <summary>A tinted background when pressed.</summary>
    Soft,
    /// <summary>No border and no background until hovered or pressed.</summary>
    Ghost,
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyToggle"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record ToggleUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyToggle"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyToggle"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyToggle"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "base" => Base,
        "label" => Label,
        "icon" => Icon,
        _ => null,
    };
}
