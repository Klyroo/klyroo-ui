using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyContainer"/> slots.</summary>
public sealed record ContainerUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyContainer"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "base" => Base,
        _ => null,
    };
}
