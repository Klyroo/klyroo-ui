namespace Klyroo.UI.Maui;

/// <summary>An entry of <see cref="KlyEditorSuggestionMenu"/>: a block or a format the user can insert.</summary>
public sealed record EditorSuggestionItem
{
    /// <summary>Creates an entry for an action.</summary>
    /// <param name="action">What the entry inserts or applies.</param>
    /// <param name="label">Text of the entry.</param>
    public EditorSuggestionItem(EditorAction action, string label)
    {
        Action = action;
        Label = label;
    }

    /// <summary>What the entry inserts or applies.</summary>
    public EditorAction Action { get; }
    /// <summary>Text of the entry.</summary>
    public string Label { get; }

    /// <summary>Heading level for <see cref="EditorAction.Heading"/>.</summary>
    public int Level { get; init; }
    /// <summary>Text shown under the label.</summary>
    public string? Description { get; init; }

    /// <summary>Extra words the typed text is matched against.</summary>
    public string? Keywords { get; init; }
    /// <summary>Disables the entry.</summary>
    public bool Disabled { get; init; }

    /// <summary>
    /// Text, headings, lists, quote, code block and divider, in English: what the slash menu offers by default. The keywords are
    /// markup shorthands (h1, ul, hr). Pass your own list to <see cref="KlyEditorSuggestionMenu.Items"/> to translate it.
    /// </summary>
    public static IReadOnlyList<EditorSuggestionItem> Defaults { get; } =
    [
        new(EditorAction.Paragraph, "Text") { Description = "Plain paragraph", Keywords = "paragraph p" },
        new(EditorAction.Heading, "Heading 1") { Level = 1, Description = "Big section heading", Keywords = "h1 title" },
        new(EditorAction.Heading, "Heading 2") { Level = 2, Description = "Medium section heading", Keywords = "h2 subtitle" },
        new(EditorAction.Heading, "Heading 3") { Level = 3, Description = "Small section heading", Keywords = "h3" },
        new(EditorAction.BulletList, "Bulleted list") { Description = "A simple list of points", Keywords = "ul unordered" },
        new(EditorAction.OrderedList, "Numbered list") { Description = "A list with numbers", Keywords = "ol ordered" },
        new(EditorAction.Blockquote, "Quote") { Description = "Quote a passage", Keywords = "blockquote citation" },
        new(EditorAction.CodeBlock, "Code block") { Description = "Monospaced code", Keywords = "pre snippet" },
        new(EditorAction.HorizontalRule, "Divider") { Description = "A horizontal line", Keywords = "hr rule separator" },
    ];
}
