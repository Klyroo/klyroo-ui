using Klyroo.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Klyroo.Maui.Shared;

public static class KlyrooSetup
{
    /// <summary>
    /// The Klyroo configuration of the app. Both hosts call it (the MAUI app and the web app), so a screen looks the same everywhere.
    /// </summary>
    public static IServiceCollection AddSampleKlyroo(this IServiceCollection services) =>
        services.AddKlyrooUi(o =>
        {
            o.Colors.Primary = KlyPalette.Blue;
        });
}
