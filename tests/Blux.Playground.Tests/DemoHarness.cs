using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Blux.Playground.Client.Templates;
using Blux.Playground.Templates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Blux.Playground.Tests;

/// <summary>
/// A docs-like host on a free port with the template demo machinery wired for one fake template, started from a copy of the output of
/// <c>Blux.UI.FakeDemo</c> in a temporary "publish root". Dispose it to stop the host and delete the folder.
/// </summary>
public sealed class DemoHarness : IAsyncDisposable
{
    public const string Name = "fake";

    private readonly string _root;
    private readonly WebApplication _app;

    public HttpClient Http { get; }
    public string BaseUrl { get; }
    public TemplateDemoSupervisor Supervisor { get; }

    /// <summary>The folder the fake demo runs from (a file named "unhealthy" in it makes the fake answer as something else).</summary>
    public string DemoFolder => Path.Combine(_root, Name);

    private DemoHarness(WebApplication app, string baseUrl, string root)
    {
        _root = root;
        _app = app;
        BaseUrl = baseUrl;
        Http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
        Supervisor = app.Services.GetRequiredService<TemplateDemoSupervisor>();
    }

    public static async Task<DemoHarness> StartAsync(double livenessSeconds = 1, bool waitReady = true, bool populate = true)
    {
        var root = Path.Combine(Path.GetTempPath(), "blux-demo-tests-" + Guid.NewGuid().ToString("N"));
        var fakeOutput = FakeDemoOutput();
        var port = FreePort();
        var url = $"http://127.0.0.1:{port}";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(new[] { url });
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Blux:Templates:PublishRoot"] = root,
            ["Blux:Templates:StartupTimeoutSeconds"] = "60",
            ["Blux:Templates:LivenessIntervalSeconds"] = livenessSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
        });
        var template = new TemplateInfo(Name, "Fake", "A fake demo.", [], TemplateStatus.Available, WebProject: "unused", WebAssembly: "FakeDemo");
        builder.Services.AddTemplateDemos(builder.Configuration, [template]);
        var app = builder.Build();
        app.UseTemplateDemoResponsesAsIs();
        app.MapTemplateDemos();

        if (populate) CopyDirectory(fakeOutput, Path.Combine(root, Name));
        var harness = new DemoHarness(app, url, root);
        await app.StartAsync();
        if (waitReady) await harness.WaitForStateAsync("ready");
        return harness;
    }

    public async Task WaitForStateAsync(string state, int? minRestarts = null, int seconds = 60)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < deadline)
        {
            var status = Supervisor.Statuses.Single();
            if (status.State == state && (minRestarts is null || status.Restarts >= minRestarts)) return;
            await Task.Delay(50);
        }
        throw new TimeoutException($"The demo did not reach '{state}' within {seconds} s; it is {Supervisor.Statuses.Single()}.");
    }

    public int Pid => Supervisor.ProcessIds[Name];

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        Http.Dispose();
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // A file still open on a slow machine: it is a temporary folder.
        }
    }

    public static bool IsRunning(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static async Task<bool> WaitUntilGoneAsync(int pid, int seconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < deadline)
        {
            if (!IsRunning(pid)) return true;
            await Task.Delay(100);
        }
        return !IsRunning(pid);
    }

    public static string FakeDemoOutput()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Blux.UI.slnx")))
            {
                var bin = Path.Combine(dir.FullName, "tests", "Blux.UI.FakeDemo", "bin");
                var dll = Directory.EnumerateFiles(bin, "FakeDemo.dll", SearchOption.AllDirectories).FirstOrDefault()
                    ?? throw new FileNotFoundException("FakeDemo.dll is not built.");
                return Path.GetDirectoryName(dll)!;
            }
        }
        throw new DirectoryNotFoundException("Blux.UI.slnx not found above the test output directory.");
    }

    public static void CopyDirectory(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.EnumerateFiles(from)) File.Copy(file, Path.Combine(to, Path.GetFileName(file)), overwrite: true);
    }

    public static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
