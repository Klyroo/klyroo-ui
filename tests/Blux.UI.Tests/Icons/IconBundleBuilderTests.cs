using Blux.UI.Build;
using Blux.UI.Tests.Fonts;

namespace Blux.UI.Tests.Icons;

public sealed class IconBundleBuilderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "blux-icons-" + Guid.NewGuid().ToString("N"));
    private string Cache => Path.Combine(_root, "cache");

    public IconBundleBuilderTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, true);

    private IconBuildOptions Options(string source = "", params string[] explicitNames)
    {
        var options = new IconBuildOptions { CacheDir = Cache };
        if (source.Length > 0) options.Sources.Add(new IconSourceFile("App.razor", source));
        options.Explicit.AddRange(explicitNames);
        return options;
    }

    private static string Set(string prefix, int size, params (string Name, string Body)[] icons) =>
        "{\"prefix\":\"" + prefix + "\",\"width\":" + size + ",\"height\":" + size + ",\"icons\":{"
        + string.Join(",", icons.Select(i => "\"" + i.Name + "\":{\"body\":\"" + i.Body.Replace("\"", "\\\"") + "\"}")) + "}}";

    /// <summary>The Iconify API as it really behaves: a collection it does not have is answered with status 200 and the body 404.</summary>
    private static FakeNetwork Net() => new() { UnknownUrlAnswers200With404 = true };

    private static string Url(string prefix, params string[] names) => $"https://api.iconify.design/{prefix}.json?icons={string.Join(",", names)}";

    [Fact]
    public void Downloads_the_icons_found_in_the_sources_and_registers_them()
    {
        var network = Net();
        network.Texts[Url("ph", "house")] = Set("ph", 256, ("house", "<path d='M1 1'/>"));
        network.Texts[Url("mdi", "home")] = Set("mdi", 24, ("home", "<path d='M2 2'/>"));

        var result = IconBundleBuilder.Run(Options("""<BxIcon Name="ph:house" /> <BxButton LeadingIcon="i-mdi-home" />"""), network);

        Assert.Empty(result.Problems);
        Assert.Equal(2, result.IconCount);
        Assert.Contains("global::Blux.UI.BluxIconBundle.Add(\"mdi\", \"home\", \"<path d=\\\"M2 2\\\"/>\", \"0 0 24 24\");", result.Source);
        Assert.Contains("global::Blux.UI.BluxIconBundle.Add(\"ph\", \"house\", \"<path d=\\\"M1 1\\\"/>\", \"0 0 256 256\");", result.Source);
        Assert.Contains("[global::System.Runtime.CompilerServices.ModuleInitializer]", result.Source);
    }

    [Fact]
    public void The_generated_file_is_sorted_so_the_build_is_reproducible()
    {
        var network = Net();
        network.Texts[Url("ph", "b-icon")] = Set("ph", 24, ("b-icon", "<g/>"));
        network.Texts[Url("mdi", "a-icon")] = Set("mdi", 24, ("a-icon", "<g/>"));
        var first = IconBundleBuilder.Run(Options("", "ph:b-icon", "mdi:a-icon"), network).Source;
        var second = IconBundleBuilder.Run(Options("", "mdi:a-icon", "ph:b-icon"), network).Source;

        Assert.Equal(first, second);
        Assert.True(first.IndexOf("\"mdi\"", StringComparison.Ordinal) < first.IndexOf("\"ph\"", StringComparison.Ordinal));
    }

    [Fact]
    public void A_second_build_uses_the_cache_and_asks_for_nothing()
    {
        var network = Net();
        network.Texts[Url("ph", "house")] = Set("ph", 256, ("house", "<path d='M1 1'/>"));
        var first = IconBundleBuilder.Run(Options("", "ph:house"), network);
        Assert.Single(network.Requests);

        var offline = new FakeNetwork { Offline = true };
        var second = IconBundleBuilder.Run(Options("", "ph:house"), offline);

        Assert.Empty(offline.Requests);
        Assert.Empty(second.Problems);
        Assert.Equal(first.Source, second.Source);
    }

    [Fact]
    public void Only_the_icons_missing_from_the_cache_are_requested()
    {
        var network = Net();
        network.Texts[Url("ph", "one")] = Set("ph", 24, ("one", "<g/>"));
        IconBundleBuilder.Run(Options("", "ph:one"), network);
        network.Texts[Url("ph", "two")] = Set("ph", 24, ("two", "<g/>"));
        network.Requests.Clear();

        var result = IconBundleBuilder.Run(Options("", "ph:one", "ph:two"), network);

        Assert.Equal([Url("ph", "two")], network.Requests);
        Assert.Equal(2, result.IconCount);
    }

    [Fact]
    public void The_offline_switch_never_touches_the_network_and_names_what_is_missing()
    {
        var network = Net();
        var options = Options("", "ph:house");
        options.Offline = true;

        var result = IconBundleBuilder.Run(options, network);

        Assert.Empty(network.Requests);
        var problem = Assert.Single(result.Problems);
        Assert.True(problem.Fatal);
        Assert.Contains("ph:house", problem.Message);
        Assert.Contains("Offline", problem.Message);
    }

    [Fact]
    public void An_unreachable_network_is_a_problem_of_the_icon_not_an_exception()
    {
        var result = IconBundleBuilder.Run(Options("", "ph:house"), new FakeNetwork { Offline = true });

        var problem = Assert.Single(result.Problems);
        Assert.True(problem.Fatal);
        Assert.Contains("Could not reach", problem.Message);
        Assert.Contains(Cache, problem.Message);
    }

    [Fact]
    public void A_cached_icon_older_than_the_limit_is_still_used_when_the_network_fails()
    {
        var network = Net();
        network.Texts[Url("ph", "house")] = Set("ph", 24, ("house", "<g/>"));
        IconBundleBuilder.Run(Options("", "ph:house"), network);
        var file = Path.Combine(Cache, "ph", "house.json");
        File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddDays(-400));

        var result = IconBundleBuilder.Run(Options("", "ph:house"), new FakeNetwork { Offline = true });

        Assert.Empty(result.Problems);
        Assert.Equal(1, result.IconCount);
    }

    [Fact]
    public void A_stale_cached_icon_is_asked_again_when_the_network_is_up()
    {
        var network = Net();
        network.Texts[Url("ph", "house")] = Set("ph", 24, ("house", "<g id='old'/>"));
        IconBundleBuilder.Run(Options("", "ph:house"), network);
        File.SetLastWriteTimeUtc(Path.Combine(Cache, "ph", "house.json"), DateTime.UtcNow.AddDays(-400));
        network.Texts[Url("ph", "house")] = Set("ph", 24, ("house", "<g id='new'/>"));

        var result = IconBundleBuilder.Run(Options("", "ph:house"), network);

        Assert.Contains("id=\\\"new\\\"", result.Source);
    }

    [Fact]
    public void An_icon_the_collection_does_not_have_is_reported_where_it_is_written()
    {
        var network = Net();
        network.Texts[Url("ph", "nope")] = "{\"prefix\":\"ph\",\"width\":24,\"height\":24,\"icons\":{},\"not_found\":[\"nope\"]}";

        var result = IconBundleBuilder.Run(Options("a\n<BxIcon Name=\"ph:nope\" />"), network);

        var problem = Assert.Single(result.Problems);
        Assert.True(problem.Fatal);
        Assert.Equal("App.razor", problem.File);
        Assert.Equal(2, problem.Line);
        Assert.Contains("ph:nope", problem.Message);
        Assert.Equal(0, result.IconCount);
    }

    [Fact]
    public void An_icon_that_does_not_exist_is_remembered_so_the_next_build_does_not_ask()
    {
        var network = Net();
        network.Texts[Url("ph", "nope")] = "{\"prefix\":\"ph\",\"icons\":{},\"not_found\":[\"nope\"]}";
        IconBundleBuilder.Run(Options("", "ph:nope"), network);
        network.Requests.Clear();

        var again = IconBundleBuilder.Run(Options("", "ph:nope"), network);

        Assert.Empty(network.Requests);
        Assert.Single(again.Problems);
    }

    [Fact]
    public void A_bare_or_standalone_name_that_is_not_found_is_not_fatal()
    {
        var network = Net();
        network.Texts[Url("lucide", "brand-thing")] = "{\"prefix\":\"lucide\",\"icons\":{},\"not_found\":[\"brand-thing\"]}";
        network.Texts[Url("ph", "ghost")] = "{\"prefix\":\"ph\",\"icons\":{},\"not_found\":[\"ghost\"]}";

        var result = IconBundleBuilder.Run(Options("""<BxIcon Name="brand-thing" /> var s = "i-ph-ghost";"""), network);

        Assert.Equal(2, result.Problems.Count);
        Assert.All(result.Problems, p => Assert.False(p.Fatal));
    }

    [Fact]
    public void A_collection_the_api_does_not_know_is_reported_for_an_explicit_item()
    {
        var result = IconBundleBuilder.Run(Options("", "nowhere:thing"), Net());

        var problem = Assert.Single(result.Problems);
        Assert.True(problem.Fatal);
        Assert.Contains("not a collection", problem.Message);
    }

    [Fact]
    public void A_name_with_an_unknown_prefix_in_the_sources_is_left_alone_it_may_be_registered_at_run_time()
    {
        var network = Net();
        var result = IconBundleBuilder.Run(Options("""<BxIcon Name="brand:logo" />"""), network);

        // Attempted once, because it is a prefix:name literal, but never fatal.
        Assert.All(result.Problems, p => Assert.False(p.Fatal));
    }

    [Fact]
    public void Icons_registered_in_code_and_built_in_icons_are_not_requested()
    {
        var network = Net();
        var result = IconBundleBuilder.Run(Options("""
            o.Icons.Register("brand:logo", "<g/>");
            <BxIcon Name="brand:logo" />
            <BxIcon Name="search" /> <BxIcon Name="lucide:chevron-down" /> <BxIcon Name="i-lucide-x" />
            """), network);

        Assert.Empty(network.Requests);
        Assert.Empty(result.Problems);
        Assert.Equal(0, result.IconCount);
    }

    [Fact]
    public void A_long_list_is_split_into_requests_of_at_most_500_characters()
    {
        var names = Enumerable.Range(0, 60).Select(i => "an-icon-with-a-long-name-" + i).ToArray();
        var network = new ChunkNetwork();

        var result = IconBundleBuilder.Run(Options("", [.. names.Select(n => "ph:" + n)]), network);

        Assert.True(network.Requests.Count > 1);
        Assert.All(network.Requests, u => Assert.True(u.Length <= 500, u.Length.ToString()));
        Assert.Equal(60, result.IconCount);
    }

    [Fact]
    public void A_malformed_answer_is_a_diagnostic_and_is_not_cached()
    {
        var network = Net();
        network.Texts[Url("ph", "house")] = "<html>captive portal</html>";

        var result = IconBundleBuilder.Run(Options("", "ph:house"), network);

        var problem = Assert.Single(result.Problems);
        Assert.True(problem.Fatal);
        Assert.False(File.Exists(Path.Combine(Cache, "ph", "house.json")));
    }

    [Fact]
    public void A_corrupt_cache_file_is_asked_again()
    {
        var network = Net();
        network.Texts[Url("ph", "house")] = Set("ph", 24, ("house", "<g/>"));
        Directory.CreateDirectory(Path.Combine(Cache, "ph"));
        File.WriteAllText(Path.Combine(Cache, "ph", "house.json"), "{truncated");

        var result = IconBundleBuilder.Run(Options("", "ph:house"), network);

        Assert.Empty(result.Problems);
        Assert.Single(network.Requests);
    }

    [Fact]
    public void An_alias_is_resolved_into_the_icon_it_points_to()
    {
        var network = Net();
        network.Texts[Url("lucide", "alias-name")] =
            "{\"prefix\":\"lucide\",\"width\":24,\"height\":24,\"aliases\":{\"alias-name\":{\"parent\":\"real-name\"}},\"icons\":{\"real-name\":{\"body\":\"<path d='M5 5'/>\"}}}";

        var result = IconBundleBuilder.Run(Options("", "lucide:alias-name"), network);

        Assert.Empty(result.Problems);
        Assert.Contains("\"lucide\", \"alias-name\", \"<path d=\\\"M5 5\\\"/>\"", result.Source);
    }

    [Fact]
    public void Bodies_with_quotes_backslashes_and_line_breaks_are_escaped_in_the_generated_code()
    {
        var network = Net();
        network.Texts[Url("ph", "tricky")] = "{\"prefix\":\"ph\",\"width\":24,\"height\":24,\"icons\":{\"tricky\":{\"body\":\"<text x=\\\"1\\\">one\\ntwo \\\"q\\\" back\\\\slash\\ttab</text>\"}}}";

        var result = IconBundleBuilder.Run(Options("", "ph:tricky"), network);

        Assert.Contains("\"<text x=\\\"1\\\">one\\ntwo \\\"q\\\" back\\\\slash\\ttab</text>\"", result.Source);
        Assert.DoesNotContain("one\ntwo", result.Source);
    }

    [Fact]
    public void An_invalid_name_in_a_BluxIcon_item_is_reported()
    {
        var result = IconBundleBuilder.Run(Options("", "Not A Name"), Net());

        var problem = Assert.Single(result.Problems);
        Assert.True(problem.Fatal);
        Assert.Contains("Not A Name", problem.Message);
    }

    [Fact]
    public void A_custom_collection_bundles_every_svg_of_the_folder()
    {
        var folder = Path.Combine(_root, "icons");
        Directory.CreateDirectory(Path.Combine(folder, "brand"));
        File.WriteAllText(Path.Combine(folder, "Arrow Left.svg"), "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\"><path d=\"M1 1\"/></svg>");
        File.WriteAllText(Path.Combine(folder, "brand", "logo.svg"), "<svg width=\"40\" height=\"10\"><path d=\"M2 2\"/></svg>");
        File.WriteAllText(Path.Combine(folder, "notes.txt"), "ignored");
        var options = Options("""<BxIcon Name="i-custom-arrow-left" /> <BxIcon Name="custom:missing" />""");
        options.Folders.Add(new IconFolder(folder, "custom"));
        var network = Net();

        var result = IconBundleBuilder.Run(options, network);

        Assert.Empty(network.Requests);
        Assert.Equal(2, result.IconCount);
        Assert.Contains("Add(\"custom\", \"arrow-left\", \"<g fill=\\\"none\\\" stroke=\\\"currentColor\\\"><path d=\\\"M1 1\\\"/></g>\", \"0 0 24 24\");", result.Source);
        Assert.Contains("Add(\"custom\", \"brand-logo\", \"<path d=\\\"M2 2\\\"/>\", \"0 0 40 10\");", result.Source);
        var missing = Assert.Single(result.Problems);
        Assert.Contains("custom:missing", missing.Message);
        Assert.True(missing.Fatal);
    }

    [Fact]
    public void A_broken_svg_of_a_collection_names_the_file_and_the_reason()
    {
        var folder = Path.Combine(_root, "icons");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "bad.svg"), "<svg viewBox=\"0 0 24 24\"><style>a{}</style><path d=\"M0 0\"/></svg>");
        File.WriteAllText(Path.Combine(folder, "good.svg"), "<svg viewBox=\"0 0 24 24\"><path d=\"M0 0\"/></svg>");
        var options = Options();
        options.Folders.Add(new IconFolder(folder, "custom"));

        var result = IconBundleBuilder.Run(options, Net());

        var problem = Assert.Single(result.Problems);
        Assert.Equal(Path.Combine(folder, "bad.svg"), problem.File);
        Assert.Contains("<style>", problem.Message);
        Assert.Equal(1, result.IconCount);
    }

    [Fact]
    public void A_collection_needs_an_existing_folder_and_a_valid_prefix()
    {
        var options = Options();
        options.Folders.Add(new IconFolder(Path.Combine(_root, "nope"), "custom"));
        options.Folders.Add(new IconFolder(_root, "Bad Prefix"));

        var result = IconBundleBuilder.Run(options, Net());

        Assert.Equal(2, result.Problems.Count);
        Assert.All(result.Problems, p => Assert.True(p.Fatal));
    }

    [Theory]
    [InlineData("Arrow Left.svg", "arrow-left")]
    [InlineData("brand/logo.svg", "brand-logo")]
    [InlineData("a_b.c.svg", "a-b-c")]
    [InlineData("Double  Space.svg", "double-space")]
    public void File_names_become_icon_names(string file, string name)
    {
        Assert.Equal(name, IconBundleBuilder.FileName(file));
    }

    [Fact]
    public void An_empty_bundle_still_generates_a_valid_file()
    {
        var result = IconBundleBuilder.Run(Options(), Net());

        Assert.Equal(0, result.IconCount);
        Assert.Contains("internal static void Register()", result.Source);
        Assert.DoesNotContain("BluxIconBundle.Add", result.Source);
    }

    private sealed class ChunkNetwork : INetwork
    {
        public List<string> Requests { get; } = [];

        public string GetText(string url, string? userAgent)
        {
            Requests.Add(url);
            var names = url[(url.IndexOf("icons=", StringComparison.Ordinal) + 6)..].Split(',');
            return "{\"prefix\":\"ph\",\"width\":24,\"height\":24,\"icons\":{" + string.Join(",", names.Select(n => $"\"{n}\":{{\"body\":\"<g/>\"}}")) + "}}";
        }

        public byte[] GetBytes(string url) => throw new NotSupportedException();
    }
}
