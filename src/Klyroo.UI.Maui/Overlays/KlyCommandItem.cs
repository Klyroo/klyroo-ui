namespace Klyroo.UI.Maui;

/// <summary>A command of <see cref="KlyCommandPalette"/>: an action, or a link when <see cref="Href"/> is set.</summary>
public sealed record KlyCommandItem
{
    /// <summary>Text of the command.</summary>
    public string Label { get; init; } = "";

    /// <summary>Identifies the command to the code that handles the selection. Defaults to the label.</summary>
    public string? Value { get; init; }

    /// <summary>Text shown under the label.</summary>
    public string? Description { get; init; }

    /// <summary>Extra words the search matches, that are not shown.</summary>
    public string? Keywords { get; init; }

    /// <summary>Address to go to. When set the command is a link: a web, mail, phone or sms address is opened with the system, and the selection event is raised for every address.</summary>
    public string? Href { get; init; }

    /// <summary>Icon shown before the label: a name of <see cref="KlyIconNames"/>, or a registered one.</summary>
    public string? Icon { get; init; }

    /// <summary>Keyboard shortcut shown at the end of the row, such as <c>Ctrl+K</c>.</summary>
    public string? Shortcut { get; init; }

    /// <summary>Whether the command can be activated. Off lists it dimmed (the <c>Disabled</c> of the Blazor item).</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>A number shown at the end of the row, before the shortcut (matches, unread, items): formatted for the culture of the app and not searched.</summary>
    public int? Count { get; init; }

    /// <summary>What identifies the command: <see cref="Value"/>, or the label.</summary>
    internal string Key => Value ?? Label;
}
