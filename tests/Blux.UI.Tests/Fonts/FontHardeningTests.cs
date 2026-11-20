using System.Text;
using System.Text.RegularExpressions;
using Blux.UI.Build;
using Blux.UI.Build.Fonts;

namespace Blux.UI.Tests.Fonts;

public class CachedDownloaderTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "blux-dl-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private CachedDownloader Downloader(FakeNetwork network, bool offline = false) =>
        new(network, _dir, TimeSpan.FromDays(7), _ => { }, offline);

    private int CachedFiles() => Directory.Exists(_dir) ? Directory.EnumerateFiles(_dir, "*", SearchOption.AllDirectories).Count() : 0;

    private static void RejectHtml(string body)
    {
        if (body.TrimStart().StartsWith('<')) throw new FontException("not what was expected");
    }

    [Fact]
    public void A_text_answer_that_fails_validation_is_never_cached()
    {
        var network = new FakeNetwork();
        network.Texts["https://x/api"] = "<html>oops</html>";
        Assert.Throws<FontException>(() => Downloader(network).GetText("https://x/api", null, RejectHtml));
        Assert.Equal(0, CachedFiles());

        network.Texts["https://x/api"] = "{}";
        Assert.Equal("{}", Downloader(network).GetText("https://x/api", null, RejectHtml));
        Assert.Equal(2, network.Requests.Count);
    }

    [Fact]
    public void A_cached_answer_that_fails_validation_is_evicted_and_fetched_again()
    {
        var network = new FakeNetwork();
        network.Texts["https://x/api"] = "<html>oops</html>";
        Downloader(network).GetText("https://x/api"); // no validation: an older build cached it
        Assert.Equal(1, CachedFiles());

        network.Texts["https://x/api"] = "{}";
        Assert.Equal("{}", Downloader(network).GetText("https://x/api", null, RejectHtml));
        Assert.Equal(2, network.Requests.Count);
    }

    [Fact]
    public void Bytes_that_fail_validation_are_never_cached()
    {
        var network = new FakeNetwork { FontBytes = [1, 2, 3, 4] };
        Assert.Throws<FontException>(() => Downloader(network).GetBytes("https://x/f", b => FontChecks.RequireFont(b, "x")));
        Assert.Equal(0, CachedFiles());
    }

    [Fact]
    public void A_file_that_already_exists_is_kept_and_never_deleted()
    {
        var first = new FakeNetwork { FontBytes = [1, 2, 3] };
        Downloader(first).GetBytes("https://x/f");
        var file = Directory.EnumerateFiles(_dir, "*.bin", SearchOption.AllDirectories).Single();
        CachedDownloader.Store(file, [9, 9, 9]);
        Assert.Equal([1, 2, 3], File.ReadAllBytes(file));
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(file)!, "*.tmp"));
    }

    [Fact]
    public void A_file_that_vanished_between_the_check_and_the_read_is_a_cache_miss()
    {
        Assert.Null(CachedDownloader.TryReadBytes(Path.Combine(_dir, "nope", "missing.bin")));
    }

    [Fact]
    public async Task Concurrent_writers_and_readers_never_fail_and_never_see_a_partial_file()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "files"));
        var path = Path.Combine(_dir, "files", "shared.bin");
        var payload = Enumerable.Range(0, 200_000).Select(i => (byte)(i % 251)).ToArray();
        var failures = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var stop = false;

        var writers = Enumerable.Range(0, 6).Select(_ => Task.Run(() =>
        {
            try
            {
                for (var i = 0; i < 150; i++)
                {
                    CachedDownloader.Store(path, payload);
                    CachedDownloader.Store(path + ".meta", payload, overwrite: true);
                    if (i % 10 == 0) File.Delete(path);
                }
            }
            catch (FileNotFoundException) { /* another writer deleted it first: the test deletes, the downloader never does */ }
            catch (Exception e) { failures.Add(e); }
        })).ToList();
        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            try
            {
                while (!Volatile.Read(ref stop))
                {
                    var bytes = CachedDownloader.TryReadBytes(path);
                    if (bytes != null && bytes.Length != payload.Length) failures.Add(new InvalidDataException("partial file: " + bytes.Length));
                }
            }
            catch (Exception e) { failures.Add(e); }
        })).ToList();

        await Task.WhenAll(writers);
        Volatile.Write(ref stop, true);
        await Task.WhenAll(readers);

        Assert.Empty(failures);
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.tmp"));
    }

    [Fact]
    public async Task Two_downloaders_on_one_cache_agree()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            Downloader(new FakeNetwork { FontBytes = [7, 7, 7] }).GetBytes("https://x/f"))));
        Assert.All(results, r => Assert.Equal([7, 7, 7], r));
        Assert.Single(Directory.EnumerateFiles(_dir, "*.bin", SearchOption.AllDirectories));
    }
}

