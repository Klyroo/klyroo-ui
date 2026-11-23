using Blux.UI;
using Editor.Shared.Data;
using Editor.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Editor.Shared;

public static class EditorServices
{
    /// <summary>
    /// The services of the app. Every host calls it (the web server, the WebAssembly client and the MAUI app), so a page behaves the same
    /// wherever it runs. Blux is configured here: <c>o.Colors.Primary = BxPalette.Blue</c>, the locales, the default size of the components.
    /// </summary>
    public static IServiceCollection AddEditorUi(this IServiceCollection services)
    {
        // The HTML of a document is untrusted (storage, paste): the read-only view the server draws before the editor is ready goes through the same allow-list as everything stored.
        services.AddBluxUi(options => options.SanitizeEditorHtml = DocumentHtml.Clean);
        // A host with system bars (the MAUI app) registers its own before calling this; the others get one that does nothing.
        services.TryAddSingleton<ISystemBars, NoSystemBars>();
        // Where the documents are kept. The local storage of the browser by default: replace it to keep them somewhere else (see Services/DocumentStore.cs).
        services.TryAddScoped<IDocumentStore, LocalStorageDocumentStore>();
        services.AddScoped<DocumentState>();
        return services;
    }
}
