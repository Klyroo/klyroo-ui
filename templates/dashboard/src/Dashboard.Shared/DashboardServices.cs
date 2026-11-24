using Lux.UI;
using Lux.Table;
using Lux.Query;
using Dashboard.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dashboard.Shared;

public static class DashboardServices
{
    /// <summary>
    /// The services of the app. Every host calls it (the web server, the WebAssembly client and the MAUI app), so a page behaves the same
    /// wherever it runs. Blux is configured here: <c>o.Colors.Neutral = BxPalette.Zinc</c>, the locales, the default size of the components.
    /// </summary>
    public static IServiceCollection AddDashboardUi(this IServiceCollection services)
    {
        // Lux.UI, Lux.Query, and Lux.Table. The primary color is the one of the appearance dialog (AppearanceState), not an option here.
        services.AddLuxUi();
        services.AddLuxQuery();
        services.AddLuxTable();
        // A host with system bars (the MAUI app) registers its own before calling this; the others get one that does nothing.
        services.TryAddSingleton<ISystemBars, NoSystemBars>();

        // The sample data lives in services of the user session. Replace them with the calls to your API: the pages only use their members.
        // Where the color choice is kept: a cookie, which the server reads while it renders. A host with no request (the MAUI app) registers its own before calling this.
        services.TryAddScoped<IAppearanceStore, CookieAppearanceStore>();
        services.AddScoped<AppState>();
        services.AddScoped<AppearanceState>();
        services.AddScoped<CustomerStore>();
        services.AddScoped<MailStore>();
        services.AddScoped<NotificationStore>();
        services.AddScoped<MemberStore>();
        return services;
    }
}
