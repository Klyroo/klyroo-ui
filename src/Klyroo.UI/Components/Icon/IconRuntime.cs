using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Klyroo.UI.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Klyroo.UI;

/// <summary>
/// Written into an assembly by the Klyroo.UI build when the project bundles icons: <see cref="Registrar"/> is the generated class whose module
/// initializer registers them. <see cref="KlyrooIconBundle"/> uses it to register the icons of every assembly of the app before the first icon is
/// drawn, so an icon declared in a client (WebAssembly) project is there when the server renders a component of that project.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class KlyrooIconRegistrarAttribute : Attribute
{
    public KlyrooIconRegistrarAttribute(Type registrar) => Registrar = registrar;

    public Type Registrar { get; }
}

/// <summary>
/// The icons an app bundles at build time. The code the Klyroo.UI build targets generate calls <see cref="Add"/> from a module
/// initializer; apps do not use it directly (see <c>KlyrooOptions.Icons.Register</c> to add an icon in code).
/// </summary>
/// <remarks>
/// A module initializer runs the first time code of its assembly runs, which is too late in a Blazor Web App with an interactive client project:
/// the server renders a component of the client project before any client code ran. So the first lookup (any lookup, including the one that tells
/// which prefixes the <c>i-prefix-name</c> form may use) registers the bundles of the app first: the assemblies already loaded, the entry assembly's
/// references and what they reference in turn when they use Klyroo.UI, then every assembly loaded later. All of them go through the generated
/// <see cref="KlyrooIconRegistrarAttribute"/>, and the registration is the module initializer the assembly would run anyway (it runs once).
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class KlyrooIconBundle
{
    private static readonly ConcurrentDictionary<string, IconBody> Icons = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> Prefixes = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Assembly, byte> Seen = new();
    private static readonly object Gate = new();
    private static volatile bool _ready;
    private static bool _running;

    private static readonly string[] SkippedAssemblies = { "System,", "System.", "Microsoft.", "netstandard", "mscorlib", "WindowsBase", "Klyroo.UI," };

    /// <summary>Adds an icon: <paramref name="body"/> is the markup inside the <c>&lt;svg&gt;</c> element, <paramref name="viewBox"/> its drawing area.</summary>
    public static void Add(string prefix, string name, string body, string viewBox)
    {
        Icons[prefix + ":" + name] = new IconBody(body, viewBox);
        Prefixes[prefix] = 0;
    }

    internal static void AddPrefix(string prefix) => Prefixes[prefix] = 0;

    internal static bool HasPrefix(string prefix)
    {
        EnsureDiscovered();
        return Prefixes.ContainsKey(prefix);
    }

    internal static bool TryGet(string key, out IconBody body)
    {
        EnsureDiscovered();
        return Icons.TryGetValue(key, out body);
    }

    /// <summary>Registers the bundles of the app, once, before the first lookup answers.</summary>
    internal static void EnsureDiscovered()
    {
        if (_ready) return;
        lock (Gate)
        {
            // Same thread again (a module initializer that looks something up) or another thread that waited for the first: nothing left to do.
            if (_ready || _running) return;
            _running = true;
            try
            {
                AppDomain.CurrentDomain.AssemblyLoad += (_, e) =>
                {
                    // An assembly loaded later (a lazily loaded client assembly, a plug-in): best effort, a throwing initializer must not break the load that triggered it.
                    try { Discover(e.LoadedAssembly); } catch (Exception) { }
                };
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) Discover(assembly);
                var entry = Assembly.GetEntryAssembly();
                if (entry != null) DiscoverReferences(entry);
            }
            finally
            {
                _running = false;
                _ready = true;
            }
        }
    }

    /// <summary>Runs the generated registration of an assembly if it has one. Returns whether it announced a bundle.</summary>
    internal static bool Discover(Assembly assembly)
    {
        if (assembly.IsDynamic || IsSkipped(assembly) || !Seen.TryAdd(assembly, 0)) return false;
        KlyrooIconRegistrarAttribute[] registrars;
        try
        {
            registrars = assembly.GetCustomAttributes<KlyrooIconRegistrarAttribute>().ToArray();
        }
        catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException or TypeLoadException or NotSupportedException)
        {
            return false;
        }
        foreach (var registrar in registrars)
        {
            // The generated class registers from its module initializer: running the module constructor is that call, and it runs only once.
            RuntimeHelpers.RunModuleConstructor(registrar.Registrar.Module.ModuleHandle);
        }
        return registrars.Length > 0;
    }

    /// <summary>The references of an assembly, loaded and registered; a reference is followed further only when it uses Klyroo.UI (a library of components with icons).</summary>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Best effort: an assembly the trimmer removed has no module initializer to run, and every assembly that is kept is found in the loaded assemblies or by the AssemblyLoad event.")]
    internal static void DiscoverReferences(Assembly root)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal) { root.FullName ?? "" };
        var queue = new Queue<Assembly>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var assembly = queue.Dequeue();
            AssemblyName[] references;
            try
            {
                references = assembly.GetReferencedAssemblies();
            }
            catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException or NotSupportedException)
            {
                continue;
            }
            foreach (var reference in references)
            {
                if (IsSkipped(reference.FullName) || !visited.Add(reference.FullName)) continue;
                Assembly loaded;
                try
                {
                    loaded = Assembly.Load(reference);
                }
                catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException)
                {
                    // Not there (a lazily loaded assembly of a WebAssembly app): it registers when it is loaded.
                    continue;
                }
                Discover(loaded);
                if (UsesKlyroo(loaded)) queue.Enqueue(loaded);
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Best effort: an assembly the trimmer removed has no module initializer to run, and every assembly that is kept is found in the loaded assemblies or by the AssemblyLoad event.")]
    private static bool UsesKlyroo(Assembly assembly)
    {
        try
        {
            return assembly.GetReferencedAssemblies().Any(r => r.Name == "Klyroo.UI");
        }
        catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException or NotSupportedException)
        {
            return false;
        }
    }

    private static bool IsSkipped(Assembly assembly) => IsSkipped(assembly.FullName ?? "");

    private static bool IsSkipped(string fullName) => SkippedAssemblies.Any(s => fullName.StartsWith(s, StringComparison.Ordinal));
}

