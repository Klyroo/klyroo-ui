using System.Reflection;
using System.Text.RegularExpressions;
using Blux.Playground.Client.Docs;
using Blux.UI;
using Blux.UI.Mcp.Model;
using Blux.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Blux.UI.BundleGenerator;

/// <summary>
/// Builds the <see cref="DocsBundle"/> from what the docs site already knows: the catalog, <see cref="ApiReader"/> (parameters,
/// defaults and XML documentation), the embedded example sources and the guide pages.
/// </summary>
public static class BundleBuilder
{
    public static Blux.UI.Mcp.Model.DocsBundle Build(string repoRoot)
    {
        var options = new BluxOptions();
        var themes = options.GetType().GetProperties()
            .Where(p => p.PropertyType == typeof(ComponentTheme))
            .Select(p => (Property: p.Name, Theme: (ComponentTheme)p.GetValue(options)!))
            .ToList();

        var components = Catalog.Components
            .Select(c => new BundleComponent(
                c.Slug, c.Name, c.Group, c.Description,
                [.. c.Examples.Select(Example)],
                [.. c.Api.Select(t => ApiType(t, themes, repoRoot))]))
            .ToList();

        return new Blux.UI.Mcp.Model.DocsBundle(
            "Blux.UI",
            Version(),
            [.. Catalog.Groups.Where(g => components.Any(c => c.Group == g))],
            components,
            Guides(repoRoot),
            Icons());
    }

    private static string Version()
    {
        var info = typeof(BxButton).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(BxButton).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        var plus = info.IndexOf('+');
        return plus < 0 ? info : info[..plus];
    }

    private static BundleExample Example(ExampleEntry entry)
    {
        using var stream = typeof(Catalog).Assembly.GetManifestResourceStream($"example:{entry.Type.Name}")
            ?? throw new InvalidOperationException($"The source of example {entry.Type.Name} is not embedded in the docs site.");
        var source = new StreamReader(stream).ReadToEnd().Replace("\r\n", "\n").TrimEnd();
        return new BundleExample(entry.Type.Name, entry.Title, entry.Description, source);
    }

    private static BundleApiType ApiType(Type type, List<(string Property, ComponentTheme Theme)> themes, string repoRoot)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        var all = ApiReader.Parameters(type);
        var parameters = new List<BundleParameter>();
        var events = new List<BundleParameter>();
        var slots = new List<BundleParameter>();

        foreach (var p in all)
        {
            var property = definition.GetProperty(p.Name, BindingFlags.Public | BindingFlags.Instance)!;
            var typeName = ApiReader.TypeName(property.PropertyType);
            var bindable = definition.GetProperty(p.Name + "Changed", BindingFlags.Public | BindingFlags.Instance)?.PropertyType.Name.StartsWith("EventCallback", StringComparison.Ordinal) == true;
            var entry = new BundleParameter(
                p.Name, typeName, p.Default, p.Description,
                property.GetCustomAttribute<EditorRequiredAttribute>() is not null,
                bindable,
                ValuesOf(property.PropertyType));

            if (property.PropertyType.Name.StartsWith("EventCallback", StringComparison.Ordinal))
            {
                // XChanged is the other half of @bind-X, already shown as "bindable" on X.
                if (p.Name.EndsWith("Changed", StringComparison.Ordinal)
                    && definition.GetProperty(p.Name[..^"Changed".Length], BindingFlags.Public | BindingFlags.Instance) is not null)
                {
                    continue;
                }
                events.Add(entry);
            }
            else if (property.PropertyType.Name.StartsWith("RenderFragment", StringComparison.Ordinal))
            {
                slots.Add(entry);
            }
            else
            {
                parameters.Add(entry);
            }
        }

