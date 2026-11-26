using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace Klyroo.UI.E2E;

/// <summary>
/// How a template is started for a test. By default the way the repository runs it: <c>dotnet run --no-build</c> of its web project, in Development
/// (the static web assets of the library are served from their source folders), after the build that the project references made. When
/// <c>build/verify-templates.mjs</c> sets <c>KLYROO_TEMPLATE_PUBLISHED</c> and <c>KLYROO_TEMPLATE_ASSEMBLY</c>, the app is the published output of one exported template,
/// run in Production, and <c>KLYROO_TEMPLATE_PROBE</c> names the CSS that only the pages of that template produce.
/// </summary>
internal static class TemplateApp
{
    private static string? Published => Environment.GetEnvironmentVariable("KLYROO_TEMPLATE_PUBLISHED");

    private static string? AssemblyFile => Environment.GetEnvironmentVariable("KLYROO_TEMPLATE_ASSEMBLY");

    private static bool IsPublished => !string.IsNullOrEmpty(Published) && !string.IsNullOrEmpty(AssemblyFile);

    /// <summary>The web project of each template, below <c>templates/{name}/</c>.</summary>
    private static readonly Dictionary<string, string> Projects = new()
    {
        ["starter"] = "src/Starter.Web",
        ["dashboard"] = "src/Dashboard.Web",
        ["landing"] = "src/Landing.Web",
        ["docs"] = "src/Docs.Web",
        ["saas"] = "src/SaaS.Web",
        ["portfolio"] = "src/Portfolio.Web",
        ["changelog"] = "src/Changelog.Web",
        ["chat"] = "src/Chat.Web",
        ["editor"] = "src/Editor.Web",
    };

    /// <summary>One publish folder per template for the whole run, made the first time a test asks for it.</summary>
    private static readonly ConcurrentDictionary<string, Lazy<Task<string>>> Publishes = new();

    private static readonly string PublishRoot = Path.Combine(Path.GetTempPath(), "klyroo-ui-e2e-" + Guid.NewGuid().ToString("N"));

    /// <summary>The CSS to find in the stylesheet, or null when this run has none to check.</summary>
    public static string? Probe(string template) =>
        IsPublished ? Environment.GetEnvironmentVariable("KLYROO_TEMPLATE_PROBE") : null;

    /// <summary>The process to start for <paramref name="template"/> on <paramref name="host"/> (<c>http://127.0.0.1:port</c>), output redirected, nothing started yet.</summary>
    public static ProcessStartInfo StartInfo(string template, string host)
    {
        ProcessStartInfo start;
        if (IsPublished)
        {
            var directory = Path.GetFullPath(Published!);
            start = new ProcessStartInfo("dotnet", [Path.Combine(directory, AssemblyFile!), "--urls", host]) { WorkingDirectory = directory };
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        }
        else
        {
            var project = Path.Combine(RepositoryRoot(), "templates", template, Projects[template]);
            // "dotnet run" itself, with the project as its working directory: wwwroot and the static web assets manifest are found as a developer's run finds them.
            start = new ProcessStartInfo("dotnet", ["run", "--no-build", "-c", Configuration(), "--no-launch-profile", "--urls", host]) { WorkingDirectory = project };
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            start.Environment["DOTNET_NOLOGO"] = "1";
            start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            if (template == "changelog")
            {
                // appsettings.Development.json of the Changelog points it at the sample releases of a made-up repository, with a cache of five seconds: the tests
                // start from what appsettings.json says (no repository, one hour), as a published app does, and set the rest themselves.
                start.Environment["Changelog__Source"] = "";
                start.Environment["Changelog__Repository"] = "";
                start.Environment["Changelog__CacheDuration"] = "01:00:00";
            }
        }
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        return start;
    }

    /// <summary>
    /// Like <see cref="StartInfo"/>, for the tests that are about what a deployed app does: the output of <c>dotnet publish</c> (trimmed WebAssembly, the static web assets
    /// of the libraries copied next to the app) run in Production, where the pages hide what only a developer should see. The publish reuses the build the project
    /// reference made and happens once per template and run. With <c>KLYROO_TEMPLATE_PUBLISHED</c> set this is the same as <see cref="StartInfo"/>.
    /// </summary>
    public static async Task<ProcessStartInfo> PublishedStartInfoAsync(string template, string host)
    {
        if (IsPublished) return StartInfo(template, host);
        var directory = await Publishes.GetOrAdd(template, name => new Lazy<Task<string>>(() => PublishAsync(name))).Value;
        var assembly = Path.GetFileName(Projects[template]) + ".dll";
        var start = new ProcessStartInfo("dotnet", [Path.Combine(directory, assembly), "--urls", host]) { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true };
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        return start;
    }

    /// <summary>Deletes the publish folders of this run.</summary>
    public static void CleanUp()
    {
        if (!Directory.Exists(PublishRoot)) return;
        try
        {
            Directory.Delete(PublishRoot, recursive: true);
        }
        catch (IOException)
        {
            // A process still holds a file: the folder is in the temporary folder of the machine.
        }
    }

    private static async Task<string> PublishAsync(string template)
    {
        var project = Path.Combine(RepositoryRoot(), "templates", template, Projects[template]);
        var output = Path.Combine(PublishRoot, template);
        var start = new ProcessStartInfo("dotnet", ["publish", project, "-c", Configuration(), "--no-build", "-o", output, "--nologo"]) { RedirectStandardOutput = true, RedirectStandardError = true };
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start dotnet publish.");
        var log = new List<string>();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.Add(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.Add(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            string text;
            lock (log) text = string.Join("\n", log);
            throw new InvalidOperationException($"dotnet publish of the {template} template exited with {process.ExitCode}:\n{text}");
        }
        return output;
    }

    private static string Configuration() =>
        Metadata("BuildConfiguration") ?? "Debug";

    private static string RepositoryRoot() =>
        Metadata("RepositoryRoot") ?? throw new InvalidOperationException("The test assembly does not know the root of the repository.");

    private static string? Metadata(string key) =>
        typeof(TemplateApp).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == key)?.Value;
}
