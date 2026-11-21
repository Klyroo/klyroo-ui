using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Blux.Playground.Client.Templates;
using Microsoft.Extensions.Options;

namespace Blux.Playground.Templates;

/// <summary>
/// Settings under <c>Blux:Templates</c>: <c>Enabled</c> (default true), <c>PublishRoot</c> (a folder holding one <c>dotnet publish</c> output per
/// template, <c>{PublishRoot}/{name}/</c>; when set, the demos run published, in Production), <c>Configuration</c> (the build configuration the demos
/// are run from when no <c>PublishRoot</c> is set, default Debug), <c>StartupTimeoutSeconds</c> (default 120), <c>LivenessIntervalSeconds</c> (how often
/// a running demo is asked for its home page, default 30; two answers in a row that are not the demo's restart it), <c>MemoryLimitMegabytes</c> (the
/// managed heap limit of each demo, default 512, 0 for none) and <c>RepositoryBase</c> (see <see cref="TemplateSettings"/>).
/// </summary>
public sealed class TemplateDemoOptions
{
    public bool Enabled { get; set; } = true;
    public string? PublishRoot { get; set; }
    public string Configuration { get; set; } = "Debug";
    public int StartupTimeoutSeconds { get; set; } = 120;
    public double LivenessIntervalSeconds { get; set; } = 30;
    public int MemoryLimitMegabytes { get; set; } = 512;
    public string? RepositoryBase { get; set; }
}

/// <summary>What the status endpoint reports of a demo. <c>State</c> is starting, ready, restarting, unavailable or stopped.</summary>
public sealed record TemplateDemoStatus(string Name, string State, string? Detail, int Restarts, int? Pid = null);

/// <summary>The templates that run as a demo on this host: the registry, or the ones a test gives.</summary>
public sealed record TemplateDemoCatalog(IReadOnlyList<TemplateInfo> Templates)
{
    public IEnumerable<string> Names => Templates.Select(t => t.Name);
}

/// <summary>
/// Runs each available template as a child process of the docs host (<c>dotnet {Name}.Web.dll --urls http://127.0.0.1:{port} --PathBase=...</c>) and
/// keeps it running: a readiness probe, a liveness probe after that, a restart with a growing delay when it exits or stops answering, and the whole
/// process tree killed when the host stops. A demo also ends by itself when the host dies without cleaning up (see <c>Blux.Playground.DemoWatchdog</c>).
/// The proxy (<see cref="TemplateProxyConfigProvider"/>) is told the port of each start. Where a demo cannot be started at all (not built, not
/// published), it is reported as unavailable and not retried: a restart cannot fix that.
/// </summary>
public sealed class TemplateDemoSupervisor : BackgroundService, ITemplateDemoState
{
    private const string WatchdogAssembly = "Blux.Playground.DemoWatchdog.dll";

    private readonly TemplateDemoOptions _options;
    private readonly TemplateDemoCatalog _catalog;
    private readonly IHostEnvironment _environment;
    private readonly TemplateProxyConfigProvider _proxy;
    private readonly ILogger<TemplateDemoSupervisor> _logger;
    private readonly string? _docsPathBase;
    private readonly ConcurrentDictionary<string, TemplateDemoStatus> _status = new();
    private readonly ConcurrentDictionary<string, Process> _live = new();
    private readonly HashSet<int> _reserved = [];

    public TemplateDemoSupervisor(
        IOptions<TemplateDemoOptions> options,
        TemplateDemoCatalog catalog,
        IHostEnvironment environment,
        TemplateProxyConfigProvider proxy,
        IConfiguration configuration,
        ILogger<TemplateDemoSupervisor> logger)
    {
        _options = options.Value;
        _catalog = catalog;
        _environment = environment;
        _proxy = proxy;
        _logger = logger;
        _docsPathBase = configuration["PathBase"];
        foreach (var template in catalog.Templates)
        {
            _status[template.Name] = new(template.Name, "starting", null, 0);
        }
        // A normal exit of the host, Ctrl+C or a termination signal end up here or in StopAsync, and so does a crash of the host (an unhandled exception,
        // a failure to bind its port, which does not raise ProcessExit). A host killed outright (SIGKILL, the out of memory killer) cannot clean up:
        // the watchdog each demo carries ends it then.
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    public IReadOnlyList<TemplateDemoStatus> Statuses => [.. _status.Values.OrderBy(s => s.Name)];

    /// <summary>True when every demo that can be started (built or published) answers. A demo that cannot be started does not hold this back.</summary>
    public bool AllReady => _status.Values.All(s => s.State is "ready" or "unavailable");

    /// <summary>The ids of the demo processes running now, by template name.</summary>
    public IReadOnlyDictionary<string, int> ProcessIds => _live.Where(p => !p.Value.HasExited).ToDictionary(p => p.Key, p => p.Value.Id);

    public string? StateOf(string name) => _status.TryGetValue(name, out var status) ? status.State : null;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(_catalog.Templates.Select(t => Task.Run(() => RunAsync(t, stoppingToken), stoppingToken)));

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        KillAll();
    }

