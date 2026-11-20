using Blux.UI;
using Blux.UI.Images;

namespace Blux.Playground.Client;

/// <summary>
/// Blux configuration shared by the server and the WebAssembly client, so components look the same
/// whichever runtime renders them.
/// </summary>
public static class PlaygroundServices
{
    public static IServiceCollection AddPlaygroundBlux(this IServiceCollection services) =>
        services.AddBluxUi(o =>
        {
            // The URLs of the image optimizer the server maps at /_blux/img (Blux.UI.Images.Local): the same on both runtimes.
            // Remote images (the avatars of the examples) are not in Images.Domains, so they are left untouched.
            o.Images.AddLocal(ImageEndpoint, makeDefault: true);
        });

    /// <summary>Where the server maps the image optimizer.</summary>
    public const string ImageEndpoint = "/_blux/img";

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
