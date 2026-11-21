using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>
/// The CSS of Blux.UI.Tools is generated from its themes and committed. These tests fail when a theme changed without running
/// <c>npm run build</c>, or when Tailwind could not generate a class (typo, unsupported syntax).
/// </summary>
public class GeneratedAssetsTests
{
    private static readonly string PackageRoot = FindPackageRoot();

    private static IReadOnlyList<string> ExpectedClasses()
    {
        var options = new BluxOptions();
        var tools = options.Extension<BluxToolsOptions>();
        return typeof(BluxToolsOptions).GetProperties()
            .Where(p => p.PropertyType == typeof(Blux.UI.Theming.ComponentTheme))
            .Select(p => (Blux.UI.Theming.ComponentTheme)p.GetValue(tools)!)
            .SelectMany(t => t.AllClassTokens())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    [Fact]
    public void Class_list_is_up_to_date_with_the_themes()
    {
        var committed = File.ReadAllLines(Path.Combine(PackageRoot, "styles", "generated", "classes.txt")).Where(l => l.Length > 0).ToList();

        Assert.Equal(ExpectedClasses(), committed);
    }

    [Fact]
    public void Tailwind_theme_css_lists_the_classes_and_only_them()
    {
        var css = File.ReadAllText(Path.Combine(PackageRoot, "wwwroot", "blux-tools.theme.css"));

        Assert.Contains($"@source inline(\"{string.Join(' ', ExpectedClasses())}\");", css);
        // The tokens come from blux.theme.css of Blux.UI: repeating them would define them twice.
        Assert.DoesNotContain("@theme", css);
        Assert.DoesNotContain("@custom-variant", css);
    }

    [Fact]
    public void Every_theme_class_exists_in_the_compiled_stylesheet()
    {
        var css = File.ReadAllText(Path.Combine(PackageRoot, "wwwroot", "blux-tools.css"));

        var missing = ExpectedClasses().Where(c => !css.Contains("." + CssEscape(c), StringComparison.Ordinal)).ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void The_compiled_stylesheet_has_no_second_copy_of_the_tokens_of_Blux_UI()
    {
        var css = File.ReadAllText(Path.Combine(PackageRoot, "wwwroot", "blux-tools.css"));

        // blux.css defines the --blux-* variables; defining them again here, after it, would undo a rebrand made between the two.
        Assert.DoesNotContain("--blux-primary:", css);
    }

    [Fact]
    public void The_token_mapping_is_the_one_of_Blux_UI()
    {
        var tokens = File.ReadAllText(Path.Combine(PackageRoot, "styles", "blux-tools.tokens.css")).Replace("\r\n", "\n");
        var body = tokens[tokens.IndexOf("@custom-variant", StringComparison.Ordinal)..].TrimEnd();
        var core = File.ReadAllText(Path.Combine(PackageRoot, "..", "Blux.UI", "styles", "blux.base.css")).Replace("\r\n", "\n");

        Assert.Contains(body, core);
    }

    private static string CssEscape(string className)
    {
        var sb = new StringBuilder(className.Length * 2);
        foreach (var ch in className)
        {
            if (char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' || ch > 0x7f)
            {
                sb.Append(ch);
            }
            else
            {
                sb.Append('\\').Append(ch);
            }
        }
        return sb.ToString();
    }

    private static string FindPackageRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Blux.UI.Tools");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }
        throw new DirectoryNotFoundException("src/Blux.UI.Tools not found above the test output directory.");
    }
}
