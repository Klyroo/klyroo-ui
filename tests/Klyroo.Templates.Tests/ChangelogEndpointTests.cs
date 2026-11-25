using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Changelog.Web.ReleaseNotes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Klyroo.Templates.Tests;

public class GitHubReleaseJsonTests
{
    private static IReadOnlyList<ReleaseEntry> Parse(string json, string? repository = "octo/tern") => GitHubReleaseJson.Parse(new MemoryStream(Encoding.UTF8.GetBytes(json)), repository);

    [Fact]
    public void A_release_is_read_with_its_fields_and_a_url_built_from_the_repository_and_the_tag()
    {
        var release = Assert.Single(Parse(ChangelogSamples.Array(ChangelogSamples.Release("release/1.0 beta", "Big one", "Body\r\ntext", prerelease: true, login: "octo-fan",
            avatar: "https://avatars.githubusercontent.com/u/1?v=4"))));

        Assert.Equal("release/1.0 beta", release.Tag);
        Assert.Equal("Big one", release.Title);
        Assert.Equal("release-1.0-beta", release.Slug);
        Assert.Equal("Body\ntext", release.Body);
        Assert.True(release.Prerelease);
        Assert.Equal(DateTimeOffset.Parse("2026-01-02T03:04:05Z", System.Globalization.CultureInfo.InvariantCulture), release.PublishedAt);
        Assert.Equal("https://github.com/octo/tern/releases/tag/release%2F1.0%20beta", release.Url);
        Assert.Equal(new ReleaseAuthor("octo-fan", "https://avatars.githubusercontent.com/u/1?v=4"), release.Author);
    }

    [Fact]
    public void Drafts_and_entries_with_no_tag_are_skipped_and_a_missing_name_is_the_tag()
    {
        var releases = Parse("[" + ChangelogSamples.Release("v3", draft: true) + ",{\"name\":\"no tag\"},42,null,\"x\"," + ChangelogSamples.Release("v2", name: null) + ",{\"tag_name\":\"  \"}," + ChangelogSamples.Release("v1", name: "") + "]");

        Assert.Equal(["v2", "v1"], releases.Select(r => r.Tag));
        Assert.Equal(["v2", "v1"], releases.Select(r => r.Title));
    }

