using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyPagination"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record PaginationUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyPagination"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyPagination"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyPagination"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>page</c> slot of <see cref="KlyPagination"/>, after the theme and the app configuration.</summary>
    public string? Page { get; init; }
    /// <summary>Classes merged into the <c>control</c> slot of <see cref="KlyPagination"/>, after the theme and the app configuration.</summary>
    public string? Control { get; init; }
    /// <summary>Classes merged into the <c>ellipsis</c> slot of <see cref="KlyPagination"/>, after the theme and the app configuration.</summary>
    public string? Ellipsis { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyPagination"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "list" => List,
        "item" => Item,
        "page" => Page,
        "control" => Control,
        "ellipsis" => Ellipsis,
        "icon" => Icon,
        _ => null,
    };
}
