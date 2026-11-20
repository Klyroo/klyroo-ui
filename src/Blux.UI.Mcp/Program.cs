using System.Reflection;
using Blux.UI.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// blux-ui-mcp [--tools <name,name,...>] [--site-url <https://...>] [--list-tools] [--version] [--help]
//
// Speaks the Model Context Protocol over stdin/stdout, so nothing else may write to stdout: logs go to stderr.
const string Usage = """
    blux-ui-mcp: MCP server for Blux UI documentation (stdio transport).

    Usage: blux-ui-mcp [options]

    Options:
      --tools <names>     Expose only these tools, comma-separated (default: all). Same as the X-MCP-Tools header of the HTTP endpoint.
      --site-url <url>    Base address of the docs site, put in front of page paths in answers.
      --list-tools        Print the tool names and exit.
      --version           Print the version and exit.
      -h, --help          Show this help.

    Register it with an agent, for example:
      claude mcp add --transport stdio blux-ui -- blux-ui-mcp
    """;

if (args.Contains("-h") || args.Contains("--help"))
{
    Console.WriteLine(Usage);
    return 0;
}
if (args.Contains("--version"))
{
    Console.WriteLine(DocsBundleLoader.AssemblyVersion(Assembly.GetExecutingAssembly()));
    return 0;
}
if (args.Contains("--list-tools"))
{
    Console.WriteLine(string.Join('\n', DocsTools.Names));
    return 0;
}

string? Option(string name)
{
    var index = Array.IndexOf(args, name);
    if (index >= 0) return index + 1 < args.Length ? args[index + 1] : throw new ArgumentException($"{name} needs a value.");
    var prefix = name + "=";
    return args.FirstOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..];
}

var known = new[] { "--tools", "--site-url" };
foreach (var arg in args.Where(a => a.StartsWith('-')))
{
    if (!known.Contains(arg.Split('=')[0]))
    {
        Console.Error.WriteLine($"Unknown option {arg}.\n\n{Usage}");
        return 2;
    }
}

var tools = ToolFilter.Parse(Option("--tools"));
if (tools is not null && ToolFilter.Unknown(tools) is { Count: > 0 } unknown)
{
    Console.Error.WriteLine($"Unknown tool(s): {string.Join(", ", unknown)}. Available: {string.Join(", ", DocsTools.Names)}.");
    return 2;
}

var builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddBluxDocsServer(o =>
{
    o.SiteUrl = Option("--site-url");
    o.Tools = tools;
}).WithStdioServerTransport();

await builder.Build().RunAsync();
return 0;
