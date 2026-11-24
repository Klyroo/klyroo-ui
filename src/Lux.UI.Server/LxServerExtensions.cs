using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lux.UI.Server;

/// <summary>Options of <see cref="LxServerExtensions.AddLuxServer(IServiceCollection, Action{LuxServerOptions}?)"/>.</summary>
public sealed class LuxServerOptions
{
    /// <summary>
    /// Marks the HTTP response <c>Cache-Control: private</c> (when no <c>private</c> or <c>no-store</c> directive is already set) and adds <c>Cookie</c> to <c>Vary</c>
    /// whenever a Lux component reads a request cookie during a static render or a prerender (<see langword="true"/> by default), so a shared cache or CDN does not serve
    /// one user's cookie-shaped HTML (time zone, column preferences) to another. Turn this off only when your own middleware sets the caching headers.
    /// </summary>
    public bool SetCacheHeaders { get; set; } = true;
}

/// <summary>Registration of the server side of Lux.UI.</summary>
public static class LxServerExtensions
{
    /// <summary>
    /// Lets Lux components read the request they render for in a static render and in a prerender: it registers <c>IHttpContextAccessor</c> (<c>AddHttpContextAccessor</c>) and an
    /// <see cref="ILxRequestCookies"/> over it. Call it in the server <c>Program.cs</c> only: a WebAssembly client has no request, and Lux.UI then keeps its defaults (the data list
    /// cuts days in UTC until the browser is interactive). Safe to call more than once.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddLuxServer(this IServiceCollection services) =>
        AddLuxServer(services, configure: null);

    /// <summary>
    /// Lets Lux components read the request they render for in a static render and in a prerender, and configures <see cref="LuxServerOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the server options.</param>
    public static IServiceCollection AddLuxServer(this IServiceCollection services, Action<LuxServerOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpContextAccessor();
        var options = services.FirstOrDefault(d => d.ServiceType == typeof(LuxServerOptions))?.ImplementationInstance as LuxServerOptions;
        if (options is null)
        {
            options = new LuxServerOptions();
            services.AddSingleton(options);
        }
        configure?.Invoke(options);
        services.TryAddSingleton<ILxRequestCookies, LxRequestCookies>();
        return services;
    }
}
