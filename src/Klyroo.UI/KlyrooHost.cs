using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>
/// Where the components run. A Blazor Hybrid app (<c>BlazorWebView</c> in .NET MAUI, WPF or Windows Forms) renders in a local
/// web view: it is always interactive, has no prerendering, no <c>HttpContext</c> and no server to read cookies or to render the
/// page head. The components that depend on a server degrade to what a web view can do (browser storage instead of cookies, no
/// culture meta element, no inline script in the head); see docs/platforms.md in the repository.
/// </summary>
public static class KlyrooHost
{
    /// <summary>The <see cref="RendererInfo.Name"/> of a <c>BlazorWebView</c>.</summary>
    public const string WebViewRendererName = "WebView";

    /// <summary>True when <paramref name="renderer"/> is a Blazor Hybrid web view. In a component, read <c>RendererInfo</c>.</summary>
    public static bool IsHybrid(RendererInfo renderer) =>
        string.Equals(renderer.Name, WebViewRendererName, StringComparison.Ordinal);
}
