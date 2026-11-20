using System.Text;
using System.Text.Json;
using Blux.UI.Mcp.Model;

namespace Blux.UI.Mcp;

/// <summary>The sections of a component page, rendered as Markdown (and the metadata as JSON) for tools, resources and llms-full.txt.</summary>
public static class ComponentMarkdown
{
    public static readonly string[] Sections = ["usage", "examples", "api", "theme"];

    public static string Page(BundleComponent component, IEnumerable<string> sections, string? siteUrl = null)
    {
        var sb = new StringBuilder();
        sb.Append($"# {component.Name}\n\n");
        foreach (var section in sections)
        {
            sb.Append(section switch
            {
                "usage" => Usage(component, siteUrl),
                "examples" => Examples(component),
                "api" => Api(component),
                "theme" => Theme(component),
                _ => "",
            });
            sb.Append('\n');
        }
        return sb.ToString().TrimEnd() + "\n";
    }

    public static string Link(BundleComponent component, string? siteUrl) => $"{siteUrl?.TrimEnd('/')}/components/{component.Slug}";

    public static string Usage(BundleComponent component, string? siteUrl = null)
    {
        var sb = new StringBuilder();
        sb.Append("## Usage\n\n");
        sb.Append($"{component.Description}\n\n");
        sb.Append($"- Family: {component.Group}\n");
        sb.Append($"- Component: {string.Join(", ", component.Api.Select(a => $"`{a.Name}`"))}\n");
        sb.Append($"- Docs: {Link(component, siteUrl)}\n");
        var bindable = component.Api.SelectMany(a => a.Parameters).Where(p => p.Bindable).Select(p => $"`@bind-{p.Name}`").Distinct().ToList();
        if (bindable.Count > 0) sb.Append($"- Binding: {string.Join(", ", bindable)}\n");
        var events = component.Api.SelectMany(a => a.Events).Select(e => $"`{e.Name}`").Distinct().ToList();
        if (events.Count > 0) sb.Append($"- Events: {string.Join(", ", events)}\n");
        sb.Append("- Every component also takes `Class` (classes for the root element) and, when it has slots, `Ui` (classes per slot).\n");

        if (component.Examples.Count > 0)
        {
            var first = component.Examples[0];
            sb.Append($"\n### {first.Title}\n\n");
            if (first.Description is not null) sb.Append($"{first.Description}\n\n");
            sb.Append(Fence(first.Source));
            if (component.Examples.Count > 1)
            {
                sb.Append($"\nMore examples (`get-example`): {string.Join(", ", component.Examples.Skip(1).Select(e => $"`{e.Id}`"))}\n");
            }
        }
        return sb.ToString();
    }

    public static string Examples(BundleComponent component)
    {
        var sb = new StringBuilder("## Examples\n");
        foreach (var example in component.Examples)
        {
            sb.Append($"\n### {example.Title} (`{example.Id}`)\n\n");
            if (example.Description is not null) sb.Append($"{example.Description}\n\n");
            sb.Append(Fence(example.Source));
        }
        return sb.ToString();
    }

    public static string Api(BundleComponent component)
    {
        var sb = new StringBuilder("## API\n");
        foreach (var api in component.Api)
        {
            sb.Append($"\n### {api.Name}\n\n");
            if (api.Summary is not null) sb.Append($"{api.Summary}\n\n");
            AppendParameters(sb, "Parameters", api.Parameters);
            AppendParameters(sb, "Events", api.Events);
            AppendParameters(sb, "Slots (RenderFragment)", api.Slots);
            if (api.Ui is { Slots.Count: > 0 } ui)
            {
                sb.Append($"**Ui** (`{ui.Type}`, classes per slot): {string.Join(", ", ui.Slots.Select(s => $"`{s.Name}`"))}\n");
            }
        }
        return sb.ToString();
    }

    private static void AppendParameters(StringBuilder sb, string title, IReadOnlyList<BundleParameter> parameters)
    {
        if (parameters.Count == 0) return;
        sb.Append($"**{title}**\n\n");
        foreach (var p in parameters)
        {
            sb.Append($"- `{p.Name}`: `{p.Type}`");
            if (p.Default is not null) sb.Append($" = `{p.Default}`");
            if (p.Required) sb.Append(" (required)");
            if (p.Bindable) sb.Append($" (bindable: `@bind-{p.Name}`)");
            if (p.Description is not null) sb.Append($". {p.Description.TrimEnd('.')}.");
            if (p.Values is { Count: > 0 }) sb.Append($" Values: {string.Join(", ", p.Values.Select(v => $"`{v}`"))}.");
            sb.Append('\n');
        }
        sb.Append('\n');
    }

