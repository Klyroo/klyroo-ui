namespace Klyroo.UI.Maui;

/// <summary>An entry of <see cref="KlyEditorMentionMenu"/>: someone or something that can be mentioned.</summary>
public sealed record EditorMentionItem
{
    /// <summary>Creates an entry for something that can be mentioned.</summary>
    /// <param name="id">Identifies the entry in <see cref="KlyEditorMentionMenu.Mentioned"/>.</param>
    /// <param name="label">Shown in the menu and in the text after the @.</param>
    public EditorMentionItem(string id, string label)
    {
        Id = id;
        Label = label;
    }

    /// <summary>Identifies the entry. The Markdown text keeps only the label: <see cref="KlyEditorMentionMenu.Mentioned"/> hands the id to the app.</summary>
    public string Id { get; }

    /// <summary>Shown in the menu and in the text after the @.</summary>
    public string Label { get; }
    /// <summary>Text shown under the label.</summary>
    public string? Description { get; init; }
    /// <summary>Disables the entry.</summary>
    public bool Disabled { get; init; }
}
