using System.ComponentModel;
using System.Reflection;
using System.Text;
using Blux.UI.Mcp.Model;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Blux.UI.Mcp;

/// <summary>
/// The tools of the Blux UI MCP server. They mirror the Nuxt UI ones where they apply to Blazor. Every tool is read-only and
/// answers in Markdown (JSON for component metadata). Unknown names fail with a message that lists the closest matches.
/// </summary>
[McpServerToolType]
public sealed class DocsTools(DocsService docs)
{
    /// <summary>The names of every tool of this class.</summary>
    public static IReadOnlyList<string> Names { get; } =
        [.. typeof(DocsTools).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.GetCustomAttribute<McpServerToolAttribute>()?.Name)
            .OfType<string>()];

    [McpServerTool(Name = "search-components", Title = "Search components", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Find Blux UI components by name, family, alternate name (dialog = Modal) or intent (\"date picker\", \"confirm before deleting\"). Exact names rank first. Without a query it lists every component by family.")]
    public string SearchComponents(
        [Description("A name, an alternate name, a family or a description of what you need. Omit to list all components.")] string? query = null,
        [Description("Restrict to a family: Elements, Form, Overlay, Navigation, Data, Layout, Page, Dashboard, Chat or Editor.")] string? group = null,
        [Description("Maximum number of results for a query (default 10).")] int limit = 10)
    {
        if (!string.IsNullOrWhiteSpace(group) && !docs.Bundle.Groups.Contains(group.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new McpException($"Unknown family '{group}'. Use one of: {string.Join(", ", docs.Bundle.Groups)}.");
        }

        var sb = new StringBuilder();
        if (string.IsNullOrWhiteSpace(query))
        {
            var all = docs.SearchComponents(null, group).Select(h => h.Item).ToList();
            sb.Append($"{all.Count} components of Blux UI {docs.Bundle.Version}. Use `get-component` for the details of one.\n");
            foreach (var family in docs.Bundle.Groups)
            {
                var inFamily = all.Where(c => c.Group == family).ToList();
                if (inFamily.Count == 0) continue;
                sb.Append($"\n## {family}\n\n");
                foreach (var c in inFamily) sb.Append(Line(c));
            }
            return sb.ToString();
        }

        var hits = docs.SearchComponents(query, group, limit);
        if (hits.Count == 0)
        {
            sb.Append($"No component matches \"{query}\". Try another word, or list them all by calling this tool without a query.\n");
            return sb.ToString();
        }
        sb.Append($"{hits.Count} component(s) for \"{query}\", best first:\n\n");
        foreach (var hit in hits) sb.Append(Line(hit.Item));
        return sb.ToString();
    }

    [McpServerTool(Name = "get-component", Title = "Get a component", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("The documentation of one component as Markdown, by section: usage (description and a minimal example), examples (every example with its source), api (parameters, events, slots, Ui slots), theme (slots, variants and defaults to restyle it).")]
    public string GetComponent(
        [Description("A component name or slug: Button, BxButton, dropdown-menu.")] string name,
        [Description("Comma-separated sections: usage, examples, api, theme. Default: usage,examples,api.")] string? sections = null)
    {
        var component = Resolve(name);
        var wanted = ParseSections(sections);
        return ComponentMarkdown.Page(component, wanted, docs.SiteUrl);
    }

    [McpServerTool(Name = "get-component-metadata", Title = "Get component metadata", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("The parameters, events, slots and Ui slots of a component as JSON, without prose. Compact by default (one string per member: Name: type = default); pass full:true for descriptions, allowed values, required and bindable flags and the theme slots and variants.")]
    public string GetComponentMetadata(
        [Description("A component name or slug.")] string name,
        [Description("Return full objects instead of the compact form.")] bool full = false) =>
        ComponentMarkdown.Metadata(Resolve(name), full);

    [McpServerTool(Name = "list-examples", Title = "List examples", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists the live examples of the docs site, with the id to pass to `get-example`. Filter by component, or by words of the title.")]
    public string ListExamples(
        [Description("A component name or slug. Omit for every example.")] string? component = null,
        [Description("Words to look for in the example id, title and description.")] string? query = null)
    {
        var items = docs.AllExamples.ToList();
        if (!string.IsNullOrWhiteSpace(component))
        {
            var match = Resolve(component);
            items = [.. items.Where(i => i.Component.Slug == match.Slug)];
        }
        if (!string.IsNullOrWhiteSpace(query))
        {
            var words = query.ToLowerInvariant().Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries);
            items = [.. items.Where(i =>
            {
                var haystack = $"{i.Example.Id} {i.Example.Title} {i.Example.Description} {i.Component.Name}".ToLowerInvariant();
                return words.All(haystack.Contains);
            })];
        }
        if (items.Count == 0) return "No example matches. Call this tool without arguments to list them all.\n";

        var sb = new StringBuilder($"{items.Count} example(s). Pass an id to `get-example` for the source.\n");
        foreach (var group in items.GroupBy(i => i.Component.Slug))
        {
            sb.Append($"\n## {group.First().Component.Name}\n\n");
            foreach (var (_, example) in group)
            {
                sb.Append($"- `{example.Id}`: {example.Title}{(example.Description is null ? "" : $". {example.Description}")}\n");
            }
        }
        return sb.ToString();
    }

    [McpServerTool(Name = "get-example", Title = "Get an example", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("The Razor source of one example of the docs site. Examples are complete components: copy them and change the content.")]
    public string GetExample(
        [Description("An example id from `list-examples` or `get-component`, such as ButtonVariantsExample (the Example suffix is optional).")] string name)
    {
        var found = docs.FindExample(name);
        if (found is null)
        {
            var close = docs.CloseExamples(name);
            throw new McpException($"No example named '{name}'." + (close.Count == 0
                ? " Call `list-examples` to see them all."
                : $" Did you mean: {string.Join(", ", close.Select(c => c.Example.Id))}?"));
        }
        var (component, example) = found.Value;
        var sb = new StringBuilder($"# {example.Title} ({example.Id})\n\nComponent: {component.Name} ({ComponentMarkdown.Link(component, docs.SiteUrl)})\n\n");
        if (example.Description is not null) sb.Append($"{example.Description}\n\n");
        sb.Append(ComponentMarkdown.Fence(example.Source));
        return sb.ToString();
    }

    [McpServerTool(Name = "search-documentation", Title = "Search the documentation", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Full-text search of the guides (installation, theming, render modes, AI tooling) and of the component pages. Returns paths to pass to `get-documentation-page`.")]
    public string SearchDocumentation(
        [Description("What to look for.")] string query,
        [Description("Where to look: guides, components or all (default).")] string? section = null,
        [Description("Maximum number of results (default 8).")] int limit = 8)
    {
        if (!string.IsNullOrWhiteSpace(section) && !new[] { "all", "guides", "guide", "components", "component" }.Contains(section.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new McpException($"Unknown section '{section}'. Use guides, components or all.");
        }
        var hits = docs.SearchDocumentation(query, section, limit);
        if (hits.Count == 0) return $"Nothing found for \"{query}\". Pages: {string.Join(", ", docs.Bundle.Guides.Select(g => g.Path))}, and /components/{{slug}}.\n";

        var sb = new StringBuilder($"{hits.Count} result(s) for \"{query}\":\n\n");
        foreach (var hit in hits)
        {
            var passage = hit.Item;
            sb.Append($"- `{passage.Path}`: {passage.Title}{(passage.Heading is null ? "" : $" > {passage.Heading}")} ({passage.Kind}). {DocsService.Snippet(passage.Text, query)}\n");
        }
        return sb.ToString();
    }

    [McpServerTool(Name = "get-documentation-page", Title = "Get a documentation page", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("A page of the docs site as Markdown: a guide (/docs/installation, /docs/theming, /docs/render-modes, /docs/mcp...) or a component page (/components/button). Pass headings to get only some sections of a guide.")]
    public string GetDocumentationPage(
        [Description("The path of the page: /docs/theming, theming, /components/button.")] string path,
        [Description("Comma-separated headings to keep (guides only), matched by substring.")] string? headings = null)
    {
        var trimmed = (path ?? "").Trim();
        if (trimmed.TrimStart('/').StartsWith("components/", StringComparison.OrdinalIgnoreCase))
        {
            var component = Resolve(trimmed[(trimmed.IndexOf("components/", StringComparison.OrdinalIgnoreCase) + "components/".Length)..]);
            return ComponentMarkdown.Page(component, ["usage", "examples", "api"], docs.SiteUrl);
        }

        var guide = docs.FindGuide(trimmed);
        if (guide is null)
        {
            throw new McpException($"No page at '{path}'. Guides: {string.Join(", ", docs.Bundle.Guides.Select(g => g.Path))}. Component pages: /components/{{slug}} (see `search-components`).");
        }

        var markdown = guide.Markdown;
        var wanted = ToolFilter.Parse(headings);
        if (wanted is not null)
        {
            var filtered = MarkdownSections.Filter(markdown, wanted);
            if (filtered is null)
            {
                throw new McpException($"No heading of {guide.Path} matches '{headings}'. Headings: {string.Join("; ", MarkdownSections.Headings(markdown))}.");
            }
            markdown = $"# {guide.Title}\n\n{filtered}";
        }
        return $"Source: {guide.Path}\n\n{markdown}";
    }

    [McpServerTool(Name = "search-icons", Title = "Search icons", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Searches the Lucide icons built into Blux UI, usable as `<BxIcon Name=\"search\" />` (or `i-lucide-search`, `lucide:search`) with no setup. Any other Iconify icon (200,000+ in 280 collections, browse https://icones.js.org) works by name too, as `i-{collection}-{name}` or `{collection}:{name}` (`ph:house`, `mdi:home`, `simple-icons:github`): the app's build downloads the icons it writes as literals and bundles them (see the Icons guide). The icons components draw (close, check, chevrons...) are replaced with `o.Icons.Close = \"ph:x\"` in AddBluxUi.")]
    public string SearchIcons(
        [Description("A word of the icon name. Omit to list the built-in icons.")] string? query = null,
        [Description("Maximum number of results (default 30).")] int limit = 30)
    {
        var icons = docs.SearchIcons(query, Math.Max(1, limit));
        if (icons.Count == 0) return $"No built-in icon matches \"{query}\". {docs.Bundle.Icons.Count} Lucide icons are built in (call this tool without a query to list them), and any icon of the other Iconify collections works by name (`ph:house`, `i-mdi-home`): search them at https://icones.js.org and write the name as a literal, the build bundles it (Icons guide).\n";
        return $"{icons.Count} of {docs.Bundle.Icons.Count} built-in Lucide icons. Use `<BxIcon Name=\"name\" />`. Any other Iconify icon works by name as `prefix:name` or `i-prefix-name` (`ph:house`, `simple-icons:github`), bundled at build time when written as a literal; custom SVGs come from a `BluxIconCollection` folder or `BluxOptions.Icons.Register`.\n\n"
            + string.Join('\n', icons.Select(i => $"- {i}")) + "\n";
    }

    [McpServerTool(Name = "get-installation-guide", Title = "Get the installation guide", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Step-by-step setup of Blux UI in a Blazor app, for one render mode and one CSS mode: package, AddBluxUi registration (server and client), stylesheet, imports.")]
    public string GetInstallationGuide(
        [Description("auto (default), server, wasm or ssr (static server rendering).")] string? renderMode = null,
        [Description("precompiled (default, no Tailwind in the app) or tailwind (Tailwind v4 compiled by your app).")] string? css = null) =>
        InstallationGuide.Render(renderMode, css, docs.Bundle.Version);

    private BundleComponent Resolve(string? name)
    {
        var component = docs.FindComponent(name);
        if (component is not null) return component;
        var close = docs.CloseComponents(name ?? "");
        throw new McpException($"No component named '{name}'." + (close.Count == 0
            ? " Call `search-components` to see them all."
            : $" Did you mean: {string.Join(", ", close.Select(c => c.Name))}? (`search-components` finds components by intent.)"));
    }

    private static IReadOnlyList<string> ParseSections(string? sections)
    {
        if (string.IsNullOrWhiteSpace(sections)) return ["usage", "examples", "api"];
        var wanted = ToolFilter.Parse(sections)!.Select(s => s.ToLowerInvariant()).Distinct().ToList();
        var unknown = wanted.Where(s => !ComponentMarkdown.Sections.Contains(s)).ToList();
        if (unknown.Count > 0)
        {
            throw new McpException($"Unknown section(s): {string.Join(", ", unknown)}. Use: {string.Join(", ", ComponentMarkdown.Sections)}.");
        }
        return [.. ComponentMarkdown.Sections.Where(wanted.Contains)];
    }

    private string Line(BundleComponent c) => $"- **{c.Name}** (`{c.Slug}`, {c.Group}): {c.Description}{(c.InstallCommand() is { } install ? $" Package {c.Package}: `{install}`." : "")} {ComponentMarkdown.Link(c, docs.SiteUrl)}\n";
}
