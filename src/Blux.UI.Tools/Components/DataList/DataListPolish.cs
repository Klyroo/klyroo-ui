using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tools;

/// <summary>
/// Placeholder shape drawn inside a column cell while the initial rows of <see cref="BxDataListTable{TItem}"/> load.
/// </summary>
public enum DataListSkeletonKind
{
    /// <summary>A single text bar (<c>h-4 w-3/4</c>).</summary>
    Text,

    /// <summary>A circular avatar next to a text bar.</summary>
    Avatar,

    /// <summary>A small square icon placeholder.</summary>
    Icon,

    /// <summary>A status dot next to a short text bar.</summary>
    Dot,

    /// <summary>A checkbox square placeholder.</summary>
    Check,

    /// <summary>A pill badge placeholder.</summary>
    Badge,

    /// <summary>A short numeric bar aligned to the end edge.</summary>
    Number,

    /// <summary>A horizontal progress track placeholder.</summary>
    Progress,

    /// <summary>Leaves the placeholder cell blank.</summary>
    None,
}

/// <summary>
/// First-use action button or link displayed inside the unfiltered empty state of <see cref="BxDataListTable{TItem}"/> or <see cref="BxDataListGrid{TItem}"/>
/// when <see cref="DataListEmptyState.Action"/> is configured. Never shown on a filtered empty state.
/// </summary>
public sealed class DataListEmptyAction
{
    /// <summary>Label of the action button or link.</summary>
    public required string Label { get; init; }

    /// <summary>Optional leading icon name.</summary>
    public string? Icon { get; init; }

    /// <summary>Optional navigation link URL. When set, renders a link button (works in static SSR too).</summary>
    public string? Href { get; init; }

    /// <summary>Callback invoked when the button is clicked in an interactive render.</summary>
    public EventCallback OnClick { get; init; }

    /// <summary>Button color (<see cref="BxColor.Primary"/> by default).</summary>
    public BxColor Color { get; init; } = BxColor.Primary;

    /// <summary>Button visual variant (<see cref="ButtonVariant.Solid"/> by default).</summary>
    public ButtonVariant Variant { get; init; } = ButtonVariant.Solid;
}

/// <summary>
/// Declarative configuration for the unfiltered empty state of <see cref="BxDataListTable{TItem}"/> or <see cref="BxDataListGrid{TItem}"/>.
/// When search or filters are active and match nothing, the filtered empty state (with Clear search / Reset filters) is still shown instead,
/// and <see cref="Action"/> is omitted so a first-use creation action is never offered while results are merely filtered out.
/// </summary>
public sealed class DataListEmptyState
{
    /// <summary>Optional icon name overriding the default empty-state icon (or empty string to hide the icon).</summary>
    public string? Icon { get; init; }

    /// <summary>Optional title overriding the localized <see cref="BxToolsMessages.NoResults"/>.</summary>
    public string? Title { get; init; }

    /// <summary>Optional description overriding the localized <see cref="BxToolsMessages.NoResultsDescription"/>.</summary>
    public string? Description { get; init; }

    /// <summary>When <see langword="true"/>, hides the description line on the unfiltered empty state.</summary>
    public bool HideDescription { get; init; }

    /// <summary>Optional first-use call-to-action shown only when the list is empty without active search or filters.</summary>
    public DataListEmptyAction? Action { get; init; }
}
