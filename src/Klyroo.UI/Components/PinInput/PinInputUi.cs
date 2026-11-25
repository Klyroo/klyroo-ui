using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Characters a <see cref="KlyPinInput"/> accepts.</summary>
public enum PinInputType
{
    /// <summary>Digits only.</summary>
    Numeric,
    /// <summary>Letters and digits.</summary>
    Alphanumeric,
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyPinInput"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record PinInputUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyPinInput"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>cell</c> slot of <see cref="KlyPinInput"/>, after the theme and the app configuration.</summary>
    public string? Cell { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "cell" => Cell,
        _ => null,
    };
}
