using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of the slots of <see cref="KlyListbox"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record ListboxUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyListbox"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyListbox"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyListbox"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>itemLabel</c> slot of <see cref="KlyListbox"/>, after the theme and the app configuration.</summary>
    public string? ItemLabel { get; init; }
    /// <summary>Classes merged into the <c>itemIcon</c> slot of <see cref="KlyListbox"/>, after the theme and the app configuration.</summary>
    public string? ItemIcon { get; init; }
    /// <summary>Classes merged into the <c>itemLeadingIcon</c> slot of <see cref="KlyListbox"/>, after the theme and the app configuration.</summary>
    public string? ItemLeadingIcon { get; init; }
    /// <summary>Classes merged into the <c>itemCount</c> slot of <see cref="KlyListbox"/>, after the theme and the app configuration.</summary>
    public string? ItemCount { get; init; }
    /// <summary>Classes merged into the <c>group</c> slot of <see cref="KlyListbox"/>, after the theme and the app configuration.</summary>
    public string? Group { get; init; }
    /// <summary>Classes merged into the <c>groupLabel</c> slot of <see cref="KlyListbox"/>, after the theme and the app configuration.</summary>
    public string? GroupLabel { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "item" => Item,
        "itemLabel" => ItemLabel,
        "itemIcon" => ItemIcon,
        "itemLeadingIcon" => ItemLeadingIcon,
        "itemCount" => ItemCount,
        "group" => Group,
        "groupLabel" => GroupLabel,
        _ => null,
    };
}
