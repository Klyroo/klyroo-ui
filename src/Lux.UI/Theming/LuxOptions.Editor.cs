using Lux.UI.Theming;

namespace Lux.UI;

public sealed partial class LuxOptions
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
