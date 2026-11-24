using System.Net;
using Changelog.Web.ReleaseNotes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lux.Templates.Tests;

public class GitHubReleaseClientTests
{
    private static (GitHubReleaseClient Client, FakeGitHub Handler, RecordingLogger<GitHubReleaseClient> Log) Create(Func<HttpRequestMessage, int, HttpResponseMessage> respond, TimeSpan? timeout = null)
    {
        var handler = new FakeGitHub(respond);
        var http = new HttpClient(handler);
        GitHubReleaseClient.Configure(http, new ChangelogOptions { RequestTimeout = timeout ?? TimeSpan.FromSeconds(5) });
        var log = new RecordingLogger<GitHubReleaseClient>();
        return (new GitHubReleaseClient(http, log), handler, log);
    }

    private static string One(string tag = "v1.0.0") => ChangelogSamples.Array(ChangelogSamples.Release(tag, $"Release {tag}", "Notes"));

    // ---- What is sent

    [Fact]
    public async Task It_asks_for_the_releases_of_the_repository_with_a_generic_user_agent_and_no_personal_data()
    {
        var (client, handler, _) = Create((_, _) => FakeGitHub.Json(One()));

        var outcome = await client.FetchAsync("octo/tern", null, 30, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.Ok, outcome.Status);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.github.com/repos/octo/tern/releases?per_page=30", request.Url);
        Assert.Equal("lux-template-changelog", request.Headers["User-Agent"]);
        Assert.Equal("application/vnd.github+json", request.Headers["Accept"]);
        Assert.Equal("2022-11-28", request.Headers["X-GitHub-Api-Version"]);
        // Nothing that says who runs the site or where.
        Assert.Equal(["Accept", "User-Agent", "X-GitHub-Api-Version"], request.Headers.Keys.Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task A_page_is_at_most_100_releases()
    {
        var (client, handler, _) = Create((_, _) => FakeGitHub.Json(One()));

        await client.FetchAsync("octo/tern", null, 5000, null, TestContext.Current.CancellationToken);

        Assert.EndsWith("per_page=100", Assert.Single(handler.Requests).Url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task The_token_header_is_sent_only_when_a_token_is_set(string? token)
    {
        var (client, handler, _) = Create((_, _) => FakeGitHub.Json(One()));

        await client.FetchAsync("octo/tern", token, 30, null, TestContext.Current.CancellationToken);

        Assert.DoesNotContain("Authorization", Assert.Single(handler.Requests).Headers.Keys);
    }

    [Fact]
    public async Task A_token_is_sent_as_a_bearer_header_on_every_page_and_is_never_logged()
    {
        const string token = "ghp_SECRET_TOKEN_VALUE";
        var (client, handler, log) = Create((request, n) => n == 1
            ? FakeGitHub.Json(One("v2"), link: "<https://api.github.com/repositories/1/releases?per_page=1&page=2>; rel=\"next\"")
            : n == 2 ? FakeGitHub.Json(One("v1")) : FakeGitHub.Status(HttpStatusCode.InternalServerError));

        await client.FetchAsync("octo/tern", token, 30, null, TestContext.Current.CancellationToken);
        await client.FetchAsync("octo/tern", token, 30, null, TestContext.Current.CancellationToken);

        Assert.All(handler.Requests, r => Assert.Equal("Bearer " + token, r.Headers["Authorization"]));
        Assert.DoesNotContain(log.Lines, line => line.Contains(token, StringComparison.Ordinal));
        Assert.NotEmpty(log.Lines);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task The_token_is_not_in_the_outcome_of_a_failure(HttpStatusCode status)
    {
        const string token = "ghp_SECRET_TOKEN_VALUE";
        var (client, _, log) = Create((_, _) => FakeGitHub.Status(status));

        var outcome = await client.FetchAsync("octo/tern", token, 30, null, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(token, outcome.Detail ?? "", StringComparison.Ordinal);
        Assert.DoesNotContain(log.Lines, line => line.Contains(token, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("../x")]
    [InlineData("octo/..")]
    [InlineData("octo")]
    [InlineData("octo/tern/extra")]
    [InlineData("octo/tern?x=1")]
    [InlineData("octo/tern\n")]
    [InlineData("octo /tern")]
    [InlineData("")]
    public async Task A_repository_that_is_not_owner_slash_name_is_refused_without_a_request(string repository)
    {
        var (client, handler, _) = Create((_, _) => FakeGitHub.Json(One()));

        var outcome = await client.FetchAsync(repository, "token", 30, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.Failed, outcome.Status);
        Assert.Empty(handler.Requests);
    }

    // ---- Pagination

    [Fact]
    public async Task It_follows_the_next_link_until_there_is_none_and_keeps_the_order()
    {
        var (client, handler, _) = Create((request, n) => n switch
        {
            1 => FakeGitHub.Json(ChangelogSamples.Array(ChangelogSamples.Release("v3"), ChangelogSamples.Release("v2")),
                link: "<https://api.github.com/repositories/1/releases?per_page=2&page=2>; rel=\"next\", <https://api.github.com/repositories/1/releases?per_page=2&page=2>; rel=\"last\""),
            _ => FakeGitHub.Json(ChangelogSamples.Array(ChangelogSamples.Release("v1")),
                link: "<https://api.github.com/repositories/1/releases?per_page=2&page=1>; rel=\"prev\", <https://api.github.com/repositories/1/releases?per_page=2&page=1>; rel=\"first\""),
        });

        var outcome = await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.Ok, outcome.Status);
        Assert.Equal(["v3", "v2", "v1"], outcome.Releases!.Select(r => r.Tag));
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("https://api.github.com/repositories/1/releases?per_page=2&page=2", handler.Requests[1].Url);
    }

    [Fact]
    public async Task It_stops_at_the_limit_and_cuts_the_last_page()
    {
        var (client, handler, _) = Create((request, n) => FakeGitHub.Json(
            ChangelogSamples.Array(ChangelogSamples.Release($"a{n}"), ChangelogSamples.Release($"b{n}")),
            link: "<https://api.github.com/repositories/1/releases?page=" + (n + 1) + ">; rel=\"next\""));

        var outcome = await client.FetchAsync("octo/tern", null, 3, null, TestContext.Current.CancellationToken);

        Assert.Equal(["a1", "b1", "a2"], outcome.Releases!.Select(r => r.Tag));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task A_chain_of_next_links_that_never_ends_ends_anyway()
    {
        var (client, handler, _) = Create((request, n) => FakeGitHub.Json(ChangelogSamples.Array(ChangelogSamples.Release("v" + n)),
            link: "<https://api.github.com/repositories/1/releases?page=" + (n + 1) + ">; rel=\"next\""));

        var outcome = await client.FetchAsync("octo/tern", null, 100_000, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.Ok, outcome.Status);
        Assert.Equal(10, handler.Requests.Count);
    }

    [Theory]
    [InlineData("<https://evil.example/steal>; rel=\"next\"")]
    [InlineData("<http://api.github.com/repositories/1/releases?page=2>; rel=\"next\"")]
    [InlineData("<https://api.github.com.evil.example/x>; rel=\"next\"")]
    [InlineData("<https://user:pass@api.github.com/x>; rel=\"next\"")]
    [InlineData("<https://api.github.com:8443/x>; rel=\"next\"")]
    [InlineData("</repositories/1/releases?page=2>; rel=\"next\"")]
    [InlineData("not a link header")]
    public async Task A_next_link_to_another_host_or_scheme_is_not_followed_so_the_token_goes_nowhere_else(string link)
    {
        var (client, handler, _) = Create((request, n) => FakeGitHub.Json(One(), link: link));

        var outcome = await client.FetchAsync("octo/tern", "token", 100, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.Ok, outcome.Status);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Tags_that_repeat_across_pages_still_get_one_page_each()
    {
        var (client, _, _) = Create((request, n) => n == 1
            ? FakeGitHub.Json(One("v1"), link: "<https://api.github.com/repositories/1/releases?page=2>; rel=\"next\"")
            : FakeGitHub.Json(One("v1")));

        var outcome = await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken);

        Assert.Equal(["v1", "v1-2"], outcome.Releases!.Select(r => r.Slug));
    }

    // ---- ETag

    [Fact]
    public async Task The_etag_of_the_first_page_is_returned_and_sent_back_and_a_304_is_not_modified()
    {
        var (client, handler, _) = Create((request, n) => request.Headers.TryGetValues("If-None-Match", out var etag) && etag.Single() == "W/\"abc\""
            ? FakeGitHub.Status(HttpStatusCode.NotModified)
            : FakeGitHub.Json(One(), etag: "W/\"abc\""));

        var first = await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken);
        var second = await client.FetchAsync("octo/tern", null, 100, first.ETag, TestContext.Current.CancellationToken);

        Assert.Equal("W/\"abc\"", first.ETag);
        Assert.False(handler.Requests[0].Headers.ContainsKey("If-None-Match"));
        Assert.Equal("W/\"abc\"", handler.Requests[1].Headers["If-None-Match"]);
        Assert.Equal(FetchStatus.NotModified, second.Status);
        Assert.Null(second.Releases);
    }

    [Fact]
    public async Task A_changed_page_with_an_old_etag_is_read_in_full_with_the_new_etag()
    {
        var (client, _, _) = Create((_, _) => FakeGitHub.Json(One("v2"), etag: "\"new\""));

        var outcome = await client.FetchAsync("octo/tern", null, 100, "\"old\"", TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.Ok, outcome.Status);
        Assert.Equal("\"new\"", outcome.ETag);
    }

    // ---- Rate limit

    [Fact]
    public async Task A_403_with_no_requests_left_is_a_rate_limit_that_says_when_it_resets()
    {
        var reset = DateTimeOffset.UtcNow.AddMinutes(17).ToUnixTimeSeconds();
        var (client, _, _) = Create((_, _) => FakeGitHub.Status(HttpStatusCode.Forbidden, ("X-RateLimit-Remaining", "0"), ("X-RateLimit-Reset", reset.ToString())));

        var outcome = await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.RateLimited, outcome.Status);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(reset), outcome.RetryAt);
    }

    [Fact]
    public async Task A_429_with_retry_after_is_a_rate_limit_that_says_how_long_to_wait()
    {
        var (client, _, _) = Create((_, _) => FakeGitHub.Status(HttpStatusCode.TooManyRequests, ("Retry-After", "120")));

        var outcome = await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.RateLimited, outcome.Status);
        Assert.InRange((outcome.RetryAt!.Value - DateTimeOffset.UtcNow).TotalSeconds, 100, 125);
    }

    [Fact]
    public async Task A_secondary_limit_403_with_retry_after_is_a_rate_limit()
    {
        var (client, _, _) = Create((_, _) => FakeGitHub.Status(HttpStatusCode.Forbidden, ("Retry-After", "30"), ("X-RateLimit-Remaining", "41")));

        var outcome = await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.RateLimited, outcome.Status);
    }

    [Fact]
    public async Task A_429_with_no_headers_is_a_rate_limit_with_no_known_end()
    {
        var (client, _, _) = Create((_, _) => FakeGitHub.Status(HttpStatusCode.TooManyRequests));

        var outcome = await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.RateLimited, outcome.Status);
        Assert.Null(outcome.RetryAt);
    }

    [Fact]
    public async Task A_403_that_is_a_refusal_and_not_a_limit_is_a_failure()
    {
        var (client, _, _) = Create((_, _) => FakeGitHub.Status(HttpStatusCode.Forbidden, ("X-RateLimit-Remaining", "4999")));

        var outcome = await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.Failed, outcome.Status);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("99999999999999999999")]
    [InlineData("soon")]
    public async Task A_reset_header_that_makes_no_sense_gives_no_retry_time_instead_of_failing(string reset)
    {
        var (client, _, _) = Create((_, _) => FakeGitHub.Status(HttpStatusCode.Forbidden, ("X-RateLimit-Remaining", "0"), ("X-RateLimit-Reset", reset)));

        var outcome = await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.RateLimited, outcome.Status);
        Assert.Null(outcome.RetryAt);
    }

