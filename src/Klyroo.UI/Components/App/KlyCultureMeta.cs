using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Klyroo.UI;

/// <summary>
/// Place in <c>&lt;head&gt;</c> of <c>App.razor</c>. It writes the culture and the UI culture of the request
/// (<c>&lt;meta name="klyroo-culture" content="c=en-GB|uic=en"&gt;</c>) so that a WebAssembly client, which has no request, formats
/// dates and numbers exactly like the prerender did: see <see cref="KlyrooCulture.ApplyServerCultureAsync"/>.
/// In a Blazor Hybrid web view there is no server request: it renders nothing (and <see cref="KlyrooCulture.ApplyServerCultureAsync"/> keeps the
/// culture of the device).
/// </summary>
public sealed class KlyCultureMeta : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (KlyrooHost.IsHybrid(RendererInfo))
        {
            return;
        }
        builder.OpenElement(0, "meta");
        builder.AddAttribute(1, "name", "klyroo-culture");
        builder.AddAttribute(2, "content", $"c={CultureInfo.CurrentCulture.Name}|uic={CultureInfo.CurrentUICulture.Name}");
        builder.CloseElement();
    }
}
