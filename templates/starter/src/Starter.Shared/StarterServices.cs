using Klyroo.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Starter.Shared.Services;

namespace Starter.Shared;

public static class StarterServices
{
    /// <summary>
    /// The services of the app. Every host calls it (the web server, the WebAssembly client and the MAUI app), so a page behaves the same
    /// wherever it runs. Klyroo is configured here: <c>o.Colors.Primary = KlyPalette.Blue</c>, the locales, the default size of the components.
    /// </summary>
    public static IServiceCollection AddStarterUi(this IServiceCollection services)
    {
        services.AddKlyrooUi();
        // A host with system bars (the MAUI app) registers its own before calling this; the others get one that does nothing.
        services.TryAddSingleton<ISystemBars, NoSystemBars>();
        return services;
    }
}
