using System.Globalization;
using Klyroo.Maui.Services;
using Klyroo.Maui.Shared;
using Klyroo.Maui.Shared.Services;
using Klyroo.UI;
using Microsoft.Extensions.Logging;

namespace Klyroo.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        ApplyStoredCulture();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Device specific services used by the shared project.
        builder.Services.AddSingleton<IFormFactor, FormFactor>();
        builder.Services.AddSingleton<ISystemBars, SystemBars>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSampleKlyroo();
        // Where KlyLocaleSelect keeps the language the user picks. The default is the web view's localStorage; Preferences is read
        // here, before the first page, so the app starts in the right language without a flash.
        builder.Services.AddSingleton<IKlyCultureStore, PreferencesCultureStore>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// A .NET process takes its culture from the device. The language the user picked in KlyLocaleSelect is read here, before anything
    /// renders, and becomes the default culture of every thread. KlyApp (ApplyStoredCulture="false" in the layout would turn its own
    /// lookup off) then finds the culture already in place.
    /// </summary>
    private static void ApplyStoredCulture()
    {
        if (Preferences.Default.Get<string?>(PreferencesCultureStore.Key, null) is not { Length: > 0 } stored)
        {
            return;
        }
        try
        {
            var culture = CultureInfo.GetCultureInfo(stored);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch (CultureNotFoundException)
        {
            // A value this device does not know: keep the culture of the device.
        }
    }
}
