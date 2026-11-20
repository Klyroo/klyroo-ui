using Blux.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.Maui.Shared;

public static class BluxSetup
{
    /// <summary>
    /// The Blux configuration of the app. Both hosts call it (the MAUI app and the web app), so a screen looks the same everywhere.
    /// </summary>
    public static IServiceCollection AddSampleBlux(this IServiceCollection services) =>
        services.AddBluxUi(o =>
        {
            o.Colors.Primary = BxPalette.Blue;
        });
}
