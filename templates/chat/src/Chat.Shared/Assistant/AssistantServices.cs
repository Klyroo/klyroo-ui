using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Chat.Shared.Assistant;

/// <summary>Registers the provider of the replies. Each host picks its own: the pages only know <see cref="ICompletionProvider"/>.</summary>
public static class AssistantServices
{
    /// <summary>
    /// A provider that runs in this process: the web host (for the server render mode and for the endpoint the WebAssembly half calls) and the MAUI app. It is
    /// the configured one, <see cref="SimulatedCompletionProvider"/> or <see cref="OpenAiCompletionProvider"/>, behind an <see cref="AssistantGuard"/> that
    /// refuses oversized requests, unknown models, too many requests and too many replies at once.
    /// </summary>
    /// <param name="options">The settings. The key and the address stay in this process.</param>
    /// <param name="http">The client for the service. Required for <c>OpenAi</c>; give it one that does not follow redirects and has no timeout of its own (the provider sets it).</param>
    public static IServiceCollection AddAssistantProvider(this IServiceCollection services, AssistantOptions options, HttpClient? http = null)
    {
        var guard = new AssistantGuard(options);
        services.AddSingleton(options);
        services.AddSingleton(guard);
        services.AddScoped<CallerKey>();
        // The real provider is made once, with the logger of the host: what the visitor reads is generic, and the hint for the operator goes to the log.
        services.AddSingleton(sp => new Inner(options.IsSimulated
            ? new SimulatedCompletionProvider(options)
            : new OpenAiCompletionProvider(http ?? throw new ArgumentNullException(nameof(http), "The OpenAi provider needs an HttpClient."), options, sp.GetService<ILoggerFactory>()?.CreateLogger<OpenAiCompletionProvider>())));
        services.AddScoped<ICompletionProvider>(sp => new GuardedCompletionProvider(sp.GetRequiredService<Inner>().Provider, guard, sp.GetRequiredService<CallerKey>(), sp.GetService<IAssistantAuthorizer>()));
        return services;
    }

    /// <summary>The provider of the WebAssembly half: it calls the web host at <paramref name="hostAddress"/> (the <c>&lt;base href&gt;</c> of the page).</summary>
    public static IServiceCollection AddRemoteAssistant(this IServiceCollection services, Uri hostAddress)
    {
        var provider = new RemoteCompletionProvider(new HttpClient { BaseAddress = hostAddress });
        services.AddSingleton<ICompletionProvider>(provider);
        return services;
    }

    private sealed record Inner(ICompletionProvider Provider);
}