    [Theory]
    [InlineData("http://avatars.githubusercontent.com/u/1")]
    [InlineData("https://evil.example/u/1")]
    [InlineData("https://avatars.githubusercontent.com.evil.example/u/1")]
    [InlineData("https://evil.example@avatars.githubusercontent.com/u/1")]
    [InlineData("https://user:pw@avatars.githubusercontent.com/u/1")]
    [InlineData("https://avatars.githubusercontent.com:8443/u/1")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/svg+xml;base64,PHN2Zz4=")]
    [InlineData("//avatars.githubusercontent.com/u/1")]
    [InlineData("/u/1")]
    public void An_avatar_that_is_not_https_on_the_avatar_host_is_dropped_and_the_author_stays(string avatar)
    {
        var release = Assert.Single(Parse(ChangelogSamples.Array(ChangelogSamples.Release("v1", avatar: avatar))));

        Assert.Equal("octo-fan", release.Author!.Login);
        Assert.Null(release.Author.AvatarUrl);
    }

    [Theory]
    [InlineData("octo\"><script>")]
    [InlineData("../admin")]
    [InlineData("a b")]
    [InlineData("-leading")]
    [InlineData("octo/cat")]
    [InlineData("")]
    [InlineData("an-extremely-long-login-name-that-github-would-never-allow-at-all-ever")]
    [InlineData("octo[bot]x")]
    public void A_login_that_github_would_not_allow_drops_the_author(string login)
    {
        var release = Assert.Single(Parse(ChangelogSamples.Array(ChangelogSamples.Release("v1", login: login))));

        Assert.Null(release.Author);
    }

    [Theory]
    [InlineData("github-actions[bot]")]
    [InlineData("a")]
    [InlineData("A1-b2")]
    public void A_login_in_the_form_github_uses_is_kept(string login) =>
        Assert.Equal(login, Assert.Single(Parse(ChangelogSamples.Array(ChangelogSamples.Release("v1", login: login)))).Author!.Login);

    [Fact]
    public void Names_and_tags_are_one_line_and_of_limited_length()
    {
        var release = Assert.Single(Parse(ChangelogSamples.Array(ChangelogSamples.Release(new string('t', 400) + "\n", "line one\r\nline two\u0000" + new string('n', 400)))));

        Assert.Equal(255, release.Tag.Length);
        Assert.True(release.Title.Length <= 200);
        Assert.DoesNotContain(release.Title, c => char.IsControl(c));
        Assert.StartsWith("line one  line two", release.Title);
    }

    [Fact]
    public void A_body_over_the_limit_is_cut_and_marked()
    {
        var release = Assert.Single(Parse(ChangelogSamples.Array(ChangelogSamples.Release("v1", body: new string('x', GitHubReleaseJson.MaxBodyLength + 500)))));

        Assert.True(release.Body.Length < GitHubReleaseJson.MaxBodyLength + 10);
        Assert.EndsWith("…", release.Body);
    }

    [Fact]
    public void Cutting_a_body_does_not_split_a_surrogate_pair()
    {
        var body = new string('x', GitHubReleaseJson.MaxBodyLength - 1) + "😀" + "tail";

        var release = Assert.Single(Parse(ChangelogSamples.Array(ChangelogSamples.Release("v1", body: body))));

        Assert.DoesNotContain(release.Body, char.IsLowSurrogate);
        Assert.DoesNotContain(release.Body, char.IsHighSurrogate);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("\"x\"")]
    [InlineData("42")]
    public void The_root_must_be_an_array(string json) => Assert.ThrowsAny<JsonException>(() => Parse(json));

    [Fact]
    public void A_tag_with_a_code_point_that_is_not_a_character_is_cleaned_and_does_not_stop_the_list()
    {
        var releases = Parse(ChangelogSamples.Array(
            ChangelogSamples.Release("v1.0\uFFFE\uFFFF", "Name\uFFFE"), ChangelogSamples.Release("v0.9")));

        Assert.Equal(["v1.0", "v0.9"], releases.Select(r => r.Tag));
        Assert.Equal(["v1.0", "v0.9"], releases.Select(r => r.Slug));
        Assert.Equal("Name", releases[0].Title);
    }

    [Fact]
    public void A_string_that_is_not_valid_text_leaves_out_that_release_and_keeps_the_others()
    {
        var json = "[" + ChangelogSamples.Release("v2") + ",{\"tag_name\":\"bad\\ud800\",\"body\":\"x\"},{\"tag_name\":\"v1\",\"body\":\"lone \\udc00\"}," + ChangelogSamples.Release("v0") + "]";

        var releases = Parse(json);

        Assert.Equal(["v2", "v0"], releases.Select(r => r.Tag));
    }

    [Fact]
    public void Deeply_nested_json_is_refused()
    {
        var json = ChangelogSamples.Array(string.Concat(Enumerable.Repeat("{\"a\":", 200)) + "1" + string.Concat(Enumerable.Repeat("}", 200)));

        Assert.ThrowsAny<JsonException>(() => Parse(json));
    }

    [Fact]
    public void A_date_that_is_not_a_date_is_no_date_and_the_creation_date_is_the_fallback()
    {
        var releases = Parse("[{\"tag_name\":\"a\",\"published_at\":\"soon\"},{\"tag_name\":\"b\",\"published_at\":null,\"created_at\":\"2026-03-04T05:06:07Z\"}]");

        Assert.Null(releases[0].PublishedAt);
        Assert.Equal(DateTimeOffset.Parse("2026-03-04T05:06:07Z", System.Globalization.CultureInfo.InvariantCulture), releases[1].PublishedAt);
    }

    [Fact]
    public void Without_a_repository_a_release_has_no_github_address()
    {
        Assert.Null(Assert.Single(Parse(ChangelogSamples.Array(ChangelogSamples.Release("v1")), repository: null)).Url);
    }

    [Fact]
    public void The_sample_releases_of_the_template_parse_and_have_what_the_pages_show()
    {
        using var file = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "changelog-releases.json"));

        var releases = GitHubReleaseJson.Parse(file, "tern-cli/tern");

        Assert.Equal(12, releases.Count);
        Assert.Equal(releases.Count, releases.Select(r => r.Slug).Distinct().Count());
        Assert.Equal("v1.4.0", releases[0].Tag);
        Assert.Contains(releases, r => r.Prerelease);
        Assert.DoesNotContain(releases, r => r.Tag.Contains("draft"));
        Assert.Contains(releases, r => r.Title == r.Tag);
        Assert.Contains(releases, r => r.Body.Length == 0);
        Assert.All(releases, r => Assert.Null(r.Author?.AvatarUrl));
        // Newest first.
        Assert.Equal(releases.OrderByDescending(r => r.PublishedAt), releases);
    }
}

