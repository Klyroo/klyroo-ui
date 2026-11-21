using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using Blux.Playground.Client.Templates;

namespace Blux.Playground.Templates;

/// <summary>
/// Settings under <c>Blux:Templates</c>: <c>Enabled</c> (default true), <c>PublishRoot</c> (a folder holding one <c>dotnet publish</c> output per
/// template, <c>{PublishRoot}/{name}/</c>; when set, the demos run published, in Production), <c>Configuration</c> (the build configuration the demos
/// are run from when no <c>PublishRoot</c> is set, default Debug) and <c>StartupTimeoutSeconds</c> (default 120).
/// </summary>
public sealed class TemplateDemoOptions
{
    public bool Enabled { get; set; } = true;
    public string? PublishRoot { get; set; }
    public string Configuration { get; set; } = "Debug";
    public int StartupTimeoutSeconds { get; set; } = 120;
    public string? RepositoryBase { get; set; }
}

/// <summary>What the status endpoint reports of a demo. <c>State</c> is starting, ready, restarting, unavailable or stopped.</summary>
public sealed record TemplateDemoStatus(string Name, string State, string? Detail, int Restarts);

/// <summary>
/// Runs each available template as a child process of the docs host (<c>dotnet {Name}.Web.dll --urls http://127.0.0.1:{port} --PathBase=...</c>) and
/// keeps it running: a readiness probe, a restart with a growing delay when it exits, and the whole process tree killed when the host stops.
/// The proxy (<see cref="TemplateProxyConfigProvider"/>) is told the port of each start. Where a demo cannot be started at all (not built, not
/// published), it is reported as unavailable and not retried: a restart cannot fix that.
/// </summary>
public sealed class TemplateDemoSupervisor : BackgroundService
{
    private readonly TemplateDemoOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly TemplateProxyConfigProvider _proxy;
    private readonly ILogger<TemplateDemoSupervisor> _logger;
    private readonly string? _docsPathBase;
    private readonly ConcurrentDictionary<string, TemplateDemoStatus> _status = new();
    private readonly ConcurrentDictionary<string, Process> _live = new();
    private readonly HashSet<int> _reserved = [];

    public TemplateDemoSupervisor(
        IOptions<TemplateDemoOptions> options,
        IHostEnvironment environment,
        TemplateProxyConfigProvider proxy,
        IConfiguration configuration,
        ILogger<TemplateDemoSupervisor> logger)
    {
        _options = options.Value;
        _environment = environment;
        _proxy = proxy;
        _logger = logger;
        _docsPathBase = configuration["PathBase"];
        foreach (var template in TemplateRegistry.All.Where(t => t.HasDemo))
        {
            _status[template.Name] = new(template.Name, "starting", null, 0);
        }
        // A normal exit of the host, Ctrl+C or a termination signal end up here or in StopAsync, and so does a crash of the host (an unhandled exception,
        // a failure to bind its port, which does not raise ProcessExit). A host killed outright (SIGKILL) cannot clean up.
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    public IReadOnlyList<TemplateDemoStatus> Statuses => [.. _status.Values.OrderBy(s => s.Name)];

    public bool AllReady => _status.Values.All(s => s.State == "ready");

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(TemplateRegistry.All.Where(t => t.HasDemo).Select(t => Task.Run(() => RunAsync(t, stoppingToken), stoppingToken)));

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

    private void KillAll()
    {
        foreach (var (_, process) in _live)
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
                var command = TemplateLaunch.Build(mode, template.WebAssembly!, directory, workingDirectory, port, pathBase);
                var started = Stopwatch.StartNew();
                using var process = Start(name, command);
                _live[name] = process;
                _proxy.SetPort(name, port);
                _status[name] = new(name, "starting", null, restarts);
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
            if (_status.TryGetValue(name, out var last) && last.State != "unavailable") _status[name] = last with { State = "stopped" };
        }
    }

    /// <summary>Waits until the home page of the demo answers, then marks it ready. A demo that has not answered by the timeout is killed (and restarted).</summary>
    private async Task ProbeAsync(string name, Process process, int port, string pathBase, int restarts, CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var url = TemplateLaunch.ProbeUrl(port, pathBase);
        var deadline = DateTime.UtcNow.AddSeconds(Math.Max(5, _options.StartupTimeoutSeconds));
        try
        {
            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
            {
                try
                {
                    using var response = await http.GetAsync(url, cancellationToken);
                    if ((int)response.StatusCode < 500)
                    {
                        _status[name] = new(name, "ready", null, restarts);
                        _logger.LogInformation("Demo {Template}: ready", name);
                        return;
                    }
                }
                catch (HttpRequestException)
                {
                    // Not listening yet.
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // One probe timed out: try again.
                }
                await Task.Delay(250, cancellationToken);
            }
            if (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("Demo {Template}: no answer from {Url} within {Seconds} s, killing it", name, url, _options.StartupTimeoutSeconds);
                Terminate(process);
            }
        }
        catch (OperationCanceledException)
        {
            // The process exited or the host is stopping.
        }
    }

    private Process Start(string name, TemplateCommand command)
    {
        var start = new ProcessStartInfo(command.FileName)
        {
            WorkingDirectory = command.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in command.Arguments) start.ArgumentList.Add(argument);
        foreach (var (key, value) in command.Environment) start.Environment[key] = value;
        // The docs host may have been started with its own path base or URLs in the environment: they are not the demo's.
        start.Environment.Remove("PathBase");
        start.Environment.Remove("ASPNETCORE_URLS");
        start.Environment.Remove("DOTNET_URLS");
        var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start the {name} demo.");
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) _logger.LogInformation("[{Template}] {Line}", name, e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) _logger.LogWarning("[{Template}] {Line}", name, e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
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
    /// in this repository (the docs host runs from a checkout), in Development.
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
