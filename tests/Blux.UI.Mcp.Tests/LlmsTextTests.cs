using System.Text.RegularExpressions;

namespace Blux.UI.Mcp.Tests;

public partial class LlmsTextTests
{
    private const string Origin = "https://blux.example";

    private static string Index => LlmsText.Index(TestSupport.Bundle, Origin);

    [Fact]
    public void Index_follows_the_llms_txt_convention()
    {
        var lines = Index.Split('\n');

        Assert.Equal("# Blux UI", lines[0]);
        Assert.Equal("", lines[1]);
        Assert.StartsWith("> ", lines[2]);
        Assert.Equal(1, lines.Count(l => l.StartsWith("# ")));
        Assert.All(lines.Where(l => l.StartsWith('#')), l => Assert.True(l.StartsWith("# ") || l.StartsWith("## "), l));
        // Every list item of a section is a link with a description.
        Assert.All(lines.Where(l => l.StartsWith("- ")), l => Assert.Matches(@"^- \[[^\]]+\]\(https://blux\.example/[^)]*\)(: .+)?$", l));
    }

    [Fact]
    public void Index_stays_a_short_overview()
    {
        // Roughly 5K tokens in Nuxt UI; ours lists one line per component.
        Assert.True(Index.Length < 30_000, $"llms.txt is {Index.Length} characters");
        Assert.DoesNotContain("```", Index);
    }

    [Fact]
    public void Every_link_resolves_to_a_page_of_the_docs_site()
    {
        var slugs = TestSupport.Bundle.Components.Select(c => c.Slug).ToHashSet();
        var guides = TestSupport.Bundle.Guides.Select(g => g.Path).ToHashSet();
        var links = Link().Matches(Index).Select(m => m.Groups[1].Value).ToList();

        Assert.NotEmpty(links);
        foreach (var link in links)
        {
            Assert.StartsWith(Origin, link);
            var path = link[Origin.Length..];
            if (path.StartsWith("/components/")) Assert.Contains(path["/components/".Length..], slugs);
            else if (path == "/llms-full.txt") continue;
            else Assert.Contains(path, guides);
        }
    }

    [Fact]
    public void Index_lists_every_component_and_guide_once()
    {
        foreach (var component in TestSupport.Bundle.Components)
        {
            Assert.Single(Regex.Matches(Index, Regex.Escape($"]({Origin}/components/{component.Slug})")));
            Assert.Contains(component.Description, Index);
        }
        foreach (var guide in TestSupport.Bundle.Guides) Assert.Contains($"]({Origin}{guide.Path})", Index);
        Assert.Contains($"{Origin}/mcp", Index);
    }

    [Fact]
    public void Links_are_site_relative_without_an_origin()
    {
        var relative = LlmsText.Index(TestSupport.Bundle);

        Assert.Contains("](/components/button)", relative);
        Assert.Contains("](/docs/installation)", relative);
    }

    [Fact]
    public void Full_text_has_every_component_with_description_parameters_and_example_source()
    {
        var full = LlmsText.Full(TestSupport.Bundle, Origin);

        Assert.StartsWith("# Blux UI\n\n> ", full);
        foreach (var component in TestSupport.Bundle.Components)
        {
            Assert.Contains($"\n## {component.Name}\n", full);
            Assert.Contains(component.Description, full);
            Assert.All(component.Examples, e => Assert.Contains(e.Source.Split('\n')[0].TrimEnd(), full));
            Assert.All(component.Api, api => Assert.Contains($"### {api.Name}", full));
        }
        foreach (var guide in TestSupport.Bundle.Guides) Assert.Contains($"## {guide.Title}", full);
        Assert.Contains("`Variant`: `ButtonVariant` = `ButtonVariant.Solid`", full);
    }

    [Fact]
    public void Full_text_keeps_a_single_top_level_heading_per_section_and_code_fences_balanced()
    {
        var full = LlmsText.Full(TestSupport.Bundle, Origin);

        var fences = full.Split('\n').Count(l => l.StartsWith("```"));
        Assert.Equal(0, fences % 2);
        var h1 = full.Split('\n').Where(l => l.StartsWith("# ")).ToList();
        Assert.Equal("# Blux UI", h1[0]);
        Assert.All(h1.Skip(1), l => Assert.True(l.StartsWith("# Components: ") || TestSupport.Bundle.Guides.Any(g => l == "# " + g.Section), l));
    }

    [GeneratedRegex(@"\]\(([^)]+)\)")]
    private static partial Regex Link();
}
