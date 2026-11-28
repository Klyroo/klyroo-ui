namespace Klyroo.UI.Maui;

/// <summary>What a toolbar button or a slash-menu item does to the Markdown text of a <see cref="KlyEditor"/>.</summary>
public enum EditorAction
{
    /// <summary>Bold text: <c>**text**</c>.</summary>
    Bold,
    /// <summary>Italic text: <c>*text*</c>.</summary>
    Italic,
    /// <summary>Struck-through text: <c>~~text~~</c>.</summary>
    Strike,
    /// <summary>Underlined text. Markdown has no underline: the text is wrapped in <c>&lt;u&gt;</c> tags, which renderers that allow inline HTML show.</summary>
    Underline,
    /// <summary>Inline code: <c>`text`</c>.</summary>
    Code,
    /// <summary>A plain paragraph: removes the heading or list marker of the lines.</summary>
    Paragraph,

    /// <summary>A heading; the level comes from the <c>Level</c> of the item (1 to 6).</summary>
    Heading,
    /// <summary>A list with bullets: <c>- item</c>.</summary>
    BulletList,
    /// <summary>A list with numbers: <c>1. item</c>.</summary>
    OrderedList,

    /// <summary>A list of items with a checkbox each: <c>- [ ] item</c>.</summary>
    TaskList,
    /// <summary>A quotation block: <c>&gt; text</c>.</summary>
    Blockquote,
    /// <summary>A fenced block of code.</summary>
    CodeBlock,
    /// <summary>A horizontal divider: <c>---</c>.</summary>
    HorizontalRule,

    /// <summary>Sets or removes the link of the selection, <c>[text](address)</c>; the toolbar asks for the address in a small field.</summary>
    Link,

    /// <summary>Inserts an image, <c>![alt](address)</c>, from an http or https address; the toolbar asks for the address in a small field.</summary>
    Image,
    /// <summary>Removes the formatting marks of the selection and the block markers of its lines.</summary>
    ClearFormatting,
    /// <summary>Undoes the last change.</summary>
    Undo,
    /// <summary>Redoes the change that was undone.</summary>
    Redo,

    /// <summary>Moves the block holding the cursor up. A block is a paragraph, a list or a fenced code block: lines up to the next blank line.</summary>
    MoveUp,

    /// <summary>Moves the block holding the cursor down.</summary>
    MoveDown,
}
