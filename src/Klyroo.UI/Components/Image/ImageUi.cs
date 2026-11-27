using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of the slots of the figure of <see cref="KlyImage"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record ImageUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>figure</c> slot of <see cref="KlyImage"/>, after the theme and the app configuration.</summary>
    public string? Figure { get; init; }
    /// <summary>Classes merged into the <c>caption</c> slot of <see cref="KlyImage"/>, after the theme and the app configuration.</summary>
    public string? Caption { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "figure" => Figure,
        "caption" => Caption,
        _ => null,
    };
}
