using Lux.UI.Build;
using Lux.UI.Tests.Fonts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Lux.UI.Tests.Icons;

/// <summary>What the bundle does with the answers of a third party: sanitizes them, tells a typo from an outage, and never lets the cache cost an icon.</summary>
public sealed class IconBundleHardeningTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "blux-icons-hard-" + Guid.NewGuid().ToString("N"));
    private string Cache => Path.Combine(_root, "cache");

    public IconBundleHardeningTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, true);

    private IconBuildOptions Options(string source = "", params string[] explicitNames)
    {
        var options = new IconBuildOptions { CacheDir = Cache };
        if (source.Length > 0) options.Sources.Add(new IconSourceFile("App.razor", source));
        options.Explicit.AddRange(explicitNames);
        return options;
    }

    private static FakeNetwork Net() => new() { UnknownUrlAnswers200With404 = true };

    private static string Url(string prefix, params string[] names) => $"https://api.iconify.design/{prefix}.json?icons={string.Join(",", names)}";

    private static string Answer(string prefix, string name, string body, string? extra = null) =>
        "{\"prefix\":\"" + prefix + "\",\"width\":24,\"height\":24,\"icons\":{\"" + name + "\":{\"body\":" + Json(body) + "}}" + extra + "}";

    private static string Json(string text) => IconBundleBuilder.Quote(text, '"');

    // 1. Sanitizing

    [Fact]
    public void An_api_body_is_sanitized_before_it_is_compiled_in()
    {
        var network = Net();
        network.Texts[Url("ph", "evil")] = Answer("ph", "evil", "<path d=\"M0 0h4\" onload=\"alert(1)\"/><image href=\"https://evil.example/x.png\"/><a href=\"javascript:alert(1)\"><circle r=\"2\"/></a><script>alert(1)</script>");

        var result = IconBundleBuilder.Run(Options("", "ph:evil"), network);

        Assert.Empty(result.Problems);
        Assert.Contains("\"<path d=\\\"M0 0h4\\\"/><circle r=\\\"2\\\"/>\"", result.Source);
        Assert.DoesNotContain("alert", result.Source);
        Assert.DoesNotContain("evil.example", result.Source);
    }

    [Fact]
    public void A_cached_body_written_before_the_sanitizer_existed_is_sanitized_too_even_offline()
    {
        Directory.CreateDirectory(Path.Combine(Cache, "ph"));
        File.WriteAllText(Path.Combine(Cache, "ph", "old.json"), "{\"body\":\"<path d=\\\"M0 0\\\" onclick=\\\"alert(1)\\\"/><script>alert(1)</script>\",\"viewBox\":\"0 0 24 24\"}");
        var options = Options("", "ph:old");
        options.Offline = true;

        var result = IconBundleBuilder.Run(options, Net());

        Assert.Empty(result.Problems);
        Assert.Contains("\"<path d=\\\"M0 0\\\"/>\"", result.Source);
        Assert.DoesNotContain("alert", result.Source);
    }

    [Fact]
    public void A_body_that_cannot_be_read_as_svg_is_left_out_and_reported()
    {
        var network = Net();
        network.Texts[Url("ph", "broken")] = Answer("ph", "broken", "<path d=\"M0 0\" onerror=alert(1)/>");
        network.Texts[Url("ph", "empty")] = Answer("ph", "empty", "<script>alert(1)</script>");

        var result = IconBundleBuilder.Run(Options("", "ph:broken", "ph:empty"), network);

        Assert.Equal(0, result.IconCount);
        Assert.Equal(2, result.Problems.Count);
        Assert.All(result.Problems, p => Assert.True(p.Fatal));
        Assert.DoesNotContain("LuxIconBundle.Add", result.Source);
    }

    [Fact]
    public void A_folder_svg_is_sanitized_through_the_same_code()
    {
        var folder = Path.Combine(_root, "icons");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "logo.svg"), "<svg viewBox=\"0 0 8 8\"><path d=\"M0 0\" onclick=\"alert(1)\"/><use href=\"https://evil.example/a.svg#x\"/></svg>");
        var options = Options();
        options.Folders.Add(new IconFolder(folder, "custom"));

        var result = IconBundleBuilder.Run(options, Net());

        Assert.Empty(result.Problems);
        Assert.Contains("\"<path d=\\\"M0 0\\\"/>\"", result.Source);
    }

    // 2. A collection that does not exist

    [Fact]
    public void A_collection_the_api_answers_with_200_and_the_body_404_is_a_typo_not_an_outage()
    {
        var result = IconBundleBuilder.Run(Options("", "phh:house"), Net());

        var problem = Assert.Single(result.Problems);
        Assert.True(problem.Fatal);
        Assert.Contains("phh:house", problem.Message);
        Assert.Contains("is not a collection", problem.Message);
        Assert.DoesNotContain("Could not reach", problem.Message);
        Assert.DoesNotContain("without network", problem.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void An_unknown_collection_is_remembered_for_the_cache_window(bool httpStatus404)
    {
        var network = httpStatus404 ? new FakeNetwork() : Net();
        IconBundleBuilder.Run(Options("", "phh:house"), network);
        Assert.Single(network.Requests);
        network.Requests.Clear();

        var again = IconBundleBuilder.Run(Options("", "phh:house", "phh:home"), network);

        Assert.Empty(network.Requests);
        Assert.Equal(2, again.Problems.Count);
        Assert.All(again.Problems, p => Assert.Contains("is not a collection", p.Message));
    }

    [Fact]
    public void The_memory_of_an_unknown_collection_expires_and_an_offline_build_trusts_it()
    {
        var network = Net();
        IconBundleBuilder.Run(Options("", "phh:house"), network);
        var marker = Path.Combine(Cache, "phh", "_collection.json");
        Assert.True(File.Exists(marker));
        File.SetLastWriteTimeUtc(marker, DateTime.UtcNow.AddDays(-400));
        network.Requests.Clear();

        var offline = Options("", "phh:house");
        offline.Offline = true;
        IconBundleBuilder.Run(offline, network);
        Assert.Empty(network.Requests);

        network.Texts[Url("phh", "house")] = Answer("phh", "house", "<g/>");
        var online = IconBundleBuilder.Run(Options("", "phh:house"), network);

        Assert.Single(network.Requests);
        Assert.Empty(online.Problems);
        Assert.Equal(1, online.IconCount);
    }

    [Fact]
    public void An_outage_is_still_an_outage_and_nothing_is_remembered()
    {
        var result = IconBundleBuilder.Run(Options("", "ph:house"), new FakeNetwork { Offline = true });

        Assert.Contains("Could not reach", Assert.Single(result.Problems).Message);
        Assert.False(Directory.Exists(Path.Combine(Cache, "ph")));
    }

    // 3. The cache never costs an icon

    [Fact]
    public void A_cache_that_cannot_be_written_warns_and_keeps_the_icon()
    {
        var blocker = Path.Combine(_root, "blocker");
        File.WriteAllText(blocker, "a file where the cache folder should be");
        var network = Net();
        network.Texts[Url("ph", "house")] = Answer("ph", "house", "<path d=\"M1 1\"/>");
        var options = Options("", "ph:house");
        options.CacheDir = Path.Combine(blocker, "cache");

        var result = IconBundleBuilder.Run(options, network);

        Assert.Empty(result.Problems);
        Assert.Equal(1, result.IconCount);
        Assert.Contains("LuxIconBundle.Add(\"ph\", \"house\"", result.Source);
        Assert.NotNull(result.CacheWarning);
        Assert.Contains("cannot be written", result.CacheWarning);
    }

    [Fact]
    public void A_failed_cache_write_leaves_no_temporary_file()
    {
        var network = Net();
        network.Texts[Url("ph", "house")] = Answer("ph", "house", "<g/>");
        Directory.CreateDirectory(Path.Combine(Cache, "ph"));
        // A directory where the icon file goes: the move cannot succeed.
        Directory.CreateDirectory(Path.Combine(Cache, "ph", "house.json", "keep"));

        var result = IconBundleBuilder.Run(Options("", "ph:house"), network);

        Assert.Empty(result.Problems);
        Assert.Equal(1, result.IconCount);
        Assert.Empty(Directory.GetFiles(Path.Combine(Cache, "ph"), "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public void Two_builds_writing_the_same_cache_file_both_succeed()
    {
        var network = Net();
        network.Texts[Url("ph", "house")] = Answer("ph", "house", "<g/>");

        var results = new IconBuildResult[8];
        Parallel.For(0, results.Length, i => results[i] = IconBundleBuilder.Run(Options("", "ph:house"), network));

        Assert.All(results, r => { Assert.Empty(r.Problems); Assert.Equal(1, r.IconCount); Assert.Null(r.CacheWarning); });
        Assert.Empty(Directory.GetFiles(Path.Combine(Cache, "ph"), "*.tmp"));
        Assert.Contains("\"body\"", File.ReadAllText(Path.Combine(Cache, "ph", "house.json")));
    }

    // 4. Incomplete answers

    [Fact]
    public void An_answer_that_neither_has_the_icon_nor_lists_it_as_not_found_is_a_failed_download()
    {
        var network = Net();
        network.Texts[Url("ph", "house")] = "{}";

        var result = IconBundleBuilder.Run(Options("", "ph:house"), network);

        var problem = Assert.Single(result.Problems);
        Assert.True(problem.Fatal);
        Assert.Contains("incomplete", problem.Message);
        Assert.False(File.Exists(Path.Combine(Cache, "ph", "house.json")));
    }

    [Fact]
    public void An_incomplete_answer_is_not_remembered_so_the_next_build_asks_again()
    {
        var network = Net();
        network.Texts[Url("ph", "house")] = "{\"prefix\":\"ph\",\"icons\":{}}";
        IconBundleBuilder.Run(Options("", "ph:house"), network);
        network.Requests.Clear();
        network.Texts[Url("ph", "house")] = Answer("ph", "house", "<g/>");

        var again = IconBundleBuilder.Run(Options("", "ph:house"), network);

        Assert.Single(network.Requests);
        Assert.Empty(again.Problems);
    }

    [Fact]
    public void Only_the_names_listed_in_not_found_are_remembered_as_missing()
    {
        var network = Net();
        network.Texts[Url("ph", "ghost", "house", "other")] = "{\"prefix\":\"ph\",\"width\":24,\"height\":24,\"icons\":{\"house\":{\"body\":\"<g/>\"}},\"not_found\":[\"ghost\"]}";

        var result = IconBundleBuilder.Run(Options("", "ph:house", "ph:ghost", "ph:other"), network);

        Assert.Equal(1, result.IconCount);
        Assert.Equal(2, result.Problems.Count);
        Assert.Contains("\"missing\":true", File.ReadAllText(Path.Combine(Cache, "ph", "ghost.json")));
        Assert.False(File.Exists(Path.Combine(Cache, "ph", "other.json")));
    }

    [Fact]
    public void An_incomplete_answer_falls_back_to_a_stale_cache_entry()
    {
        var network = Net();
        network.Texts[Url("ph", "house")] = Answer("ph", "house", "<g id=\"kept\"/>");
        IconBundleBuilder.Run(Options("", "ph:house"), network);
        File.SetLastWriteTimeUtc(Path.Combine(Cache, "ph", "house.json"), DateTime.UtcNow.AddDays(-400));
        network.Texts[Url("ph", "house")] = "{}";

        var result = IconBundleBuilder.Run(Options("", "ph:house"), network);

        Assert.Empty(result.Problems);
        Assert.Contains("kept", result.Source);
    }

    // 7. The cache format

    [Fact]
    public void The_cache_holds_the_unsanitized_answer_with_a_format_version()
    {
        var network = Net();
        network.Texts[Url("ph", "house")] = Answer("ph", "house", "<path d=\"M0 0\" onclick=\"alert(1)\"/>");
        IconBundleBuilder.Run(Options("", "ph:house"), network);

        var cached = File.ReadAllText(Path.Combine(Cache, "ph", "house.json"));

        Assert.Contains("\"v\":" + IconBundleBuilder.CacheFormat, cached);
        Assert.Contains("onclick", cached);
    }

    [Fact]
    public void A_cache_entry_of_an_older_format_is_asked_again_when_the_network_is_up_and_used_when_it_is_not()
    {
        Directory.CreateDirectory(Path.Combine(Cache, "ph"));
        var file = Path.Combine(Cache, "ph", "house.json");
        File.WriteAllText(file, "{\"body\":\"<g id=\\\"old\\\"/>\",\"viewBox\":\"0 0 24 24\"}");
        var network = Net();
        network.Texts[Url("ph", "house")] = Answer("ph", "house", "<g id=\"new\"/>");

        var online = IconBundleBuilder.Run(Options("", "ph:house"), network);

        Assert.Single(network.Requests);
        Assert.Contains("new", online.Source);
        Assert.Contains("\"v\":" + IconBundleBuilder.CacheFormat, File.ReadAllText(file));

        File.WriteAllText(file, "{\"body\":\"<g id=\\\"old\\\"/>\",\"viewBox\":\"0 0 24 24\"}");
        var offline = Options("", "ph:house");
        offline.Offline = true;
        var used = IconBundleBuilder.Run(offline, Net());

        Assert.Contains("old", used.Source);
        Assert.Empty(used.Problems);
    }

    [Fact]
    public void A_missing_marker_of_the_first_format_is_not_trusted()
    {
        // The first format cached {"missing":true} for any name an answer lacked, including answers that were incomplete.
        Directory.CreateDirectory(Path.Combine(Cache, "ph"));
        File.WriteAllText(Path.Combine(Cache, "ph", "house.json"), "{\"missing\":true}");
        var network = Net();
        network.Texts[Url("ph", "house")] = Answer("ph", "house", "<g/>");

        var result = IconBundleBuilder.Run(Options("", "ph:house"), network);

        Assert.Empty(result.Problems);
        Assert.Equal(1, result.IconCount);
    }

    // 6. C# escaping

    [Fact]
    public void Line_terminators_of_every_kind_are_escaped_so_the_generated_file_compiles()
    {
        var network = Net();
        network.Texts[Url("ph", "terminators")] = Answer("ph", "terminators", "<text>a\u2028b\u2029c\u0085de\ng</text>");

        var result = IconBundleBuilder.Run(Options("", "ph:terminators"), network);

        Assert.Equal(1, result.IconCount);
        foreach (var c in new[] { '\u2028', '\u2029', '\u0085', '\u000b', '\u000c' }) Assert.DoesNotContain(c.ToString(), result.Source);
        AssertParses(result.Source);
    }

    [Fact]
    public void The_generated_file_has_the_value_of_the_body_once_compiled()
    {
        var literal = IconBundleBuilder.Quote("a\u2028b\u2029c\u0085d \"q\" \\ \n\t\u0001", '"');
        var tree = Compile("class C { string S = " + literal + "; }");

        AssertParses(tree);
        var value = tree.GetRoot(Xunit.TestContext.Current.CancellationToken).DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax>().Single().Token.ValueText;
        Assert.Equal("a\u2028b\u2029c\u0085d \"q\" \\ \n\t\u0001", value);
    }

    private static SyntaxTree Compile(string source) => CSharpSyntaxTree.ParseText(source, cancellationToken: Xunit.TestContext.Current.CancellationToken);

    private static void AssertParses(string source) => AssertParses(Compile(source));

    private static void AssertParses(SyntaxTree tree) =>
        Assert.DoesNotContain(tree.GetDiagnostics(Xunit.TestContext.Current.CancellationToken), d => d.Severity == DiagnosticSeverity.Error);

    [Fact]
    public void A_generated_file_with_icons_and_without_parses_cleanly()
    {
        var network = Net();
        network.Texts[Url("ph", "house")] = Answer("ph", "house", "<path d=\"M0 0\"/>");

        AssertParses(IconBundleBuilder.Run(Options("", "ph:house"), network).Source);
        AssertParses(IconBundleBuilder.Run(Options(), network).Source);
    }

    [Fact]
    public void The_generated_file_announces_its_registrar_with_an_assembly_attribute()
    {
        var source = IconBundleBuilder.Run(Options(), Net()).Source;

        Assert.Contains("[assembly: global::Lux.UI.LuxIconRegistrar(typeof(global::Lux.UI.Generated.LuxIconBundleInitializer))]", source);
    }
}
