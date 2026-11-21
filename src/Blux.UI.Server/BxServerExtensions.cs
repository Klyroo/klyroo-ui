using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Blux.UI.Server;

/// <summary>Registration of the server side of Blux UI.</summary>
public static class BxServerExtensions
{
    /// <summary>
    /// Lets Blux components read the request they render for in a static render and in a prerender: it registers <c>IHttpContextAccessor</c> (<c>AddHttpContextAccessor</c>) and an
    /// <see cref="IBxRequestCookies"/> over it. Call it in the server <c>Program.cs</c> only: a WebAssembly client has no request, and Blux.UI then keeps its defaults (the data list
    /// cuts days in UTC until the browser is interactive). Safe to call more than once.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddBluxServer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IBxRequestCookies, BxRequestCookies>();
        return services;
    }
}
