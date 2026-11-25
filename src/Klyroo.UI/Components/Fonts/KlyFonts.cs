using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Klyroo.UI;

/// <summary>
/// Written into the app's assembly at build time by the <c>KlyrooFont</c> targets: the manifest <see cref="KlyFonts"/> reads.
/// Not meant to be used by hand.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class KlyrooFontsManifestAttribute(string json) : Attribute
{
    /// <summary>The manifest, as JSON: the route of the generated stylesheet and the faces to preload.</summary>
    public string Json { get; } = json;
}

/// <summary>
/// Place in <c>&lt;head&gt;</c>, before your own stylesheet. Links the stylesheet generated at build time from the
/// <c>KlyrooFont</c> items of the project (the <c>@font-face</c> rules, the fallback faces and <c>--klyroo-font-sans</c> /
/// <c>--klyroo-font-mono</c>) and preloads the main face of every font that asks for it, so the text that needs it is not held up
/// by a stylesheet that is read first and a font that is requested after. Everything comes from the app's own origin: nothing is
/// requested from a font provider while the page runs.
/// </summary>
/// <remarks>
/// It renders nothing when the project declares no font (or <c>KlyrooFonts</c> is <c>false</c>). The manifest is read from the entry
/// assembly, so declare the <c>KlyrooFont</c> items in the project that hosts the app (the server project of an Auto app): fonts
/// declared in a Blazor WebAssembly project are a build error. In a .NET MAUI Blazor Hybrid app, declare them in the MAUI project and
/// render <c>KlyFonts</c> through <c>KlyHead</c> (a <c>HeadOutlet</c> root component renders it into the head): the manifest is found in the
/// loaded assemblies when there is no entry assembly, and the links are the plain routes served from the app's <c>wwwroot</c>. The manifest is an assembly
/// attribute generated at build, so it is the same in static rendering, prerendering and every interactive mode; the
/// stylesheet link follows the fingerprint of the static web assets (<c>@Assets</c>), the preload links the stable route of the
/// file, which is also what the stylesheet requests, so the font is downloaded once. See the Fonts guide.
/// </remarks>
public sealed class KlyFonts : ComponentBase
{
    private static readonly Lazy<FontManifest?> Generated = new(() =>
    {
        var attribute = FindAttribute();
        return attribute is null ? null : FontManifest.Parse(attribute.Json);
    });

    // The entry assembly in a Blazor Web App. A .NET MAUI app has none on every platform (Assembly.GetEntryAssembly() is null on
    // Android), so the loaded assemblies are searched for the one the build stamped: the app that declares the KlyrooFont items.
    private static KlyrooFontsManifestAttribute? FindAttribute()
    {
        if (Assembly.GetEntryAssembly()?.GetCustomAttribute<KlyrooFontsManifestAttribute>() is { } entry)
        {
            return entry;
        }
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic) continue;
            try
            {
                if (assembly.GetCustomAttribute<KlyrooFontsManifestAttribute>() is { } found) return found;
            }
            catch (Exception e) when (e is FileNotFoundException or FileLoadException or NotSupportedException)
            {
                // An assembly that cannot be inspected cannot hold the manifest.
            }
        }
        return null;
    }

    /// <summary>
    /// A manifest to render instead of the one generated at build, for tests and previews. Same shape as the generated
    /// <c>klyroo.fonts.json</c>.
    /// </summary>
    [Parameter] public string? ManifestJson { get; set; }

    // A web view has no fingerprinted asset map (Assets is empty), so the route is used as it is.
    private bool Hybrid => KlyrooHost.IsHybrid(RendererInfo);

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var manifest = ManifestJson is null ? Generated.Value : FontManifest.Parse(ManifestJson);
        if (manifest is null)
        {
            return;
        }

        // Sequence numbers follow the position in the code, not the iteration.
        foreach (var href in manifest.Preload)
        {
            builder.OpenElement(0, "link");
            builder.AddAttribute(1, "rel", "preload");
            builder.AddAttribute(2, "as", "font");
            builder.AddAttribute(3, "type", FontManifest.MimeType(href));
            builder.AddAttribute(4, "crossorigin", true);
            builder.AddAttribute(5, "href", href);
            builder.CloseElement();
        }
        builder.OpenElement(6, "link");
        builder.AddAttribute(7, "rel", "stylesheet");
        builder.AddAttribute(8, "href", Hybrid ? manifest.Css : Assets[manifest.Css]);
        builder.CloseElement();
    }

    internal sealed record FontManifest(string Css, IReadOnlyList<string> Preload)
    {
        public static FontManifest? Parse(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("css", out var css) || css.GetString() is not { Length: > 0 } route)
            {
                return null;
            }
            var preload = new List<string>();
            if (root.TryGetProperty("fonts", out var fonts))
            {
                foreach (var font in fonts.EnumerateArray())
                {
                    if (!font.TryGetProperty("preload", out var files)) continue;
                    foreach (var file in files.EnumerateArray())
                    {
                        if (file.GetString() is { Length: > 0 } href && !preload.Contains(href)) preload.Add(href);
                    }
                }
            }
            return new FontManifest(route, preload);
        }

        public static string MimeType(string href) => Path.GetExtension(href).ToLowerInvariant() switch
        {
            ".woff" => "font/woff",
            ".ttf" => "font/ttf",
            ".otf" => "font/otf",
            _ => "font/woff2",
        };
    }
}
