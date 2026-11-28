namespace Klyroo.UI.Maui;

/// <summary>An option of a suggestion menu, as shown: what the user sees and what choosing it does.</summary>
internal sealed record EditorMenuOption(
    string Value,
    string Label,
    string? Description = null,
    string? Keywords = null,
    EditorAction? Action = null,
    int Level = 0,
    bool Disabled = false,
    string? IconName = null,
    string? Prefix = null);
