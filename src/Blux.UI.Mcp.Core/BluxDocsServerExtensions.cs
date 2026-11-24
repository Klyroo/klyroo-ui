using Blux.UI.Mcp.Model;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Blux.UI.Mcp;

/// <summary>Options of the Blux UI documentation server.</summary>
public sealed class BluxDocsOptions
{
    /// <summary>What to serve; defaults to the bundle embedded at build time.</summary>
    public DocsBundle? Bundle { get; set; }

    /// <summary>Base address of the docs site, put in front of page paths in answers. Null keeps paths relative.</summary>
    public string? SiteUrl { get; set; }

    /// <summary>Restrict the tools to these names for every client (stdio). The HTTP endpoint also reads the <c>X-MCP-Tools</c> header.</summary>
    public ISet<string>? Tools { get; set; }
}

public static class BluxDocsServerExtensions
{
    /// <summary>
    /// Registers the docs bundle, its tools, prompts and resources. Add a transport on the returned builder:
    /// <c>WithStdioServerTransport()</c> for the local tool, <c>WithHttpTransport()</c> for the docs site.
    /// </summary>
    public static IMcpServerBuilder AddBluxDocsServer(this IServiceCollection services, Action<BluxDocsOptions>? configure = null)
    {
        var options = new BluxDocsOptions();
        configure?.Invoke(options);
        var bundle = options.Bundle ?? DocsBundleLoader.Embedded;

        services.AddSingleton(new DocsService(bundle, options.SiteUrl));

        var builder = services.AddMcpServer(server =>
        {
            server.ServerInfo = new() { Name = "blux-ui", Version = bundle.Version };
            server.ServerInstructions =
                "Documentation of Blux UI, an accessible, themable Blazor component library styled with Tailwind CSS v4. " +
                "Start with search-components (by name, family or intent) and get-component (usage, examples, api, theme); " +
                "use get-component-metadata for the exact parameters before writing markup, and get-installation-guide for setup. " +
                "Never invent a parameter: every one returned by these tools exists in the library.";
        })
            .WithTools<DocsTools>()
            .WithPrompts<DocsPrompts>()
            .WithResources<DocsResources>();

        if (options.Tools is { } allowed)
        {
            services.PostConfigure<McpServerOptions>(o => ToolFilter.Apply(o, allowed));
        }
        return builder;
    }
}
