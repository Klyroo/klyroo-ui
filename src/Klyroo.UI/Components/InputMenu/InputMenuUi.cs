using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyInputMenu"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record InputMenuUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>trailing</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? Trailing { get; init; }
    /// <summary>Classes merged into the <c>trailingIcon</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? TrailingIcon { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>listbox</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? Listbox { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>itemLabel</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? ItemLabel { get; init; }
    /// <summary>Classes merged into the <c>itemIcon</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? ItemIcon { get; init; }
    /// <summary>Classes merged into the <c>itemLeadingIcon</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? ItemLeadingIcon { get; init; }
    /// <summary>Classes merged into the <c>itemCount</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? ItemCount { get; init; }
    /// <summary>Classes merged into the <c>group</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? Group { get; init; }
    /// <summary>Classes merged into the <c>groupLabel</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? GroupLabel { get; init; }
    /// <summary>Classes merged into the <c>empty</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? Empty { get; init; }

    /// <summary>Classes merged into the <c>scroller</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? Scroller { get; init; }
    /// <summary>Classes merged into the <c>loading</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? Loading { get; init; }
    /// <summary>Classes merged into the <c>spinner</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? Spinner { get; init; }
    /// <summary>Classes merged into the <c>error</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? Error { get; init; }
    /// <summary>Classes merged into the <c>announcer</c> slot of <see cref="KlyInputMenu"/>, after the theme and the app configuration.</summary>
    public string? Announcer { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "trailing" => Trailing,
        "trailingIcon" => TrailingIcon,
        "content" => Content,
        "listbox" => Listbox,
        "item" => Item,
        "itemLabel" => ItemLabel,
        "itemIcon" => ItemIcon,
        "itemLeadingIcon" => ItemLeadingIcon,
        "itemCount" => ItemCount,
        "group" => Group,
        "groupLabel" => GroupLabel,
        "empty" => Empty,
        "scroller" => Scroller,
        "loading" => Loading,
        "spinner" => Spinner,
        "error" => Error,
        "announcer" => Announcer,
        _ => null,
    };
}
