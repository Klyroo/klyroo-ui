using Klyroo.UI.Build;
using Klyroo.UI.Build.Fonts;

namespace Klyroo.UI.Tests.Fonts;

/// <summary>Answers from saved provider responses: the unit tests never touch the network.</summary>
internal sealed class FakeNetwork : INetwork
{
    public static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fonts", "Fixtures", name);

    public Dictionary<string, string> Texts { get; } = new();
    public List<string> Requests { get; } = [];
    public bool Offline { get; set; }
    /// <summary>
    /// What the Iconify API really does for a collection it does not have: HTTP 200 with the body <c>404</c> (confirmed against api.iconify.design),
    /// not an HTTP 404. Off by default, because the font providers do answer 404.
    /// </summary>
    public bool UnknownUrlAnswers200With404 { get; set; }
    public byte[] FontBytes { get; set; } = File.ReadAllBytes(Fixture("geist-latin-400.woff2"));
    /// <summary>Per-URL answers that replace <see cref="FontBytes"/>.</summary>
    public Dictionary<string, byte[]> BytesByUrl { get; } = new();

    public string GetText(string url, string? userAgent)
    {
        Requests.Add(url);
        if (Offline) throw new HttpRequestException("no network");
        if (Texts.TryGetValue(url, out var text)) return text;
        if (UnknownUrlAnswers200With404) return "404";
        throw new DownloadException("404 Not Found for " + url, 404);
    }

    public byte[] GetBytes(string url)
    {
        Requests.Add(url);
        if (Offline) throw new HttpRequestException("no network");
        return BytesByUrl.TryGetValue(url, out var bytes) ? bytes : FontBytes;
    }
}

public class FontProviderTests
{
    private static FontSpec Spec(string family = "Geist", string? weights = null, string? styles = null, string subsets = "latin") =>
        FontBuilder.ParseSpec(family, new Dictionary<string, string> { ["Weights"] = weights ?? "", ["Styles"] = styles ?? "", ["Subsets"] = subsets }, "/");

    [Theory]
    [InlineData("400", 400, 400)]
    [InlineData("400..700", 400, 700)]
    [InlineData("400 700", 400, 700)]
    public void Weights_accept_a_value_or_a_range(string text, int min, int max)
    {
        var w = WeightSpec.Parse(text, "X");
        Assert.Equal((min, max), (w.Min, w.Max));
    }

    [Fact]
    public void A_bad_weight_says_what_is_accepted()
    {
        var e = Assert.Throws<FontException>(() => WeightSpec.Parse("bold", "Geist"));
        Assert.Contains("Geist", e.Message);
        Assert.Contains("400..700", e.Message);
    }

    [Fact]
    public void Google_url_for_static_weights()
    {
        var url = GoogleProvider.BuildUrl("https://fonts.googleapis.com/css2", Spec("Public Sans", "400;700"));
        Assert.Equal("https://fonts.googleapis.com/css2?family=Public+Sans:wght@400;700&display=swap", url);
    }

    [Fact]
    public void Google_url_with_italics_lists_tuples_in_the_order_the_api_requires()
    {
        var url = GoogleProvider.BuildUrl("https://fonts.googleapis.com/css2", Spec("Geist", "700;400", "italic;normal"));
        Assert.Equal("https://fonts.googleapis.com/css2?family=Geist:ital,wght@0,400;0,700;1,400;1,700&display=swap", url);
    }

    [Fact]
    public void Google_url_for_a_range_asks_for_the_variable_font()
    {
        var url = GoogleProvider.BuildUrl("https://fonts.googleapis.com/css2", Spec("Geist Mono", "400..600"));
        Assert.Equal("https://fonts.googleapis.com/css2?family=Geist+Mono:wght@400..600&display=swap", url);
    }

    [Fact]
    public void Google_url_rejects_overlapping_weights()
    {
        Assert.Throws<FontException>(() => GoogleProvider.BuildUrl("https://x/css2", Spec("Geist", "400..500;500..600")));
    }

    [Fact]
    public void Bunny_url_uses_the_documented_v1_format_with_static_weights()
    {
        Assert.Equal("https://fonts.bunny.net/css?family=geist:400,700&display=swap",
            BunnyProvider.BuildUrl("https://fonts.bunny.net/css", Spec("Geist", "400;700", "normal")));
        // Italic: weights of the upright style first, then the italic ones with the i suffix.
        Assert.Equal("https://fonts.bunny.net/css?family=geist-mono:400,500,400i,500i&display=swap",
            BunnyProvider.BuildUrl("https://fonts.bunny.net/css", Spec("Geist Mono", "400..500", "normal;italic")));
    }

