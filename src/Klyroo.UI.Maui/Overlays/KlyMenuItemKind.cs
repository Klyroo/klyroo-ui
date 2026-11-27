namespace Klyroo.UI.Maui;

/// <summary>What a <see cref="KlyMenuItem"/> is.</summary>
public enum KlyMenuItemKind
{
    /// <summary>An action, or a link when <see cref="KlyMenuItem.Href"/> is set.</summary>
    Item,
    /// <summary>A line between two groups of items.</summary>
    Separator,
    /// <summary>A non-interactive title of a group of items.</summary>
    Heading,
}
