using System.Text.RegularExpressions;
using Blux.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI;

/// <summary>
/// Place in <c>&lt;head&gt;</c>, next to <see cref="BxColorModeScript"/> and before your own stylesheet. Renders one
/// <c>&lt;style&gt;</c> with the <c>:root</c> and <c>.dark</c> variables of the palettes configured in
/// <c>AddBluxUi(o =&gt; o.Colors...)</c>, and a <c>[data-color]</c> rule for every extra semantic color, so
/// <c>Color="@("tertiary")"</c> works without hand-written CSS. It renders nothing when the colors are the defaults, which
/// <c>blux.css</c> already contains.
/// </summary>
/// <remarks>
/// The output is static markup computed once from the options, so it is the same in static rendering, prerendering and every
/// interactive mode, on the server and in WebAssembly. The rules are unlayered: they win over the library's, and your own
/// stylesheet, loaded after, wins over them. <see cref="Colors"/> and <see cref="Scope"/> render the colors of a
/// subtree instead, for a live preview.
/// </remarks>
public sealed partial class BxThemeStyle : ComponentBase
{
    [Inject] private BluxOptions Options { get; set; } = default!;

    /// <summary>Nonce for a Content-Security-Policy that forbids inline styles without one.</summary>
    [Parameter] public string? Nonce { get; set; }

    /// <summary>
    /// Colors to render instead of the ones of <c>AddBluxUi</c>, for a preview. They become read-only. Components only follow
    /// them under <see cref="Scope"/>.
    /// </summary>
    [Parameter] public BxColorOptions? Colors { get; set; }

    /// <summary>
    /// A simple selector (<c>#preview</c>, <c>.demo</c>) that receives the variables instead of <c>:root</c>: the elements
    /// inside it get these colors, in light and dark mode.
    /// </summary>
    [Parameter] public string? Scope { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        string? css;
        if (Colors is null && Scope is null)
        {
            css = Options.ThemeCss;
        }
        else
        {
            var scope = Scope ?? ":root";
            if (!ScopePattern().IsMatch(scope))
            {
                throw new ArgumentException($"Scope '{scope}' must be a simple selector such as '#preview' or '.demo'.", nameof(Scope));
            }
            var colors = Colors ?? Options.Colors;
            colors.Freeze();
            css = BxThemeTokens.Compute(colors).ToCss(colors, scope);
        }

        if (css is null)
        {
            return;
        }
        var nonce = Nonce is null ? "" : $" nonce=\"{System.Net.WebUtility.HtmlEncode(Nonce)}\"";
        builder.AddMarkupContent(0, $"<style data-bx-theme{nonce}>{css}</style>");
    }

    [GeneratedRegex(@"^(:root|[#.][A-Za-z_][\w-]*)$")]
    private static partial Regex ScopePattern();
}
