using System.Collections.Concurrent;
using System.ComponentModel;
using Blux.UI.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI;

/// <summary>
/// The icons an app bundles at build time. The code the Blux.UI build targets generate calls <see cref="Add"/> from a module
/// initializer; apps do not use it directly (see <c>BluxOptions.Icons.Register</c> to add an icon in code).
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class BluxIconBundle
{
    private static readonly ConcurrentDictionary<string, IconBody> Icons = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> Prefixes = new(StringComparer.Ordinal);

    /// <summary>Adds an icon: <paramref name="body"/> is the markup inside the <c>&lt;svg&gt;</c> element, <paramref name="viewBox"/> its drawing area.</summary>
    public static void Add(string prefix, string name, string body, string viewBox)
    {
        Icons[prefix + ":" + name] = new IconBody(body, viewBox);
        Prefixes[prefix] = 0;
    }

    internal static void AddPrefix(string prefix) => Prefixes[prefix] = 0;

    internal static bool HasPrefix(string prefix) => Prefixes.ContainsKey(prefix);

    internal static bool TryGet(string key, out IconBody body) => Icons.TryGetValue(key, out body);
}

/// <summary>Finds the data of an icon name: icons registered on the options, then the bundle of the app, then the Lucide icons built into the library.</summary>
internal static class IconResolver
{
    public static bool TryResolve(string? name, BluxIconOptions? options, out IconBody body)
    {
        body = default;
        if (string.IsNullOrWhiteSpace(name)) return false;
        options?.Freeze();
        if (!IconNames.TryParse(name, BluxIconBundle.HasPrefix, out var icon, out _)) return false;
        var key = icon.Key;
        if (options is not null && options.TryGetRegistered(key, out body)) return true;
        if (BluxIconBundle.TryGet(key, out body)) return true;
        if (icon.Prefix == IconNames.DefaultPrefix && BuiltinIcons.Lucide.TryGetValue(icon.Name, out var builtin))
        {
            body = new IconBody(builtin, BuiltinIcons.ViewBox);
            return true;
        }
        return false;
    }
}

internal static class IconRender
{
    public const string DefaultViewBox = BuiltinIcons.ViewBox;

    /// <summary>An icon as a render fragment, for the parameters that take one. An unknown name renders an empty icon marked <c>data-bx-missing-icon</c>.</summary>
    public static RenderFragment Fragment(BluxIconOptions? options, string? name, string? @class) => builder =>
    {
        var found = IconResolver.TryResolve(name, options, out var body);
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "viewBox", found ? body.ViewBox : DefaultViewBox);
        builder.AddAttribute(2, "fill", "currentColor");
        builder.AddAttribute(3, "aria-hidden", "true");
        builder.AddAttribute(4, "class", @class);
        if (!found) builder.AddAttribute(5, "data-bx-missing-icon", name);
        if (found) builder.AddMarkupContent(6, body.Body);
        builder.CloseElement();
    };

    /// <summary>The markup of an icon for places that build their own element (a <c>data-*</c> attribute read by script).</summary>
    public static string Svg(BluxIconOptions? options, string? name, string @class)
    {
        if (!IconResolver.TryResolve(name, options, out var body)) return "";
        return $"<svg viewBox=\"{body.ViewBox}\" fill=\"currentColor\" aria-hidden=\"true\" class=\"{@class}\">{body.Body}</svg>";
    }
}
