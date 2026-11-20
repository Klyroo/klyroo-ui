using System.Net;
using System.Threading.RateLimiting;
using Blux.UI.Mcp;
using Microsoft.AspNetCore.HttpOverrides;
using ModelContextProtocol.AspNetCore;

namespace Blux.Playground;

/// <summary>
/// The AI tooling of the docs site: the MCP server at <c>/mcp</c> and the <c>llms.txt</c> files. All of it is generated from the
/// docs bundle embedded in Blux.UI.Mcp.Core, the same one the <c>blux-ui-mcp</c> dotnet tool serves over stdio.
/// <para>
/// Configuration: <c>Blux:SiteUrl</c> (public address, e.g. <c>https://ui.example.com</c>; links in llms.txt and tool answers use it instead of the
/// request's Host header), <c>Blux:Ai:RequestsPerMinute</c> (per client IP on /mcp, default 120), <c>Blux:Ai:RequestTimeoutSeconds</c> (default 30)
/// and <c>Blux:KnownProxies</c> (IP addresses of reverse proxies whose X-Forwarded-* headers are trusted, besides loopback).
/// </para>
/// </summary>
public static class AiEndpoints
{
    private const string PolicyName = "blux-mcp";

    public static IServiceCollection AddBluxAi(this IServiceCollection services, IConfiguration configuration)
    {
        // Validates the address (an invalid one stops the start) and decides what the links of llms.txt are made of.
        var pages = new LlmsPages(Blux.UI.Mcp.DocsBundleLoader.Embedded, configuration["Blux:SiteUrl"]);
        services.AddSingleton(pages);

        var perMinute = Math.Max(1, configuration.GetValue("Blux:Ai:RequestsPerMinute", 120));
        var timeout = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("Blux:Ai:RequestTimeoutSeconds", 30)));
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(PolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = perMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        services.AddRequestTimeouts(options => options.AddPolicy(PolicyName, timeout));

        // Behind a TLS-terminating proxy the app sees http and the proxy's address: read the original scheme, host and client IP, but only
        // from loopback and the proxies listed in Blux:KnownProxies, so a client cannot forge them.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            foreach (var proxy in configuration.GetSection("Blux:KnownProxies").Get<string[]>() ?? [])
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        });

        services.AddBluxDocsServer(o => o.SiteUrl = pages.SiteUrl).WithHttpTransport(http =>
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

    /// <summary>Forwarded headers, rate limiting and request timeouts; call early in the pipeline, before <see cref="MapBluxAi"/>.</summary>
    public static WebApplication UseBluxAi(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseRateLimiter();
        app.UseRequestTimeouts();
        return app;
    }

    public static WebApplication MapBluxAi(this WebApplication app)
    {
        app.MapMcp("/mcp").RequireRateLimiting(PolicyName).WithRequestTimeout(PolicyName);
        app.MapGet("/llms.txt", (HttpRequest request, LlmsPages pages) =>
            Results.Text(pages.Index(Origin(request)), "text/plain; charset=utf-8"));
        app.MapGet("/llms-full.txt", (HttpRequest request, LlmsPages pages) =>
            Results.Text(pages.Full(Origin(request)), "text/plain; charset=utf-8"));
        return app;
    }

    private static string Origin(HttpRequest request) => $"{request.Scheme}://{request.Host}";
}
