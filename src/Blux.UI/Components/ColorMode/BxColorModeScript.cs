using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI;

/// <summary>
/// Place in <c>&lt;head&gt;</c>, before the stylesheets. Applies the stored color mode (or the system one) before the
/// first paint, so a dark page never flashes light. Toggle at runtime with <c>Blux.setColorMode('light' | 'dark' | 'system')</c>.
/// </summary>
public sealed class BxColorModeScript : ComponentBase
{
    internal const string StorageKey = "blux-color-mode";

    /// <summary>Nonce for a Content-Security-Policy that forbids inline scripts without one.</summary>
    [Parameter] public string? Nonce { get; set; }

    private const string Body =
        "(function(){try{var m=localStorage.getItem('" + StorageKey + "')||'system';" +
        "var d=m==='dark'||(m==='system'&&matchMedia('(prefers-color-scheme: dark)').matches);" +
        "document.documentElement.classList.toggle('dark',d);document.documentElement.style.colorScheme=d?'dark':'light';}catch(e){}})();";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var nonce = Nonce is null ? "" : $" nonce=\"{System.Net.WebUtility.HtmlEncode(Nonce)}\"";
        builder.AddMarkupContent(0, $"<script{nonce}>{Body}</script>");
    }
}
