using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>A page of the trail of a <see cref="KlyBreadcrumb"/>.</summary>
/// <param name="Label">Text of the item.</param>
public sealed record KlyBreadcrumbItem(string Label)
{
    /// <summary>Makes the item a link. The last item is the current page whether it has one or not.</summary>
    public string? Href { get; init; }
    /// <summary>Icon shown before the label, as markup such as a <see cref="KlyIcon"/>.</summary>
    public RenderFragment? Icon { get; init; }
}

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyBreadcrumb"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record BreadcrumbUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyBreadcrumb"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyBreadcrumb"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyBreadcrumb"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyBreadcrumb"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyBreadcrumb"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyBreadcrumb"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>separator</c> slot of <see cref="KlyBreadcrumb"/>, after the theme and the app configuration.</summary>
    public string? Separator { get; init; }
    /// <summary>Classes merged into the <c>separatorIcon</c> slot of <see cref="KlyBreadcrumb"/>, after the theme and the app configuration.</summary>
    public string? SeparatorIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "list" => List,
        "item" => Item,
        "link" => Link,
        "icon" => Icon,
        "label" => Label,
        "separator" => Separator,
        "separatorIcon" => SeparatorIcon,
        _ => null,
    };
}
