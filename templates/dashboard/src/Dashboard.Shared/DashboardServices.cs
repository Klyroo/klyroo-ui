using Blux.UI;
using Blux.UI.Tools;
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
        // Blux.UI and the data list. The primary color is the one of the appearance dialog (AppearanceState), not an option here.
        services.AddBluxUiTools();
        // A host with system bars (the MAUI app) registers its own before calling this; the others get one that does nothing.
        services.TryAddSingleton<ISystemBars, NoSystemBars>();

        // The sample data lives in services of the user session. Replace them with the calls to your API: the pages only use their members.
        services.AddScoped<AppState>();
        services.AddScoped<AppearanceState>();
        services.AddScoped<CustomerStore>();
        services.AddScoped<MailStore>();
        services.AddScoped<NotificationStore>();
        services.AddScoped<MemberStore>();
        return services;
    }
}
