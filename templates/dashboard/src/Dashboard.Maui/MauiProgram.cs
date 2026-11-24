using System.Globalization;
using Microsoft.Extensions.Logging;
using Dashboard.Maui.Services;
using Dashboard.Shared;
using Dashboard.Shared.Services;
using Lux.UI;

namespace Dashboard.Maui;

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
        // The system bars are the host's: registered before AddDashboardUi, which would otherwise add the one that does nothing.
        builder.Services.AddSingleton<ISystemBars, SystemBars>();
        // No request in the app, so no cookie to read: the web view keeps the color choice in its local storage. Registered before AddDashboardUi, which would add the cookie one.
        builder.Services.AddScoped<IAppearanceStore, LocalStorageAppearanceStore>();
        builder.Services.AddDashboardUi();
        // Where LxLocaleSelect keeps the language the user picks. The default is the web view's localStorage; Preferences is read
        // here, before the first page, so the app starts in the right language without a flash.
        builder.Services.AddSingleton<ILxCultureStore, PreferencesCultureStore>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// A .NET process takes its culture from the device. The language the user picked in LxLocaleSelect is read here, before anything
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
