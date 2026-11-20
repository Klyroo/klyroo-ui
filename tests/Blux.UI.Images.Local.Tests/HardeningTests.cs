using System.Net;
using System.Net.Http.Headers;
using NetVips;

namespace Blux.UI.Images.Local.Tests;

/// <summary>The endpoint is open to the internet: availability, geometry, source handling and cache behavior.</summary>
public class HardeningTests(EndpointFixture fixture) : IClassFixture<EndpointFixture>
{
    private const string Url = "/_blux/img?src=";

    private static Image Decode(byte[] bytes) => Image.NewFromBuffer(bytes);

    private static HttpResponseMessage Png(byte[] bytes, string type = "image/png")
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(type);
        return response;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }

    // 1. Source canonicalization

    [Fact]
    public async Task Path_spellings_of_one_file_share_one_cache_directory()
    {
        string? cache = null;
        var (app, client) = await fixture.StartAsync(o => cache = o.CacheDirectory);
        await using var _ = app;

        foreach (var src in new[] { "/img/photo.png", "/img//photo.png", "/img///photo.png", "img/photo.png", "img//photo.png" })
        {
            var response = await client.GetAsync($"{Url}{Uri.EscapeDataString(src)}&w=256&f=webp");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        Assert.Single(Directory.GetDirectories(cache!));
        Assert.Single(Directory.GetFiles(cache!, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void Remote_urls_are_canonical_whatever_the_port_spelling_or_fragment()
    {
        var options = new BxLocalImagesOptions();
        options.AllowedRemoteHosts.Add("images.test");
        string Source(string src) => BxImageRequestParser.Parse(k => k == "src" ? src : null, options).Request!.Source;

        Assert.Equal(Source("https://images.test/a.png"), Source("https://IMAGES.test:443/a.png#top"));
        Assert.Equal(Source("http://images.test/a.png"), Source("http://images.test:80/a.png"));
    }

    // 2. Variants are evicted, never refused

    [Fact]
    public async Task Beyond_the_variant_budget_the_oldest_variant_goes_instead_of_new_ones_being_refused()
    {
        string? cache = null;
        var (app, client) = await fixture.StartAsync(o => { cache = o.CacheDirectory; o.MaxVariantsPerSource = 2; });
        await using var _ = app;

        foreach (var width in new[] { 16, 32, 48, 64, 16 })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Url}/img/photo.png&w={width}&f=webp")).StatusCode);
        }

        Assert.True(Directory.GetFiles(cache!, "*", SearchOption.AllDirectories).Length <= 2);
    }

    // 3 and 10. Geometry: every fit, every source shape, never above the limits

    [Theory]
    // portrait 1000x3000 in a 384x384 box
    [InlineData("portrait", "outside", 384, 1152)]
    [InlineData("portrait", "cover", 384, 384)]
    [InlineData("portrait", "contain", 384, 384)]
    [InlineData("portrait", "inside", 128, 384)]
    [InlineData("portrait", "fill", 384, 384)]
    // landscape 3000x1000
    [InlineData("landscape", "outside", 1152, 384)]
    [InlineData("landscape", "cover", 384, 384)]
    [InlineData("landscape", "contain", 384, 384)]
    [InlineData("landscape", "inside", 384, 128)]
    [InlineData("landscape", "fill", 384, 384)]
    // smaller than the box 100x50: cover, outside, contain and fill fill the box, inside never enlarges
    [InlineData("small", "outside", 768, 384)]
    [InlineData("small", "cover", 384, 384)]
    [InlineData("small", "contain", 384, 384)]
    [InlineData("small", "inside", 100, 50)]
    [InlineData("small", "fill", 384, 384)]
    public async Task Each_fit_gives_the_documented_size(string source, string fit, int width, int height)
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var response = await client.GetAsync($"{Url}/img/{source}.png&w=384&h=384&fit={fit}&f=png");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var image = Decode(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal((width, height), (image.Width, image.Height));
        Assert.True(image.Width <= 4096 && image.Height <= 4096);
    }

    [Theory]
    [InlineData("thin", "contain")]
    [InlineData("thin", "cover")]
    [InlineData("thin", "outside")]
    [InlineData("thin", "inside")]
    [InlineData("thin", "fill")]
    [InlineData("portrait", "contain")]
    [InlineData("landscape", "outside")]
    public async Task No_fit_goes_beyond_the_maximum_size_whatever_the_ratio(string source, string fit)
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        foreach (var query in new[] { "w=3840", "h=4096", "w=3840&h=4096", "w=3840&h=16", "w=16&h=4096" })
        {
            var response = await client.GetAsync($"{Url}/img/{source}.png&{query}&fit={fit}&f=webp");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var image = Decode(await response.Content.ReadAsByteArrayAsync());
            Assert.True(image.Width <= 4096 && image.Height <= 4096, $"{source} {fit} {query} gave {image.Width}x{image.Height}");
        }
    }

    [Fact]
    public async Task Contain_with_one_side_keeps_the_ratio_without_bars_and_never_enlarges()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        using var byWidth = Decode(await client.GetByteArrayAsync($"{Url}/img/portrait.png&w=256&fit=contain&f=png"));
        using var thin = Decode(await client.GetByteArrayAsync($"{Url}/img/thin.png&w=3840&fit=contain&f=png"));

        Assert.Equal((256, 768), (byWidth.Width, byWidth.Height));
        Assert.Equal((2, 1000), (thin.Width, thin.Height));
    }

    [Fact]
    public async Task Contain_pads_with_transparency_for_formats_with_alpha_and_white_for_jpeg()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        using var png = Decode(await client.GetByteArrayAsync($"{Url}/img/landscape.png&w=384&h=384&fit=contain&f=png"));
        using var jpeg = Decode(await client.GetByteArrayAsync($"{Url}/img/landscape.png&w=384&h=384&fit=contain&f=jpeg"));

        Assert.True(png.HasAlpha());
        Assert.Equal(0, png.Getpoint(2, 2)[3]);
        Assert.Equal(255, png.Getpoint(192, 192)[3]);
        Assert.All(jpeg.Getpoint(2, 2), v => Assert.True(v > 245));
        Assert.True(jpeg.Getpoint(192, 192)[2] > jpeg.Getpoint(192, 192)[0]);
    }

    // 4. Coalescing, slots, no-size cap

    [Fact]
    public async Task Identical_concurrent_requests_do_one_fetch_and_one_computation()
    {
        var fetched = 0;
        var png = EndpointFixture.Png(600, 300);
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.MaxConcurrency = 8;
            o.AllowedRemoteHosts.Add("images.test");
            o.RemoteHandlerFactory = () => new StubHandler(_ => { Interlocked.Increment(ref fetched); Thread.Sleep(300); return Png(png); });
        });
        await using var _ = app;

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => client.GetAsync($"{Url}https://images.test/a.png&w=256&f=webp")));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(1, fetched);
    }

    [Fact]
    public async Task A_slow_remote_fetch_does_not_hold_a_processing_slot()
    {
        var gate = new TaskCompletionSource();
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.MaxConcurrency = 1;
            o.QueueTimeout = TimeSpan.FromMilliseconds(200);
            o.AllowedRemoteHosts.Add("images.test");
            o.RemoteHandlerFactory = () => new StubHandler(_ => { gate.Task.Wait(TimeSpan.FromSeconds(10)); return Png(EndpointFixture.Png(50, 50)); });
        });
        await using var _ = app;

        var slow = client.GetAsync($"{Url}https://images.test/slow.png&w=16");
        await Task.Delay(300);
        var local = await client.GetAsync($"{Url}/img/photo.png&w=16&f=webp");
        gate.SetResult();
        await slow;

        Assert.Equal(HttpStatusCode.OK, local.StatusCode);
    }

    [Fact]
    public async Task Without_a_size_the_output_is_bounded_by_the_maximum_size()
    {
        var (app, client) = await fixture.StartAsync(o => { o.MaxWidth = 512; o.MaxHeight = 512; });
        await using var _ = app;

        using var image = Decode(await client.GetByteArrayAsync($"{Url}/img/landscape.png&f=png&blur=2"));

        Assert.Equal((512, 171), (image.Width, image.Height));
    }

    // 5. Disk cache accounting and vanishing files

    [Fact]
    public async Task The_cache_limit_counts_allocated_blocks_not_just_bytes()
    {
        string? cache = null;
        var (app, client) = await fixture.StartAsync(o => { cache = o.CacheDirectory; o.MaxCacheBytes = 16384; });
        await using var _ = app;

        foreach (var width in new[] { 16, 32, 48, 64, 96, 128 })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Url}/img/photo.png&w={width}&f=png")).StatusCode);
        }

        long Allocated() => new DirectoryInfo(cache!).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => (f.Length + 4095) / 4096 * 4096);
        for (var i = 0; i < 100 && Allocated() > 16384; i++) await Task.Delay(50);
        Assert.True(Allocated() <= 16384, $"{Allocated()} allocated bytes");
    }

    [Fact]
    public async Task Eviction_racing_with_requests_never_gives_a_500()
    {
        var (app, client) = await fixture.StartAsync(o => { o.MaxCacheBytes = 9000; o.MaxConcurrency = 4; });
        await using var _ = app;

        var responses = await Task.WhenAll(Enumerable.Range(0, 60).Select(i => client.GetAsync($"{Url}/img/photo.png&w={new[] { 16, 32, 48, 64, 96, 128 }[i % 6]}&f=png")));

        Assert.DoesNotContain(responses, r => r.StatusCode == HttpStatusCode.InternalServerError);
        Assert.All(responses, r => Assert.True(r.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable, r.StatusCode.ToString()));
    }

    // 6. SSRF leftovers

    [Fact]
    public void Remote_urls_only_use_default_ports_unless_the_allowlist_names_one()
    {
        BxImageParseResult Parse(string src, string allowed)
        {
            var options = new BxLocalImagesOptions();
            options.AllowedRemoteHosts.Add(allowed);
            return BxImageRequestParser.Parse(k => k == "src" ? src : null, options);
        }

        Assert.Equal(403, Parse("https://images.test:8443/a.png", "images.test").Status);
        Assert.Equal(403, Parse("http://images.test:8080/a.png", "images.test").Status);
        Assert.Equal(403, Parse("https://images.test:9999/a.png", "images.test:8443").Status);
        Assert.Equal(403, Parse("https://images.test/a.png", "images.test:8443").Status);
        Assert.NotNull(Parse("https://images.test:8443/a.png", "images.test:8443").Request);
        Assert.NotNull(Parse("https://images.test:443/a.png", "images.test").Request);
    }

    [Theory]
    [InlineData("https://images.test:8443/b.png", HttpStatusCode.BadGateway)]
    [InlineData("http://images.test/b.png", HttpStatusCode.BadGateway)]
    [InlineData("https://other.test/b.png", HttpStatusCode.BadGateway)]
    [InlineData("https://images.test:443/b.png", HttpStatusCode.OK)]
    [InlineData("/b.png", HttpStatusCode.OK)]
    public async Task Redirects_keep_scheme_host_and_port(string location, HttpStatusCode expected)
    {
        var png = EndpointFixture.Png(60, 30);
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.AllowedRemoteHosts.Add("images.test");
            o.RemoteHandlerFactory = () => new StubHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath == "/a.png")
                {
                    var redirect = new HttpResponseMessage(HttpStatusCode.Found);
                    redirect.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
                    return redirect;
                }
                return Png(png);
            });
        });
        await using var _ = app;

        Assert.Equal(expected, (await client.GetAsync($"{Url}https://images.test/a.png&f=png")).StatusCode);
    }

    // 7. SVG detection and serving

    [Fact]
    public async Task An_svg_after_a_long_prolog_is_still_an_svg_served_sandboxed()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var response = await client.GetAsync($"{Url}/img/prolog.svg&w=100");

        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("sandbox", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task An_svg_named_png_is_served_as_an_svg_not_handed_to_libvips()
    {
        var (app, client) = await fixture.StartAsync(o => o.RasterizeSvg = false);
        await using var _ = app;

        var plain = await client.GetAsync($"{Url}/img/trap.png&w=100");
        var raster = await client.GetAsync($"{Url}/img/trap.png&w=100&f=webp");

        Assert.Equal("image/svg+xml", plain.Content.Headers.ContentType!.MediaType);
        Assert.True(plain.Headers.Contains("Content-Security-Policy"));
        Assert.Equal(HttpStatusCode.BadRequest, raster.StatusCode);
    }

    [Fact]
    public async Task A_raster_file_holding_svg_markup_early_is_not_served_as_an_svg()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var response = await client.GetAsync($"{Url}/img/texty.png&w=32");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType!.MediaType);
        Assert.False(response.Headers.Contains("Content-Security-Policy"));
    }

    [Fact]
    public async Task Svg_is_not_rasterized_unless_the_option_is_on()
    {
        var (app, client) = await fixture.StartAsync();
        var (rasterizing, rasterClient) = await fixture.StartAsync(o => o.RasterizeSvg = true);
        await using var _ = app;
        await using var __ = rasterizing;

        Assert.False(new BxLocalImagesOptions().RasterizeSvg);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"{Url}/img/logo.svg&w=128&f=png")).StatusCode);
        var response = await rasterClient.GetAsync($"{Url}/img/logo.svg&w=128&f=png");
        Assert.Equal("image/png", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task The_remote_content_decides_what_is_an_svg_not_its_declared_type()
    {
        var png = EndpointFixture.Png(60, 30);
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.AllowedRemoteHosts.Add("images.test");
            o.RemoteHandlerFactory = () => new StubHandler(request => request.RequestUri!.AbsolutePath == "/liar.svg"
                ? Png(png, "image/svg+xml")
                : Png(System.Text.Encoding.UTF8.GetBytes(EndpointFixture.SvgText), "image/png"));
        });
        await using var _ = app;

        var raster = await client.GetAsync($"{Url}https://images.test/liar.svg");
        var svg = await client.GetAsync($"{Url}https://images.test/real.png");

        Assert.Equal("image/png", raster.Content.Headers.ContentType!.MediaType);
        Assert.Equal("image/svg+xml", svg.Content.Headers.ContentType!.MediaType);
        Assert.True(svg.Headers.Contains("Content-Security-Policy"));
    }

    // 8. Revalidation

    [Fact]
    public async Task A_304_does_not_claim_a_jpeg_and_an_svg_304_keeps_its_policy()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        foreach (var (url, mime, svg) in new[] { ($"{Url}/img/photo.png&w=256&f=webp", "image/webp", false), ($"{Url}/img/logo.svg&w=256", "image/svg+xml", true) })
        {
            var first = await client.GetAsync(url);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.IfNoneMatch.Add(first.Headers.ETag!);
            var second = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
            Assert.True(second.Content.Headers.ContentType is null || second.Content.Headers.ContentType.MediaType == mime, second.Content.Headers.ContentType?.MediaType);
            Assert.Equal(svg, second.Headers.Contains("Content-Security-Policy"));
        }
    }

    [Fact]
    public async Task The_etag_of_a_remote_image_follows_its_content_and_a_304_respects_the_ttl()
    {
        var content = EndpointFixture.Png(600, 300);
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.RemoteCacheTtl = TimeSpan.FromMilliseconds(100);
            o.AllowedRemoteHosts.Add("images.test");
            o.RemoteHandlerFactory = () => new StubHandler(_ => Png(content));
        });
        await using var _ = app;
        const string url = $"{Url}https://images.test/a.png&w=256&f=webp";

        var first = await client.GetAsync(url);
        content = EndpointFixture.Png(600, 301);
        var fresh = new HttpRequestMessage(HttpMethod.Get, url);
        fresh.Headers.IfNoneMatch.Add(first.Headers.ETag!);
        var whileFresh = await client.SendAsync(fresh);
        await Task.Delay(250);
        var stale = new HttpRequestMessage(HttpMethod.Get, url);
        stale.Headers.IfNoneMatch.Add(first.Headers.ETag!);
        var afterTtl = await client.SendAsync(stale);

        Assert.Equal(HttpStatusCode.NotModified, whileFresh.StatusCode);
        Assert.Equal(HttpStatusCode.OK, afterTtl.StatusCode);
        Assert.NotEqual(first.Headers.ETag, afterTtl.Headers.ETag);
    }

    // 12. gif

    [Fact]
    public async Task Gif_output_is_supported()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var response = await client.GetAsync($"{Url}/img/photo.png&w=64&f=gif");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/gif", response.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("GIF8", System.Text.Encoding.ASCII.GetString(await response.Content.ReadAsByteArrayAsync(), 0, 4));
    }

    [Fact]
    public async Task An_animated_gif_is_served_as_a_gif_and_a_multi_page_tiff_is_refused()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var gif = await client.GetAsync($"{Url}/img/anim.gif&w=16");
        var tiff = await client.GetAsync($"{Url}/img/multi.png");

        Assert.Equal("image/gif", gif.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, tiff.StatusCode);
    }

    // 13. The server snaps the width, so the height follows

    [Fact]
    public void When_the_server_changes_the_width_the_height_is_scaled_by_the_same_factor()
    {
        var options = new BxLocalImagesOptions { Widths = [100, 200, 400] };

        var request = BxImageRequestParser.Parse(k => k switch { "src" => "/a.png", "w" => "150", "h" => "75", _ => null }, options).Request!;

        Assert.Equal(200, request.Width);
        Assert.Equal(100, request.Height);
    }
}
