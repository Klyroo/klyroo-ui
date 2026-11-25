using System.Globalization;
using Microsoft.Extensions.Logging;
using Starter.Maui.Services;
using Starter.Shared;
using Starter.Shared.Services;
using Klyroo.UI;

namespace Starter.Maui;

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

        builder.Services.AddMauiBlazorWebView();
        // The system bars are the host's: registered before AddStarterUi, which would otherwise add the one that does nothing.
        builder.Services.AddSingleton<ISystemBars, SystemBars>();
        builder.Services.AddStarterUi();
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
    /// renders, and becomes the default culture of every thread.
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