/// <summary>Finds the data of an icon name: icons registered on the options, then the bundle of the app, then the Lucide icons built into the library.</summary>
internal static class IconResolver
{
    public static bool TryResolve(string? name, KlyrooIconOptions? options, out IconBody body)
    {
        body = default;
        if (string.IsNullOrWhiteSpace(name)) return false;
        options?.Freeze();
        // HasPrefix registers the bundles of the app first, so "i-custom-logo" reads the same whichever assembly renders first.
        if (!IconNames.TryParse(name, KlyrooIconBundle.HasPrefix, out var icon, out _)) return false;
        var key = icon.Key;
        if (options is not null && options.TryGetRegistered(key, out body)) return true;
        if (KlyrooIconBundle.TryGet(key, out body)) return true;
        if (icon.Prefix == IconNames.DefaultPrefix && BuiltinIcons.Lucide.TryGetValue(icon.Name, out var builtin))
        {
            body = new IconBody(builtin, BuiltinIcons.ViewBox);
            return true;
        }
        return false;
    }
}

/// <summary>
/// An id of an icon (a gradient, a clip path, an animation another one starts after) is global to the page, so two icons that use the same id
/// would draw each other's gradient. Every drawing of an icon gets its own ids: <c>id="a"</c> becomes <c>id="a-lx1"</c>, with every reference to it
/// (<c>url(#a)</c>, <c>href="#a"</c>, <c>begin="a.end"</c>) rewritten. A body with no id (the Lucide icons, most Iconify icons) is returned as it is.
/// </summary>
internal static class IconIds
{
    private static long _counter;

    private static readonly Regex IdAttribute = new(@"(?<=\s)id\s*=\s*(?<q>[""'])(?<id>[^""']*)\k<q>", RegexOptions.Compiled);
    private static readonly Regex UrlReference = new(@"url\(\s*(?<q>[""']?)#(?<id>[^)""'\s]+)\k<q>\s*\)", RegexOptions.Compiled);
    private static readonly Regex HrefReference = new(@"(?<pre>\b(?:xlink:)?href\s*=\s*)(?<q>[""'])#(?<id>[^""']+)\k<q>", RegexOptions.Compiled);
    private static readonly Regex Timing = new(@"(?<pre>\b(?:begin|end)\s*=\s*)(?<q>[""'])(?<value>[^""']*)\k<q>", RegexOptions.Compiled);

