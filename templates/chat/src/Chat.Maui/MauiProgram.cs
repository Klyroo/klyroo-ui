using System.Globalization;
using Microsoft.Extensions.Logging;
using Chat.Maui.Services;
using Chat.Shared;
using Chat.Shared.Assistant;
using Chat.Shared.Services;
using Blux.UI;

namespace Chat.Maui;

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
        // The system bars are the host's: registered before AddChatUi, which would otherwise add the one that does nothing.
        builder.Services.AddSingleton<ISystemBars, SystemBars>();
        // The conversations are kept in the local storage of the web view, on the device (LocalStorageConversationStore, the default of AddChatUi).
        builder.Services.AddChatUi();
        // Who answers. The app ships with the simulated provider (canned replies, no network), so it runs with nothing configured. Choose one:
        //  - the web host of this solution, which holds the key (recommended: a key inside an app can be read by anyone who has the app):
        //      builder.Services.AddRemoteAssistant(new Uri("https://your-host.example/"));
        //  - the AI service directly, with a key that ships inside the app (only for an app that is yours alone, or a service that needs no key):
        //      builder.Services.AddAssistantProvider(new AssistantOptions { Provider = AssistantOptions.OpenAi, BaseUrl = "...", ApiKey = "...", Models = [new() { Id = "your-model-id" }] },
        //          new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan });
        builder.Services.AddAssistantProvider(new AssistantOptions { Provider = AssistantOptions.Simulated });
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
