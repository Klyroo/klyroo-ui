using System.Text;
using Blux.UI.Mcp.Model;

namespace Blux.UI.Mcp;

/// <summary>
/// <c>llms.txt</c> (an overview with links, for an agent to decide what to read) and <c>llms-full.txt</c> (the documentation
/// in one file), both generated from the docs bundle. Follows https://llmstxt.org: an H1 title, a blockquote summary,
/// then H2 sections of Markdown links.
/// </summary>
public static class LlmsText
{
    public const string Summary =
        "Accessible, themable Blazor components styled with Tailwind CSS v4. " +
        "Every component works in Interactive Server, WebAssembly, Auto and static server rendering: UI state (open, focus, keyboard navigation) runs in the browser, and .NET only hears about state you bind. " +
        "They also run in .NET MAUI Blazor Hybrid apps (BlazorWebView): Android is tested on an emulator, iOS 17 or later, Windows and macOS (Mac Catalyst) have CI build jobs that have not run yet and are untested on devices, Android TV is experimental, Tizen is not supported.";

    /// <param name="bundle">What to describe.</param>
    /// <param name="baseUrl">Origin of the docs site (<c>https://example.com</c>); empty gives site-relative links.</param>
    public static string Index(DocsBundle bundle, string baseUrl = "")
    {
        baseUrl = baseUrl.TrimEnd('/');
        var sb = new StringBuilder();
        sb.Append("# Blux UI\n\n");
        sb.Append($"> {Summary}\n\n");
        sb.Append($"{bundle.Components.Count} components in {bundle.Groups.Count} families, version {bundle.Version}. ");
        sb.Append("Components are prefixed `Bx` (`<BxButton>`), register them with `builder.Services.AddBluxUi()` in the server and, for WebAssembly and Auto, the client `Program.cs`. ");
        sb.Append("Bind state with `@bind-Value` or `@bind-Open`; style with `Class` (root) and `Ui` (per slot), globally through `AddBluxUi(o => o.Button.Slot(...))`. ");
        sb.Append($"For exact parameters use the MCP server at {baseUrl}/mcp (or the `blux-ui-mcp` dotnet tool) rather than guessing.\n");

        foreach (var section in bundle.Guides.Select(g => g.Section).Distinct())
        {
            sb.Append($"\n## {section}\n\n");
            foreach (var guide in bundle.Guides.Where(g => g.Section == section))
            {
                sb.Append($"- [{guide.Title}]({baseUrl}{guide.Path}): {Absolute(Lead(guide.Markdown), baseUrl)}\n");
            }
        }

        foreach (var group in bundle.Groups)
        {
            sb.Append($"\n## Components: {group}\n\n");
            foreach (var component in bundle.Components.Where(c => c.Group == group))
            {
                sb.Append($"- [{component.Name}]({baseUrl}/components/{component.Slug}): {component.Description}{(component.InstallCommand() is { } install ? $" Package {component.Package}: `{install}`." : "")}\n");
            }
        }

        sb.Append("\n## Optional\n\n");
        sb.Append($"- [llms-full.txt]({baseUrl}/llms-full.txt): the guides and every component with its examples and API, in one file\n");
        return sb.ToString();
    }

    /// <inheritdoc cref="Index"/>
    public static string Full(DocsBundle bundle, string baseUrl = "")
    {
        baseUrl = baseUrl.TrimEnd('/');
        var sb = new StringBuilder();
        sb.Append("# Blux UI\n\n");
        sb.Append($"> {Summary}\n\n");
        sb.Append($"Version {bundle.Version}, {bundle.Components.Count} components. This file holds the guides, then every component with its description, examples (Razor source) and API. ");
        sb.Append($"The shorter overview is at {baseUrl}/llms.txt.\n");

        foreach (var section in bundle.Guides.Select(g => g.Section).Distinct())
        {
            sb.Append($"\n# {section}\n");
            foreach (var guide in bundle.Guides.Where(g => g.Section == section))
            {
                sb.Append($"\n## {guide.Title}\n\nSource: {baseUrl}{guide.Path}\n\n");
                sb.Append(Absolute(ShiftHeadings(StripTitle(guide.Markdown), 1), baseUrl).TrimEnd()).Append('\n');
            }
        }

        foreach (var group in bundle.Groups)
        {
            sb.Append($"\n# Components: {group}\n");
            foreach (var component in bundle.Components.Where(c => c.Group == group))
            {
                sb.Append($"\n## {component.Name}\n\nSource: {baseUrl}/components/{component.Slug}\n\n");
                var page = ComponentMarkdown.Page(component, ["usage", "examples", "api"], string.IsNullOrEmpty(baseUrl) ? null : baseUrl);
                sb.Append(ShiftHeadings(StripTitle(page), 1).TrimEnd()).Append('\n');
            }
        }
        return sb.ToString();
    }

    /// <summary>Makes the site-relative Markdown links of a guide (<c>[x](/docs/...)</c>) absolute, so they work outside the site.</summary>
    internal static string Absolute(string markdown, string baseUrl) =>
        baseUrl.Length == 0 ? markdown : System.Text.RegularExpressions.Regex.Replace(markdown, @"\]\((/[^)\s]*)\)", m => $"]({baseUrl}{m.Groups[1].Value})");

    /// <summary>The first paragraph of a guide after its title: what it is for.</summary>
    internal static string Lead(string markdown)
    {
        foreach (var block in markdown.Replace("\r\n", "\n").Split("\n\n"))
        {
            var text = block.Trim();
            if (text.Length > 0 && !text.StartsWith('#') && !text.StartsWith("```") && !text.StartsWith("**")) return text.Replace('\n', ' ');
        }
        return "";
    }

    private static string StripTitle(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n').ToList();
        if (lines.Count > 0 && lines[0].StartsWith("# ", StringComparison.Ordinal)) lines.RemoveAt(0);
        return string.Join('\n', lines).TrimStart('\n');
    }

    /// <summary>Moves every heading down by <paramref name="levels"/>, leaving code fences alone.</summary>
    internal static string ShiftHeadings(string markdown, int levels)
    {
        var sb = new StringBuilder();
        var fenced = false;
        foreach (var line in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.StartsWith("```", StringComparison.Ordinal)) fenced = !fenced;
            sb.Append(!fenced && line.StartsWith('#') && line.TrimStart('#').StartsWith(' ') ? new string('#', levels) + line : line).Append('\n');
        }
        return sb.ToString();
    }
}