    [Fact]
    public void Fontsource_url_is_the_slug_of_the_family()
    {
        Assert.Equal("https://api.fontsource.org/v1/fonts/geist-mono", FontsourceProvider.BuildUrl("https://api.fontsource.org/v1/fonts", Spec("Geist Mono")));
    }

    [Fact]
    public void Google_css_gives_one_block_per_subset_with_its_unicode_range()
    {
        var blocks = GoogleCss.Parse(File.ReadAllText(FakeNetwork.Fixture("google-geist.css")));
        Assert.Equal(15, blocks.Count);
        var latin = blocks.Where(b => b.Subset == "latin").ToList();
        Assert.Equal(3, latin.Count);
        Assert.All(latin, b => Assert.StartsWith("U+0000-00FF", b.UnicodeRange));
        Assert.All(blocks, b => Assert.Equal("woff2", b.Extension));
        Assert.Contains(blocks, b => b.Style == "italic" && b.Weight.Min == 400 && b.Subset == "latin-ext");
    }

    [Fact]
    public void Bunny_css_prefers_woff2_over_the_woff_listed_after_it()
    {
        var blocks = GoogleCss.Parse(File.ReadAllText(FakeNetwork.Fixture("bunny-geist.css")));
        Assert.Equal(10, blocks.Count);
        Assert.All(blocks, b => Assert.EndsWith(".woff2", b.Url));
    }

    [Fact]
    public void Google_provider_keeps_only_the_requested_subsets_and_styles()
    {
        var spec = Spec("Geist", "400", "normal;italic", "latin;latin-ext");
        var network = new FakeNetwork();
        network.Texts[GoogleProvider.BuildUrl("https://fonts.googleapis.com/css2", spec)] = File.ReadAllText(FakeNetwork.Fixture("google-geist.css"));
        var cache = Path.Combine(Path.GetTempPath(), "klyroo-fonts-" + Guid.NewGuid().ToString("N"));
        var faces = new GoogleProvider().Resolve(spec, new CachedDownloader(network, cache, TimeSpan.FromDays(1), _ => { }));
        Assert.All(faces, f => Assert.Contains(f.Subset, new[] { "latin", "latin-ext" }));
        Assert.Equal(6, faces.Count); // the saved answer also holds 700 upright, which the provider returned
    }

    [Fact]
    public void A_weight_the_provider_does_not_return_is_an_error_that_names_it()
    {
        var spec = Spec("Geist", "400;700", "normal", "latin");
        var network = new FakeNetwork();
        // The fixture holds 400 and 700 upright and 400 italic: ask for 700 italic.
        var italic = Spec("Geist", "400;700", "italic", "latin");
        network.Texts[GoogleProvider.BuildUrl("https://fonts.googleapis.com/css2", italic)] = File.ReadAllText(FakeNetwork.Fixture("google-geist.css"));
        var cache = Path.Combine(Path.GetTempPath(), "klyroo-fonts-" + Guid.NewGuid().ToString("N"));
        var e = Assert.Throws<FontException>(() => new GoogleProvider().Resolve(italic, new CachedDownloader(network, cache, TimeSpan.FromDays(1), _ => { })));
        Assert.Contains("weight 700", e.Message);
        Assert.NotNull(spec);
    }

    [Fact]
    public void Fontsource_lists_files_by_weight_style_and_subset()
    {
        var spec = Spec("Geist", "400;700", "normal", "latin");
        var network = new FakeNetwork();
        network.Texts["https://api.fontsource.org/v1/fonts/geist"] = File.ReadAllText(FakeNetwork.Fixture("fontsource-geist.json"));
        var cache = Path.Combine(Path.GetTempPath(), "klyroo-fonts-" + Guid.NewGuid().ToString("N"));
        var faces = new FontsourceProvider().Resolve(spec, new CachedDownloader(network, cache, TimeSpan.FromDays(1), _ => { }));
        Assert.Equal([400, 700], faces.Select(f => f.Weight.Min).OrderBy(w => w));
        Assert.All(faces, f => Assert.EndsWith("-normal.woff2", f.Url));
        Assert.All(faces, f => Assert.StartsWith("U+0000-00FF", f.UnicodeRange));
    }

