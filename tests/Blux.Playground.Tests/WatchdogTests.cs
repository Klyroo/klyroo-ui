using System.Diagnostics;
using Blux.Playground.Templates;

namespace Blux.Playground.Tests;

/// <summary>
/// The startup hook the docs host injects into a demo (<c>Blux.Playground.DemoWatchdog</c>): the demo ends when the pipe on its standard input closes,
/// and when the process it names as its parent is gone. The processes here are started by the test, and only those are ever killed.
/// </summary>
public class WatchdogTests : IDisposable
{
    private readonly List<Process> _started = [];
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "blux-watchdog-tests-" + Guid.NewGuid().ToString("N"));

    public WatchdogTests() => DemoHarness.CopyDirectory(DemoHarness.FakeDemoOutput(), _folder);

    public void Dispose()
    {
        foreach (var process in _started)
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
            process.Dispose();
        }
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private string Hook => Path.Combine(AppContext.BaseDirectory, "Blux.Playground.DemoWatchdog.dll");

    private async Task<Process> StartDemoAsync(Func<Dictionary<string, string>, Dictionary<string, string>> environment, bool redirectInput)
    {
        Assert.True(File.Exists(Hook), $"{Hook} is not next to the tests");
        var port = DemoHarness.FreePort();
        var settings = new TemplateLaunch.ChildSettings(Hook, Environment.ProcessId, 0);
        var command = TemplateLaunch.Build(TemplateRunMode.Published, "FakeDemo", _folder, _folder, port, "/templates/fake/demo", [], settings);
        var start = new ProcessStartInfo(command.FileName)
        {
            WorkingDirectory = command.WorkingDirectory,
            RedirectStandardInput = redirectInput,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in command.Arguments) start.ArgumentList.Add(argument);
        start.Environment.Clear();
        foreach (var (key, value) in environment(new Dictionary<string, string>(command.Environment))) start.Environment[key] = value;
        var process = Process.Start(start)!;
        _started.Add(process);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        // Up and answering: the hook is in place by then.
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var response = await http.GetAsync($"http://127.0.0.1:{port}/templates/fake/demo/", TestContext.Current.CancellationToken);
                if (response.IsSuccessStatusCode) return process;
            }
            catch (HttpRequestException)
            {
            }
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
        throw new TimeoutException("The fake demo did not answer.");
    }

    [Fact]
    public async Task The_demo_ends_when_its_standard_input_closes_as_it_does_with_the_host()
    {
        var demo = await StartDemoAsync(env => env, redirectInput: true);
        Assert.False(demo.HasExited);

        // The host holds the write end of the pipe; the operating system closes it when the host dies.
        demo.StandardInput.Close();

        Assert.True(demo.WaitForExit(TimeSpan.FromSeconds(10)), "the demo is still running 10 s after its standard input closed");
    }

    [Fact]
    public async Task The_demo_ends_when_the_parent_process_it_names_is_gone()
    {
        // A stand-in for the host, started by the test: a second fake demo with no watchdog of its own.
        var parent = await StartDemoAsync(env => { env.Remove("DOTNET_STARTUP_HOOKS"); return env; }, redirectInput: false);
        var demo = await StartDemoAsync(env =>
        {
            env.Remove("BLUX_WATCHDOG_STDIN");
            env["BLUX_WATCHDOG_PARENT_PID"] = parent.Id.ToString();
            return env;
        }, redirectInput: false);
        Assert.False(demo.HasExited);

        parent.Kill();

        Assert.True(demo.WaitForExit(TimeSpan.FromSeconds(15)), "the demo is still running 15 s after its parent was killed");
    }

    [Fact]
    public async Task Without_the_variables_the_hook_does_nothing()
    {
        var demo = await StartDemoAsync(env =>
        {
            env.Remove("BLUX_WATCHDOG_STDIN");
            env.Remove("BLUX_WATCHDOG_PARENT_PID");
            return env;
        }, redirectInput: false);

        await Task.Delay(4000, TestContext.Current.CancellationToken);

        Assert.False(demo.HasExited);
    }
}
