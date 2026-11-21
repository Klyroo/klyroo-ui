using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Blux.Templates.Content;

/// <summary>The YAML block at the top of a page. Keys are camelCase; keys that are not listed here are ignored.</summary>
public sealed class FrontMatter
{
    public string? Title { get; set; }
    public string? Description { get; set; }

    /// <summary>Position among the pages and folders next to it, smallest first. Without it a page comes after the ones that have one.</summary>
    public int? Order { get; set; }

    /// <summary>The text of the link in the navigation, when it should be shorter than the title.</summary>
    public string? NavigationTitle { get; set; }

    /// <summary>An icon name (<c>ph:rocket-launch</c>). It has to be bundled: list it in a <c>BluxIcon</c> item of the project file.</summary>
    public string? Icon { get; set; }

    /// <summary>A draft is not served, listed or searched (unless <see cref="ContentOptions.IncludeDrafts"/> is on).</summary>
    public bool Draft { get; set; }
}

/// <summary>The <c>_dir.yml</c> file of a folder: how the folder shows in the navigation.</summary>
public sealed class DirectoryMetadata
{
    public string? Title { get; set; }
    public int? Order { get; set; }
    public string? Icon { get; set; }
}

public static class FrontMatterParser
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Splits a page into its front matter and its Markdown. The front matter is the block between a first line of <c>---</c> and the next
    /// line of <c>---</c>; a file that does not start with it has none. Throws <see cref="ContentException"/> naming <paramref name="path"/>
    /// when the YAML is invalid or never closed.
    /// </summary>
    public static (FrontMatter Data, string Body) Parse(string text, string path)
    {
        text = text.TrimStart('﻿').Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = text.Split('\n');
        if (lines[0].TrimEnd() != "---")
        {
            return (new FrontMatter(), text);
        }

        var close = Array.FindIndex(lines, 1, line => line.TrimEnd() == "---");
        if (close < 0)
        {
            throw new ContentException($"{path}: the front matter is not closed: add a line with --- after it.");
        }

        var yaml = string.Join('\n', lines[1..close]);
        var body = string.Join('\n', lines[(close + 1)..]);
        return (Deserialize<FrontMatter>(yaml, path) ?? new FrontMatter(), body);
    }

    /// <summary>Reads the YAML of a <c>_dir.yml</c> file.</summary>
    public static DirectoryMetadata ParseDirectory(string yaml, string path) => Deserialize<DirectoryMetadata>(yaml, path) ?? new DirectoryMetadata();

    private static T? Deserialize<T>(string yaml, string path) where T : class
    {
        try
        {
            return string.IsNullOrWhiteSpace(yaml) ? null : Yaml.Deserialize<T>(yaml);
        }
        catch (YamlException error)
        {
            var where = error.Start.Line > 0 ? $" (line {error.Start.Line})" : "";
            throw new ContentException($"{path}: invalid YAML{where}: {error.InnerException?.Message ?? error.Message}", error);
        }
    }
}