    /// <summary>A suffix no other drawing has.</summary>
    public static string NextSuffix() => "-kly" + Interlocked.Increment(ref _counter).ToString("x", CultureInfo.InvariantCulture);

    /// <summary>The body with its ids made unique; <paramref name="suffix"/> is taken from <see cref="NextSuffix"/> the first time it is needed and kept, so a component that draws again keeps its ids.</summary>
    public static string Uniquify(string body, ref string? suffix)
    {
        if (body.IndexOf("id=", StringComparison.Ordinal) < 0 && body.IndexOf("id =", StringComparison.Ordinal) < 0) return body;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in IdAttribute.Matches(body)) ids.Add(m.Groups["id"].Value);
        if (ids.Count == 0) return body;

        var s = suffix ??= NextSuffix();
        var ordered = ids.OrderByDescending(i => i.Length).ToList();

        var result = IdAttribute.Replace(body, m => "id=" + m.Groups["q"].Value + m.Groups["id"].Value + s + m.Groups["q"].Value);
        result = UrlReference.Replace(result, m => ids.Contains(m.Groups["id"].Value) ? "url(" + m.Groups["q"].Value + "#" + m.Groups["id"].Value + s + m.Groups["q"].Value + ")" : m.Value);
        result = HrefReference.Replace(result, m => ids.Contains(m.Groups["id"].Value) ? m.Groups["pre"].Value + m.Groups["q"].Value + "#" + m.Groups["id"].Value + s + m.Groups["q"].Value : m.Value);
        result = Timing.Replace(result, m =>
        {
            var parts = m.Groups["value"].Value.Split(';').Select(part => RewriteTiming(part, ordered, s));
            return m.Groups["pre"].Value + m.Groups["q"].Value + string.Join(";", parts) + m.Groups["q"].Value;
        });
        return result;
    }

    // "a.end", "a.begin+0.1s", "a.repeat(2)", "0.5s", "click": only a value that starts with "<id>." names an element.
    private static string RewriteTiming(string part, List<string> ids, string suffix)
    {
        var trimmed = part.TrimStart();
        foreach (var id in ids)
        {
            if (trimmed.Length > id.Length && trimmed[id.Length] == '.' && trimmed.StartsWith(id, StringComparison.Ordinal))
            {
                return part.Substring(0, part.Length - trimmed.Length) + id + suffix + trimmed.Substring(id.Length);
            }
        }
        return part;
    }
}

internal static class IconRender
{
    public const string DefaultViewBox = BuiltinIcons.ViewBox;

    /// <summary>An icon as a render fragment, for the parameters that take one. An unknown name renders an empty icon marked <c>data-kly-missing-icon</c>.</summary>
    /// <remarks>An icon with ids gets new ids each time the fragment is built (so each drawing has its own): a parent that renders again redraws such an icon, which restarts its animation.</remarks>
    public static RenderFragment Fragment(KlyrooIconOptions? options, string? name, string? @class) => builder =>
    {
        var found = IconResolver.TryResolve(name, options, out var body);
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "viewBox", found ? body.ViewBox : DefaultViewBox);
        builder.AddAttribute(2, "fill", "currentColor");
        builder.AddAttribute(3, "aria-hidden", "true");
        builder.AddAttribute(4, "class", @class);
        if (!found) builder.AddAttribute(5, "data-kly-missing-icon", name);
        if (found)
        {
            string? suffix = null;
            builder.AddMarkupContent(6, IconIds.Uniquify(body.Body, ref suffix));
        }
        builder.CloseElement();
    };

    /// <summary>The markup of an icon for places that build their own element (a <c>data-*</c> attribute read by script).</summary>
    public static string Svg(KlyrooIconOptions? options, string? name, string @class)
    {
        if (!IconResolver.TryResolve(name, options, out var body)) return "";
        string? suffix = null;
        return $"<svg viewBox=\"{body.ViewBox}\" fill=\"currentColor\" aria-hidden=\"true\" class=\"{@class}\">{IconIds.Uniquify(body.Body, ref suffix)}</svg>";
    }
}