public class ChangelogOptionsTests
{
    [Theory]
    [InlineData("dotnet/aspnetcore")]
    [InlineData("a/b")]
    [InlineData("Some-Org/some.repo_name")]
    [InlineData("...a/b...")]
    public void A_repository_is_owner_slash_name_of_the_characters_github_allows(string value) => Assert.True(ChangelogOptions.IsRepository(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("owner")]
    [InlineData("/repo")]
    [InlineData("owner/")]
    [InlineData("owner/repo/")]
    [InlineData("owner/repo\n")]
    [InlineData("owner/re po")]
    [InlineData("owner/repo?x")]
    [InlineData("owner/repo#x")]
    [InlineData("owner/repo%2f..")]
    [InlineData("../repo")]
    [InlineData("./repo")]
    [InlineData("owner/..")]
    [InlineData("owner/.")]
    [InlineData("https://github.com/owner/repo")]
    [InlineData("owner/repo:8080")]
    [InlineData("owner@evil/repo")]
    public void Anything_else_is_not(string? value) => Assert.False(ChangelogOptions.IsRepository(value));

    [Fact]
    public void The_github_source_needs_a_repository_and_the_message_names_the_setting_not_a_value()
    {
        Assert.Contains("Changelog:Repository is not set", new ChangelogOptions { Source = ChangelogSource.GitHub, Repository = "" }.Problem());
        Assert.Contains("owner/repo", new ChangelogOptions { Repository = "nonsense" }.Problem());
        Assert.Null(new ChangelogOptions { Repository = "octo/tern", GitHubToken = "ghp_secret" }.Problem());
        Assert.DoesNotContain("ghp_secret", new ChangelogOptions { Repository = "bad repo", GitHubToken = "ghp_secret" }.Problem());
    }

    [Fact]
    public void The_file_source_needs_no_repository_but_one_that_is_set_must_be_valid()
    {
        Assert.Null(new ChangelogOptions { Source = ChangelogSource.File }.Problem());
        Assert.NotNull(new ChangelogOptions { Source = ChangelogSource.File, Repository = "../x" }.Problem());
    }

    [Theory]
    [InlineData("http://api.github.com/")]
    [InlineData("ftp://x")]
    [InlineData("api.github.com")]
    public void The_api_address_must_be_https_so_the_token_is_never_sent_in_clear(string url) => Assert.NotNull(new ChangelogOptions { Repository = "a/b", ApiUrl = url }.Problem());

    [Fact]
    public void Numbers_must_be_positive()
    {
        Assert.NotNull(new ChangelogOptions { Repository = "a/b", PageSize = 0 }.Problem());
        Assert.NotNull(new ChangelogOptions { Repository = "a/b", MaxReleases = 0 }.Problem());
        Assert.NotNull(new ChangelogOptions { Repository = "a/b", RequestTimeout = TimeSpan.Zero }.Problem());
        Assert.NotNull(new ChangelogOptions { Repository = "a/b", CacheDuration = TimeSpan.FromSeconds(-1) }.Problem());
        Assert.Null(new ChangelogOptions { Repository = "a/b", CacheDuration = TimeSpan.Zero }.Problem());
    }

    [Fact]
    public void The_repository_page_is_built_from_a_valid_repository_only()
    {
        Assert.Equal("https://github.com/octo/tern", new ChangelogOptions { Repository = "octo/tern" }.RepositoryUrl);
        Assert.Null(new ChangelogOptions { Repository = "octo/tern/../x" }.RepositoryUrl);
    }

    [Theory]
    [InlineData(null, "", ChangelogSource.File, true)]
    [InlineData(null, "  ", ChangelogSource.File, true)]
    [InlineData(null, "octo/tern", ChangelogSource.GitHub, false)]
    [InlineData(ChangelogSource.GitHub, "", ChangelogSource.GitHub, false)]
    [InlineData(ChangelogSource.File, "octo/tern", ChangelogSource.File, false)]
    [InlineData(ChangelogSource.File, "", ChangelogSource.File, true)]
    public void With_no_source_the_repository_decides_and_a_source_that_is_set_wins(ChangelogSource? source, string repository, ChangelogSource effective, bool sample)
    {
        var options = new ChangelogOptions { Source = source, Repository = repository };

        Assert.Equal(effective, options.EffectiveSource);
        Assert.Equal(sample, options.IsSample);
    }

    [Fact]
    public void The_defaults_are_the_github_source_with_a_one_hour_cache()
    {
        var options = new ChangelogOptions();

        Assert.Null(options.Source);
        Assert.Equal(ChangelogSource.File, options.EffectiveSource);
        Assert.True(options.IsSample);
        Assert.Equal(TimeSpan.FromHours(1), options.CacheDuration);
        Assert.Equal("https://api.github.com/", options.ApiUrl);
    }

    [Fact]
    public void The_options_bind_from_configuration_like_environment_variables_do()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Changelog:Source"] = "File",
            ["Changelog:Repository"] = "octo/tern",
            ["Changelog:CacheDuration"] = "00:10:00",
            ["Changelog:PageSize"] = "5",
        }).Build();

        var options = configuration.GetSection("Changelog").Get<ChangelogOptions>()!;

        Assert.Equal(ChangelogSource.File, options.Source);
        Assert.Equal(TimeSpan.FromMinutes(10), options.CacheDuration);
        Assert.Equal(5, options.PageSize);
    }

    // ---- The address of the site, for the feed

    private static HttpRequest Request(string host, string scheme = "https", string pathBase = "")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        context.Request.PathBase = pathBase;
        return context.Request;
    }

    [Theory]
    [InlineData("example.com", "https", "", "https://example.com/")]
    [InlineData("example.com:8443", "https", "", "https://example.com:8443/")]
    [InlineData("127.0.0.1:5000", "http", "/templates/changelog/demo", "http://127.0.0.1:5000/templates/changelog/demo/")]
    [InlineData("[::1]:5000", "http", "", "http://[::1]:5000/")]
    public void The_site_address_comes_from_the_request_when_it_looks_like_a_host(string host, string scheme, string pathBase, string expected) =>
        Assert.Equal(expected, ChangelogServices.SiteUrl(Request(host, scheme, pathBase), new ChangelogOptions()));

    [Theory]
    [InlineData("evil.example\"><x")]
    [InlineData("evil.example/path")]
    [InlineData("a b")]
    [InlineData("evil.example@other")]
    [InlineData("exa<mple.com")]
    public void A_host_header_that_is_not_a_host_gives_no_address(string host) =>
        Assert.Null(ChangelogServices.SiteUrl(Request(host), new ChangelogOptions()));

    [Theory]
    [InlineData("https://news.example.com", "https://news.example.com/")]
    [InlineData("https://example.com/blog/", "https://example.com/blog/")]
    [InlineData("https://example.com/blog?x=1#y", "https://example.com/blog/")]
    public void A_configured_site_address_wins_over_the_request(string configured, string expected) =>
        Assert.Equal(expected, ChangelogServices.SiteUrl(Request("evil.example"), new ChangelogOptions { SiteUrl = configured }));

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/")]
    [InlineData("not a url")]
    [InlineData("https://user:pw@example.com/")]
    public void A_configured_site_address_that_is_not_http_or_https_gives_no_address(string configured) =>
        Assert.Null(ChangelogServices.SiteUrl(Request("example.com"), new ChangelogOptions { SiteUrl = configured }));
}

