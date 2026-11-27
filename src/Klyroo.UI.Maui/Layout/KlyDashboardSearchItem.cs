namespace Klyroo.UI.Maui;

/// <summary>A command of a <see cref="KlyDashboardSearch"/>: an action, or a link when <see cref="Href"/> is set.</summary>
public sealed record KlyDashboardSearchItem
{
    /// <summary>Text of the command.</summary>
    public string Label { get; init; } = "";

    /// <summary>Identifies the command to the app. Defaults to the label.</summary>
    public string? Value { get; init; }

    /// <summary>Text shown under the label.</summary>
    public string? Description { get; init; }

    /// <summary>Extra words the search matches, that are not shown.</summary>
    public string? Keywords { get; init; }

    /// <summary>Address to go to. When set the command is a link: the app follows it in <see cref="KlyDashboardSearch.ItemSelected"/>.</summary>
    public string? Href { get; init; }

    /// <summary>The name of an icon, drawn before the label. See <see cref="KlyIconNames"/>.</summary>
    public string? Icon { get; init; }

    /// <summary>Keyboard shortcut shown at the end of the row, such as <c>Ctrl+K</c>, for a desktop app.</summary>
    public string? Shortcut { get; init; }

    /// <summary>A number shown at the end of the row, before the shortcut (matches, unread, items). It is not searched.</summary>
    public int? Count { get; init; }

    /// <summary>Lists the command but does not let it be activated.</summary>
    public bool Disabled { get; init; }
}
