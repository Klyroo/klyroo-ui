using Lux.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Lux.Maui.Shared;

public static class LuxSetup
{
    /// <summary>
    /// The Lux configuration of the app. Both hosts call it (the MAUI app and the web app), so a screen looks the same everywhere.
    /// </summary>
    public static IServiceCollection AddSampleLux(this IServiceCollection services) =>
        services.AddLuxUi(o =>
        {
            o.Colors.Primary = LxPalette.Blue;
        });
}
