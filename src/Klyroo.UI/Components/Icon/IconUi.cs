using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyIcon"/> slots.</summary>
public sealed record IconUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyIcon"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "base" => Base,
        _ => null,
    };
}
