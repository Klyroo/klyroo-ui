using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Blux.UI.Mcp.Tests;

/// <summary>
/// The agent skill must never teach an API that does not exist: every component, record and parameter its Razor samples use
/// is checked against the library.
/// </summary>
public partial class SkillTests
{
    private static readonly string SkillDirectory = Path.Combine(TestSupport.RepoRoot, "skills", "blux-ui");

    // Blux.UI and the sister package whose components the generated reference lists.
    private static readonly Dictionary<string, Type> LibraryTypes = new[] { typeof(BxButton).Assembly, typeof(Blux.UI.Tools.BxDataListEmpty).Assembly }
        .SelectMany(a => a.GetTypes())
        .Where(t => t.IsPublic && t.Namespace is "Blux.UI" or "Blux.UI.Theming" or "Blux.UI.Tools")
        .GroupBy(t => t.Name.Split('`')[0])
        .ToDictionary(g => g.Key, g => g.First());

    public static TheoryData<string> Files()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.EnumerateFiles(SkillDirectory, "*.md", SearchOption.AllDirectories)) data.Add(Path.GetRelativePath(SkillDirectory, file));
        return data;
    }

    private static string Read(string relative) => File.ReadAllText(Path.Combine(SkillDirectory, relative)).Replace("\r\n", "\n");

    [Fact]
    public void Skill_md_has_the_frontmatter_an_agent_needs()
    {
        var text = Read("SKILL.md");

        var match = Regex.Match(text, @"^---\nname: (?<name>[^\n]+)\ndescription: (?<description>[^\n]+)\n---\n");
        Assert.True(match.Success, "SKILL.md must start with name and description frontmatter");
        Assert.Equal("blux-ui", match.Groups["name"].Value);
        Assert.InRange(match.Groups["description"].Value.Length, 60, 1024);
    }

    [Fact]
    public void Skill_md_routes_to_every_reference_file_and_only_to_files_that_exist()
    {
        var skill = Read("SKILL.md");
        var linked = ReferenceLink().Matches(skill).Select(m => m.Value).Distinct().ToList();
        var existing = Directory.EnumerateFiles(Path.Combine(SkillDirectory, "references"), "*.md").Select(f => "references/" + Path.GetFileName(f)).ToList();

        Assert.Equal(existing.Order(), linked.Order());
        string[] expected =
            ["references/components.md", "references/component-selection.md", "references/conventions.md", "references/design-system.md", "references/forms.md", "references/layouts.md", "references/recipes.md"];
        Assert.Equal(expected.Order(), existing.Order());
    }

    [Fact]
    public void Skill_md_sends_the_agent_to_the_mcp_server_for_api_details()
    {
        var skill = Read("SKILL.md");

        Assert.Contains("get-component-metadata", skill);
        Assert.Contains("claude mcp add --transport stdio blux-ui -- blux-ui-mcp", skill);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Every_Bx_type_the_skill_names_exists_in_the_library(string file)
    {
        var missing = BxName().Matches(Read(file)).Select(m => m.Value).Distinct()
            .Where(name => !LibraryTypes.ContainsKey(name) && name is not ("BxIcons" or "BxColor"))
            .ToList();

        Assert.True(missing.Count == 0, $"{file} names types that do not exist: {string.Join(", ", missing)}");
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Every_Ui_record_the_skill_names_exists(string file)
    {
        var missing = UiName().Matches(Read(file)).Select(m => m.Value).Distinct().Where(name => name != "AddBluxUi" && !LibraryTypes.ContainsKey(name)).ToList();

        Assert.True(missing.Count == 0, $"{file} names Ui records that do not exist: {string.Join(", ", missing)}");
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Every_parameter_used_in_a_Razor_sample_is_a_parameter_of_that_component(string file)
    {
        var problems = new List<string>();
        foreach (var block in RazorFence().Matches(Read(file)).Select(m => m.Groups["code"].Value))
        {
            foreach (var (tag, attributes) in Tags(block))
            {
                if (!LibraryTypes.TryGetValue(tag, out var type)) continue; // reported by the type test
                var parameters = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                    .Select(p => p.Name)
                    .ToHashSet(StringComparer.Ordinal);

                foreach (var attribute in attributes)
                {
                    var name = attribute.StartsWith("@bind-", StringComparison.Ordinal) ? attribute["@bind-".Length..] : attribute;
                    var plain = name is "class" or "id" or "role" or "style" or "novalidate" or "key" or "@key" || name.StartsWith("data-") || name.StartsWith("aria-");
                    var typeArgument = type.IsGenericType && type.GetGenericTypeDefinition().GetGenericArguments().Any(a => a.Name == name);
                    if (!plain && !typeArgument && !parameters.Contains(name))
                    {
                        problems.Add($"{file}: <{tag} {attribute}> is not a parameter of {type.Name}");
                    }
                }
            }
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void The_skill_samples_use_enough_components_to_make_the_check_meaningful()
    {
        var tags = Directory.EnumerateFiles(SkillDirectory, "*.md", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(SkillDirectory, f)).SelectMany(f => RazorFence().Matches(Read(f)).SelectMany(m => Tags(m.Groups["code"].Value))).ToList();

        Assert.True(tags.Count > 80, $"only {tags.Count} component tags are checked");
        Assert.True(tags.Select(t => t.Tag).Distinct().Count() > 40);
    }

    // The tags of a Razor sample that are Blux components, with the names of their attributes. Quotes may nest inside
    // @(...) expressions, so values are read with a parenthesis depth.
    private static IEnumerable<(string Tag, List<string> Attributes)> Tags(string code)
    {
        foreach (Match start in Regex.Matches(code, @"<(Bx[A-Za-z]+)(?=[\s/>])"))
        {
            var attributes = new List<string>();
            var i = start.Index + start.Length;
            while (i < code.Length)
            {
                while (i < code.Length && char.IsWhiteSpace(code[i])) i++;
                if (i >= code.Length || code[i] == '>' || (code[i] == '/' && i + 1 < code.Length && code[i + 1] == '>')) break;
                var nameStart = i;
                while (i < code.Length && !char.IsWhiteSpace(code[i]) && code[i] != '=' && code[i] != '>' && code[i] != '/') i++;
                attributes.Add(code[nameStart..i]);
                if (i < code.Length && code[i] == '=')
                {
                    i++;
                    if (i < code.Length && code[i] is '"' or '\'')
                    {
                        var quote = code[i++];
                        var depth = 0;
                        while (i < code.Length && !(code[i] == quote && depth <= 0))
                        {
                            if (code[i] == '(') depth++;
                            else if (code[i] == ')') depth--;
                            i++;
                        }
                        i++;
                    }
                    else
                    {
                        while (i < code.Length && !char.IsWhiteSpace(code[i]) && code[i] != '>') i++;
                    }
                }
            }
            yield return (start.Groups[1].Value, attributes);
        }
    }

    [GeneratedRegex(@"references/[a-z-]+\.md")]
    private static partial Regex ReferenceLink();

    [GeneratedRegex(@"\bBx[A-Z][A-Za-z]+")]
    private static partial Regex BxName();

    [GeneratedRegex(@"\b[A-Z][A-Za-z]+Ui\b")]
    private static partial Regex UiName();

    [GeneratedRegex(@"```razor\n(?<code>.*?)\n```", RegexOptions.Singleline)]
    private static partial Regex RazorFence();
}
