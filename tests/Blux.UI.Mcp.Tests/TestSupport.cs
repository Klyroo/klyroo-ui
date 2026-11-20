using Blux.UI.Mcp.Model;

namespace Blux.UI.Mcp.Tests;

internal static class TestSupport
{
    /// <summary>The bundle the build embedded: what the tool and the docs site serve.</summary>
    public static DocsBundle Bundle => DocsBundleLoader.Embedded;

    public static DocsService Service { get; } = new(DocsBundleLoader.Embedded);

    public static DocsTools Tools { get; } = new(Service);

    public static string RepoRoot { get; } = FindRepoRoot();

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Blux.UI.slnx"))) return dir.FullName;
        }
        throw new DirectoryNotFoundException("Blux.UI.slnx not found above the test output directory.");
    }

    /// <summary>The built stdio tool, next to the configuration of this test assembly.</summary>
    public static string ToolAssembly()
    {
        var configuration = new DirectoryInfo(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory)).Parent!.Name; // bin/<Configuration>/net10.0
        var path = Path.Combine(RepoRoot, "src", "Blux.UI.Mcp", "bin", configuration, "net10.0", "Blux.UI.Mcp.dll");
        Assert.True(File.Exists(path), $"The tool is not built: {path}");
        return path;
    }
}
