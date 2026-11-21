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
/// once every demo answers (503 before, or when one cannot be started): the E2E suite waits on it.
/// </para>
/// </summary>
public static class TemplateDemos
{
    public static IServiceCollection AddTemplateDemos(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Blux:Templates");
        services.Configure<TemplateDemoOptions>(section);
        var options = section.Get<TemplateDemoOptions>() ?? new TemplateDemoOptions();
        services.AddSingleton(new TemplateSettings(options.RepositoryBase));
        if (!options.Enabled) return services;

        var names = TemplateRegistry.All.Where(t => t.HasDemo).Select(t => t.Name).ToArray();
        var provider = new TemplateProxyConfigProvider(names, configuration["PathBase"]);
        services.AddSingleton(provider);
        services.AddSingleton<IProxyConfigProvider>(provider);
        services.AddReverseProxy();
        services.AddSingleton<TemplateDemoSupervisor>();
        services.AddHostedService(sp => sp.GetRequiredService<TemplateDemoSupervisor>());
        return services;
    }

    /// <summary>
    /// Turns the status code pages off for the demos (a demo answers its own 404, the docs site must not re-execute to its not-found page). Call right
    /// after <c>UseStatusCodePagesWithReExecute</c>: the middleware of the status code pages is the one that reads the switch.
    /// </summary>
    public static IApplicationBuilder UseTemplateDemoResponsesAsIs(this IApplicationBuilder app)
    {
        var names = TemplateRegistry.All.Where(t => t.HasDemo).Select(t => t.Name).ToArray();
        return app.Use((context, next) =>
        {
            if (TemplateLaunch.IsDemoPath(context.Request.Path.Value ?? "", names) && context.Features.Get<IStatusCodePagesFeature>() is { } feature)
            {
                feature.Enabled = false;
            }
            return next(context);
        });
    }

    public static WebApplication MapTemplateDemos(this WebApplication app)
    {
        var supervisor = app.Services.GetService<TemplateDemoSupervisor>();
        if (supervisor is null) return app;
        app.MapReverseProxy();
        app.MapGet("/_blux/templates/status", () => Results.Json(supervisor.Statuses));
        app.MapGet("/_blux/templates/ready", () =>
            Results.Json(supervisor.Statuses, statusCode: supervisor.AllReady ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable));
        return app;
    }
}
