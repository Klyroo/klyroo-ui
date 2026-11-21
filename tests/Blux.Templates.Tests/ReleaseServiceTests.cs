using Changelog.Web.ReleaseNotes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Blux.Templates.Tests;

public class ReleaseServiceTests
{
    private sealed class FakeSource(Func<string?, int, Task<FetchOutcome>> fetch, bool configured = true) : IReleaseSource
    {
        public int Calls;
        public List<string?> ETags { get; } = [];
        public bool IsConfigured { get; } = configured;
        public string? RepositoryUrl => "https://github.com/octo/tern";

        public Task<FetchOutcome> FetchAsync(string? etag, CancellationToken cancellationToken)
        {
            int n;
            lock (ETags)
            {
                ETags.Add(etag);
                n = ++Calls;
            }
            return fetch(etag, n);
        }
    }

    private static FetchOutcome Ok(string etag = "\"e1\"", params string[] tags) =>
        new(FetchStatus.Ok, [.. (tags.Length == 0 ? ["v1"] : tags).Select(t => ChangelogSamples.Entry(t))], etag);

    private static (ReleaseService Service, FakeSource Source, ManualTime Time) Create(Func<string?, int, Task<FetchOutcome>> fetch, bool configured = true)
    {
        var source = new FakeSource(fetch, configured);
        var time = new ManualTime(DateTimeOffset.Parse("2026-06-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var service = new ReleaseService(source, new MemoryCache(new MemoryCacheOptions()), Options.Create(new ChangelogOptions { CacheDuration = TimeSpan.FromHours(1) }), time, NullLogger<ReleaseService>.Instance);
        return (service, source, time);
    }

    private static Func<string?, int, Task<FetchOutcome>> Always(FetchOutcome outcome) => (_, _) => Task.FromResult(outcome);

    // ---- Cache

    [Fact]
    public async Task The_first_request_reads_the_releases_and_the_next_ones_within_the_hour_do_not()
    {
        var (service, source, time) = Create(Always(Ok(tags: ["v2", "v1"])));

        var first = await service.GetAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(59));
        var later = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ReleaseStatus.Ok, first.Status);
        Assert.Equal(["v2", "v1"], first.Releases.Select(r => r.Tag));
        Assert.Equal(ReleaseStatus.Ok, later.Status);
        Assert.Equal(1, source.Calls);
        Assert.Same(first.Releases, later.Releases);
        Assert.Equal("https://github.com/octo/tern", later.RepositoryUrl);
    }

    [Fact]
    public async Task Requests_that_arrive_while_the_first_read_runs_share_it()
    {
        var gate = new TaskCompletionSource<FetchOutcome>();
        var (service, source, _) = Create((_, _) => gate.Task);

        var requests = Enumerable.Range(0, 20).Select(_ => service.GetAsync(TestContext.Current.CancellationToken)).ToList();
        await Task.Delay(100, TestContext.Current.CancellationToken);
        gate.SetResult(Ok());
        var lists = await Task.WhenAll(requests);

        Assert.Equal(1, source.Calls);
        Assert.All(lists, list => Assert.Equal(ReleaseStatus.Ok, list.Status));
    }

    [Fact]
    public async Task After_the_hour_the_visitor_gets_the_saved_copy_at_once_and_the_refresh_runs_in_the_background()
    {
        var gate = new TaskCompletionSource<FetchOutcome>();
        var (service, source, time) = Create((_, n) => n == 1 ? Task.FromResult(Ok("\"e1\"", "v1")) : gate.Task);
        await service.GetAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(61));

