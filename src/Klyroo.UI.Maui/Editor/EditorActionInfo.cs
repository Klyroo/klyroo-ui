namespace Klyroo.UI.Maui;

/// <summary>Names and icons of the editor actions: the English labels of the web editor, and its Lucide icons.</summary>
internal static class EditorActionInfo
{
    public static string Label(EditorAction action, int level) => action switch
    {
        EditorAction.Bold => "Bold",
        EditorAction.Italic => "Italic",
        EditorAction.Strike => "Strikethrough",
        EditorAction.Underline => "Underline",
        EditorAction.Code => "Code",
        EditorAction.Paragraph => "Paragraph",
        EditorAction.Heading => $"Heading {level}",
        EditorAction.BulletList => "Bulleted list",
        EditorAction.OrderedList => "Numbered list",
        EditorAction.TaskList => "Task list",
        EditorAction.Blockquote => "Quote",
        EditorAction.CodeBlock => "Code block",
        EditorAction.HorizontalRule => "Divider",
        EditorAction.Link => "Link",
        EditorAction.Image => "Image",
        EditorAction.ClearFormatting => "Clear formatting",
        EditorAction.Undo => "Undo",
        EditorAction.Redo => "Redo",
        EditorAction.MoveUp => "Move block up",
        EditorAction.MoveDown => "Move block down",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    public static string IconName(EditorAction action, int level) => action switch
    {
        EditorAction.Bold => "bold",
        EditorAction.Italic => "italic",
        EditorAction.Strike => "strikethrough",
        EditorAction.Underline => "underline",
        EditorAction.Code => "code",
        EditorAction.Paragraph => "pilcrow",
        EditorAction.Heading => level switch { 1 => "heading-1", 2 => "heading-2", 3 => "heading-3", _ => "heading-4" },
        EditorAction.BulletList => "list",
        EditorAction.OrderedList => "list-ordered",
        EditorAction.TaskList => "list-todo",
        EditorAction.Blockquote => "quote",
        EditorAction.CodeBlock => "square-code",
        EditorAction.HorizontalRule => "minus",
        EditorAction.Link => "link",
        EditorAction.Image => "image",
        EditorAction.ClearFormatting => "remove-formatting",
        EditorAction.Undo => "undo-2",
        EditorAction.Redo => "redo-2",
        EditorAction.MoveUp => KlyIconNames.ArrowUp,
        EditorAction.MoveDown => KlyIconNames.ArrowDown,
        _ => "",
    };

    /// <summary>Buttons that stay pressed while the selection has the format.</summary>
    public static bool IsToggle(EditorAction action) => action is
        EditorAction.Bold or EditorAction.Italic or EditorAction.Strike or EditorAction.Underline or EditorAction.Code or EditorAction.Heading or
        EditorAction.BulletList or EditorAction.OrderedList or EditorAction.TaskList or EditorAction.Blockquote or EditorAction.CodeBlock or EditorAction.Link;
}