public class FontChecksTests
{
    [Theory]
    [InlineData("U+0000-00FF, U+0131, U+0152-0153")]
    [InlineData("U+25??")]
    [InlineData("u+0-7f")]
    [InlineData("U+?")]
    [InlineData("U+0000-00FF,U+0131")]
    public void Valid_unicode_ranges(string value) => Assert.True(FontChecks.IsValidUnicodeRange(value));

    [Theory]
    [InlineData("")]
    [InlineData("U+")]
    [InlineData("U+00G0")]
    [InlineData("U+0000-00FF; } body { display: none } @font-face {")]
    [InlineData("U+0000-00FF, url(https://evil/x)")]
    [InlineData("U+1??-2??")]
    [InlineData("U+?4")]
    [InlineData("U+1234567")]
    [InlineData("0000-00FF")]
    public void Invalid_unicode_ranges(string value) => Assert.False(FontChecks.IsValidUnicodeRange(value));

    [Fact]
    public void Only_font_signatures_are_accepted()
    {
        byte[] Make(params byte[] head) => head.Concat(new byte[16]).ToArray();
        Assert.True(FontChecks.HasFontSignature(Make(0x77, 0x4F, 0x46, 0x32)));
        Assert.True(FontChecks.HasFontSignature(Make(0x77, 0x4F, 0x46, 0x46)));
        Assert.True(FontChecks.HasFontSignature(Make(0, 1, 0, 0)));
        Assert.True(FontChecks.HasFontSignature(Make(0x4F, 0x54, 0x54, 0x4F)));
        Assert.True(FontChecks.HasFontSignature(Make(0x74, 0x72, 0x75, 0x65)));
        Assert.False(FontChecks.HasFontSignature(Encoding.ASCII.GetBytes("<!DOCTYPE html><html>")));
        Assert.False(FontChecks.HasFontSignature([1, 2, 3]));
    }

    [Fact]
    public void Corrupt_woff2_data_is_a_font_exception_not_a_crash()
    {
        var bytes = File.ReadAllBytes(FakeNetwork.Fixture("geist-latin-400.woff2"));
        for (var i = bytes.Length / 2; i < bytes.Length; i++) bytes[i] = 0xFF;
        Assert.ThrowsAny<FontException>(() => FontMetrics.Read(bytes));
    }

    [Theory]
    [InlineData("<html>")]
    [InlineData("{")]
    [InlineData("{\"a\":")]
    [InlineData("[1,")]
    [InlineData("tru")]
    [InlineData("{\"a\":\"\\u12\"}")]
    [InlineData("")]
    public void Non_json_is_a_format_exception_whatever_the_garbage(string text) =>
        Assert.Throws<FormatException>(() => MiniJson.Parse(text));

    [Fact]
    public void Deeply_nested_json_is_a_format_exception()
    {
        Assert.Throws<FormatException>(() => MiniJson.Parse(new string('[', 5000)));
    }
}