        return new BundleApiType(
            DisplayName(type), ApiReader.Describe(definition), parameters, events, slots,
            UiOf(definition), ThemeOf(definition, themes, repoRoot));
    }

    /// <summary><c>BxTable&lt;TItem&gt;</c> for a generic component, its type name otherwise.</summary>
    private static string DisplayName(Type type)
    {
        if (!type.IsGenericType) return type.Name;
        var definition = type.GetGenericTypeDefinition();
        return $"{definition.Name[..definition.Name.IndexOf('`')]}<{string.Join(", ", definition.GetGenericArguments().Select(a => a.Name))}>";
    }

    private static IReadOnlyList<string>? ValuesOf(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsEnum) return Enum.GetNames(type);
        if (type == typeof(BxColor))
        {
            return [.. typeof(BxColor).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(BxColor)).Select(p => ((BxColor)p.GetValue(null)!).Value)];
        }
        return null;
    }

    private static BundleUi? UiOf(Type component)
    {
        var ui = component.GetProperty("Ui", BindingFlags.Public | BindingFlags.Instance);
        if (ui is null) return null;
        var uiType = Nullable.GetUnderlyingType(ui.PropertyType) ?? ui.PropertyType;
        var slots = uiType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => p.Name != "EqualityContract" && p.PropertyType == typeof(string))
            .Select(p => new BundleUiSlot(p.Name, ApiReader.Describe(p)))
            .ToList();
        return new BundleUi(ApiReader.TypeName(uiType), slots);
    }

    private static BundleTheme? ThemeOf(Type component, List<(string Property, ComponentTheme Theme)> themes, string repoRoot)
    {
        var name = component.Name.StartsWith("Bx", StringComparison.Ordinal) ? component.Name[2..] : component.Name;
        if (name.Contains('`')) name = name[..name.IndexOf('`')];

        // The component says which theme it resolves: the first "Options.X" of its source that names a theme.
        var used = SourceFiles(repoRoot, component.Name.Contains('`') ? component.Name[..component.Name.IndexOf('`')] : component.Name)
            .SelectMany(file => Regex.Matches(File.ReadAllText(file), @"\bOptions\.(\w+)").Select(m => m.Groups[1].Value))
            .FirstOrDefault(property => themes.Any(t => t.Property == property));
        var match = used is null ? default : themes.First(t => t.Property == used);
        if (match.Theme is null) match = themes.FirstOrDefault(t => string.Equals(t.Property, name, StringComparison.OrdinalIgnoreCase));
        if (match.Theme is null) return null;

        var theme = match.Theme;
        return new BundleTheme(
            theme.Name,
            match.Property,
            theme.Slots.ToDictionary(s => s.Key, s => s.Value),
            theme.Variants.ToDictionary(
                v => v.Key,
                v => (IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>)v.Value.ToDictionary(
                    value => value.Key,
                    value => (IReadOnlyDictionary<string, string>)value.Value.ToDictionary(s => s.Key, s => s.Value))),
            theme.DefaultVariants.ToDictionary(d => d.Key, d => d.Value),
            [.. theme.CompoundVariants.Select(c => new BundleCompound(
                c.When.ToDictionary(w => w.Key, w => w.Value),
                c.Classes.ToDictionary(s => s.Key, s => s.Value)))]);
    }

    private static IEnumerable<string> SourceFiles(string repoRoot, string componentName) =>
        Directory.EnumerateFiles(Path.Combine(repoRoot, "src", "Blux.UI", "Components"), componentName + ".razor*", SearchOption.AllDirectories);

    private static IReadOnlyList<string> Icons()
    {
        // The Lucide icons embedded in the library; the class is internal, so it is read by reflection.
        var registry = typeof(BxButton).Assembly.GetType("Blux.UI.BuiltinIcons");
        var builtin = registry?.GetField("Lucide", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as IReadOnlyDictionary<string, string>;
        return builtin is null ? [] : [.. builtin.Keys.Order(StringComparer.Ordinal)];
    }

    private static IReadOnlyList<BundleGuide> Guides(string repoRoot)
    {
        var pagesDirectory = Path.Combine(repoRoot, "playground", "Blux.Playground", "Blux.Playground.Client", "Pages", "Docs");
        var routes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(pagesDirectory, "*.razor"))
        {
            var text = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(text, """^@page\s+"([^"]+)"\s*$""", RegexOptions.Multiline))
            {
                routes[m.Groups[1].Value] = file;
            }
        }

        var guides = new List<BundleGuide>();
        foreach (var (href, title, section) in Catalog.Guides)
        {
            if (!routes.TryGetValue(href, out var file))
            {
                throw new InvalidOperationException($"Catalog.Guides lists {href}, but no page in {pagesDirectory} declares that route.");
            }
            var markdown = GuideMarkdown.Convert(File.ReadAllText(file), new Dictionary<string, string>
            {
                ["Blux.Playground.Client.Docs.Catalog.Components.Length"] = Catalog.Components.Length.ToString(),
            });
            guides.Add(new BundleGuide(href, title, section, markdown));
        }
        return guides;
    }
}
