using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyInputRating"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record InputRatingUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyInputRating"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>input</c> slot of <see cref="KlyInputRating"/>, after the theme and the app configuration.</summary>
    public string? Input { get; init; }
    /// <summary>Classes merged into the <c>star</c> slot of <see cref="KlyInputRating"/>, after the theme and the app configuration.</summary>
    public string? Star { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyInputRating"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>fill</c> slot of <see cref="KlyInputRating"/>, after the theme and the app configuration.</summary>
    public string? Fill { get; init; }
    /// <summary>Classes merged into the <c>filledIcon</c> slot of <see cref="KlyInputRating"/>, after the theme and the app configuration.</summary>
    public string? FilledIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "input" => Input,
        "star" => Star,
        "icon" => Icon,
        "fill" => Fill,
        "filledIcon" => FilledIcon,
        _ => null,
    };
}