/// <summary>The feed endpoint and the file source, in a real (in memory) server with the sample releases of the template.</summary>
public class ChangelogEndpointTests
{
    private static async Task<(HttpClient Client, IAsyncDisposable Host)> StartAsync(Dictionary<string, string?> settings)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Services.AddChangelog(builder.Configuration);
        var app = builder.Build();
        app.MapReleaseFeed("Tern releases", "What changed");
        await app.StartAsync();
        return (app.GetTestClient(), app);
    }

    private static Dictionary<string, string?> FileSource(string? file = null) => new()
    {
        ["Changelog:Source"] = "File",
        ["Changelog:Repository"] = "tern-cli/tern",
        ["Changelog:File"] = file ?? Path.Combine(AppContext.BaseDirectory, "changelog-releases.json"),
    };

    [Fact]
    public async Task The_feed_of_the_sample_releases_is_valid_atom_with_the_latest_twenty_or_fewer_and_the_right_headers()
    {
        var (client, host) = await StartAsync(FileSource());
        await using var _ = host;

        var response = await client.GetAsync("/feed.xml", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/atom+xml; charset=utf-8", response.Content.Headers.ContentType!.ToString());
        Assert.Equal("public, max-age=300", response.Headers.CacheControl!.ToString());
        var doc = XDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        XNamespace atom = "http://www.w3.org/2005/Atom";
        Assert.Equal(12, doc.Descendants(atom + "entry").Count());
        Assert.StartsWith("http://localhost/releases/", (string)doc.Descendants(atom + "entry").First().Element(atom + "id")!);
    }

    [Fact]
    public async Task The_feed_answers_304_to_the_etag_it_sent()
    {
        var (client, host) = await StartAsync(FileSource());
        await using var _ = host;
        var first = await client.GetAsync("/feed.xml", TestContext.Current.CancellationToken);
        var etag = first.Headers.ETag!.ToString();

        var request = new HttpRequestMessage(HttpMethod.Get, "/feed.xml");
        request.Headers.TryAddWithoutValidation("If-None-Match", etag);
        var again = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotModified, again.StatusCode);
    }

    [Fact]
    public async Task The_links_of_the_feed_use_the_configured_site_address_not_the_host_header()
    {
        var settings = FileSource();
        settings["Changelog:SiteUrl"] = "https://news.example.com/tern";
        var (client, host) = await StartAsync(settings);
        await using var _ = host;
        var request = new HttpRequestMessage(HttpMethod.Get, "/feed.xml");
        request.Headers.Host = "evil.example";

        var xml = await (await client.SendAsync(request, TestContext.Current.CancellationToken)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("https://news.example.com/tern/feed.xml", xml);
        Assert.DoesNotContain("evil.example", xml);
    }

    [Fact]
    public async Task A_host_header_that_is_not_a_host_is_a_400_and_nothing_of_it_is_in_the_answer()
    {
        var (client, host) = await StartAsync(FileSource());
        await using var _ = host;
        var request = new HttpRequestMessage(HttpMethod.Get, "/feed.xml");
        request.Headers.TryAddWithoutValidation("Host", "evil.example\"><x");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("evil", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task When_the_releases_are_not_available_the_feed_is_a_503_with_retry_after_and_not_an_exception()
    {
        var (client, host) = await StartAsync(FileSource(Path.Combine(AppContext.BaseDirectory, "no-such-file.json")));
        await using var _ = host;

        var response = await client.GetAsync("/feed.xml", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(response.Headers.Contains("Retry-After"));
        Assert.Equal("text/plain; charset=utf-8", response.Content.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task With_nothing_set_the_sample_releases_are_used_and_the_feed_works()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Changelog:File"] = Path.Combine(AppContext.BaseDirectory, "changelog-releases.json") });
        builder.Services.AddChangelog(builder.Configuration);
        await using var app = builder.Build();
        app.MapReleaseFeed("Tern releases", "What changed");
        await app.StartAsync(TestContext.Current.CancellationToken);

        var response = await app.GetTestClient().GetAsync("/feed.xml", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task With_AllowedHosts_set_a_request_for_another_host_never_reaches_the_feed_and_the_feed_of_an_allowed_host_has_only_that_host()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(FileSource()) { ["AllowedHosts"] = "news.example.com" });
        builder.Services.AddChangelog(builder.Configuration);
        await using var app = builder.Build();
        app.MapReleaseFeed("Tern releases", "What changed");
        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        string Get(string host) => host;
        async Task<HttpResponseMessage> Request(string host)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/feed.xml");
            request.Headers.Host = Get(host);
            return await client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        var evil = await Request("evil.example");
        var good = await Request("news.example.com");
        var other = await Request("news.example.com");

        Assert.Equal(HttpStatusCode.BadRequest, evil.StatusCode);
        Assert.DoesNotContain("evil", await evil.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var xml = await good.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        Assert.Contains("http://news.example.com/feed.xml", xml);
        Assert.DoesNotContain("evil", xml);
        // The same host gives the same document, with the same ETag: nothing of the request but the host is in it.
        Assert.Equal(good.Headers.ETag, other.Headers.ETag);
    }

    [Fact]
    public async Task The_github_source_without_a_repository_is_a_503_not_a_crash_and_makes_no_request()
    {
        var (client, host) = await StartAsync(new() { ["Changelog:Source"] = "GitHub", ["Changelog:Repository"] = "" });
        await using var _ = host;

        var response = await client.GetAsync("/feed.xml", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData("http://api.github.example/")]
    [InlineData("not a url")]
    public async Task An_api_address_that_cannot_be_used_is_a_503_and_not_a_crash(string api)
    {
        var (client, host) = await StartAsync(new() { ["Changelog:Source"] = "GitHub", ["Changelog:Repository"] = "octo/tern", ["Changelog:GitHubToken"] = "ghp_secret", ["Changelog:ApiUrl"] = api });
        await using var _ = host;

        var response = await client.GetAsync("/feed.xml", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task A_file_that_is_not_releases_is_a_503_too()
    {
        var path = Path.Combine(Path.GetTempPath(), $"releases-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, "{\"not\":\"an array\"}", TestContext.Current.CancellationToken);
        try
        {
            var (client, host) = await StartAsync(FileSource(path));
            await using var _ = host;

            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/feed.xml", TestContext.Current.CancellationToken)).StatusCode);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