    [Fact]
    public void An_unknown_fontsource_family_is_reported_as_not_found()
    {
        var network = new FakeNetwork();
        var cache = Path.Combine(Path.GetTempPath(), "klyroo-fonts-" + Guid.NewGuid().ToString("N"));
        var e = Assert.Throws<FontException>(() => new FontsourceProvider().Resolve(Spec("Nope Sans"), new CachedDownloader(network, cache, TimeSpan.FromDays(1), _ => { })));
        Assert.Contains("not found at fontsource", e.Message);
    }

    [Theory]
    [InlineData("Inter-400", "Inter", 400, 400, "normal")]
    [InlineData("Inter-700-italic", "Inter", 700, 700, "italic")]
    [InlineData("Inter-BoldItalic", "Inter", 700, 700, "italic")]
    [InlineData("Inter_SemiBold", "Inter", 600, 600, "normal")]
    [InlineData("Inter-Regular", "Inter", 400, 400, "normal")]
    [InlineData("Inter", "Inter", 400, 400, "normal")]
    [InlineData("InterVariable", "Inter", 100, 900, "normal")]
    [InlineData("public-sans-300", "Public Sans", 300, 300, "normal")]
    [InlineData("Inter-ExtraLight", "Inter", 200, 200, "normal")]
    public void Local_file_names_carry_weight_and_style(string stem, string family, int min, int max, string style)
    {
        var parsed = LocalProvider.ParseFileName(stem, family);
        Assert.NotNull(parsed);
        Assert.Equal((min, max, style), (parsed.Value.Weight.Min, parsed.Value.Weight.Max, parsed.Value.Style));
    }

    [Theory]
    [InlineData("InterTight-400", "Inter")]
    [InlineData("Roboto-400", "Inter")]
    public void Local_files_of_another_family_are_ignored(string stem, string family) =>
        Assert.Null(LocalProvider.ParseFileName(stem, family));

    [Fact]
    public void Local_provider_reads_a_folder_and_prefers_woff2()
    {
        var dir = Path.Combine(Path.GetTempPath(), "klyroo-local-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        foreach (var n in new[] { "Geist-400.woff2", "Geist-400.ttf", "Geist-700-italic.woff", "Other-400.woff2", "notes.txt" })
        {
            File.WriteAllBytes(Path.Combine(dir, n), [1, 2, 3]);
        }
        var spec = FontBuilder.ParseSpec("Geist", new Dictionary<string, string> { ["Src"] = dir }, "/");
        var faces = new LocalProvider().Resolve(spec, new CachedDownloader(new FakeNetwork(), dir, TimeSpan.Zero, _ => { }));
        Assert.Equal(2, faces.Count);
        Assert.Contains(faces, f => f.Weight.Min == 400 && f.Extension == "woff2" && f.Style == "normal");
        Assert.Contains(faces, f => f.Weight.Min == 700 && f.Extension == "woff" && f.Style == "italic");
    }

    [Fact]
    public void A_missing_local_folder_says_what_to_set()
    {
        var spec = FontBuilder.ParseSpec("Geist", new Dictionary<string, string> { ["Src"] = "/nowhere/fonts" }, "/");
        var e = Assert.Throws<FontException>(() => new LocalProvider().Resolve(spec, new CachedDownloader(new FakeNetwork(), "/tmp", TimeSpan.Zero, _ => { })));
        Assert.Contains("Src", e.Message);
    }

    [Theory]
    [InlineData("Role", "heading")]
    [InlineData("Display", "sometimes")]
    [InlineData("Preload", "yes")]
    [InlineData("Category", "fancy")]
    [InlineData("Weights", "heavy")]
    public void Item_metadata_is_validated(string key, string value)
    {
        Assert.Throws<FontException>(() => FontBuilder.ParseSpec("Geist", new Dictionary<string, string> { [key] = value }, "/"));
    }
}

public class FontMetricsTests
{
    [Theory]
    [InlineData("geist-latin-400.woff2")]
    [InlineData("geist-latin-400.woff")]
    [InlineData("geist-latin-400.ttf")]
    public void Metrics_are_the_same_whatever_the_container(string file)
    {
        var m = FontMetrics.Read(File.ReadAllBytes(FakeNetwork.Fixture(file)));
        Assert.Equal(1000, m.UnitsPerEm);
        Assert.Equal(1005, m.Ascent);
        Assert.Equal(-295, m.Descent);
        Assert.Equal(0, m.LineGap);
        Assert.InRange(m.XWidthAvg, 469.3, 469.5);
    }

