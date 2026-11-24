using Lux.UI;
using Chat.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chat.Shared;

public static class ChatServices
{
    /// <summary>
    /// The services of the app. Every host calls it (the web server, the WebAssembly client and the MAUI app), so a page behaves the same
    /// wherever it runs. Blux is configured here: <c>o.Colors.Primary = BxPalette.Blue</c>, the locales, the default size of the components.
    /// Each host also registers the provider of the replies (<c>AddAssistantProvider</c> or <c>AddRemoteAssistant</c>, see <c>Assistant/AssistantServices.cs</c>).
    /// </summary>
    public static IServiceCollection AddChatUi(this IServiceCollection services)
    {
        services.AddLuxUi();
        // A host with system bars (the MAUI app) registers its own before calling this; the others get one that does nothing.
        services.TryAddSingleton<ISystemBars, NoSystemBars>();
        // Where the conversations are kept. The local storage of the browser by default: replace it to keep them somewhere else (see Services/ConversationStore.cs).
        services.TryAddScoped<IConversationStore, LocalStorageConversationStore>();
        services.AddScoped<ConversationState>();
        return services;
    }
}
