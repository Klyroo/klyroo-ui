using System.Text;

namespace Blux.UI.Mcp;

/// <summary>Splits the Markdown of a guide at its <c>##</c> headings (headings inside code fences do not count).</summary>
public static class MarkdownSections
{
    /// <summary>The text before the first <c>##</c> heading (heading ""), then one entry per <c>##</c> section.</summary>
    public static IReadOnlyList<(string Heading, string Text)> Split(string markdown)
    {
        var sections = new List<(string Heading, string Text)>();
        var heading = "";
        var text = new StringBuilder();
        var fenced = false;
        foreach (var line in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.StartsWith("```", StringComparison.Ordinal)) fenced = !fenced;
            if (!fenced && line.StartsWith("## ", StringComparison.Ordinal))
            {
                sections.Add((heading, text.ToString().Trim('\n')));
                heading = line[3..].Trim();
                text.Clear();
                continue;
            }
            text.Append(line).Append('\n');
        }
        sections.Add((heading, text.ToString().Trim('\n')));
        return sections;
    }

    /// <summary>The introduction and the sections whose heading contains one of <paramref name="headings"/> (case-insensitive), or null for none.</summary>
    public static string? Filter(string markdown, IReadOnlyCollection<string> headings)
    {
        var picked = Split(markdown)
            .Where(s => s.Heading.Length > 0 && headings.Any(h => s.Heading.Contains(h, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (picked.Count == 0) return null;
        return string.Join("\n\n", picked.Select(s => $"## {s.Heading}\n\n{s.Text}".TrimEnd())) + "\n";
    }

    public static IReadOnlyList<string> Headings(string markdown) =>
        [.. Split(markdown).Where(s => s.Heading.Length > 0).Select(s => s.Heading)];

    /// <summary>The fragment of a heading, as the docs site would link it.</summary>
    public static string Anchor(string heading) =>
        string.Join('-', heading.ToLowerInvariant().Split([' '], StringSplitOptions.RemoveEmptyEntries).Select(w => new string([.. w.Where(char.IsLetterOrDigit)])).Where(w => w.Length > 0));
}
