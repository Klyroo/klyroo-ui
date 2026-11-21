using System.Net;
using Blux.Playground.Client.Templates;
using Microsoft.AspNetCore.Diagnostics;
using Yarp.ReverseProxy.Configuration;

namespace Blux.Playground.Templates;

/// <summary>
/// The live demos of the templates, inside the docs deployment: <c>/templates/{name}/demo/</c> is proxied (YARP, WebSockets included) to a child
/// process of the docs host that <see cref="TemplateDemoSupervisor"/> keeps running. The demos are separate programs: the docs host never references a
/// template project, so their <c>_framework</c> and static assets cannot collide with its own.
/// <para>
/// Configuration under <c>Blux:Templates</c>, see <see cref="TemplateDemoOptions"/>; <c>Blux:Templates:RepositoryBase</c> is what the /templates page
/// links its "Source" buttons to. <c>GET /_blux/templates/status</c> lists the state of each demo and <c>GET /_blux/templates/ready</c> answers 200
/// once every demo that can be started answers (503 before). Both are answered to loopback clients only, and neither is a health check of the
/// site: <c>GET /_blux/health</c> is (it only says the host is up). The E2E suite waits on <c>ready</c>.
/// </para>
/// </summary>
public static class TemplateDemos
{
    /// <summary>
    /// Registers the demos of <paramref name="templates"/> (the registry when null). Nothing but the settings and a "no demos" state is registered when
    /// <c>Blux:Templates:Enabled</c> is false.
    /// </summary>
    public static IServiceCollection AddTemplateDemos(this IServiceCollection services, IConfiguration configuration, IReadOnlyList<TemplateInfo>? templates = null)
    {
        var section = configuration.GetSection("Blux:Templates");
        services.Configure<TemplateDemoOptions>(section);
        var options = section.Get<TemplateDemoOptions>() ?? new TemplateDemoOptions();
        services.AddSingleton(new TemplateSettings(options.RepositoryBase));
        if (!options.Enabled)
        {
            services.AddSingleton<ITemplateDemoState, NoTemplateDemos>();
            return services;
        }

        var catalog = new TemplateDemoCatalog(templates ?? [.. TemplateRegistry.All.Where(t => t.HasDemo)]);
        var provider = new TemplateProxyConfigProvider(catalog.Names, configuration["PathBase"]);
        services.AddSingleton(catalog);
        services.AddSingleton(provider);
        services.AddSingleton<IProxyConfigProvider>(provider);
        services.AddReverseProxy();
        services.AddSingleton<TemplateDemoSupervisor>();
        services.AddSingleton<ITemplateDemoState>(sp => sp.GetRequiredService<TemplateDemoSupervisor>());
        services.AddHostedService(sp => sp.GetRequiredService<TemplateDemoSupervisor>());
        return services;
    }

    /// <summary>
    /// Keeps the answers of the demos as they are. Call right after <c>UseStatusCodePagesWithReExecute</c>: the docs site must not re-execute to its
    /// not-found page for a 404 a demo sent, and when the proxy itself fails to reach a demo (it is restarting) the visitor gets a small page that says so
    /// instead of an empty response.
    /// </summary>
    public static IApplicationBuilder UseTemplateDemoResponsesAsIs(this IApplicationBuilder app)
    {
        var catalog = app.ApplicationServices.GetService<TemplateDemoCatalog>();
        if (catalog is null) return app;
        var names = catalog.Names.ToArray();
        return app.Use(async (context, next) =>
        {
            if (!TemplateLaunch.IsDemoPath(context.Request.Path.Value ?? "", names))
            {
                await next(context);
                return;
            }
            if (context.Features.Get<IStatusCodePagesFeature>() is { } feature) feature.Enabled = false;
            await next(context);
            // The proxy answers with a bare status when it cannot reach the demo; a demo's own 502 or 503 has a body and has already started the response.
            if (!context.Response.HasStarted && context.Response.StatusCode is 502 or 503 or 504 && context.Response.ContentLength is null or 0)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.Headers.RetryAfter = "5";
                context.Response.Headers.CacheControl = "no-store";
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(RestartingPage.Replace("{TEMPLATES}", System.Net.WebUtility.HtmlEncode(context.Request.PathBase + "/templates")));
            }
        });
    }

    private const string RestartingPage =
        "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
        + "<meta http-equiv=\"refresh\" content=\"5\"><title>Demo restarting</title>"
        + "<style>body{font:16px/1.5 system-ui,sans-serif;max-width:32rem;margin:20vh auto;padding:0 1rem;color:#222;background:#fff}"
        + "@media (prefers-color-scheme:dark){body{color:#eee;background:#111}}a{color:inherit}</style></head><body>"
        + "<main><h1>The demo is restarting</h1><p>This demo runs as its own app and is not answering at the moment. This page reloads every few seconds.</p>"
        + "<p><a href=\"{TEMPLATES}\">Back to the templates</a></p></main></body></html>";

    public static WebApplication MapTemplateDemos(this WebApplication app)
    {
        // The host answers its own health check whatever the demos do.
        app.MapGet("/_blux/health", () => Results.Text("ok"));
        var supervisor = app.Services.GetService<TemplateDemoSupervisor>();
        if (supervisor is null) return app;
        app.MapReverseProxy();
        app.MapGet("/_blux/templates/status", (HttpContext context) =>
            IsLoopback(context) ? Results.Json(supervisor.Statuses) : Results.NotFound());
        app.MapGet("/_blux/templates/ready", (HttpContext context) =>
            !IsLoopback(context) ? Results.NotFound()
            : Results.Json(supervisor.Statuses, statusCode: supervisor.AllReady ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable));
        return app;
    }

    /// <summary>
    /// The client address after the forwarded headers middleware (configured with the known proxies of the site): behind a trusted reverse proxy it
    /// is the visitor's, so the endpoints stay private to the machine even though every proxied request arrives from loopback.
    /// </summary>
    private static bool IsLoopback(HttpContext context) =>
        context.Connection.RemoteIpAddress is { } address && IPAddress.IsLoopback(address)
        && !context.Request.Headers.ContainsKey("X-Forwarded-For");

    private sealed class NoTemplateDemos : ITemplateDemoState
    {
        public string? StateOf(string name) => null;
    }
}
