using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using NetVips;

namespace Lux.UI.Images.Local.Tests;

/// <summary>Rate limiting, eviction order, cancellation and coalescing, observed through the processing hook.</summary>
public class ConcurrencyTests(EndpointFixture fixture) : IClassFixture<EndpointFixture>
{
    private const string Url = "/_blux/img?src=";

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }

    [Fact]
    public async Task One_client_cannot_start_more_computations_than_its_allowance_but_cached_variants_stay_free()
    {
        var (app, client) = await fixture.StartAsync(o => { o.RateLimitPermits = 3; o.RateLimitWindow = TimeSpan.FromMinutes(5); });
        await using var _ = app;

        foreach (var width in new[] { 16, 32, 48 })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Url}/img/photo.png&w={width}&f=webp")).StatusCode);
        }
        var refused = await client.GetAsync($"{Url}/img/photo.png&w=64&f=webp");
        var cached = await client.GetAsync($"{Url}/img/photo.png&w=32&f=webp");

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.True(refused.Headers.RetryAfter!.Delta!.Value.TotalSeconds >= 1);
        Assert.Equal(HttpStatusCode.OK, cached.StatusCode);
    }

    [Fact]
    public async Task The_rate_limit_can_be_turned_off()
    {
        var (app, client) = await fixture.StartAsync(o => o.RateLimitPermits = 0);
        await using var _ = app;

        foreach (var width in new[] { 16, 32, 48, 64, 96 })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Url}/img/photo.png&w={width}&f=webp")).StatusCode);
        }
    }

    [Fact]
    public async Task The_least_recently_used_variant_is_the_one_evicted()
    {
        var processed = new List<int?>();
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.MaxVariantsPerSource = 2;
            o.AccessTouchInterval = TimeSpan.Zero;
            o.OnProcess = (request, _) => { lock (processed) processed.Add(request.Width); };
        });
        await using var _ = app;

        await client.GetAsync($"{Url}/img/photo.png&w=16&f=webp");
        await Task.Delay(30);
        await client.GetAsync($"{Url}/img/photo.png&w=32&f=webp");
        await Task.Delay(30);
        await client.GetAsync($"{Url}/img/photo.png&w=16&f=webp");   // a hit: 16 is now newer than 32
        await Task.Delay(30);
        await client.GetAsync($"{Url}/img/photo.png&w=48&f=webp");   // evicts 32
        await client.GetAsync($"{Url}/img/photo.png&w=16&f=webp");   // still cached
        await client.GetAsync($"{Url}/img/photo.png&w=32&f=webp");   // computed again

        Assert.Equal([16, 32, 48, 32], processed);
    }

    [Fact]
    public async Task Identical_requests_share_one_processing_run()
    {
        var runs = 0;
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.MaxConcurrency = 8;
            o.OnProcess = (_, _) => { Interlocked.Increment(ref runs); Thread.Sleep(300); };
        });
        await using var _ = app;

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.GetAsync($"{Url}/img/photo.png&w=256&f=webp")));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(1, runs);
        Assert.Single(responses.Select(r => r.Headers.ETag!.Tag).Distinct());
    }

    [Fact]
    public async Task A_client_that_disconnects_cancels_work_nobody_else_waits_for()
    {
        var seen = new TaskCompletionSource<bool>();
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.OnProcess = (_, token) =>
            {
                var canceled = token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                seen.TrySetResult(canceled);
            };
        });
        await using var _ = app;
        using var source = new CancellationTokenSource();

        var call = client.GetAsync($"{Url}/img/photo.png&w=256&f=webp", source.Token);
        await Task.Delay(200);
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => call);

        Assert.True(await seen.Task.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public async Task One_client_leaving_does_not_cancel_work_another_still_waits_for()
    {
        var canceled = false;
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.OnProcess = (_, token) => { Thread.Sleep(400); canceled = token.IsCancellationRequested; };
        });
        await using var _ = app;
        using var source = new CancellationTokenSource();

        var leaving = client.GetAsync($"{Url}/img/photo.png&w=256&f=webp", source.Token);
        var staying = client.GetAsync($"{Url}/img/photo.png&w=256&f=webp");
        await Task.Delay(150);
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => leaving);

        Assert.Equal(HttpStatusCode.OK, (await staying).StatusCode);
        Assert.False(canceled);
    }

    [Fact]
    public async Task Cancellation_stops_libvips_and_frees_the_slot()
    {
        var name = "big-" + Guid.NewGuid().ToString("N") + ".jpg";
        using (var big = Image.Sines(9000, 9000, hfreq: 0.05, vfreq: 0.03).Linear([127.5], [127.5]).Cast(Enums.BandFormat.Uchar))
        {
            File.WriteAllBytes(Path.Combine(fixture.WebRoot, "img", name), big.WriteToBuffer(".jpg[Q=40]"));
        }
        var (app, client) = await fixture.StartAsync(o => { o.MaxConcurrency = 1; o.QueueTimeout = TimeSpan.FromMilliseconds(250); o.MaxWidth = 8000; o.MaxHeight = 8000; });
        await using var _ = app;
        using var source = new CancellationTokenSource();

        // No size and a blur: the heaviest request the endpoint accepts.
        var heavy = client.GetAsync($"{Url}/img/{name}&f=png&blur=20", source.Token);
        await Task.Delay(700);
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => heavy);
        var started = DateTime.UtcNow;
        var next = await client.GetAsync($"{Url}/img/photo.png&w=16&f=webp");

        Assert.Equal(HttpStatusCode.OK, next.StatusCode);
        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task The_background_trim_keeps_the_cache_within_its_limit_without_blocking_requests()
    {
        var (app, client) = await fixture.StartAsync(o => o.MaxCacheBytes = 12000);
        await using var _ = app;
        var processor = app.Services.GetRequiredService<BxImageProcessor>();

        foreach (var width in new[] { 16, 32, 48, 64, 96, 128, 256 })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Url}/img/photo.png&w={width}&f=png")).StatusCode);
        }
        await processor.DiskCache.IdleAsync();
        processor.DiskCache.SchedulePrune();
        await processor.DiskCache.IdleAsync();

        Assert.True(processor.DiskCache.Bytes <= 12000, processor.DiskCache.Bytes.ToString());
    }

    [Fact]
    public async Task A_temporary_file_of_a_write_in_progress_survives_a_trim_but_a_stale_one_goes()
    {
        string? cache = null;
        var (app, client) = await fixture.StartAsync(o => { cache = o.CacheDirectory; o.MaxCacheBytes = 4000; });
        await using var _ = app;
        var processor = app.Services.GetRequiredService<BxImageProcessor>();
        var directory = Path.Combine(cache!, "0123456789abcdef0123456789abcdef");
        Directory.CreateDirectory(directory);
        var fresh = Path.Combine(directory, "a.png.0001.tmp");
        var stale = Path.Combine(directory, "b.png.0002.tmp");
        File.WriteAllBytes(fresh, new byte[100]);
        File.WriteAllBytes(stale, new byte[100]);
        File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddHours(-1));

        processor.DiskCache.SchedulePrune();
        await processor.DiskCache.IdleAsync();

        Assert.True(File.Exists(fresh));
        Assert.False(File.Exists(stale));
    }

    [Fact]
    public async Task A_remote_fetch_for_many_variants_of_one_address_happens_once()
    {
        var fetched = 0;
        var png = EndpointFixture.Png(600, 300);
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.MaxConcurrency = 8;
            o.AllowedRemoteHosts.Add("images.test");
            o.RemoteHandlerFactory = () => new StubHandler(_ =>
            {
                Interlocked.Increment(ref fetched);
                Thread.Sleep(300);
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(png) };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return response;
            });
        });
        await using var _ = app;

        var responses = await Task.WhenAll(new[] { 16, 32, 48, 64 }.Select(w => client.GetAsync($"{Url}https://images.test/a.png&w={w}&f=webp")));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(1, fetched);
    }
}
