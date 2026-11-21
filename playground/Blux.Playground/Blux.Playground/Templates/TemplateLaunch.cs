namespace Blux.Playground.Templates;

/// <summary>How a template is run: from the output of its own build (Development) or from <c>dotnet publish</c> output (Production).</summary>
public enum TemplateRunMode
{
    Development,
    Published,
}

/// <summary>A process to start: what <see cref="TemplateLaunch.Build"/> decides, with no side effect, so it can be tested.</summary>
public sealed record TemplateCommand(string FileName, IReadOnlyList<string> Arguments, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment);

/// <summary>
/// The pure decisions of the supervisor of the template demos: the path base of a demo, the command line of its process, the port it gets and the
/// delay before a restart. This file has no dependency, and the unit tests compile it as it is.
/// </summary>
public static class TemplateLaunch
{
    /// <summary>The path of the demo below the root of the docs site: <c>/templates/starter/demo</c>.</summary>
    public static string DemoPath(string name) => $"/templates/{name}/demo";

    /// <summary>
    /// The path base the process of a demo is given: the one of the docs site, when it is served below a prefix, then <see cref="DemoPath"/>.
    /// The proxy does not strip it, so the process serves its pages, its static files and its circuit endpoint below it.
    /// </summary>
    public static string DemoPathBase(string? docsPathBase, string name)
    {
        var prefix = string.IsNullOrWhiteSpace(docsPathBase) ? "" : "/" + docsPathBase.Trim('/');
        return prefix == "/" ? DemoPath(name) : prefix + DemoPath(name);
    }

    /// <summary>
    /// True when <paramref name="path"/> (below the path base of the docs site) belongs to the demo of one of <paramref name="names"/>: those requests
    /// are answered by the demo, whatever the status, and the docs site must not replace a 404 with its own page.
    /// </summary>
    public static bool IsDemoPath(string path, IEnumerable<string> names)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 3 && segments[0] == "templates" && segments[2] == "demo" && names.Contains(segments[1]);
    }

    /// <summary>
    /// The command that starts one demo. <paramref name="directory"/> holds <c>{assembly}.dll</c> (the publish folder, or the build output),
    /// <paramref name="workingDirectory"/> is where the process runs (the publish folder, or the project folder so that <c>wwwroot</c> and the
    /// static web assets manifest are found as <c>dotnet run</c> would find them).
    /// </summary>
    public static TemplateCommand Build(TemplateRunMode mode, string assembly, string directory, string workingDirectory, int port, string pathBase)
    {
        var environment = new Dictionary<string, string>
        {
            // Development serves the static web assets of referenced libraries from their source folders; Production reads the publish folder.
            ["ASPNETCORE_ENVIRONMENT"] = mode == TemplateRunMode.Published ? "Production" : "Development",
            // The proxy sends X-Forwarded-For and X-Forwarded-Proto; the demo reads them like any app behind a reverse proxy.
            ["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true",
            ["DOTNET_NOLOGO"] = "1",
            ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
        };
        string[] arguments =
        [
            Path.Combine(directory, assembly + ".dll"),
            "--urls", $"http://127.0.0.1:{port}",
            $"--PathBase={pathBase}",
        ];
        return new TemplateCommand("dotnet", arguments, workingDirectory, environment);
    }

    /// <summary>The address the supervisor asks to know that a demo is up: its home page, below its path base.</summary>
    public static string ProbeUrl(int port, string pathBase) => $"http://127.0.0.1:{port}{pathBase.TrimEnd('/')}/";

    /// <summary>The address the proxy sends the requests of a demo to.</summary>
    public static string Destination(int port) => $"http://127.0.0.1:{port}/";

    /// <summary>
    /// Takes a free port from <paramref name="probe"/> (which asks the system for one) that is not in <paramref name="reserved"/>, the ports other
    /// demos were given: the system may hand out a port again after it was released, before its process bound it.
    /// </summary>
    public static int AllocatePort(Func<int> probe, ISet<int> reserved, int attempts = 20)
    {
        for (var i = 0; i < attempts; i++)
        {
            var port = probe();
            if (port is > 0 and <= 65535 && reserved.Add(port)) return port;
        }
        throw new InvalidOperationException($"No free port found after {attempts} attempts.");
    }

    /// <summary>The first delay, doubled by every failure in a row, up to <see cref="MaxDelay"/>.</summary>
    public static readonly TimeSpan FirstDelay = TimeSpan.FromSeconds(1);

    public static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

    /// <summary>A process that ran this long before it exited did not fail in a loop: the failures in a row start over.</summary>
    public static readonly TimeSpan StableAfter = TimeSpan.FromSeconds(60);

    /// <summary>The delay before the restart that follows the <paramref name="failures"/>th failure in a row (1 for the first).</summary>
    public static TimeSpan RestartDelay(int failures)
    {
        if (failures <= 1) return FirstDelay;
        var seconds = FirstDelay.TotalSeconds * Math.Pow(2, Math.Min(failures - 1, 16));
        return TimeSpan.FromSeconds(Math.Min(seconds, MaxDelay.TotalSeconds));
    }

    /// <summary>The number of failures in a row after a process that ran for <paramref name="ranFor"/> exited.</summary>
    public static int NextFailureCount(int failures, TimeSpan ranFor) => ranFor >= StableAfter ? 1 : failures + 1;
}
