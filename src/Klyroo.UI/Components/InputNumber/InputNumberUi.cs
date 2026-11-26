using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyInputNumber{TNumber}"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record InputNumberUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyInputNumber{TNumber}"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyInputNumber{TNumber}"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>decrement</c> slot of <see cref="KlyInputNumber{TNumber}"/>, after the theme and the app configuration.</summary>
    public string? Decrement { get; init; }
    /// <summary>Classes merged into the <c>increment</c> slot of <see cref="KlyInputNumber{TNumber}"/>, after the theme and the app configuration.</summary>
    public string? Increment { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyInputNumber{TNumber}"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "decrement" => Decrement,
        "increment" => Increment,
        "icon" => Icon,
        _ => null,
    };
}
