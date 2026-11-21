using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

[assembly: AssemblyFixture(typeof(Blux.UI.E2E.PlaygroundServer))]

namespace Blux.UI.E2E;

/// <summary>
/// Starts the playground once for the whole test run, and one browser chosen by BLUX_BROWSER
/// (chromium, firefox or webkit; chromium by default).
/// </summary>
public sealed class PlaygroundServer : IAsyncLifetime
{
    private Process? _process;
    private IPlaywright? _playwright;

    public string BaseUrl { get; private set; } = "";
    public IBrowser Browser { get; private set; } = default!;

    /// <summary>True when every live demo of the templates answered before the tests started. False only when the docs run published with no templates to run.</summary>
    public bool TemplatesReady { get; private set; }

    public async ValueTask InitializeAsync()
    {
        (_process, BaseUrl) = await StartPlaygroundAsync(null);
        TemplatesReady = await WaitForTemplateDemosAsync(BaseUrl);

        _playwright = await Playwright.CreateAsync();
        var browserType = (Environment.GetEnvironmentVariable("BLUX_BROWSER") ?? "chromium") switch
        {
            "firefox" => _playwright.Firefox,
            "webkit" => _playwright.Webkit,
            _ => _playwright.Chromium,
        };
        Browser = await browserType.LaunchAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (Browser is not null) await Browser.DisposeAsync();
        _playwright?.Dispose();
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }
        _process?.Dispose();
    }

    /// <summary>
    /// Starts one instance of the playground on a free port. <paramref name="pathBase"/> serves it below a prefix ("/sub", the
    /// <c>PathBase</c> setting of its Program.cs); the returned address is the root of the host, the prefix is not part of it.
    /// </summary>
    internal static async Task<(Process Process, string BaseUrl)> StartPlaygroundAsync(string? pathBase)
    {
        var port = FreePort();
        var baseUrl = $"http://127.0.0.1:{port}";
        // BLUX_PLAYGROUND_PUBLISHED points to a "dotnet publish" output: the trimmed WebAssembly build is what apps ship,
        // and trimming can remove members only reached through reflection.
        var published = Environment.GetEnvironmentVariable("BLUX_PLAYGROUND_PUBLISHED");
        ProcessStartInfo start;
        if (string.IsNullOrEmpty(published))
        {
            var project = Path.Combine(RepoRoot(), "playground", "Blux.Playground", "Blux.Playground");
            start = new ProcessStartInfo("dotnet", ["run", "--no-build", "--project", project, "--urls", baseUrl]);
            // Static web assets of referenced libraries are only served from their source folders in Development.
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        }
        else
        {
            var directory = Path.GetFullPath(published);
            start = new ProcessStartInfo("dotnet", [Path.Combine(directory, "Blux.Playground.dll"), "--urls", baseUrl])
            {
                WorkingDirectory = directory,
            };
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        }
        if (pathBase is not null)
        {
            start.Environment["PathBase"] = pathBase;
            // The instance below a path base is for the path base tests: it does not start its own copy of the template demos.
            start.Environment["Blux__Templates__Enabled"] = "false";
        }
        // BLUX_TEMPLATES_PUBLISHED: a folder with one "dotnet publish" output per template ({name}/), run in Production instead of the build output.
        if (Environment.GetEnvironmentVariable("BLUX_TEMPLATES_PUBLISHED") is { Length: > 0 } templates)
        {
            start.Environment["Blux__Templates__PublishRoot"] = Path.GetFullPath(templates);
        }
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the playground.");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await WaitUntilReady(process, baseUrl + (pathBase ?? ""));
        return (process, baseUrl);
    }

    /// <summary>Starts another instance of the playground below <paramref name="pathBase"/>; dispose it to stop it.</summary>
    public async Task<PlaygroundInstance> StartAtPathBaseAsync(string pathBase)
    {
        var (process, baseUrl) = await StartPlaygroundAsync(pathBase);
        return new PlaygroundInstance(process, baseUrl, pathBase);
    }

    /// <summary>
    /// Waits until the docs host reports every template demo as running (they are child processes it starts: the first request must not race them).
    /// A demo that cannot be started stops the run with what the host says, except when the docs run published without a folder of published
    /// templates, where there is nothing to start and the tests of the demos are skipped.
    /// </summary>
    private static async Task<bool> WaitForTemplateDemosAsync(string baseUrl)
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(240);
        var body = "";
        while (DateTime.UtcNow < deadline)
        {
            var response = await http.GetAsync(baseUrl + "/_blux/templates/ready");
            body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode) return true;
            // Not found: the demos are switched off. Unavailable: a demo that is neither built nor published, which no restart fixes.
            if (response.StatusCode == HttpStatusCode.NotFound) return false;
            if (!body.Contains("\"starting\"") && !body.Contains("\"restarting\"")) break;
            await Task.Delay(500);
        }
        var publishedDocs = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BLUX_PLAYGROUND_PUBLISHED"));
        if (publishedDocs && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BLUX_TEMPLATES_PUBLISHED"))) return false;
        throw new InvalidOperationException("The template demos did not start: " + body);
    }

    private static async Task WaitUntilReady(Process process, string url)
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"The playground exited with code {process.ExitCode}. Build it first.");
            }
            try
            {
                if ((await http.GetAsync(url)).IsSuccessStatusCode) return;
            }
            catch (HttpRequestException)
            {
                // Not listening yet.
            }
            await Task.Delay(250);
        }
        throw new TimeoutException("The playground did not answer within 60 seconds.");
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Blux.UI.slnx"))) return dir.FullName;
        }
        throw new DirectoryNotFoundException("Blux.UI.slnx not found above the test output directory.");
    }
}

/// <summary>A playground started by <see cref="PlaygroundServer.StartAtPathBaseAsync"/>.</summary>
public sealed class PlaygroundInstance(Process process, string host, string pathBase) : IAsyncDisposable
{
    /// <summary>The origin, without the path base.</summary>
    public string Host { get; } = host;

    /// <summary>The path base, "/sub".</summary>
    public string PathBase { get; } = pathBase;

    /// <summary>The address of the app: the origin and the path base, with a trailing slash like <c>&lt;base href&gt;</c>.</summary>
    public string BaseUrl => Host + PathBase.TrimEnd('/') + "/";

    public async ValueTask DisposeAsync()
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
        process.Dispose();
    }
}
