using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI;

/// <summary>
/// Place in <c>&lt;head&gt;</c> of <c>App.razor</c>. It writes the culture and the UI culture of the request
/// (<c>&lt;meta name="blux-culture" content="c=en-GB|uic=en"&gt;</c>) so that a WebAssembly client, which has no request, formats
/// dates and numbers exactly like the prerender did: see <see cref="BluxCulture.ApplyServerCultureAsync"/>.
/// </summary>
public sealed class BxCultureMeta : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "meta");
        builder.AddAttribute(1, "name", "blux-culture");
        builder.AddAttribute(2, "content", $"c={CultureInfo.CurrentCulture.Name}|uic={CultureInfo.CurrentUICulture.Name}");
        builder.CloseElement();
    }
}
