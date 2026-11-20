using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Blux.UI.Mcp;

/// <summary>Read-only listings of the documentation, for clients that browse resources instead of calling tools.</summary>
[McpServerResourceType]
public sealed class DocsResources(DocsService docs)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    [McpServerResource(UriTemplate = "resource://blux-ui/components", Name = "components", Title = "Components", MimeType = "application/json")]
    [Description("Every component: slug, name, family, description and docs path.")]
    public string Components() => JsonSerializer.Serialize(
        docs.Components.Select(c => new { c.Slug, c.Name, c.Group, c.Description, path = $"/components/{c.Slug}" }), Json);

    [McpServerResource(UriTemplate = "resource://blux-ui/examples", Name = "examples", Title = "Examples", MimeType = "application/json")]
    [Description("Every example of the docs site: id, title, component and description. Read the source with the get-example tool.")]
    public string Examples() => JsonSerializer.Serialize(
        docs.AllExamples.Select(i => new { i.Example.Id, i.Example.Title, component = i.Component.Slug, i.Example.Description }), Json);

    [McpServerResource(UriTemplate = "resource://blux-ui/documentation-pages", Name = "documentation-pages", Title = "Documentation pages", MimeType = "application/json")]
    [Description("The guides and the component pages of the docs site, with their paths.")]
    public string DocumentationPages() => JsonSerializer.Serialize(
        docs.Bundle.Guides.Select(g => new { path = g.Path, title = g.Title, section = g.Section })
            .Concat(docs.Components.Select(c => new { path = $"/components/{c.Slug}", title = c.Name, section = c.Group })), Json);

    [McpServerResource(UriTemplate = "resource://blux-ui/components/{slug}", Name = "component", Title = "Component page", MimeType = "text/markdown")]
    [Description("The page of one component (usage, examples, API) as Markdown.")]
    public string Component([Description("The slug of the component, such as button or dropdown-menu.")] string slug)
    {
        var component = docs.FindComponent(slug)
            ?? throw new McpException($"No component '{slug}'. Read resource://blux-ui/components for the list.");
        return ComponentMarkdown.Page(component, ["usage", "examples", "api"], docs.SiteUrl);
    }
}
