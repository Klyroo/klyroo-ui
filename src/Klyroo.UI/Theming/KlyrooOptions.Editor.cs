using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// Theme of <see cref="KlyEditor"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Editor { get; } = Themes.Editor();
    /// <summary>
    /// Theme of <see cref="KlyEditorToolbar"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme EditorToolbar { get; } = Themes.EditorToolbar();
    /// <summary>
    /// Theme of <see cref="KlyEditorDragHandle"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme EditorDragHandle { get; } = Themes.EditorDragHandle();
    /// <summary>
    /// Theme shared by <see cref="KlyEditorSuggestionMenu"/>, <see cref="KlyEditorMentionMenu"/> and <see cref="KlyEditorEmojiMenu"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme EditorMenu { get; } = Themes.EditorMenu();

    /// <summary>
    /// Sanitizes editor HTML before the server shows it as markup (the read-only view displayed until the editor
    /// script loads, and on static pages). Without one, that view shows the text only, never raw markup: the value is
    /// user input and rendering it as HTML would let stored script run. Plug an allow-list sanitizer here, for example
    /// <c>o.SanitizeEditorHtml = html => new Ganss.Xss.HtmlSanitizer().Sanitize(html);</c>
    /// </summary>
    public Func<string, string>? SanitizeEditorHtml { get; set; }
}
