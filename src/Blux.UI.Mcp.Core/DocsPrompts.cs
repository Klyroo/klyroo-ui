using System.ComponentModel;
using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Blux.UI.Mcp;

/// <summary>Prompts that start the two most common jobs: choosing a component and writing one with the right parameters.</summary>
[McpServerPromptType]
public sealed class DocsPrompts(DocsService docs)
{
    [McpServerPrompt(Name = "find-component-for-usecase", Title = "Find the component for a use case")]
    [Description("Finds the Blux UI component that fits a use case and explains how to use it.")]
    public string FindComponentForUsecase([Description("What the user is trying to build, in their own words.")] string useCase)
    {
        var hits = docs.SearchComponents(useCase, null, 5);
        var sb = new StringBuilder();
        sb.Append($"Find the Blux UI component that best fits this need: {useCase}\n\n");
        if (hits.Count > 0)
        {
            sb.Append("Candidates from a first search, best first:\n");
            foreach (var hit in hits) sb.Append($"- {hit.Item.Name} (`{hit.Item.Slug}`): {hit.Item.Description}\n");
            sb.Append('\n');
        }
        sb.Append("Steps:\n");
        sb.Append("1. If none of the candidates fits, call `search-components` with other words.\n");
        sb.Append("2. Call `get-component` for the best one (sections usage, api) and compare it with the need; mention a close alternative when there is one.\n");
        sb.Append("3. Answer with the component, why it fits, and a short Razor snippet that uses only parameters returned by `get-component`.\n");
        sb.Append("4. Remember: UI state (open, focus, selection) runs in the browser; bind with `@bind-Value` or `@bind-Open` only when .NET needs the state.\n");
        return sb.ToString();
    }

    [McpServerPrompt(Name = "implement-component-with-props", Title = "Implement a component with its parameters")]
    [Description("Writes the Razor markup of a Blux UI component for a stated requirement, using only parameters that exist.")]
    public string ImplementComponentWithProps(
        [Description("The component name or slug, such as Button or dropdown-menu.")] string component,
        [Description("What the component must do or show: labels, variants, bindings, handlers.")] string requirements)
    {
        var found = docs.FindComponent(component)
            ?? throw new McpException($"No component named '{component}'. Did you mean: {string.Join(", ", docs.CloseComponents(component).Select(c => c.Name))}?");
        var sb = new StringBuilder();
        sb.Append($"Write the Razor markup for {found.Name} that satisfies: {requirements}\n\n");
        sb.Append("Its API, as JSON (compact: Name: type = default):\n\n");
        sb.Append(ComponentMarkdown.Fence(ComponentMarkdown.Metadata(found, false), "json"));
        sb.Append("\nRules:\n");
        sb.Append("- Use only the parameters, events and slots above. Call `get-component` with sections api or examples when you need descriptions or a pattern to copy.\n");
        sb.Append("- Pass a parameter of a bindable component as `@bind-Name` when the value must flow back to .NET.\n");
        sb.Append("- Style with `Class` and `Ui`; do not invent parameters for styling. Call `get-component` with section theme to see the slots.\n");
        sb.Append("- Components work in every render mode; a `BxButton` with `OnClick` is disabled until the page is interactive, so use `Href` or a submit button when the page may be static.\n");
        sb.Append("- Return the markup first, then a line for each assumption you made.\n");
        return sb.ToString();
    }
}
