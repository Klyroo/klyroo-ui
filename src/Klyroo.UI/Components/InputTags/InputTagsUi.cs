using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyInputTags"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record InputTagsUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyInputTags"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>tags</c> slot of <see cref="KlyInputTags"/>, after the theme and the app configuration.</summary>
    public string? Tags { get; init; }
    /// <summary>Classes merged into the <c>tag</c> slot of <see cref="KlyInputTags"/>, after the theme and the app configuration.</summary>
    public string? Tag { get; init; }
    /// <summary>Classes merged into the <c>remove</c> slot of <see cref="KlyInputTags"/>, after the theme and the app configuration.</summary>
    public string? Remove { get; init; }
    /// <summary>Classes merged into the <c>input</c> slot of <see cref="KlyInputTags"/>, after the theme and the app configuration.</summary>
    public string? Input { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "tags" => Tags,
        "tag" => Tag,
        "remove" => Remove,
        "input" => Input,
        _ => null,
    };
}
