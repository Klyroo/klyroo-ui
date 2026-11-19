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

    public async ValueTask InitializeAsync()
    {
        var port = FreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        // BLUX_PLAYGROUND_PUBLISHED points to a "dotnet publish" output: the trimmed WebAssembly build is what apps ship,
        // and trimming can remove members only reached through reflection.
        var published = Environment.GetEnvironmentVariable("BLUX_PLAYGROUND_PUBLISHED");
        ProcessStartInfo start;
        if (string.IsNullOrEmpty(published))
        {
            var project = Path.Combine(RepoRoot(), "playground", "Blux.Playground", "Blux.Playground");
            start = new ProcessStartInfo("dotnet", ["run", "--no-build", "--project", project, "--urls", BaseUrl]);
            // Static web assets of referenced libraries are only served from their source folders in Development.
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        }
        else
        {
            var directory = Path.GetFullPath(published);
            start = new ProcessStartInfo("dotnet", [Path.Combine(directory, "Blux.Playground.dll"), "--urls", BaseUrl])
            {
                WorkingDirectory = directory,
            };
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        }
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        _process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the playground.");
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        await WaitUntilReady();

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

    private async Task WaitUntilReady()
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (_process!.HasExited)
            {
                throw new InvalidOperationException($"The playground exited with code {_process.ExitCode}. Build it first.");
            }
            try
            {
                if ((await http.GetAsync(BaseUrl)).IsSuccessStatusCode) return;
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
