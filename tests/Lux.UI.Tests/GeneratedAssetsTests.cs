using System.Text;
using Lux.UI.Theming;

namespace Lux.UI.Tests;

/// <summary>
/// The CSS is generated from the themes and committed. These tests fail when a theme changed without
/// running <c>npm run build</c>, or when Tailwind could not generate a class (typo, unsupported syntax).
/// </summary>
public class GeneratedAssetsTests
{
    private static readonly string LibraryRoot = FindLibraryRoot();

    [Fact]
    public void Class_list_is_up_to_date_with_the_themes()
    {
        var expected = ThemeCatalog.AllClassTokens(new LuxOptions());
        var committed = File.ReadAllLines(Path.Combine(LibraryRoot, "styles", "generated", "classes.txt"))
            .Where(l => l.Length > 0)
            .ToList();

        Assert.Equal(expected, committed);
    }

    [Fact]
    public void Every_theme_class_exists_in_the_compiled_stylesheet()
    {
        var css = File.ReadAllText(Path.Combine(LibraryRoot, "wwwroot", "lux.css"));

        var missing = ThemeCatalog.AllClassTokens(new LuxOptions())
            .Where(c => !css.Contains("." + CssEscape(c), StringComparison.Ordinal))
            .ToList();

        Assert.Empty(missing);
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

    private static string FindLibraryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Lux.UI");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }
        throw new DirectoryNotFoundException("src/Lux.UI not found above the test output directory.");
    }
}