    [Fact]
    public void Fallback_face_resizes_the_system_font_to_the_web_font()
    {
        var m = FontMetrics.Read(File.ReadAllBytes(FakeNetwork.Fixture("geist-latin-400.woff2")));
        var css = FallbackFace.Css("Geist", m, SystemFont.For("sans"));
        Assert.Contains("font-family: \"Geist Fallback\";", css);
        Assert.Contains("src: local(\"Arial\"), local(\"Liberation Sans\"), local(\"Arimo\");", css);
        // (469.394 / 1000) / (909.62 / 2048) = 1.0568: Geist is wider than Arial, so the fallback is enlarged.
        Assert.Contains("size-adjust: 105.68%;", css);
        Assert.Contains("ascent-override: 95.1%;", css);
        Assert.Contains("descent-override: 27.91%;", css);
        Assert.Contains("line-gap-override: 0%;", css);
    }

    [Fact]
    public void A_file_that_is_not_a_font_is_a_clear_error()
    {
        Assert.ThrowsAny<FontException>(() => FontMetrics.Read([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]));
    }
}

public class FontBuilderTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "klyroo-build-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private FontBuildOptions Options(params FontSpec[] specs) => new()
    {
        Specs = [.. specs],
        CacheDir = Path.Combine(_dir, "cache"),
        OutputDir = Path.Combine(_dir, "out"),
    };

    private static FontSpec Spec(string family, string role, string weights = "400;700", string subsets = "latin", string styles = "normal", string provider = "google") =>
        FontBuilder.ParseSpec(family, new Dictionary<string, string> { ["Role"] = role, ["Weights"] = weights, ["Subsets"] = subsets, ["Styles"] = styles, ["Provider"] = provider }, "/");

    private static FakeNetwork Network(FontSpec spec)
    {
        var network = new FakeNetwork();
        network.Texts[GoogleProvider.BuildUrl("https://fonts.googleapis.com/css2", spec)] = File.ReadAllText(FakeNetwork.Fixture("google-geist.css"));
        return network;
    }

    [Fact]
    public void Generates_font_faces_the_fallback_the_role_variable_and_a_preload()
    {
        var spec = Spec("Geist", "sans", subsets: "latin;latin-ext");
        var options = Options(spec);
        var result = FontBuilder.Run(options, Network(spec));

        Assert.Empty(result.Errors);
        var css = File.ReadAllText(Path.Combine(options.OutputDir, FontBuilder.CssFileName));
        Assert.Contains("font-family: \"Geist\";", css);
        Assert.Contains("font-display: swap;", css);
        Assert.Contains("unicode-range: U+0000-00FF", css);
        Assert.Contains("font-family: \"Geist Fallback\";", css);
        Assert.Contains("--klyroo-font-sans: \"Geist\", \"Geist Fallback\", -apple-system", css);
        // The stylesheet refers to the files by name, next to it.
        Assert.Matches("url\\(\"Geist-400-normal-latin\\.[0-9a-f]{8}\\.woff2\"\\) format\\(\"woff2\"\\)", css);

        Assert.Contains("\"css\":\"_klyroo/fonts/klyroo.fonts.css\"", result.ManifestJson);
        var preload = System.Text.RegularExpressions.Regex.Matches(result.ManifestJson, "_klyroo/fonts/Geist-[^\"]+\\.woff2");
        Assert.Single(preload);
        Assert.Contains("Geist-400-normal-latin.", preload[0].Value);
        Assert.All(result.Files.Where(f => f.EndsWith(".woff2")), f => Assert.True(File.Exists(Path.Combine(options.OutputDir, f))));
        Assert.Contains(FontBuilder.CssFileName, result.Files);
    }

    [Fact]
    public void A_font_without_role_gets_font_faces_but_no_variable_and_no_preload()
    {
        var spec = Spec("Geist", "");
        var options = Options(spec);
        var result = FontBuilder.Run(options, Network(spec));
        var css = File.ReadAllText(Path.Combine(options.OutputDir, FontBuilder.CssFileName));
        Assert.DoesNotContain("--klyroo-font-", css);
        Assert.Contains("\"preload\":[]", result.ManifestJson);
    }

    [Fact]
    public void Files_are_named_by_content_hash_and_identical_files_are_written_once()
    {
        var spec = Spec("Geist", "sans");
        var options = Options(spec);
        var result = FontBuilder.Run(options, Network(spec));
        // Every requested face downloads the same fixture bytes, so one hash names them all (per weight and subset name).
        var hashes = result.Files.Where(f => f.EndsWith(".woff2")).Select(f => f.Split('.')[^2]).Distinct().ToList();
        Assert.Single(hashes);
    }

