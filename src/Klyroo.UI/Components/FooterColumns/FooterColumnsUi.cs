using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyFooterColumns"/> slots.</summary>
public sealed record FooterColumnsUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyFooterColumns"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>left</c> slot of <see cref="KlyFooterColumns"/>, after the theme and the app configuration.</summary>
    public string? Left { get; init; }
    /// <summary>Classes merged into the <c>nav</c> slot of <see cref="KlyFooterColumns"/>, after the theme and the app configuration.</summary>
    public string? Nav { get; init; }
    /// <summary>Classes merged into the <c>column</c> slot of <see cref="KlyFooterColumns"/>, after the theme and the app configuration.</summary>
    public string? Column { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyFooterColumns"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyFooterColumns"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyFooterColumns"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyFooterColumns"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyFooterColumns"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>right</c> slot of <see cref="KlyFooterColumns"/>, after the theme and the app configuration.</summary>
    public string? Right { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "left" => Left,
        "nav" => Nav,
        "column" => Column,
        "label" => Label,
        "list" => List,
        "item" => Item,
        "link" => Link,
        "icon" => Icon,
        "right" => Right,
        _ => null,
    };
}