public class FontHardeningBuildTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "blux-harden-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private FontBuildOptions Options(List<string> warnings, params FontSpec[] specs) => new()
    {
        Specs = [.. specs],
        CacheDir = Path.Combine(_dir, "cache"),
        OutputDir = Path.Combine(_dir, "out"),
        Warn = warnings.Add,
    };

    private static FontSpec Spec(string family = "Geist", string role = "sans", string weights = "400;700", string provider = "google", string subsets = "latin") =>
        FontBuilder.ParseSpec(family, new Dictionary<string, string> { ["Role"] = role, ["Weights"] = weights, ["Subsets"] = subsets, ["Provider"] = provider }, "/");

    private static string GoogleUrl(FontSpec spec) => GoogleProvider.BuildUrl("https://fonts.googleapis.com/css2", spec);
    private static string BunnyUrl(FontSpec spec) => BunnyProvider.BuildUrl("https://fonts.bunny.net/css", spec);
    private static string GoogleCss => File.ReadAllText(FakeNetwork.Fixture("google-geist.css"));
    private static string BunnyCss => File.ReadAllText(FakeNetwork.Fixture("bunny-geist.css"));

    private static FakeNetwork Network(FontSpec spec)
    {
        var network = new FakeNetwork();
        network.Texts[GoogleUrl(spec)] = GoogleCss;
        return network;
    }

    private static string WithUrl(string css, string url) => Regex.Replace(css, @"url\([^)]*\)", "url(" + url + ")");

    private string Css() => File.ReadAllText(Path.Combine(_dir, "out", FontBuilder.CssFileName));

    [Fact]
    public void An_incremental_run_lists_only_the_fonts_and_the_stylesheet()
    {
        var spec = Spec();
        var warnings = new List<string>();
        var options = Options(warnings, spec);
        var network = Network(spec);
        FontBuilder.Run(options, network);
        // Leftovers of an older layout: they must never be listed (they would be published as static web assets).
        File.WriteAllText(Path.Combine(options.OutputDir, "BluxFontsManifest.g.cs"), "// old");
        File.WriteAllText(Path.Combine(options.OutputDir, "notes.txt"), "x");

        var again = FontBuilder.Run(options, network);
        Assert.True(again.UpToDate);
        Assert.NotEmpty(again.Files);
        Assert.All(again.Files, f => Assert.Matches(@"\.(woff2|woff|ttf|otf|css)$", f));
        Assert.DoesNotContain(again.Files, f => f.EndsWith(".cs") || f.EndsWith(".json") || f.Contains("stamp"));
    }

    [Fact]
    public void A_missing_output_file_makes_the_run_regenerate()
    {
        var spec = Spec();
        var options = Options([], spec);
        var network = Network(spec);
        var first = FontBuilder.Run(options, network);
        File.Delete(Path.Combine(options.OutputDir, first.Files.First(f => f.EndsWith(".woff2"))));
        Assert.False(FontBuilder.Run(options, network).UpToDate);
    }

    [Fact]
    public void A_corrupt_woff2_is_a_warning_and_the_font_is_still_served()
    {
        var spec = Spec();
        var warnings = new List<string>();
        var network = Network(spec);
        var corrupt = File.ReadAllBytes(FakeNetwork.Fixture("geist-latin-400.woff2"));
        for (var i = corrupt.Length / 2; i < corrupt.Length; i++) corrupt[i] = 0xFF;
        network.FontBytes = corrupt;

        var result = FontBuilder.Run(Options(warnings, spec), network);

        Assert.Empty(result.Errors);
        Assert.Contains(warnings, w => w.Contains("No fallback face for 'Geist'"));
        Assert.DoesNotContain("Geist Fallback\";\n  src", Css());
        Assert.Contains(result.Files, f => f.EndsWith(".woff2"));
    }

    [Fact]
    public void A_metrics_failure_on_a_remote_face_is_a_warning()
    {
        var spec = Spec();
        var warnings = new List<string>();
        var options = Options(warnings, spec);
        options.ReadMetrics = _ => throw new CorruptFontException("broken");
        var result = FontBuilder.Run(options, Network(spec));
        Assert.Empty(result.Errors);
        Assert.Single(warnings);
        Assert.DoesNotContain("size-adjust", Css());
    }

    [Fact]
    public void A_non_json_fontsource_answer_is_not_cached_and_the_next_provider_is_used()
    {
        var spec = Spec(provider: "");
        var options = Options([], spec);
        options.ProviderPriority = ["fontsource", "google"];
        var network = Network(spec);
        network.Texts["https://api.fontsource.org/v1/fonts/geist"] = "<html><body>Bad gateway</body></html>";

        var result = FontBuilder.Run(options, network);
        Assert.Empty(result.Errors);
        Assert.Contains("(google)", Css());

        // Nothing of the bad answer was kept: a new build asks again instead of failing for a week.
        Directory.Delete(options.OutputDir, recursive: true);
        FontBuilder.Run(options, network);
        Assert.Equal(2, network.Requests.Count(r => r == "https://api.fontsource.org/v1/fonts/geist"));
    }

    [Fact]
    public void A_non_json_answer_alone_is_an_error_of_the_font_and_not_a_crash()
    {
        var spec = Spec(provider: "fontsource");
        var network = new FakeNetwork();
        network.Texts["https://api.fontsource.org/v1/fonts/geist"] = "not json {";
        var result = FontBuilder.Run(Options([], spec), network);
        Assert.Single(result.Errors);
        Assert.Contains("not JSON", result.Errors[0]);
    }

    [Theory]
    [InlineData("/fonts/geist.woff2")]
    [InlineData("//fonts.gstatic.com/s/geist.woff2")]
    [InlineData("http://fonts.gstatic.com/s/geist.woff2")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://x/geist.woff2")]
    public void A_font_url_that_is_not_an_absolute_https_url_is_rejected(string url)
    {
        var spec = Spec();
        var network = new FakeNetwork();
        network.Texts[GoogleUrl(spec)] = WithUrl(GoogleCss, url);
        var result = FontBuilder.Run(Options([], spec), network);
        Assert.Single(result.Errors);
        Assert.Matches("not (absolute|https)", result.Errors[0]);
        Assert.DoesNotContain(network.Requests, r => !r.StartsWith("https://fonts.googleapis.com"));
    }

    [Fact]
    public void A_bad_url_falls_back_to_the_next_provider()
    {
        var spec = Spec(provider: "");
        var options = Options([], spec);
        options.ProviderPriority = ["google", "bunny"];
        var network = new FakeNetwork();
        network.Texts[GoogleUrl(spec)] = WithUrl(GoogleCss, "/relative.woff2");
        network.Texts[BunnyUrl(spec)] = BunnyCss;
        var result = FontBuilder.Run(options, network);
        Assert.Empty(result.Errors);
        Assert.Contains("(bunny)", Css());
    }

    [Fact]
    public void A_download_that_is_not_a_font_is_an_error_and_is_never_cached()
    {
        var spec = Spec();
        var network = Network(spec);
        network.FontBytes = Encoding.UTF8.GetBytes("<!DOCTYPE html><html><body>captive portal</body></html>");
        var options = Options([], spec);
        var result = FontBuilder.Run(options, network);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("not a font file", result.Errors[0]);
        var files = Path.Combine(options.CacheDir, "files");
        Assert.Empty(Directory.Exists(files) ? Directory.EnumerateFiles(files) : []);
    }

    [Fact]
    public void A_non_font_from_one_provider_falls_back_to_the_next()
    {
        var spec = Spec(provider: "");
        var options = Options([], spec);
        options.ProviderPriority = ["google", "bunny"];
        var network = new FakeNetwork();
        network.Texts[GoogleUrl(spec)] = GoogleCss;
        network.Texts[BunnyUrl(spec)] = BunnyCss;
        var html = Encoding.UTF8.GetBytes("<!DOCTYPE html><html>nope</html>");
        foreach (var block in GoogleCss.Split("url(").Skip(1)) network.BytesByUrl[block.Split(')')[0].Trim('"', '\'')] = html;
        var result = FontBuilder.Run(options, network);
        Assert.Empty(result.Errors);
        Assert.Contains("(bunny)", Css());
    }

    [Fact]
    public void A_local_file_that_is_not_a_font_is_an_error()
    {
        var src = Path.Combine(_dir, "src");
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "Geist-400.woff2"), "this is not a font at all");
        var spec = FontBuilder.ParseSpec("Geist", new Dictionary<string, string> { ["Src"] = src }, "/");
        var result = FontBuilder.Run(Options([], spec), new FakeNetwork { Offline = true });
        Assert.Single(result.Errors);
        Assert.Contains("not a font file", result.Errors[0]);
    }

    [Fact]
    public void An_unicode_range_that_could_break_out_of_the_declaration_is_rejected()
    {
        var spec = Spec(provider: "fontsource");
        var json = File.ReadAllText(FakeNetwork.Fixture("fontsource-geist.json"));
        var range = Regex.Match(json, "\"latin\"\\s*:\\s*\"(U\\+[^\"]*)\"").Groups[1].Value;
        Assert.NotEmpty(range);
        var network = new FakeNetwork();
        network.Texts["https://api.fontsource.org/v1/fonts/geist"] = json.Replace(range, "U+0000-00FF; } body { display: none } @font-face { src: url(https://evil/x)");
        var result = FontBuilder.Run(Options([], spec), network);
        Assert.Single(result.Errors);
        Assert.Contains("unicode-range", result.Errors[0]);
        var cssPath = Path.Combine(_dir, "out", FontBuilder.CssFileName);
        Assert.False(File.Exists(cssPath) && File.ReadAllText(cssPath).Contains("display: none"));
    }

    [Fact]
    public void A_changed_task_build_regenerates_the_output()
    {
        var spec = Spec();
        var options = Options([], spec);
        var network = Network(spec);
        options.ToolVersion = "build-1";
        FontBuilder.Run(options, network);
        Assert.True(FontBuilder.Run(options, network).UpToDate);
        options.ToolVersion = "build-2";
        Assert.False(FontBuilder.Run(options, network).UpToDate);
    }

    [Fact]
    public void Fallbacks_skipped_for_lack_of_brotli_are_computed_once_brotli_is_available()
    {
        var spec = Spec();
        var options = Options([], spec);
        var network = Network(spec);
        options.BrotliAvailable = () => false;
        options.ReadMetrics = _ => throw new BrotliUnavailableException();
        FontBuilder.Run(options, network);
        Assert.DoesNotContain("size-adjust", Css());
        Assert.True(FontBuilder.Run(options, network).UpToDate); // still no Brotli: nothing to gain

        options.BrotliAvailable = () => true;
        options.ReadMetrics = FontMetrics.Read;
        var after = FontBuilder.Run(options, network);
        Assert.False(after.UpToDate);
        Assert.Contains("size-adjust", Css());
        Assert.True(FontBuilder.Run(options, network).UpToDate);
    }

    [Fact]
    public void Identical_bytes_are_one_file_with_one_rule_per_face()
    {
        var spec = Spec(weights: "400;700");
        var result = FontBuilder.Run(Options([], spec), Network(spec));
        Assert.Empty(result.Errors);
        var fonts = result.Files.Where(f => f.EndsWith(".woff2")).ToList();
        Assert.Single(fonts);
        var css = Css();
        var rules = Regex.Matches(css, "@font-face \\{\\s*font-family: \"Geist\";").Count;
        Assert.Equal(2, rules);
        Assert.Equal(rules, css.Split("url(\"" + fonts[0] + "\")").Length - 1);
    }

    [Fact]
    public void Different_bytes_stay_different_files()
    {
        var spec = FontBuilder.ParseSpec("Geist", new Dictionary<string, string> { ["Weights"] = "400", ["Styles"] = "normal;italic", ["Subsets"] = "latin", ["Provider"] = "google" }, "/");
        var network = Network(spec);
        var urls = GoogleCss.Split("url(").Skip(1).Select(b => b.Split(')')[0].Trim('"', '\'')).Distinct().ToList();
        for (var i = 0; i < urls.Count; i++)
        {
            var bytes = (byte[])network.FontBytes.Clone();
            bytes[^1] ^= (byte)(i + 1);
            network.BytesByUrl[urls[i]] = bytes;
        }
        var result = FontBuilder.Run(Options([], spec), network);
        Assert.Empty(result.Errors);
        // Upright and italic have distinct URLs here (400 and 700 upright share one variable font).
        Assert.Equal(2, result.Files.Count(f => f.EndsWith(".woff2")));
    }
}
