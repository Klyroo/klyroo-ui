using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Changelog.Web.ReleaseNotes;

public enum ReleaseStatus
{
    /// <summary>The releases are current (read within <c>Changelog:CacheDuration</c>, or the copy is still being used while it refreshes).</summary>
    Ok,

    /// <summary>The last attempt to refresh failed: the releases are the last copy that worked, see <see cref="ReleaseList.Problem"/>.</summary>
    Stale,

    /// <summary>No repository is set (<c>Changelog:Repository</c>), so nothing was asked for.</summary>
    NotConfigured,

    /// <summary>No copy to show, and GitHub's rate limit was reached.</summary>
    RateLimited,

    /// <summary>No copy to show, and the repository does not exist or cannot be seen.</summary>
    NotFound,

    /// <summary>No copy to show, and GitHub or the network failed, or the file could not be read.</summary>
    Unavailable,
}

/// <summary>What a page gets: the releases, when they were read, and what is wrong when something is.</summary>
/// <param name="Status">See <see cref="ReleaseStatus"/>.</param>
/// <param name="Releases">Newest first. Empty unless <see cref="Status"/> is <see cref="ReleaseStatus.Ok"/> or <see cref="ReleaseStatus.Stale"/> (or the repository has no release).</param>
/// <param name="FetchedAt">When the releases shown were read, or null when there are none.</param>
/// <param name="Problem">For <see cref="ReleaseStatus.Stale"/>: why the copy could not be refreshed.</param>
/// <param name="RetryAt">When the service will ask again after a problem, if it knows.</param>
/// <param name="RepositoryUrl">The repository on GitHub, or null.</param>
public sealed record ReleaseList(
    ReleaseStatus Status,
    IReadOnlyList<ReleaseEntry> Releases,
    DateTimeOffset? FetchedAt,
    ReleaseStatus? Problem,
    DateTimeOffset? RetryAt,
    string? RepositoryUrl)
{
    /// <summary>True when there are releases to show, current or not.</summary>
    public bool Available => Status is ReleaseStatus.Ok or ReleaseStatus.Stale;

    public ReleaseEntry? Find(string? slug) =>
        string.IsNullOrEmpty(slug) ? null : Releases.FirstOrDefault(r => string.Equals(r.Slug, slug, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// The releases for the pages and the feed. One copy is kept in memory (<see cref="IMemoryCache"/>) for <c>Changelog:CacheDuration</c>, an hour by default.
/// <list type="bullet">
/// <item>The first request waits for the first read. Requests that arrive meanwhile share it: GitHub is asked once, not once per visitor.</item>
/// <item>After the hour the next request gets the saved copy at once and a refresh starts in the background, so no visitor waits for GitHub.
///   The refresh sends the ETag of the last answer: if nothing changed, GitHub answers 304, which does not count against the rate limit.</item>
/// <item>When GitHub answers with its rate limit, a 5xx, or does not answer, the saved copy keeps being shown (<see cref="ReleaseStatus.Stale"/>) and the
///   service does not ask again until the limit resets (at most an hour) or for a minute after another failure. With no saved copy the status says what
///   happened and the page explains it; nothing here throws.</item>
/// </list>
/// </summary>
public sealed class ReleaseService
{
    private const string CacheKey = "changelog:releases";
    private static readonly TimeSpan FailureBackoff = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan NotFoundBackoff = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultRateLimitBackoff = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(1);

    private sealed record Snapshot(IReadOnlyList<ReleaseEntry> Releases, string? ETag, DateTimeOffset FetchedAt);

    private readonly IReleaseSource _source;
    private readonly IMemoryCache _cache;
    private readonly ChangelogOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<ReleaseService> _logger;
    private readonly object _gate = new();

    private Snapshot? _last;
    private Task? _refresh;
    private ReleaseStatus? _failure;
    private DateTimeOffset _retryAt;

    public ReleaseService(IReleaseSource source, IMemoryCache cache, IOptions<ChangelogOptions> options, TimeProvider time, ILogger<ReleaseService> logger)
    {
        _source = source;
        _cache = cache;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    /// <summary>The releases now. Waits only when there is no copy at all; <paramref name="cancellationToken"/> stops the wait, not the read.</summary>
    public async Task<ReleaseList> GetAsync(CancellationToken cancellationToken)
    {
        if (!_source.IsConfigured)
        {
            return new ReleaseList(ReleaseStatus.NotConfigured, [], null, null, null, _source.RepositoryUrl);
        }
        if (_cache.TryGetValue(CacheKey, out Snapshot? fresh) && fresh is not null && _time.GetUtcNow() - fresh.FetchedAt < _options.CacheDuration)
        {
            return Of(fresh, ReleaseStatus.Ok, null, default);
        }

        Task refresh;
        Snapshot? last;
        lock (_gate)
        {
            last = _last;
            var now = _time.GetUtcNow();
            if (now < _retryAt && _failure is { } failure)
            {
                // Asked and failed a moment ago: do not ask again yet.
                return last is null
                    ? new ReleaseList(failure, [], null, null, _retryAt, _source.RepositoryUrl)
                    : Of(last, ReleaseStatus.Stale, failure, _retryAt);
            }
            _refresh ??= Task.Run(() => RefreshAsync());
            refresh = _refresh;
        }

        if (last is not null)
        {
            // Stale while it revalidates: the visitor gets the saved copy now.
            return Of(last, ReleaseStatus.Ok, null, default);
        }
        await refresh.WaitAsync(cancellationToken);
        return await GetAfterRefreshAsync();
    }

    /// <summary>The refresh that is running now, or a completed task. A refresh never throws, so awaiting this is safe.</summary>
    public Task WhenIdleAsync()
    {
        lock (_gate)
        {
            return _refresh ?? Task.CompletedTask;
        }
    }

    /// <summary>Reads the releases again now and waits for it (a refresh that is already running is shared). For a start-up warm-up and for tests.</summary>
    public async Task RefreshNowAsync(CancellationToken cancellationToken = default)
    {
        if (!_source.IsConfigured)
        {
            return;
        }
        Task refresh;
        lock (_gate)
        {
            _refresh ??= Task.Run(() => RefreshAsync());
            refresh = _refresh;
        }
        await refresh.WaitAsync(cancellationToken);
    }

    private Task<ReleaseList> GetAfterRefreshAsync()
    {
        Snapshot? last;
        ReleaseStatus? failure;
        DateTimeOffset retryAt;
        lock (_gate)
        {
            (last, failure, retryAt) = (_last, _failure, _retryAt);
        }
        if (last is not null)
        {
            return Task.FromResult(Of(last, failure is null ? ReleaseStatus.Ok : ReleaseStatus.Stale, failure, retryAt));
        }
        return Task.FromResult(new ReleaseList(failure ?? ReleaseStatus.Unavailable, [], null, null, retryAt, _source.RepositoryUrl));
    }

    private ReleaseList Of(Snapshot snapshot, ReleaseStatus status, ReleaseStatus? problem, DateTimeOffset retryAt) =>
        new(status, snapshot.Releases, snapshot.FetchedAt, problem, problem is null ? null : retryAt, _source.RepositoryUrl);

    // Never throws: whatever goes wrong becomes a failure state, because the caller may be a background task nobody observes.
    private async Task RefreshAsync()
    {
        FetchOutcome outcome;
        try
        {
            outcome = await _source.FetchAsync(_last?.ETag, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Reading the releases failed");
            outcome = new FetchOutcome(FetchStatus.Failed, Detail: "unexpected error");
        }

        lock (_gate)
        {
            var now = _time.GetUtcNow();
            switch (outcome.Status)
            {
                case FetchStatus.Ok when outcome.Releases is not null:
                    Store(new Snapshot(outcome.Releases, outcome.ETag, now));
                    break;
                case FetchStatus.NotModified when _last is not null:
                    _logger.LogDebug("The releases did not change (304)");
                    Store(_last with { FetchedAt = now });
                    break;
                case FetchStatus.RateLimited:
                    var wait = outcome.RetryAt is { } at ? at - now : DefaultRateLimitBackoff;
                    Fail(ReleaseStatus.RateLimited, now, Clamp(wait, FailureBackoff, MaxBackoff), outcome.Detail);
                    break;
                case FetchStatus.NotFound:
                    Fail(ReleaseStatus.NotFound, now, NotFoundBackoff, outcome.Detail);
                    break;
                default:
                    Fail(ReleaseStatus.Unavailable, now, FailureBackoff, outcome.Detail);
                    break;
            }
            _refresh = null;
        }
    }

    private void Store(Snapshot snapshot)
    {
        _last = snapshot;
        _failure = null;
        _retryAt = default;
        if (_options.CacheDuration > TimeSpan.Zero)
        {
            _cache.Set(CacheKey, snapshot, _options.CacheDuration);
        }
    }

    private void Fail(ReleaseStatus failure, DateTimeOffset now, TimeSpan backoff, string? detail)
    {
        _failure = failure;
        _retryAt = now + backoff;
        _logger.LogWarning("The releases could not be refreshed ({Failure}): {Detail}. {Copy}; the next attempt is not before {RetryAt:u}",
            failure, detail, _last is null ? "There is no saved copy" : "The saved copy is kept", _retryAt);
    }

    private static TimeSpan Clamp(TimeSpan value, TimeSpan min, TimeSpan max) => value < min ? min : value > max ? max : value;
}
