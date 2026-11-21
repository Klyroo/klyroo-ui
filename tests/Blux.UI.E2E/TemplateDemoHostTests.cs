using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>A second docs host below <c>/sub</c> that runs its own template demos (the one of <see cref="PathBaseFixture"/> has them switched off).</summary>
public sealed class DemosPathBaseFixture(PlaygroundServer server) : IAsyncLifetime
{
    public const string PathBase = "/sub";

    public PlaygroundServer Server { get; } = server;
    public PlaygroundInstance Instance { get; private set; } = default!;

    public async ValueTask InitializeAsync() => Instance = await Server.StartAtPathBaseAsync(PathBase, demos: true);

    public ValueTask DisposeAsync() => Instance is null ? ValueTask.CompletedTask : Instance.DisposeAsync();
}

/// <summary>
/// The docs served below a path base with the demos running: the gallery's links, the proxy, the demo's own path base (<c>/sub/templates/starter/demo/</c>)
/// and its circuit all stay below <c>/sub/</c>. And the docs with the demos switched off, where the gallery must not offer a link it cannot honour.
/// </summary>
public class TemplatesPathBaseTests(DemosPathBaseFixture demos, PathBaseFixture off) : IClassFixture<DemosPathBaseFixture>, IClassFixture<PathBaseFixture>, IAsyncLifetime
{
    private IBrowserContext _context = default!;
    private IPage _page = default!;

    public async ValueTask InitializeAsync()
    {
        _context = await demos.Server.Browser.NewContextAsync(new() { BaseURL = demos.Instance.BaseUrl });
        _page = await _context.NewPageAsync();
    }

    public ValueTask DisposeAsync() => _context.DisposeAsync();

    [Fact]
    public async Task The_demo_runs_below_the_path_base_of_the_docs_and_stays_there()
    {
        Assert.True(demos.Server.TemplatesReady, "the template demos are not running");
        var requests = new List<string>();
        var sockets = new List<string>();
        var failures = new List<string>();
        _page.Request += (_, request) => { lock (requests) requests.Add(request.Url); };
        _page.WebSocket += (_, socket) => { lock (sockets) sockets.Add(socket.Url); };
        _page.Response += (_, response) => { if (response.Status >= 400 && !response.Url.Contains("/_framework/dotnet-browser-tools/")) lock (failures) failures.Add($"{response.Status} {response.Url}"); };

        await _page.GotoAsync("templates");
        var preview = _page.GetByTestId("template-preview");
        // The link is relative to the base href, so it lands below the path base.
        Assert.Equal(demos.Instance.Host + "/sub/templates/starter/demo/", await preview.EvaluateAsync<string>("a => a.href"));
        lock (requests) requests.Clear();
        await preview.ClickAsync();
        await _page.WaitForURLAsync(url => url.EndsWith("/sub/templates/starter/demo/"));

        await Assertions.Expect(_page.GetByTestId("renderer")).ToHaveTextAsync(new Regex("Running on (Server|WebAssembly)"), new() { Timeout = 30_000 });
        await _page.GetByTestId("counter-button").ClickAsync();
        await Assertions.Expect(_page.GetByTestId("counter")).ToHaveTextAsync("Clicked 1 times", new() { Timeout = 30_000 });

        var prefix = demos.Instance.Host + "/sub/templates/starter/demo/";
        lock (requests)
        {
            var outside = requests
                .Where(url => url.StartsWith(demos.Instance.Host + "/", StringComparison.Ordinal) && !url.Contains("/_framework/dotnet-browser-tools/", StringComparison.Ordinal))
                .Where(url => !url.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            Assert.True(outside.Count == 0, "Requests outside /sub/templates/starter/demo/:\n" + string.Join("\n", outside));
        }
        lock (failures) Assert.True(failures.Count == 0, "Failed requests:\n" + string.Join("\n", failures));
        if (await _page.GetByTestId("renderer").InnerTextAsync() == "Running on Server")
        {
            lock (sockets)
            {
                Assert.NotEmpty(sockets);
                Assert.All(sockets, url => Assert.StartsWith("ws://" + new Uri(demos.Instance.Host).Authority + "/sub/templates/starter/demo/", url));
            }
        }
    }

    [Fact]
    public async Task With_the_demos_switched_off_the_gallery_says_so_and_offers_no_link_to_them()
    {
        await using var context = await off.Server.Browser.NewContextAsync(new() { BaseURL = off.Instance.BaseUrl });
        var page = await context.NewPageAsync();

        await page.GotoAsync("templates");

        var starter = page.GetByTestId("template-card").Filter(new() { Has = page.GetByRole(AriaRole.Heading, new() { Name = "Starter" }) });
        await Assertions.Expect(starter.GetByTestId("template-state")).ToHaveTextAsync("Demo offline");
        await Assertions.Expect(starter.GetByTestId("template-preview")).ToHaveCountAsync(0);
        await Assertions.Expect(starter.GetByTestId("template-offline")).ToBeVisibleAsync();
        await Assertions.Expect(starter.Locator("a")).ToHaveCountAsync(0);
    }
}

/// <summary>
/// A docs host that is killed outright (SIGKILL, the out of memory killer) cannot stop its demos, and a child outlives its parent: each demo carries a
/// watchdog (a startup hook of the host) that ends it when the host is gone. This runs a real host as a process of its own and kills only that process.
/// </summary>
public class TemplateWatchdogTests
{
    [Fact]
    public async Task A_demo_ends_by_itself_when_the_host_is_killed()
    {
        var (host, baseUrl) = await PlaygroundServer.StartPlaygroundAsync(null, demos: true, direct: true);
        int? child = null;
        try
        {
            using var http = new HttpClient();
            using var status = JsonDocument.Parse(await http.GetStringAsync(baseUrl + "/_blux/templates/status", TestContext.Current.CancellationToken));
            var pids = status.RootElement.EnumerateArray().Where(e => e.TryGetProperty("pid", out var pid) && pid.ValueKind == JsonValueKind.Number).Select(e => e.GetProperty("pid").GetInt32()).ToList();
            if (pids.Count == 0)
            {
                Assert.False(Environment.GetEnvironmentVariable("CI") is { Length: > 0 }, "No demo is running to watch.");
                Assert.SkipWhen(true, "No demo is running on this host (the docs run published with no templates).");
            }
            child = pids[0];
            Assert.True(IsRunning(child.Value));

            // SIGKILL on Unix: no shutdown hook, no chance to stop the demo.
            host.Kill();
            await host.WaitForExitAsync(TestContext.Current.CancellationToken);

            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (IsRunning(child.Value) && DateTime.UtcNow < deadline) await Task.Delay(200, TestContext.Current.CancellationToken);
            Assert.False(IsRunning(child.Value), $"the demo (pid {child}) is still running 20 s after its host was killed");
        }
        finally
        {
            // Only the processes this test started: the host, and the demo the host started.
            if (!host.HasExited) host.Kill(entireProcessTree: true);
            if (child is { } pid && IsRunning(pid))
            {
                using var process = Process.GetProcessById(pid);
                process.Kill(entireProcessTree: true);
            }
            host.Dispose();
        }
    }

    private static bool IsRunning(int pid)
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
}
