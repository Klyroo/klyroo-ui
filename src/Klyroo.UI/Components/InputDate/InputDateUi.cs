using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyInputDate"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record InputDateUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyInputDate"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyInputDate"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>trigger</c> slot of <see cref="KlyInputDate"/>, after the theme and the app configuration.</summary>
    public string? Trigger { get; init; }
    /// <summary>Classes merged into the <c>triggerButton</c> slot of <see cref="KlyInputDate"/>, after the theme and the app configuration.</summary>
    public string? TriggerButton { get; init; }
    /// <summary>Classes merged into the <c>triggerIcon</c> slot of <see cref="KlyInputDate"/>, after the theme and the app configuration.</summary>
    public string? TriggerIcon { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyInputDate"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "trigger" => Trigger,
        "triggerButton" => TriggerButton,
        "triggerIcon" => TriggerIcon,
        "content" => Content,
        _ => null,
    };
}
