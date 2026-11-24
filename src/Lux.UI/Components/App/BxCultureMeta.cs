using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Lux.UI;

/// <summary>
/// Place in <c>&lt;head&gt;</c> of <c>App.razor</c>. It writes the culture and the UI culture of the request
/// (<c>&lt;meta name="blux-culture" content="c=en-GB|uic=en"&gt;</c>) so that a WebAssembly client, which has no request, formats
/// dates and numbers exactly like the prerender did: see <see cref="LuxCulture.ApplyServerCultureAsync"/>.
/// In a Blazor Hybrid web view there is no server request: it renders nothing (and <see cref="LuxCulture.ApplyServerCultureAsync"/> keeps the
/// culture of the device).
/// </summary>
public sealed class BxCultureMeta : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (LuxHost.IsHybrid(RendererInfo))
        {
            return;
        }
        builder.OpenElement(0, "meta");
        builder.AddAttribute(1, "name", "blux-culture");
        builder.AddAttribute(2, "content", $"c={CultureInfo.CurrentCulture.Name}|uic={CultureInfo.CurrentUICulture.Name}");
        builder.CloseElement();
    }
}
