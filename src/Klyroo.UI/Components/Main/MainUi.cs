using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyMain"/> slots.</summary>
public sealed record MainUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyMain"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "base" => Base,
        _ => null,
    };
}
