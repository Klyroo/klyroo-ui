namespace Klyroo.UI;

public sealed partial record KlyMessages
{
    /// <summary>Accessible name of the editable area when no label is given.</summary>
    public string EditorLabel { get; init; } = "Rich text editor";

    /// <summary>Accessible name of the editor toolbar.</summary>
    public string EditorToolbar { get; init; } = "Formatting";

    /// <summary>Label of the bold button of the editor toolbar (<see cref="KlyEditorToolbar"/>).</summary>
    public string EditorBold { get; init; } = "Bold";
    /// <summary>Label of the italic button of the editor toolbar (<see cref="KlyEditorToolbar"/>).</summary>
    public string EditorItalic { get; init; } = "Italic";
    /// <summary>Label of the strikethrough button of the editor toolbar (<see cref="KlyEditorToolbar"/>).</summary>
    public string EditorStrike { get; init; } = "Strikethrough";
    /// <summary>Label of the underline button of the editor toolbar (<see cref="KlyEditorToolbar"/>).</summary>
    public string EditorUnderline { get; init; } = "Underline";
    /// <summary>Label of the inline code button of the editor toolbar (<see cref="KlyEditorToolbar"/>).</summary>
    public string EditorCode { get; init; } = "Code";
    /// <summary>Label of the paragraph button of the editor toolbar and of its slash menu entry.</summary>
    public string EditorParagraph { get; init; } = "Paragraph";

    /// <summary>Label of a heading button; <c>{0}</c> is the level.</summary>
    public string EditorHeading { get; init; } = "Heading {0}";
    /// <summary>Label of the bulleted list button of the editor toolbar and of its slash menu entry.</summary>
    public string EditorBulletList { get; init; } = "Bulleted list";
    /// <summary>Label of the numbered list button of the editor toolbar and of its slash menu entry.</summary>
    public string EditorOrderedList { get; init; } = "Numbered list";
    /// <summary>Label of the task list button of the editor toolbar.</summary>
    public string EditorTaskList { get; init; } = "Task list";
    /// <summary>Label of the quote button of the editor toolbar and of its slash menu entry.</summary>
    public string EditorBlockquote { get; init; } = "Quote";
    /// <summary>Label of the code block button of the editor toolbar and of its slash menu entry.</summary>
    public string EditorCodeBlock { get; init; } = "Code block";
    /// <summary>Label of the divider button of the editor toolbar and of its slash menu entry.</summary>
    public string EditorHorizontalRule { get; init; } = "Divider";
    /// <summary>Label of the link button of the editor toolbar (<see cref="KlyEditorToolbar"/>).</summary>
    public string EditorLink { get; init; } = "Link";
    /// <summary>Label of the button of the editor toolbar that removes the formatting of the selection.</summary>
    public string EditorClearFormatting { get; init; } = "Clear formatting";
    /// <summary>Label of the undo button of the editor toolbar (<see cref="KlyEditorToolbar"/>).</summary>
    public string EditorUndo { get; init; } = "Undo";
    /// <summary>Label of the redo button of the editor toolbar (<see cref="KlyEditorToolbar"/>).</summary>
    public string EditorRedo { get; init; } = "Redo";
    /// <summary>Accessible name of the action that moves the current block of the editor up.</summary>
    public string EditorMoveUp { get; init; } = "Move block up";
    /// <summary>Accessible name of the action that moves the current block of the editor down.</summary>
    public string EditorMoveDown { get; init; } = "Move block down";

    /// <summary>Accessible name of the field that takes the address of a link.</summary>
    public string EditorLinkUrl { get; init; } = "Link address";
    /// <summary>Label of the button of the link and image address form of <see cref="KlyEditorToolbar"/> that applies the address.</summary>
    public string EditorLinkApply { get; init; } = "Apply";
    /// <summary>Label of the button of the link form of <see cref="KlyEditorToolbar"/> that removes the link.</summary>
    public string EditorLinkRemove { get; init; } = "Remove link";

    /// <summary>Label of the image button of the editor toolbar (<see cref="KlyEditorToolbar"/>).</summary>
    public string EditorImage { get; init; } = "Image";

    /// <summary>Accessible name of the field that takes the address of an image (the button is <see cref="EditorLinkApply"/>).</summary>
    public string EditorImageUrl { get; init; } = "Image address";

    /// <summary>Explains why the editor refused an address when it takes only some schemes; <c>{0}</c> lists them ("https, http, mailto").</summary>
    public string EditorSchemesOnly { get; init; } = "Only {0} addresses are accepted.";

    /// <summary>Explains why the editor refused an address when no list of schemes is known.</summary>
    public string EditorAddressRefused { get; init; } = "This address is not accepted.";

    /// <summary>Accessible name of the checkbox of a task item; <c>{0}</c> is the text of the item.</summary>
    public string EditorTaskItem { get; init; } = "Task: {0}";

    /// <summary>Accessible name of the button that drags a block (it also moves with Alt and the arrow keys).</summary>
    public string EditorDragHandle { get; init; } = "Drag to move block";
    /// <summary>Text shown by the slash, mention and emoji menus of <see cref="KlyEditor"/> when nothing matches what was typed.</summary>
    public string EditorNoResults { get; init; } = "No results";

    /// <summary>Accessible name of the list of the slash menu.</summary>
    public string EditorSuggestionMenu { get; init; } = "Commands";

    /// <summary>Accessible name of the list of the mention menu.</summary>
    public string EditorMentionMenu { get; init; } = "Mentions";

    /// <summary>Accessible name of the list of the emoji menu.</summary>
    public string EditorEmojiMenu { get; init; } = "Emoji";

    /// <summary>Name of the plain text entry of the slash menu.</summary>
    public string EditorSuggestionText { get; init; } = "Text";

    /// <summary>Description of the paragraph entry of the slash menu of <see cref="KlyEditor"/>.</summary>
    public string EditorDescriptionParagraph { get; init; } = "Plain paragraph";
    /// <summary>Description of the level 1 heading entry of the slash menu of <see cref="KlyEditor"/>.</summary>
    public string EditorDescriptionHeading1 { get; init; } = "Big section heading";
    /// <summary>Description of the level 2 heading entry of the slash menu of <see cref="KlyEditor"/>.</summary>
    public string EditorDescriptionHeading2 { get; init; } = "Medium section heading";
    /// <summary>Description of the level 3 heading entry of the slash menu of <see cref="KlyEditor"/>.</summary>
    public string EditorDescriptionHeading3 { get; init; } = "Small section heading";
    /// <summary>Description of the bulleted list entry of the slash menu of <see cref="KlyEditor"/>.</summary>
    public string EditorDescriptionBulletList { get; init; } = "A simple list of points";
    /// <summary>Description of the numbered list entry of the slash menu of <see cref="KlyEditor"/>.</summary>
    public string EditorDescriptionOrderedList { get; init; } = "A list with numbers";
    /// <summary>Description of the quote entry of the slash menu of <see cref="KlyEditor"/>.</summary>
    public string EditorDescriptionBlockquote { get; init; } = "Quote a passage";
    /// <summary>Description of the code block entry of the slash menu of <see cref="KlyEditor"/>.</summary>
    public string EditorDescriptionCodeBlock { get; init; } = "Monospaced code";
    /// <summary>Description of the divider entry of the slash menu of <see cref="KlyEditor"/>.</summary>
    public string EditorDescriptionHorizontalRule { get; init; } = "A horizontal line";
}
