using Lux.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Lux.Maui.Shared;

public static class BluxSetup
{
    /// <summary>
    /// The Blux configuration of the app. Both hosts call it (the MAUI app and the web app), so a screen looks the same everywhere.
    /// </summary>
    public static IServiceCollection AddSampleBlux(this IServiceCollection services) =>
        services.AddLuxUi(o =>
        {
            o.Colors.Primary = BxPalette.Blue;
        });
}