    public override void Dispose()
    {
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        KillAll();
        base.Dispose();
    }

    private void OnProcessExit(object? sender, EventArgs e) => KillAll();

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e) => KillAll();

    /// <summary>Kills every demo process, with its children. Safe to call more than once.</summary>
    public void KillAll()
    {
        foreach (var (_, process) in _live) Terminate(process);
    }

    private async Task RunAsync(TemplateInfo template, CancellationToken stoppingToken)
    {
        var name = template.Name;
        var pathBase = TemplateLaunch.DemoPathBase(_docsPathBase, name);
        var failures = 0;
        var restarts = 0;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!TryResolve(template, out var mode, out var directory, out var workingDirectory, out var problem))
                {
                    _logger.LogWarning("The demo of the {Template} template is unavailable: {Problem}", name, problem);
                    _status[name] = new(name, "unavailable", problem, restarts);
                    return;
                }

                int port;
                lock (_reserved) port = TemplateLaunch.AllocatePort(OsAssignedPort, _reserved);
                var settings = new TemplateLaunch.ChildSettings(WatchdogPath(), Environment.ProcessId, _options.MemoryLimitMegabytes);
                var command = TemplateLaunch.Build(mode, template.WebAssembly!, directory, workingDirectory, port, pathBase, Host(), settings);
                var started = Stopwatch.StartNew();
                using var process = Start(name, command);
                _live[name] = process;
                _proxy.SetPort(name, port);
                _status[name] = new(name, "starting", null, restarts, process.Id);
                _logger.LogInformation("Demo {Template}: started (pid {Pid}) on port {Port}, path base {PathBase}", name, process.Id, port, pathBase);

                using var exited = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var probe = ProbeAsync(name, process, port, pathBase, restarts, exited.Token);
                try
                {
                    await process.WaitForExitAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    Terminate(process);
                    break;
                }
                finally
                {
                    await exited.CancelAsync();
                    _live.TryRemove(name, out _);
                    lock (_reserved) _reserved.Remove(port);
                }
                await probe;

                _proxy.SetPort(name, null);
                restarts++;
                failures = TemplateLaunch.NextFailureCount(failures, started.Elapsed);
                var delay = TemplateLaunch.RestartDelay(failures);
                _status[name] = new(name, "restarting", $"exited with code {process.ExitCode}, restarting in {delay.TotalSeconds:0} s", restarts);
                _logger.LogWarning("Demo {Template}: exited with code {Code} after {Seconds:0} s, restarting in {Delay} s", name, process.ExitCode, started.Elapsed.TotalSeconds, delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // The host is stopping.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "The supervisor of the {Template} demo stopped", name);
            _status[name] = new(name, "unavailable", exception.Message, restarts);
        }
        finally
        {
            _proxy.SetPort(name, null);
            if (_status.TryGetValue(name, out var last) && last.State != "unavailable") _status[name] = last with { State = "stopped", Pid = null };
        }
    }

    /// <summary>
    /// Waits until the home page of the demo answers (and is the demo's), marks it ready, then asks again every <c>LivenessIntervalSeconds</c>. A demo
    /// that has not answered by the startup timeout, or fails two probes in a row once ready, is killed (and restarted by the loop).
    /// </summary>
    private async Task ProbeAsync(string name, Process process, int port, string pathBase, int restarts, CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var url = TemplateLaunch.ProbeUrl(port, pathBase);
        var deadline = DateTime.UtcNow.AddSeconds(Math.Max(5, _options.StartupTimeoutSeconds));
        var interval = TimeSpan.FromSeconds(Math.Max(0.2, _options.LivenessIntervalSeconds));
        try
        {
            var ready = false;
            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
            {
                if (await AnswersAsync(http, url, pathBase, cancellationToken))
                {
                    ready = true;
                    _status[name] = new(name, "ready", null, restarts, process.Id);
                    _logger.LogInformation("Demo {Template}: ready", name);
                    break;
                }
                await Task.Delay(250, cancellationToken);
            }
            if (!ready)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError("Demo {Template}: no answer from {Url} within {Seconds} s, killing it", name, url, _options.StartupTimeoutSeconds);
                    Terminate(process);
                }
                return;
            }

            var misses = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(interval, cancellationToken);
                if (await AnswersAsync(http, url, pathBase, cancellationToken))
                {
                    misses = 0;
                    continue;
                }
                if (++misses < 2) continue;
                _logger.LogError("Demo {Template}: {Url} stopped answering as the demo, killing it", name, url);
                Terminate(process);
                return;
            }
        }
        catch (OperationCanceledException)
        {
            // The process exited or the host is stopping.
        }
    }

    private static async Task<bool> AnswersAsync(HttpClient http, string url, string pathBase, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return TemplateLaunch.IsDemoResponse((int)response.StatusCode, body, pathBase);
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private Process Start(string name, TemplateCommand command)
    {
        var start = new ProcessStartInfo(command.FileName)
        {
            WorkingDirectory = command.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // The write end of this pipe stays open in this host: the watchdog of the demo reads its end, and the operating system closes ours with the host.
            RedirectStandardInput = true,
            UseShellExecute = false,
        };
        foreach (var argument in command.Arguments) start.ArgumentList.Add(argument);
        // Not the environment of the host: its settings and secrets are not the demo's.
        start.Environment.Clear();
        foreach (var (key, value) in command.Environment) start.Environment[key] = value;
        var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start the {name} demo.");
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) _logger.LogInformation("[{Template}] {Line}", name, e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) _logger.LogWarning("[{Template}] {Line}", name, e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static IEnumerable<KeyValuePair<string, string?>> Host() =>
        Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>()
            .Select(e => new KeyValuePair<string, string?>((string)e.Key, (string?)e.Value));

    private string? WatchdogPath()
    {
        var path = Path.Combine(AppContext.BaseDirectory, WatchdogAssembly);
        if (File.Exists(path)) return path;
        _logger.LogWarning("{File} is not next to the host: a demo will not end by itself if the host is killed", path);
        return null;
    }

    private static void Terminate(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch (Exception)
        {
            // Already gone.
        }
    }

    /// <summary>
    /// Finds what to run. With a <c>PublishRoot</c>: the publish folder of the template, in Production. Without one: the build output of the template
    /// in this repository (the docs host runs from a checkout), in Development. The problem is for the log and the loopback status endpoint only.
    /// </summary>
    private bool TryResolve(TemplateInfo template, out TemplateRunMode mode, out string directory, out string workingDirectory, out string problem)
    {
        var assembly = template.WebAssembly + ".dll";
        directory = workingDirectory = "";
        problem = "";
        if (!string.IsNullOrWhiteSpace(_options.PublishRoot))
        {
            mode = TemplateRunMode.Published;
            directory = workingDirectory = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _options.PublishRoot, template.Name));
            if (File.Exists(Path.Combine(directory, assembly))) return true;
            problem = $"{Path.Combine(directory, assembly)} does not exist; publish the template there (dotnet publish -c Release -o <folder>)";
            return false;
        }

        mode = TemplateRunMode.Development;
        var repository = FindRepository();
        if (repository is null)
        {
            problem = "Blux:Templates:PublishRoot is not set and no checkout of the repository was found to run the template from";
            return false;
        }
        var project = Path.Combine(repository, "templates", template.Name, template.WebProject!);
        var output = Path.Combine(project, "bin", _options.Configuration);
        var dll = Directory.Exists(output)
            ? Directory.EnumerateFiles(output, assembly, SearchOption.AllDirectories).FirstOrDefault()
            : null;
        if (dll is null)
        {
            problem = $"{Path.Combine(output, "*", assembly)} does not exist; build it (dotnet build {project})";
            return false;
        }
        directory = Path.GetDirectoryName(dll)!;
        workingDirectory = project;
        return true;
    }

    private string? FindRepository()
    {
        foreach (var start in new[] { _environment.ContentRootPath, AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Blux.UI.slnx"))) return dir.FullName;
            }
        }
        return null;
    }

    private static int OsAssignedPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
