using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>One row of a <see cref="KlyRecordList"/>.</summary>
/// <param name="Title">The row title. It is a link when <see cref="Href"/> is set.</param>
public sealed record KlyRecordItem(string Title)
{
    /// <summary>A reference such as an order number, shown above the title in a monospace font.</summary>
    public string? Reference { get; init; }

    /// <summary>The secondary line under the title.</summary>
    public string? Description { get; init; }

    /// <summary>The name of an icon (see <see cref="KlyIcon"/>) shown before <see cref="Description"/>.</summary>
    public string? DescriptionIcon { get; init; }

    /// <summary>The name of an icon (see <see cref="KlyIcon"/>) shown in the leading tile. It takes precedence over <see cref="AvatarText"/>, and <see cref="ImageSrc"/> takes precedence over it.</summary>
    public string? Icon { get; init; }

    /// <summary>Short text, such as initials, shown in the leading tile.</summary>
    public string? AvatarText { get; init; }

    /// <summary>The URL of an image shown in the leading tile instead of the icon or avatar text. It is decorative (empty <c>alt</c>), since the title names the row.</summary>
    public string? ImageSrc { get; init; }

    /// <summary>A color for the leading tile and the trailing value.</summary>
    public KlyColor? Color { get; init; }

    /// <summary>Badges shown at the end of the row, before <see cref="Badge"/>.</summary>
    public IReadOnlyList<KlyBadgeItem>? Badges { get; init; }

    /// <summary>A badge shown at the end of the row.</summary>
    public KlyBadgeItem? Badge { get; init; }

    /// <summary>A value shown at the end of the row, such as an amount.</summary>
    public string? Trailing { get; init; }

    /// <summary>A line of smaller text under the trailing value, such as a date.</summary>
    public string? TrailingCaption { get; init; }

    /// <summary>The URL the title links to.</summary>
    public string? Href { get; init; }

    /// <summary>A callback invoked when the title is clicked. Without <see cref="Href"/> the title becomes a button, disabled until the page is interactive; with it, the link is followed and the callback runs too.</summary>
    public EventCallback OnClick { get; init; }
}

/// <summary>A list of entity rows, each with an image, avatar or icon, a reference, a title, a secondary line, badges and a trailing value.</summary>
public partial class KlyRecordList;
