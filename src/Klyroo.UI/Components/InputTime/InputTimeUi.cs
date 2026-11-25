using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyInputTime"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record InputTimeUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyInputTime"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>segment</c> slot of <see cref="KlyInputTime"/>, after the theme and the app configuration.</summary>
    public string? Segment { get; init; }
    /// <summary>Classes merged into the <c>separator</c> slot of <see cref="KlyInputTime"/>, after the theme and the app configuration.</summary>
    public string? Separator { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "segment" => Segment,
        "separator" => Separator,
        _ => null,
    };
}