    [Fact]
    public void A_second_run_with_the_same_input_does_not_ask_anything()
    {
        var spec = Spec("Geist", "sans");
        var options = Options(spec);
        var network = Network(spec);
        FontBuilder.Run(options, network);
        var requests = network.Requests.Count;
        var again = FontBuilder.Run(options, network);
        Assert.True(again.UpToDate);
        Assert.Equal(requests, network.Requests.Count);
        Assert.Contains("klyroo.fonts.css", again.Files);
    }

    [Fact]
    public void Without_network_a_build_works_from_the_cache()
    {
        var spec = Spec("Geist", "sans");
        var options = Options(spec);
        var network = Network(spec);
        FontBuilder.Run(options, network);
        Directory.Delete(options.OutputDir, recursive: true);
        network.Offline = true;
        // The answer is fresh in the cache and the files are there: nothing needs the network.
        var result = FontBuilder.Run(options, network);
        Assert.Empty(result.Errors);
        Assert.True(File.Exists(Path.Combine(options.OutputDir, FontBuilder.CssFileName)));
    }

    [Fact]
    public void Offline_mode_fails_with_a_message_when_the_cache_is_empty()
    {
        var spec = Spec("Geist", "sans");
        var options = Options(spec);
        options.Offline = true;
        var result = FontBuilder.Run(options, Network(spec));
        Assert.Single(result.Errors);
        Assert.Contains("Offline build", result.Errors[0]);
    }

    [Fact]
    public void An_unresolved_family_is_reported_with_every_provider_that_was_tried()
    {
        var spec = Spec("Geist", "sans", provider: "");
        var options = Options(spec);
        var result = FontBuilder.Run(options, new FakeNetwork());
        Assert.Single(result.Errors);
        Assert.Contains("google:", result.Errors[0]);
        Assert.Contains("bunny:", result.Errors[0]);
        Assert.Contains("fontsource:", result.Errors[0]);
    }

    [Fact]
    public void Stale_files_of_an_earlier_build_are_removed()
    {
        var spec = Spec("Geist", "sans");
        var options = Options(spec);
        Directory.CreateDirectory(options.OutputDir);
        File.WriteAllText(Path.Combine(options.OutputDir, "old.deadbeef.woff2"), "x");
        FontBuilder.Run(options, Network(spec));
        Assert.False(File.Exists(Path.Combine(options.OutputDir, "old.deadbeef.woff2")));
    }

    [Fact]
    public void Local_fonts_are_copied_with_hashed_names_and_no_unicode_range()
    {
        var src = Path.Combine(_dir, "src");
        Directory.CreateDirectory(src);
        File.Copy(FakeNetwork.Fixture("geist-latin-400.woff2"), Path.Combine(src, "Geist-400.woff2"));
        var spec = FontBuilder.ParseSpec("Geist", new Dictionary<string, string> { ["Src"] = src, ["Role"] = "sans" }, "/");
        var options = Options(spec);
        var result = FontBuilder.Run(options, new FakeNetwork { Offline = true });
        Assert.Empty(result.Errors);
        var css = File.ReadAllText(Path.Combine(options.OutputDir, FontBuilder.CssFileName));
        Assert.DoesNotContain("unicode-range", css);
        Assert.Contains("Geist-400-normal.", css);
        Assert.Contains("(local)", css);
    }

    [Fact]
    public void Mono_fonts_are_measured_against_a_monospace_system_font()
    {
        var spec = Spec("Geist Mono", "mono");
        var options = Options(spec);
        var network = new FakeNetwork();
        network.Texts[GoogleProvider.BuildUrl("https://fonts.googleapis.com/css2", spec)] = File.ReadAllText(FakeNetwork.Fixture("google-geist.css"));
        FontBuilder.Run(options, network);
        var css = File.ReadAllText(Path.Combine(options.OutputDir, FontBuilder.CssFileName));
        Assert.Contains("local(\"Courier New\")", css);
        Assert.Contains("--klyroo-font-mono: \"Geist Mono\", \"Geist Mono Fallback\", ui-monospace", css);
    }

    [Fact]
    public void The_default_stacks_are_the_ones_of_the_tokens()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Klyroo.UI", "styles", "klyroo.base.css"));
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null && !File.Exists(root); dir = dir.Parent)
        {
            root = Path.Combine(dir.FullName, "src", "Klyroo.UI", "styles", "klyroo.base.css");
        }
        var css = File.ReadAllText(root).Replace('\'', '"');
        Assert.Contains("--klyroo-font-sans: " + FontBuilder.DefaultSans + ";", css);
        Assert.Contains("--klyroo-font-mono: " + FontBuilder.DefaultMono + ";", css);
    }
}
