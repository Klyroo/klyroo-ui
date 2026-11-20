using System.Text.Json;
using Blux.UI.BundleGenerator;
using Blux.UI.Mcp.Markdown;
using Blux.UI.Mcp.Model;

// dotnet run --project build/Blux.UI.DocsBundle -- [--repo <dir>] [--out <docs-bundle.json>] [--skill-reference]
//
//   --out              writes the docs bundle (the build of src/Blux.UI.Mcp.Core does this and embeds the result).
//   --skill-reference  rewrites skills/blux-ui/references/components.md, the generated part of the agent skill.
var repo = Arg("--repo") ?? FindRepoRoot();
var output = Arg("--out");
var skill = args.Contains("--skill-reference");
if (output is null && !skill)
{
    Console.Error.WriteLine("Nothing to do: pass --out <file> and/or --skill-reference.");
    return 2;
}

var bundle = BundleBuilder.Build(repo);
if (output is not null)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
    File.WriteAllText(output, JsonSerializer.Serialize(bundle, DocsBundle.JsonOptions));
    Console.WriteLine($"docs bundle: {bundle.Components.Count} components, {bundle.Guides.Count} guides, {bundle.Icons.Count} icons -> {output}");
}
if (skill)
{
    var path = Path.Combine(repo, SkillReference.RelativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, SkillReference.Components(bundle));
    Console.WriteLine($"skill reference -> {path}");
}
return 0;

string? Arg(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static string FindRepoRoot()
{
    for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Blux.UI.slnx"))) return dir.FullName;
    }
    throw new DirectoryNotFoundException("Blux.UI.slnx not found above the current directory; pass --repo.");
}