    // ---- Errors never throw

    [Fact]
    public async Task A_404_is_not_found()
    {
        var (client, _, _) = Create((_, _) => FakeGitHub.Status(HttpStatusCode.NotFound));

        Assert.Equal(FetchStatus.NotFound, (await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken)).Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.Gone)]
    public async Task Another_error_status_is_a_failure_with_its_code(HttpStatusCode status)
    {
        var (client, _, _) = Create((_, _) => FakeGitHub.Status(status));

        var outcome = await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.Failed, outcome.Status);
        Assert.Contains(((int)status).ToString(), outcome.Detail);
    }

    [Fact]
    public async Task A_network_error_is_a_failure_not_an_exception()
    {
        var (client, _, _) = Create((_, _) => throw new HttpRequestException("Name or service not known"));

        var outcome = await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.Failed, outcome.Status);
        Assert.Contains("Name or service not known", outcome.Detail);
    }

    [Theory]
    [InlineData("<html>not json</html>")]
    [InlineData("{\"message\":\"Not an array\"}")]
    [InlineData("")]
    [InlineData("[{\"tag_name\":")]
    [InlineData("null")]
    public async Task An_answer_that_is_not_a_list_of_releases_is_a_failure(string body)
    {
        var (client, _, _) = Create((_, _) => FakeGitHub.Json(body));

        var outcome = await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.Failed, outcome.Status);
    }

    [Fact]
    public async Task A_request_that_takes_longer_than_the_timeout_is_a_failure()
    {
        var handler = new SlowHandler();
        var http = new HttpClient(handler);
        GitHubReleaseClient.Configure(http, new ChangelogOptions { RequestTimeout = TimeSpan.FromMilliseconds(150) });
        var client = new GitHubReleaseClient(http, NullLogger<GitHubReleaseClient>.Instance);

        var outcome = await client.FetchAsync("octo/tern", null, 100, null, TestContext.Current.CancellationToken);

        Assert.Equal(FetchStatus.Failed, outcome.Status);
        Assert.Contains("in time", outcome.Detail);
    }

    [Fact]
    public async Task Cancelling_the_fetch_cancels_it()
    {
        var http = new HttpClient(new SlowHandler());
        GitHubReleaseClient.Configure(http, new ChangelogOptions());
        var client = new GitHubReleaseClient(http, NullLogger<GitHubReleaseClient>.Instance);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.FetchAsync("octo/tern", null, 100, null, cancel.Token));
    }

    [Fact]
    public async Task A_huge_body_is_cut_and_a_hostile_number_of_releases_is_capped_by_the_limit()
    {
        var big = new string('x', GitHubReleaseJson.MaxBodyLength * 3);
        var (client, _, _) = Create((_, _) => FakeGitHub.Json(ChangelogSamples.Array(Enumerable.Range(0, 500).Select(i => ChangelogSamples.Release("v" + i, body: i == 0 ? big : "")).ToArray())));

        var outcome = await client.FetchAsync("octo/tern", null, 25, null, TestContext.Current.CancellationToken);

        Assert.Equal(25, outcome.Releases!.Count);
        Assert.True(outcome.Releases[0].Body.Length <= GitHubReleaseJson.MaxBodyLength + 10);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("http://api.github.example/")]
    [InlineData("ftp://x/")]
    [InlineData("")]
    public void An_api_address_that_is_not_https_is_never_used_and_never_throws(string api)
    {
        var handler = new FakeGitHub((_, _) => FakeGitHub.Json(One()));
        var http = new HttpClient(handler);

        GitHubReleaseClient.Configure(http, new ChangelogOptions { ApiUrl = api });

        Assert.Equal("https://api.github.com/", http.BaseAddress!.ToString());
        var source = new GitHubReleaseSource(new GitHubReleaseClient(http, NullLogger<GitHubReleaseClient>.Instance),
            Microsoft.Extensions.Options.Options.Create(new ChangelogOptions { Repository = "octo/tern", ApiUrl = api, GitHubToken = "ghp_secret" }));
        Assert.False(source.IsConfigured);
    }

    [Fact]
    public void A_source_is_configured_with_a_valid_repository_and_settings_that_can_work()
    {
        var http = new HttpClient(new FakeGitHub((_, _) => FakeGitHub.Json(One())));
        var client = new GitHubReleaseClient(http, NullLogger<GitHubReleaseClient>.Instance);

        Assert.True(new GitHubReleaseSource(client, Microsoft.Extensions.Options.Options.Create(new ChangelogOptions { Repository = "octo/tern" })).IsConfigured);
        Assert.False(new GitHubReleaseSource(client, Microsoft.Extensions.Options.Options.Create(new ChangelogOptions { Repository = "" })).IsConfigured);
        Assert.False(new GitHubReleaseSource(client, Microsoft.Extensions.Options.Options.Create(new ChangelogOptions { Repository = "octo/tern", PageSize = 0 })).IsConfigured);
        Assert.Equal("https://github.com/octo/tern", new GitHubReleaseSource(client, Microsoft.Extensions.Options.Options.Create(new ChangelogOptions { Repository = "octo/tern" })).RepositoryUrl);
    }

    private sealed class SlowHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            return FakeGitHub.Json("[]");
        }
    }
}