    public static string Theme(BundleComponent component)
    {
        var themes = component.Api.Where(a => a.Theme is not null).ToList();
        var sb = new StringBuilder("## Theme\n\n");
        if (themes.Count == 0)
        {
            sb.Append($"{component.Name} has no theme of its own; style it with `Class`.\n");
            return sb.ToString();
        }
        sb.Append("Configure globally with `AddBluxUi(o => o.<Property>.Slot(\"<slot>\", \"<classes>\"))`, `Variant(variant, value, slot, classes)` and `Default(variant, value)`. ");
        sb.Append("Classes you add are merged after the defaults, so the last conflicting utility wins. Per instance, use `Class` (root) and `Ui` (any slot).\n");
        foreach (var api in themes)
        {
            var theme = api.Theme!;
            sb.Append($"\n### {api.Name}: `o.{theme.Property}`\n\n**Slots**\n\n");
            foreach (var (slot, classes) in theme.Slots) sb.Append($"- `{slot}`: `{classes}`\n");
            if (theme.Variants.Count > 0)
            {
                sb.Append("\n**Variants**\n\n");
                foreach (var (variant, values) in theme.Variants)
                {
                    var defaultValue = theme.Defaults.GetValueOrDefault(variant);
                    sb.Append($"- `{variant}`: {string.Join(", ", values.Select(v => v.Key == defaultValue ? $"`{v.Key}` (default)" : $"`{v.Key}`"))}\n");
                    foreach (var (value, slots) in values)
                    {
                        foreach (var (slot, classes) in slots) sb.Append($"  - `{value}` on `{slot}`: `{classes}`\n");
                    }
                }
            }
            if (theme.Compounds.Count > 0)
            {
                sb.Append("\n**Compound variants**\n\n");
                foreach (var compound in theme.Compounds)
                {
                    var when = string.Join(" and ", compound.When.Select(w => $"{w.Key} = {w.Value}"));
                    foreach (var (slot, classes) in compound.Classes) sb.Append($"- when {when}, `{slot}`: `{classes}`\n");
                }
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Parameters, events, slots and <c>Ui</c> slots as JSON. Compact: one string per member (<c>Name: type = default</c>);
    /// full: an object per member with description, required, bindable and allowed values, and the theme slots and variants.
    /// </summary>
    public static string Metadata(BundleComponent component, bool full)
    {
        object Member(BundleParameter p) => full
            ? new { p.Name, p.Type, p.Default, p.Description, p.Required, p.Bindable, p.Values }
            : (object)Compact(p);

        var types = component.Api.Select(api => new
        {
            type = api.Name,
            summary = full ? api.Summary : null,
            parameters = api.Parameters.Select(Member).ToList(),
            events = api.Events.Select(Member).ToList(),
            slots = api.Slots.Select(s => s.Name).ToList(),
            ui = api.Ui is null ? null : new { type = api.Ui.Type, slots = api.Ui.Slots.Select(s => s.Name).ToList() },
            theme = full && api.Theme is not null
                ? new
                {
                    property = api.Theme.Property,
                    slots = api.Theme.Slots.Keys.ToList(),
                    variants = api.Theme.Variants.ToDictionary(v => v.Key, v => v.Value.Keys.ToList()),
                    defaults = api.Theme.Defaults,
                }
                : null,
        });
        return JsonSerializer.Serialize(
            new { slug = component.Slug, name = component.Name, group = component.Group, docs = $"/components/{component.Slug}", components = types },
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
    }

    private static string Compact(BundleParameter p)
    {
        var text = $"{p.Name}: {p.Type}";
        if (p.Default is not null) text += $" = {p.Default}";
        if (p.Bindable) text += " [bind]";
        if (p.Required) text += " [required]";
        return text;
    }

    internal static string Fence(string source, string language = "razor") =>
        $"```{language}\n{source.TrimEnd()}\n```\n";
}
