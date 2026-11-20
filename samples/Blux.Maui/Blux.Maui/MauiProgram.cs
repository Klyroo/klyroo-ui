using System.Globalization;
using Blux.Maui.Services;
using Blux.Maui.Shared;
using Blux.Maui.Shared.Services;
using Blux.UI;
using Microsoft.Extensions.Logging;

namespace Blux.Maui;

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

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSampleBlux();
        // Where BxLocaleSelect keeps the language the user picks. The default is the web view's localStorage; Preferences is read
        // here, before the first page, so the app starts in the right language without a flash.
        builder.Services.AddSingleton<IBxCultureStore, PreferencesCultureStore>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// A .NET process takes its culture from the device. The language the user picked in BxLocaleSelect is read here, before anything
    /// renders, and becomes the default culture of every thread. BxApp (ApplyStoredCulture="false" in the layout would turn its own
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