        var stale = await service.GetAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var meanwhile = await service.GetAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var refreshing = service.WhenIdleAsync();
        gate.SetResult(Ok("\"e2\"", "v2", "v1"));
        await refreshing;
        var fresh = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["v1"], stale.Releases.Select(r => r.Tag));
        Assert.Equal(ReleaseStatus.Ok, stale.Status);
        Assert.Equal(["v1"], meanwhile.Releases.Select(r => r.Tag));
        Assert.Equal(2, source.Calls);
        Assert.Equal(["v2", "v1"], fresh.Releases.Select(r => r.Tag));
        Assert.Equal(["v2", "v1"], Assert.IsAssignableFrom<IReadOnlyList<ReleaseEntry>>(fresh.Releases).Select(r => r.Tag));
    }

    [Fact]
    public async Task The_refresh_sends_the_etag_of_the_last_answer_and_a_not_modified_keeps_the_copy_and_makes_it_fresh_again()
    {
        var (service, source, time) = Create((etag, n) => Task.FromResult(n == 1 ? Ok("\"e1\"", "v1") : new FetchOutcome(FetchStatus.NotModified, ETag: etag)));
        var first = await service.GetAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(61));
        await service.GetAsync(TestContext.Current.CancellationToken);
        await service.WhenIdleAsync();

        var after = await service.GetAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(30));
        var stillFresh = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal([null, "\"e1\""], source.ETags.Take(2));
        Assert.Same(first.Releases, after.Releases);
        Assert.Equal(ReleaseStatus.Ok, stillFresh.Status);
        Assert.Equal(2, source.Calls);
        Assert.Equal(time.GetUtcNow() - TimeSpan.FromMinutes(30), after.FetchedAt);
    }

    [Fact]
    public async Task A_cache_duration_of_zero_reads_in_the_background_on_every_request_after_the_first()
    {
        var source = new FakeSource(Always(Ok()));
        var service = new ReleaseService(source, new MemoryCache(new MemoryCacheOptions()), Options.Create(new ChangelogOptions { CacheDuration = TimeSpan.Zero }), TimeProvider.System, NullLogger<ReleaseService>.Instance);

        await service.GetAsync(TestContext.Current.CancellationToken);
        await service.GetAsync(TestContext.Current.CancellationToken);
        await service.WhenIdleAsync();

        Assert.True(source.Calls >= 2);
    }

    // ---- Rate limit and failures

    [Fact]
    public async Task A_rate_limit_keeps_the_saved_copy_marks_it_stale_and_asks_again_only_after_the_reset()
    {
        var reset = DateTimeOffset.Parse("2026-06-01T13:30:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var (service, source, time) = Create((_, n) => Task.FromResult(n switch
        {
            1 => Ok("\"e1\"", "v1"),
            2 => new FetchOutcome(FetchStatus.RateLimited, RetryAt: reset),
            _ => Ok("\"e3\"", "v3", "v1"),
        }));
        await service.GetAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(61));
        await service.GetAsync(TestContext.Current.CancellationToken);
        await service.WhenIdleAsync();

        var limited = await service.GetAsync(TestContext.Current.CancellationToken);
        for (var i = 0; i < 5; i++) await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ReleaseStatus.Stale, limited.Status);
        Assert.Equal(ReleaseStatus.RateLimited, limited.Problem);
        Assert.Equal(["v1"], limited.Releases.Select(r => r.Tag));
        Assert.Equal(reset, limited.RetryAt);
        Assert.True(limited.Available);
        Assert.Equal(2, source.Calls);

        time.Advance(TimeSpan.FromMinutes(40));
        var back = await service.GetAsync(TestContext.Current.CancellationToken);
        await service.WhenIdleAsync();
        var recovered = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ReleaseStatus.Ok, back.Status);
        Assert.Equal(3, source.Calls);
        Assert.Equal(["v3", "v1"], recovered.Releases.Select(r => r.Tag));
        Assert.Equal(ReleaseStatus.Ok, recovered.Status);
    }

    [Fact]
    public async Task A_rate_limit_with_no_copy_is_a_status_with_no_releases_and_is_not_asked_again_until_it_is_over()
    {
        var (service, source, time) = Create(Always(new FetchOutcome(FetchStatus.RateLimited, RetryAt: DateTimeOffset.Parse("2026-06-01T12:20:00Z", System.Globalization.CultureInfo.InvariantCulture))));

        var first = await service.GetAsync(TestContext.Current.CancellationToken);
        var second = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ReleaseStatus.RateLimited, first.Status);
        Assert.Empty(first.Releases);
        Assert.False(first.Available);
        Assert.Equal(ReleaseStatus.RateLimited, second.Status);
        Assert.Equal(1, source.Calls);

        time.Advance(TimeSpan.FromMinutes(21));
        await service.GetAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, source.Calls);
    }

    [Theory]
    [InlineData("2026-06-01T11:00:00Z", 1)]
    [InlineData("2026-06-02T12:00:00Z", 60)]
    public async Task The_wait_after_a_rate_limit_is_between_a_minute_and_an_hour(string retryAt, int minutes)
    {
        var (service, source, time) = Create(Always(new FetchOutcome(FetchStatus.RateLimited, RetryAt: DateTimeOffset.Parse(retryAt, System.Globalization.CultureInfo.InvariantCulture))));
        await service.GetAsync(TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromMinutes(minutes) - TimeSpan.FromSeconds(1));
        await service.GetAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, source.Calls);

        time.Advance(TimeSpan.FromSeconds(2));
        await service.GetAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, source.Calls);
    }

    [Fact]
    public async Task A_rate_limit_that_gives_no_time_waits_five_minutes()
    {
        var (service, source, time) = Create(Always(new FetchOutcome(FetchStatus.RateLimited)));
        await service.GetAsync(TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromMinutes(4));
        await service.GetAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(2));
        await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, source.Calls);
    }

    [Theory]
    [InlineData(FetchStatus.Failed, ReleaseStatus.Unavailable)]
    [InlineData(FetchStatus.NotFound, ReleaseStatus.NotFound)]
    public async Task A_failure_with_no_copy_says_what_it_is_and_the_next_read_is_after_a_pause(FetchStatus failure, ReleaseStatus expected)
    {
        var (service, source, time) = Create((_, n) => Task.FromResult(n == 1 ? new FetchOutcome(failure, Detail: "boom") : Ok()));

        var failed = await service.GetAsync(TestContext.Current.CancellationToken);
        var again = await service.GetAsync(TestContext.Current.CancellationToken);
        var callsBeforePause = source.Calls;
        time.Advance(TimeSpan.FromMinutes(6));
        var recovered = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expected, failed.Status);
        Assert.Empty(failed.Releases);
        Assert.Equal(expected, again.Status);
        Assert.Equal(1, callsBeforePause);
        Assert.Equal(ReleaseStatus.Ok, recovered.Status);
        Assert.Equal(2, source.Calls);
    }

    [Fact]
    public async Task A_failure_with_a_copy_keeps_showing_it_as_stale()
    {
        var (service, source, time) = Create((_, n) => Task.FromResult(n == 1 ? Ok("\"e1\"", "v1") : new FetchOutcome(FetchStatus.Failed, Detail: "down")));
        await service.GetAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(61));
        await service.GetAsync(TestContext.Current.CancellationToken);
        await service.WhenIdleAsync();

        var list = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ReleaseStatus.Stale, list.Status);
        Assert.Equal(ReleaseStatus.Unavailable, list.Problem);
        Assert.Equal(["v1"], list.Releases.Select(r => r.Tag));
        Assert.NotNull(list.RetryAt);
    }

    [Fact]
    public async Task A_source_that_throws_is_a_failure_not_an_exception()
    {
        var (service, _, _) = Create((_, _) => throw new InvalidOperationException("surprise"));

        var list = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ReleaseStatus.Unavailable, list.Status);
    }

    [Fact]
    public async Task A_not_modified_with_no_copy_is_a_failure()
    {
        var (service, _, _) = Create(Always(new FetchOutcome(FetchStatus.NotModified)));

        Assert.Equal(ReleaseStatus.Unavailable, (await service.GetAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task Without_a_repository_nothing_is_asked_for()
    {
        var (service, source, _) = Create(Always(Ok()), configured: false);

        var list = await service.GetAsync(TestContext.Current.CancellationToken);
        await service.RefreshNowAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ReleaseStatus.NotConfigured, list.Status);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task A_visitor_who_leaves_stops_waiting_but_not_the_read()
    {
        var gate = new TaskCompletionSource<FetchOutcome>();
        var (service, source, _) = Create((_, _) => gate.Task);
        using var leaves = new CancellationTokenSource();

        var waiting = service.GetAsync(leaves.Token);
        await leaves.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        gate.SetResult(Ok());
        await service.WhenIdleAsync();
        var list = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ReleaseStatus.Ok, list.Status);
        Assert.Equal(1, source.Calls);
    }

    [Fact]
    public async Task Find_looks_a_release_up_by_slug_and_never_by_anything_else()
    {
        var (service, _, _) = Create(Always(Ok("\"e\"", "v1.0", "release/2")));
        var list = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal("v1.0", list.Find("v1.0")!.Tag);
        Assert.Equal("v1.0", list.Find("V1.0")!.Tag);
        Assert.Equal("release/2", list.Find("release-2")!.Tag);
        Assert.Null(list.Find("release/2"));
        Assert.Null(list.Find("../v1.0"));
        Assert.Null(list.Find(""));
        Assert.Null(list.Find(null));
    }
}
