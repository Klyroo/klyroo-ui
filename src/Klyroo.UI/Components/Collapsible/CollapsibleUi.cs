using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyCollapsible"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record CollapsibleUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyCollapsible"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>trigger</c> slot of <see cref="KlyCollapsible"/>, after the theme and the app configuration.</summary>
    public string? Trigger { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyCollapsible"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyCollapsible"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyCollapsible"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "trigger" => Trigger,
        "label" => Label,
        "icon" => Icon,
        "content" => Content,
        _ => null,
    };
}
