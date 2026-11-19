using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme Editor { get; } = Themes.Editor();
    public ComponentTheme EditorToolbar { get; } = Themes.EditorToolbar();
    public ComponentTheme EditorDragHandle { get; } = Themes.EditorDragHandle();
    public ComponentTheme EditorMenu { get; } = Themes.EditorMenu();

    /// <summary>
    /// Sanitizes editor HTML before the server shows it as markup (the read-only view displayed until the editor
    /// script loads, and on static pages). Without one, that view shows the text only, never raw markup: the value is
    /// user input and rendering it as HTML would let stored script run. Plug an allow-list sanitizer here, for example
    /// <c>o.SanitizeEditorHtml = html => new Ganss.Xss.HtmlSanitizer().Sanitize(html);</c>
    /// </summary>
    public Func<string, string>? SanitizeEditorHtml { get; set; }
}

public sealed partial class BluxStrings
{
    /// <summary>Accessible name of the editable area when no label is given.</summary>
    public string EditorLabel { get; set; } = "Rich text editor";

    /// <summary>Accessible name of the editor toolbar.</summary>
    public string EditorToolbar { get; set; } = "Formatting";

    public string EditorBold { get; set; } = "Bold";
    public string EditorItalic { get; set; } = "Italic";
    public string EditorStrike { get; set; } = "Strikethrough";
    public string EditorUnderline { get; set; } = "Underline";
    public string EditorCode { get; set; } = "Code";
    public string EditorParagraph { get; set; } = "Paragraph";

    /// <summary>Label of a heading button; <c>{0}</c> is the level.</summary>
    public string EditorHeading { get; set; } = "Heading {0}";
    public string EditorBulletList { get; set; } = "Bulleted list";
    public string EditorOrderedList { get; set; } = "Numbered list";
    public string EditorBlockquote { get; set; } = "Quote";
    public string EditorCodeBlock { get; set; } = "Code block";
    public string EditorHorizontalRule { get; set; } = "Divider";
    public string EditorLink { get; set; } = "Link";
    public string EditorClearFormatting { get; set; } = "Clear formatting";
    public string EditorUndo { get; set; } = "Undo";
    public string EditorRedo { get; set; } = "Redo";
    public string EditorMoveUp { get; set; } = "Move block up";
    public string EditorMoveDown { get; set; } = "Move block down";

    /// <summary>Accessible name of the field that takes the address of a link.</summary>
    public string EditorLinkUrl { get; set; } = "Link address";
    public string EditorLinkApply { get; set; } = "Apply";
    public string EditorLinkRemove { get; set; } = "Remove link";

    /// <summary>Accessible name of the button that drags a block (it also moves with Alt and the arrow keys).</summary>
    public string EditorDragHandle { get; set; } = "Drag to move block";
    public string EditorNoResults { get; set; } = "No results";

    /// <summary>Accessible name of the list of the slash menu.</summary>
    public string EditorSuggestionMenu { get; set; } = "Commands";

    /// <summary>Accessible name of the list of the mention menu.</summary>
    public string EditorMentionMenu { get; set; } = "Mentions";

    /// <summary>Accessible name of the list of the emoji menu.</summary>
    public string EditorEmojiMenu { get; set; } = "Emoji";
}
