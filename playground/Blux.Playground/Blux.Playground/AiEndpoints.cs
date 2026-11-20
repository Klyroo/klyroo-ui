using Blux.UI.Mcp;
using ModelContextProtocol.AspNetCore;

namespace Blux.Playground;

/// <summary>
/// The AI tooling of the docs site: the MCP server at <c>/mcp</c> and the <c>llms.txt</c> files. All of it is generated from the
/// docs bundle embedded in Blux.UI.Mcp.Core, the same one the <c>blux-ui-mcp</c> dotnet tool serves over stdio.
/// </summary>
public static class AiEndpoints
{
    public static IServiceCollection AddBluxAi(this IServiceCollection services)
    {
        services.AddBluxDocsServer().WithHttpTransport(http =>
        {
            // Every request is independent (nothing to remember between calls), so no session affinity is needed.
            http.Stateless = true;
            // X-MCP-Tools restricts the tools a client sees, per request.
            http.ConfigureSessionOptions = (context, options, _) =>
            {
                ToolFilter.Apply(options, ToolFilter.Parse(context.Request.Headers[ToolFilter.HeaderName].ToArray()));
                return Task.CompletedTask;
            };
        });
        return services;
    }

    public static WebApplication MapBluxAi(this WebApplication app)
    {
        app.MapMcp("/mcp");
        app.MapGet("/llms.txt", (HttpRequest request, DocsService docs) =>
            Results.Text(LlmsText.Index(docs.Bundle, Origin(request)), "text/plain; charset=utf-8"));
        app.MapGet("/llms-full.txt", (HttpRequest request, DocsService docs) =>
            Results.Text(LlmsText.Full(docs.Bundle, Origin(request)), "text/plain; charset=utf-8"));
        return app;
    }

    private static string Origin(HttpRequest request) => $"{request.Scheme}://{request.Host}";
}
