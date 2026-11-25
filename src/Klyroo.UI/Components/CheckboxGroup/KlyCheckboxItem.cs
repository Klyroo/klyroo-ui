using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>A checkbox of a <see cref="KlyCheckboxGroup{TValue}"/>.</summary>
/// <param name="Value">Value added to the selection when the checkbox is checked.</param>
/// <param name="Label">Text next to the checkbox.</param>
public sealed record KlyCheckboxItem<TValue>(TValue Value, string Label)
{
    /// <summary>Text under the label.</summary>
    public string? Description { get; init; }
    /// <summary>Disables the checkbox.</summary>
    public bool Disabled { get; init; }
}

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyCheckboxGroup{TValue}"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record CheckboxGroupUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyCheckboxGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>legend</c> slot of <see cref="KlyCheckboxGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Legend { get; init; }
    /// <summary>Classes merged into the <c>fieldset</c> slot of <see cref="KlyCheckboxGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Fieldset { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyCheckboxGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlyCheckboxGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyCheckboxGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyCheckboxGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyCheckboxGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyCheckboxGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyCheckboxGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "legend" => Legend,
        "fieldset" => Fieldset,
        "item" => Item,
        "container" => Container,
        "base" => Base,
        "icon" => Icon,
        "wrapper" => Wrapper,
        "label" => Label,
        "description" => Description,
        _ => null,
    };
}
