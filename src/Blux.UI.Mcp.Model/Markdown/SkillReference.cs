using System.Text;
using Blux.UI.Mcp.Model;

namespace Blux.UI.Mcp.Markdown;

/// <summary>
/// The part of the agent skill that is generated: a compact reference of every component. The same function feeds
/// <c>skills/blux-ui/references/components.md</c> (written by the bundle generator) and the test that keeps it current.
/// This file is also compiled into the generator, so it depends on the bundle model only.
/// </summary>
public static class SkillReference
{
    public const string RelativePath = "skills/blux-ui/references/components.md";

    public static string Components(DocsBundle bundle)
    {
        var sb = new StringBuilder();
        sb.Append("# Components quick reference\n\n");
        sb.Append("<!-- Generated from the Blux UI docs bundle by build/Blux.UI.DocsBundle. Do not edit: run\n");
        sb.Append("     dotnet run --project build/Blux.UI.DocsBundle -- --skill-reference  to refresh it. -->\n\n");
        sb.Append($"{bundle.Components.Count} components of Blux.UI {bundle.Version}, by family. Every parameter listed exists in the library; ");
        sb.Append("`Class` and `Ui` are accepted by every styled component. Parameters marked `@bind` take `@bind-Name`. ");
        sb.Append("For types, defaults, descriptions and examples, call the `get-component` and `get-component-metadata` tools of the MCP server ");
        sb.Append("or open `/components/{slug}` on the docs site.\n");

        foreach (var group in bundle.Groups)
        {
            sb.Append($"\n## {group}\n");
            foreach (var component in bundle.Components.Where(c => c.Group == group))
            {
                sb.Append($"\n### {component.Name}\n\n");
                sb.Append($"`/components/{component.Slug}`. {component.Description}\n");
                foreach (var api in component.Api)
                {
                    sb.Append($"\n- `<{TagName(api.Name)}>`");
                    if (api.Parameters.Count > 0) sb.Append("\n  - Parameters: " + string.Join(", ", api.Parameters.Select(Format)));
                    if (api.Events.Count > 0) sb.Append("\n  - Events: " + string.Join(", ", api.Events.Select(e => $"`{e.Name}`")));
                    if (api.Slots.Count > 0) sb.Append("\n  - Slots: " + string.Join(", ", api.Slots.Select(s => $"`{s.Name}`")));
                    if (api.Ui is { Slots.Count: > 0 } ui) sb.Append($"\n  - `Ui` (`{ui.Type}`): " + string.Join(", ", ui.Slots.Select(s => $"`{s.Name}`")));
                }
                sb.Append('\n');
            }
        }
        return sb.ToString();
    }

    private static string Format(BundleParameter p)
    {
        var text = $"`{p.Name}`";
        if (p.Bindable) text += " (@bind)";
        else if (p.Required) text += " (required)";
        return text;
    }

    /// <summary><c>BxTable&lt;TItem&gt;</c> is used as <c>BxTable</c> in Razor, which infers the type argument.</summary>
    private static string TagName(string name)
    {
        var generic = name.IndexOf('<');
        return generic < 0 ? name : name[..generic];
    }
}
