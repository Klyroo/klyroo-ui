namespace Klyroo.UI;

public sealed partial record KlyMessages
{
    /// <summary>Accessible name of the editable area when no label is given.</summary>
    public string EditorLabel { get; init; } = "Rich text editor";

    /// <summary>Accessible name of the editor toolbar.</summary>
    public string EditorToolbar { get; init; } = "Formatting";

    public string EditorBold { get; init; } = "Bold";
    public string EditorItalic { get; init; } = "Italic";
    public string EditorStrike { get; init; } = "Strikethrough";
    public string EditorUnderline { get; init; } = "Underline";
    public string EditorCode { get; init; } = "Code";
    public string EditorParagraph { get; init; } = "Paragraph";

    /// <summary>Label of a heading button; <c>{0}</c> is the level.</summary>
    public string EditorHeading { get; init; } = "Heading {0}";
    public string EditorBulletList { get; init; } = "Bulleted list";
    public string EditorOrderedList { get; init; } = "Numbered list";
    public string EditorTaskList { get; init; } = "Task list";
    public string EditorBlockquote { get; init; } = "Quote";
    public string EditorCodeBlock { get; init; } = "Code block";
    public string EditorHorizontalRule { get; init; } = "Divider";
    public string EditorLink { get; init; } = "Link";
    public string EditorClearFormatting { get; init; } = "Clear formatting";
    public string EditorUndo { get; init; } = "Undo";
    public string EditorRedo { get; init; } = "Redo";
    public string EditorMoveUp { get; init; } = "Move block up";
    public string EditorMoveDown { get; init; } = "Move block down";

    /// <summary>Accessible name of the field that takes the address of a link.</summary>
    public string EditorLinkUrl { get; init; } = "Link address";
    public string EditorLinkApply { get; init; } = "Apply";
    public string EditorLinkRemove { get; init; } = "Remove link";

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
    public string EditorNoResults { get; init; } = "No results";

    /// <summary>Accessible name of the list of the slash menu.</summary>
    public string EditorSuggestionMenu { get; init; } = "Commands";

    /// <summary>Accessible name of the list of the mention menu.</summary>
    public string EditorMentionMenu { get; init; } = "Mentions";

    /// <summary>Accessible name of the list of the emoji menu.</summary>
    public string EditorEmojiMenu { get; init; } = "Emoji";

    /// <summary>Name of the plain text entry of the slash menu.</summary>
    public string EditorSuggestionText { get; init; } = "Text";

    public string EditorDescriptionParagraph { get; init; } = "Plain paragraph";
    public string EditorDescriptionHeading1 { get; init; } = "Big section heading";
    public string EditorDescriptionHeading2 { get; init; } = "Medium section heading";
    public string EditorDescriptionHeading3 { get; init; } = "Small section heading";
    public string EditorDescriptionBulletList { get; init; } = "A simple list of points";
    public string EditorDescriptionOrderedList { get; init; } = "A list with numbers";
    public string EditorDescriptionBlockquote { get; init; } = "Quote a passage";
    public string EditorDescriptionCodeBlock { get; init; } = "Monospaced code";
    public string EditorDescriptionHorizontalRule { get; init; } = "A horizontal line";
}
