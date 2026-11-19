using Blux.UI;

namespace Blux.Playground.Client;

/// <summary>
/// Blux configuration shared by the server and the WebAssembly client, so components look the same
/// whichever runtime renders them.
/// </summary>
public static class PlaygroundServices
{
    public static IServiceCollection AddPlaygroundBlux(this IServiceCollection services) =>
        services.AddBluxUi();

    /// <summary>Maps the first URL segment to a render mode; anything else renders statically.</summary>
    public static string? ModeFromPath(string path) =>
        path.TrimStart('/').Split('/', 2)[0] switch
        {
            "server" => "server",
            "wasm" => "wasm",
            "auto" => "auto",
            "ssr" => "ssr",
            _ => null,
        };

    public static readonly string[] Modes = ["ssr", "server", "wasm", "auto"];
}
