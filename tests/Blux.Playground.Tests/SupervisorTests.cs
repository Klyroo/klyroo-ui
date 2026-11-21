using System.Diagnostics;
using System.Text.Json;
using Blux.Playground.Templates;

namespace Blux.Playground.Tests;

public class SupervisorTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_demo_is_proxied_ready_and_answers_its_own_404()
    {
        await using var harness = await DemoHarness.StartAsync();

        var home = await harness.Http.GetAsync("/templates/fake/demo/", Ct);
        Assert.Equal(200, (int)home.StatusCode);
        Assert.Contains("fake demo", await home.Content.ReadAsStringAsync(Ct));
        // Routing does not care about case, and neither does the proxy.
        Assert.Equal(200, (int)(await harness.Http.GetAsync("/Templates/FAKE/Demo", Ct)).StatusCode);
        // The demo's own 404 reaches the visitor untouched.
        Assert.Equal(404, (int)(await harness.Http.GetAsync("/templates/fake/demo/no-such-page", Ct)).StatusCode);
        Assert.Equal("ok", await harness.Http.GetStringAsync("/_blux/health", Ct));
        Assert.Equal(200, (int)(await harness.Http.GetAsync("/_blux/templates/ready", Ct)).StatusCode);
    }

    [Fact]
    public async Task The_demo_gets_a_minimal_environment_not_the_one_of_the_host()
    {
        Environment.SetEnvironmentVariable("Blux__Templates__Secret", "do-not-leak");
        Environment.SetEnvironmentVariable("Kestrel__Marker", "x");
        Environment.SetEnvironmentVariable("ASPNETCORE_Kestrel__Marker", "hunter2");
        Environment.SetEnvironmentVariable("ConnectionStrings__Main", "Server=secret");
        try
        {
            await using var harness = await DemoHarness.StartAsync();

            var json = await harness.Http.GetStringAsync("/templates/fake/demo/env", Ct);
            var env = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
            Assert.DoesNotContain(env.Keys, k => k.StartsWith("Blux__", StringComparison.Ordinal) || k.StartsWith("Kestrel__", StringComparison.Ordinal)
                || k.StartsWith("ASPNETCORE_Kestrel__", StringComparison.Ordinal) || k.StartsWith("ConnectionStrings__", StringComparison.Ordinal));
            Assert.Contains("PATH", env.Keys);
            Assert.Equal("Production", env["ASPNETCORE_ENVIRONMENT"]);
            Assert.Equal("5", env["DOTNET_GCConserveMemory"]);
            Assert.Equal("20000000", env["DOTNET_GCHeapHardLimit"]);
            Assert.EndsWith("Blux.Playground.DemoWatchdog.dll", env["DOTNET_STARTUP_HOOKS"]);
            Assert.Equal(Environment.ProcessId.ToString(), env["BLUX_WATCHDOG_PARENT_PID"]);
        }
        finally
        {
            foreach (var name in new[] { "Blux__Templates__Secret", "Kestrel__Marker", "ASPNETCORE_Kestrel__Marker", "ConnectionStrings__Main" })
            {
                Environment.SetEnvironmentVariable(name, null);
            }
        }
    }

    [Fact]
    public async Task A_demo_that_crashes_is_restarted_after_the_backoff_and_the_visitor_sees_a_friendly_page_meanwhile()
    {
        await using var harness = await DemoHarness.StartAsync();
        var first = harness.Pid;

        var killed = Stopwatch.StartNew();
        using (var process = Process.GetProcessById(first)) process.Kill(entireProcessTree: true);
        await harness.WaitForStateAsync("restarting");

        // While it is down the proxy has nobody to ask: not an empty response, a page that says what happens.
        var down = await harness.Http.GetAsync("/templates/fake/demo/", Ct);
        Assert.Equal(503, (int)down.StatusCode);
        Assert.Contains("restarting", await down.Content.ReadAsStringAsync(Ct));
        Assert.Equal("5", down.Headers.RetryAfter!.ToString());

        await harness.WaitForStateAsync("ready", minRestarts: 1);
        // The first failure waits one second.
        Assert.True(killed.Elapsed >= TimeSpan.FromSeconds(1), $"restarted after {killed.Elapsed}");
        Assert.NotEqual(first, harness.Pid);
        Assert.Equal(200, (int)(await harness.Http.GetAsync("/templates/fake/demo/", Ct)).StatusCode);
    }

    [Fact]
    public async Task A_demo_that_stops_answering_as_the_demo_is_restarted()
    {
        await using var harness = await DemoHarness.StartAsync();
        var first = harness.Pid;
        var unhealthy = Path.Combine(harness.DemoFolder, "unhealthy");

        // Alive, listening, answering 200: but with a page that is not the demo's (it has no base href).
        await File.WriteAllTextAsync(unhealthy, "", Ct);
        await harness.WaitForStateAsync("restarting");
        File.Delete(unhealthy);

        await harness.WaitForStateAsync("ready", minRestarts: 1);
        Assert.NotEqual(first, harness.Pid);
        Assert.False(DemoHarness.IsRunning(first));
    }

    [Fact]
    public async Task Stopping_the_host_kills_the_demo()
    {
        var harness = await DemoHarness.StartAsync();
        var pid = harness.Pid;
        Assert.True(DemoHarness.IsRunning(pid));

        await harness.DisposeAsync();

        Assert.True(await DemoHarness.WaitUntilGoneAsync(pid, 10), "the demo is still running after the host stopped");
    }

    [Fact]
    public async Task KillAll_ends_every_demo_process_and_can_be_repeated()
    {
        await using var harness = await DemoHarness.StartAsync(livenessSeconds: 300);
        var pid = harness.Pid;

        harness.Supervisor.KillAll();
        harness.Supervisor.KillAll();

        Assert.True(await DemoHarness.WaitUntilGoneAsync(pid, 10));
    }

    [Fact]
    public async Task The_status_endpoints_answer_loopback_clients_only()
    {
        await using var harness = await DemoHarness.StartAsync();

        var status = await harness.Http.GetStringAsync("/_blux/templates/status", Ct);
        Assert.Contains("\"state\":\"ready\"", status);
        // A request that went through a proxy (it carries X-Forwarded-For the site did not trust) is a visitor's, not the operator's.
        using var proxied = new HttpRequestMessage(HttpMethod.Get, "/_blux/templates/status");
        proxied.Headers.Add("X-Forwarded-For", "203.0.113.9");
        Assert.Equal(404, (int)(await harness.Http.SendAsync(proxied, Ct)).StatusCode);
        // The health check of the host does not depend on it.
        using var health = new HttpRequestMessage(HttpMethod.Get, "/_blux/health");
        health.Headers.Add("X-Forwarded-For", "203.0.113.9");
        Assert.Equal(200, (int)(await harness.Http.SendAsync(health, Ct)).StatusCode);
    }

    [Fact]
    public async Task A_demo_that_is_not_built_is_unavailable_and_does_not_hold_ready_back()
    {
        await using var harness = await DemoHarness.StartAsync(waitReady: false);
        await harness.WaitForStateAsync("ready");
        Assert.True(harness.Supervisor.AllReady);

        // Same host, a second harness whose demo folder is empty: its only demo cannot start, and /ready is still 200.
        await using var empty = await DemoHarness.StartAsync(waitReady: false, populate: false);
        await empty.WaitForStateAsync("unavailable");
        Assert.True(empty.Supervisor.AllReady);
        var response = await empty.Http.GetAsync("/_blux/templates/ready", Ct);
        Assert.Equal(200, (int)response.StatusCode);
        Assert.Contains("\"state\":\"unavailable\"", await response.Content.ReadAsStringAsync(Ct));
    }
}
