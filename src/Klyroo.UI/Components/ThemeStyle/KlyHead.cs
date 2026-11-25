using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Klyroo.UI;

/// <summary>
/// The head content of Klyroo for a Blazor Hybrid web view: the theme variables (<see cref="KlyThemeStyle"/>) and the self-hosted fonts
/// (<see cref="KlyFonts"/>), sent to the head through <c>HeadContent</c>. A web view has no server that renders <c>&lt;head&gt;</c>, so
/// place this once in the layout of the app and add a <c>HeadOutlet</c> root component with the selector <c>head::after</c> to the
/// <c>BlazorWebView</c>. It renders nothing in a browser, where <c>App.razor</c> places the head components itself, unless <see cref="Always"/> is set.
/// </summary>
/// <remarks>
/// The color mode needs nothing here: the JS initializer of Klyroo applies it before the app renders. The content lands after the
/// stylesheets of <c>index.html</c>; the theme variables are unlayered, so they win over <c>klyroo.css</c> wherever they sit.
/// </remarks>
public sealed class KlyHead : ComponentBase
{
    /// <summary>Renders the head content in a browser too (an app with a <c>HeadOutlet</c> that does not place the components in <c>App.razor</c>).</summary>
    [Parameter] public bool Always { get; set; }

    /// <summary>Nonce for a Content-Security-Policy that forbids inline styles without one.</summary>
    [Parameter] public string? Nonce { get; set; }

    /// <summary>Renders the self-hosted fonts. Turn it off when the app declares none.</summary>
    [Parameter] public bool Fonts { get; set; } = true;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!Always && !KlyrooHost.IsHybrid(RendererInfo))
        {
            return;
        }
        builder.OpenComponent<HeadContent>(0);
        builder.AddAttribute(1, "ChildContent", (RenderFragment)(content =>
        {
            content.OpenComponent<KlyThemeStyle>(0);
            content.AddAttribute(1, nameof(KlyThemeStyle.Nonce), Nonce);
            content.CloseComponent();
            if (Fonts)
            {
                content.OpenComponent<KlyFonts>(2);
                content.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }
}
