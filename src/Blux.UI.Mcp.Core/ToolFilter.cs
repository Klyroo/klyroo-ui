using ModelContextProtocol.Server;

namespace Blux.UI.Mcp;

/// <summary>
/// Restricts the tools a server exposes via the <c>X-MCP-Tools</c> header: a comma-separated list of
/// tool names. The HTTP endpoint reads the header on every request; the stdio tool reads <c>--tools</c>.
/// </summary>
public static class ToolFilter
{
    public const string HeaderName = "X-MCP-Tools";

    /// <summary>The names in a header value or option, trimmed and case-insensitive; null when there is no restriction.</summary>
    public static HashSet<string>? Parse(IEnumerable<string?>? values)
    {
        if (values is null) return null;
        var names = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .SelectMany(v => v!.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return names.Count == 0 ? null : names;
    }

    public static HashSet<string>? Parse(string? value) => Parse([value]);

    /// <summary>Removes from the server options every tool whose name is not in <paramref name="allowed"/>. Null keeps them all.</summary>
    public static void Apply(McpServerOptions options, ISet<string>? allowed)
    {
        if (allowed is null || options.ToolCollection is not { } tools) return;
        foreach (var tool in tools.ToArray())
        {
            if (!allowed.Contains(tool.ProtocolTool.Name)) tools.Remove(tool);
        }
    }

    /// <summary>The names in <paramref name="requested"/> that no tool of this server has, to report typos.</summary>
    public static IReadOnlyList<string> Unknown(IEnumerable<string> requested) =>
        [.. requested.Where(name => !DocsTools.Names.Contains(name, StringComparer.OrdinalIgnoreCase))];
}
