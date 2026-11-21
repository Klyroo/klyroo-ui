using Blux.UI.ClassExtractor;
using Blux.UI.Theming;

namespace Blux.UI.Tests;

/// <summary>The class extractor end to end: its arguments, the files it writes and what it refuses.</summary>
public class ClassExtractorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "blux-extractor-" + Guid.NewGuid().ToString("N"));
    private static readonly string TestAssembly = typeof(GaugeOptions).Assembly.Location;
    private static readonly string GaugeName = typeof(GaugeOptions).FullName!;

    public ClassExtractorTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "styles"));
        File.WriteAllText(Path.Combine(_root, "styles", "blux.base.css"), "@theme { --x: 1; }\n");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private (int Code, string Out, string Err) Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var code = Extractor.Run([_root, .. args], output, error);
        return (code, output.ToString(), error.ToString());
    }

    private string Read(params string[] path) => File.ReadAllText(Path.Combine([_root, .. path]));

    [Fact]
    public void Without_extensions_it_writes_the_core_classes_and_the_base_css_as_before()
    {
        var result = Run();

        Assert.Equal(0, result.Code);
        var expected = ThemeCatalog.AllClassTokens(new BluxOptions());
        Assert.Equal(expected, Read("styles", "generated", "classes.txt").Split('\n', StringSplitOptions.RemoveEmptyEntries));
        var css = Read("wwwroot", "blux.theme.css");
        Assert.Contains("@theme { --x: 1; }", css);
        Assert.EndsWith($"@source inline(\"{string.Join(' ', expected)}\");\n", css);
        Assert.Equal($"{expected.Count} classes written.", result.Out.Trim());
    }

    [Fact]
    public void Only_extensions_writes_the_classes_of_the_named_options_and_a_css_with_only_the_source()
    {
        var result = Run("--assembly", TestAssembly, "--extension", GaugeName, "--only-extensions", "--theme-css", "blux-gauge.theme.css");

        Assert.Equal(0, result.Code);
        Assert.Equal(
            ["absolute", "bg-blue-500", "bg-red-500", "h-4", "inline-flex", "relative", "text-xs"],
            Read("styles", "generated", "classes.txt").Split('\n', StringSplitOptions.RemoveEmptyEntries));
        var css = Read("wwwroot", "blux-gauge.theme.css");
        Assert.DoesNotContain("@theme", css);
        Assert.DoesNotContain("--x", css);
        Assert.Contains("@source inline(\"absolute bg-blue-500 bg-red-500 h-4 inline-flex relative text-xs\");", css);
        Assert.Single(css.Split("@source", StringSplitOptions.None)[1..]);
        Assert.False(File.Exists(Path.Combine(_root, "wwwroot", "blux.theme.css")));
        Assert.Equal("7 classes written.", result.Out.Trim());
    }

    [Fact]
    public void Only_the_named_types_are_registered_not_every_options_class_of_the_assembly()
    {
        var result = Run("--assembly", TestAssembly, "--extension", GaugeName, "--only-extensions");

        Assert.Equal(0, result.Code);
        Assert.DoesNotContain("gap-9", Read("styles", "generated", "classes.txt"));
    }

    [Fact]
    public void An_extension_can_be_given_by_assembly_qualified_name_and_several_can_be_combined_with_the_core()
    {
        var result = Run("--extension", typeof(GaugeOptions).AssemblyQualifiedName!, "--extension", typeof(OtherExtensionOptions).AssemblyQualifiedName!);

        Assert.Equal(0, result.Code);
        var tokens = Read("styles", "generated", "classes.txt").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("gap-9", tokens);
        Assert.Contains("bg-red-500", tokens);
        Assert.Contains("inline-flex", tokens);
        Assert.Contains("@theme { --x: 1; }", Read("wwwroot", "blux.theme.css"));
    }

    [Theory]
    [InlineData("No.Such.Type")]
    [InlineData("Blux.UI.BxRadioGroup`1")]
    [InlineData("Blux.UI.BluxOptions")]
    [InlineData("System.IO.Stream")]
    public void An_unknown_open_generic_or_unusable_type_is_an_error_and_not_a_crash(string type)
    {
        var result = Run("--assembly", TestAssembly, "--extension", type);

        Assert.Equal(1, result.Code);
        Assert.NotEmpty(result.Err);
        Assert.False(Directory.Exists(Path.Combine(_root, "wwwroot")));
    }

    [Theory]
    [InlineData("--only-extensions")]
    [InlineData("--assembly")]
    public void Options_that_need_an_extension_fail_without_one(string option)
    {
        var args = option == "--assembly" ? new[] { option, TestAssembly } : new[] { option };

        var result = Run(args);

        Assert.Equal(1, result.Code);
        Assert.Contains("usage:", result.Err);
    }

    [Fact]
    public void The_package_root_is_required()
    {
        var error = new StringWriter();

        Assert.Equal(1, Extractor.Run([], new StringWriter(), error));
        Assert.Contains("usage:", error.ToString());
    }
}
