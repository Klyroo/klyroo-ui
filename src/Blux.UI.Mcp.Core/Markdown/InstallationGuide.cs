using System.Text;
using ModelContextProtocol;

namespace Blux.UI.Mcp;

/// <summary>
/// Setup instructions for one render mode and one CSS mode. The statements come from the README and the Installation and
/// Render modes guides of the docs site; a test keeps the snippets in step with the Installation guide.
/// </summary>
public static class InstallationGuide
{
    public static readonly string[] RenderModes = ["auto", "server", "wasm", "ssr"];
    public static readonly string[] CssModes = ["precompiled", "tailwind"];

    internal const string ServerSetup = "builder.Services.AddBluxUi();";

    internal const string StylesheetHead = """
        <head>
            <BxColorModeScript />
            <link rel="stylesheet" href="@Assets["_content/Blux.UI/blux.preflight.css"]" />
            <link rel="stylesheet" href="@Assets["_content/Blux.UI/blux.css"]" />
        </head>
        """;

    internal const string TailwindImport = """
        @import "tailwindcss";
        @import "../obj/blux/blux.theme.css";
        """;

    public static string Render(string? renderMode, string? cssMode, string version)
    {
        var mode = (renderMode ?? "auto").Trim().ToLowerInvariant();
        var css = (cssMode ?? "precompiled").Trim().ToLowerInvariant();
        if (!RenderModes.Contains(mode))
        {
            throw new McpException($"Unknown render mode '{renderMode}'. Use one of: {string.Join(", ", RenderModes)}.");
        }
        if (!CssModes.Contains(css))
        {
            throw new McpException($"Unknown CSS mode '{cssMode}'. Use one of: {string.Join(", ", CssModes)}.");
        }

        var clientToo = mode is "wasm" or "auto";
        var sb = new StringBuilder();
        sb.Append($"# Install Blux UI {version} ({ModeName(mode)}, {(css == "tailwind" ? "Tailwind compiled by your app" : "precompiled stylesheet")})\n\n");
        sb.Append("Requires .NET 10. Blux UI works in Interactive Server, WebAssembly, Auto and static server rendering with the same markup.\n\n");

        sb.Append("## 1. Add the package\n\n");
        sb.Append(ComponentMarkdown.Fence("dotnet add package Blux.UI --prerelease", "bash"));

        sb.Append("\n## 2. Register the services\n\n");
        if (clientToo)
        {
            sb.Append("Components render on the server first (prerendering) and then in the browser, so register Blux UI in the server project and in the `.Client` project, with the same configuration. ");
            sb.Append("A shared method in the client project keeps both in sync, otherwise components change appearance when WebAssembly takes over.\n\n");
            sb.Append(ComponentMarkdown.Fence("""
                // Client project: BluxSetup.cs
                public static class BluxSetup
                {
                    public static IServiceCollection AddAppBlux(this IServiceCollection services) =>
                        services.AddBluxUi(options =>
                        {
                            // options.Button.Slot("base", "font-semibold");
                        });
                }

                // Program.cs of the server project AND of the client project
                builder.Services.AddAppBlux();
                """, "csharp"));
        }
        else
        {
            sb.Append("Call `AddBluxUi` in the server `Program.cs`. It takes an optional callback to configure themes; configure them here, before the first render, because a theme becomes read-only once a component has used it.\n\n");
            sb.Append(ComponentMarkdown.Fence(ServerSetup, "csharp"));
        }

        sb.Append("\n## 3. Load the styles\n\n");
        if (css == "precompiled")
        {
            sb.Append("Without Tailwind in your app, load the precompiled stylesheet and its reset in `App.razor`. `BxColorModeScript` applies the saved light or dark mode before first paint.\n\n");
            sb.Append(ComponentMarkdown.Fence(StylesheetHead));
            sb.Append("\n`Class` and `Ui` then only apply classes that already exist in `blux.css` or in your own CSS.\n");
        }
        else
        {
            sb.Append("Every build copies the Blux theme (tokens, dark variant, the classes components use) to `obj/blux/blux.theme.css`. Import it after Tailwind and load only your compiled stylesheet, not `blux.css` nor `blux.preflight.css`. ");
            sb.Append("Keep `<BxColorModeScript />` in `<head>`. The path in the import is relative to the CSS file.\n\n");
            sb.Append(ComponentMarkdown.Fence(TailwindImport, "css"));
            sb.Append("\n`Class` and `Ui` then accept any utility, because your Tailwind build sees them in your Razor files.\n");
        }
        sb.Append("\nIf the app still loads Bootstrap from the Blazor template, remove it or import it in a layer (`@import url(bootstrap.css) layer(bootstrap);`): CSS outside layers overrides Tailwind v4's layered CSS.\n");

        sb.Append("\n## 4. Use the components\n\n");
        sb.Append(ComponentMarkdown.Fence("@* _Imports.razor *@\n@using Blux.UI", "razor"));
        sb.Append(ComponentMarkdown.Fence("""
            <BxButton Label="Save" OnClick="Save" />
            <BxSwitch @bind-Value="notify" Label="Email notifications" />
            """));

        sb.Append("\n## Render mode notes\n\n");
        sb.Append(mode switch
        {
            "ssr" =>
                "- Static server rendering has no interactivity: forms still post (Switch, Checkbox, RadioGroup, Input and Select render real form fields), and overlays, tabs and the accordion keep working because their behavior lives in the browser.\n" +
                "- A `BxButton` with `OnClick` renders disabled, because its handler cannot run. Use `Href` or a submit button instead.\n",
            "server" =>
                "- Opening, closing, focus and keyboard navigation run in the browser, so a menu does not wait for a SignalR round trip. .NET only hears about state you bind (`@bind-Value`, `@bind-Open`).\n" +
                "- A `BxButton` with `OnClick` is disabled during prerendering and becomes active once the circuit is interactive.\n",
            "wasm" =>
                "- Register Blux UI on the server too: prerendering runs there before WebAssembly loads.\n" +
                "- A `BxButton` with `OnClick` is disabled until the WebAssembly runtime starts.\n",
            _ =>
                "- Auto starts on the server and switches to WebAssembly later: register `AddBluxUi` with the same options in both projects.\n" +
                "- A `BxButton` with `OnClick` is disabled until the page is interactive.\n",
        });
        sb.Append("- An overlay without `@bind-Open` never calls .NET; bind it only when your code needs the state. A Popover trigger must contain a `<button>`.\n");
        return sb.ToString();
    }

    private static string ModeName(string mode) => mode switch
    {
        "ssr" => "static server rendering",
        "server" => "Interactive Server",
        "wasm" => "Interactive WebAssembly",
        _ => "Interactive Auto",
    };
}
