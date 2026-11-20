using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NetVips;

namespace Blux.UI.Images.Local.Tests;

/// <summary>An app with a web root holding a generated 1000x500 PNG, a JPEG, an SVG and a secret file next to the root.</summary>
public sealed class EndpointFixture : IAsyncLifetime
{
    public string Directory { get; } = Path.Combine(Path.GetTempPath(), "blux-images-test-" + Guid.NewGuid().ToString("N"));
    public string WebRoot => Path.Combine(Directory, "wwwroot");
    public string Cache => Path.Combine(Directory, "cache");

    public static byte[] Png(int width, int height) =>
        Image.Black(width, height).NewFromImage(30, 120, 200).Copy(interpretation: Enums.Interpretation.Srgb).Cast(Enums.BandFormat.Uchar).WriteToBuffer(".png");

    public ValueTask InitializeAsync()
    {
        System.IO.Directory.CreateDirectory(Path.Combine(WebRoot, "img"));
        File.WriteAllBytes(Path.Combine(WebRoot, "img", "photo.png"), Png(1000, 500));
        File.WriteAllBytes(Path.Combine(WebRoot, "img", "photo.jpg"), Image.NewFromBuffer(Png(800, 400)).WriteToBuffer(".jpg"));
        File.WriteAllText(Path.Combine(WebRoot, "img", "logo.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"40\" height=\"20\"><rect width=\"40\" height=\"20\" fill=\"red\"/></svg>");
        File.WriteAllText(Path.Combine(WebRoot, "app.json"), "{}");
        File.WriteAllBytes(Path.Combine(Directory, "secret.png"), Png(10, 10));
        File.WriteAllBytes(Path.Combine(WebRoot, "img", ".hidden.png"), Png(10, 10));
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        try { System.IO.Directory.Delete(Directory, recursive: true); } catch (IOException) { }
        return ValueTask.CompletedTask;
    }

    public async Task<(WebApplication App, HttpClient Client)> StartAsync(Action<BxLocalImagesOptions>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = WebRoot, ContentRootPath = Directory, EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Services.AddBluxImagesLocal(o =>
        {
            o.CacheDirectory = Path.Combine(Cache, Guid.NewGuid().ToString("N"));
            configure?.Invoke(o);
        });
        var app = builder.Build();
        app.MapBluxImages();
        await app.StartAsync();
        return (app, app.GetTestClient());
    }
}

public class EndpointTests(EndpointFixture fixture) : IClassFixture<EndpointFixture>
{
    private static Image Decode(byte[] bytes) => Image.NewFromBuffer(bytes);

    [Fact]
    public async Task A_png_is_resized_and_converted_to_webp()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var response = await client.GetAsync("/_blux/img?src=/img/photo.png&w=384&f=webp&q=80");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/webp", response.Content.Headers.ContentType!.MediaType);
        using var image = Decode(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(384, image.Width);
        Assert.Equal(192, image.Height);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.StartsWith("public, max-age=", response.Headers.CacheControl!.ToString());
        Assert.NotNull(response.Headers.ETag);
    }

    [Fact]
    public async Task Cover_with_both_sides_crops_to_the_exact_size_and_height_only_keeps_the_ratio()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        using var cover = Decode(await client.GetByteArrayAsync("/_blux/img?src=/img/photo.png&w=128&h=128&f=png"));
        using var byHeight = Decode(await client.GetByteArrayAsync("/_blux/img?src=/img/photo.png&h=100&f=png"));

        Assert.Equal((128, 128), (cover.Width, cover.Height));
        Assert.Equal((200, 100), (byHeight.Width, byHeight.Height));
    }

    [Fact]
    public async Task A_jpeg_keeps_its_format_without_f_and_is_never_enlarged()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var response = await client.GetAsync("/_blux/img?src=/img/photo.jpg&w=2048");

        Assert.Equal("image/jpeg", response.Content.Headers.ContentType!.MediaType);
        using var image = Decode(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(800, image.Width);
    }

    [Fact]
    public async Task The_etag_gives_a_304_and_a_version_token_makes_the_response_immutable()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var first = await client.GetAsync("/_blux/img?src=/img/photo.png&w=256&f=webp&v=abc");
        var request = new HttpRequestMessage(HttpMethod.Get, "/_blux/img?src=/img/photo.png&w=256&f=webp&v=abc");
        request.Headers.IfNoneMatch.Add(first.Headers.ETag!);
        var second = await client.SendAsync(request);

        Assert.Equal("public, max-age=31536000, immutable", first.Headers.CacheControl!.ToString());
        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
    }

    [Fact]
    public async Task A_cached_variant_is_not_processed_again()
    {
        string? cache = null;
        var (app, client) = await fixture.StartAsync(o => cache = o.CacheDirectory);
        await using var _ = app;

        await client.GetByteArrayAsync("/_blux/img?src=/img/photo.png&w=256&f=webp");
        var files = Directory.GetFiles(cache!, "*", SearchOption.AllDirectories);
        var written = File.GetLastWriteTimeUtc(files.Single());
        await Task.Delay(30);
        await client.GetByteArrayAsync("/_blux/img?src=/img/photo.png&w=250&f=webp&q=78");

        Assert.Equal(written, File.GetLastWriteTimeUtc(Directory.GetFiles(cache!, "*", SearchOption.AllDirectories).Single()));
    }

    [Theory]
    [InlineData("/_blux/img?src=/../secret.png")]
    [InlineData("/_blux/img?src=/img/../../secret.png")]
    [InlineData("/_blux/img?src=%2e%2e%2fsecret.png")]
    [InlineData("/_blux/img?src=..%5csecret.png")]
    [InlineData("/_blux/img?src=%2F%2E%2E%2Fsecret.png")]
    [InlineData("/_blux/img?src=//evil.example/a.png")]
    public async Task Path_traversal_is_a_400(string url)
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Files_that_are_not_images_missing_or_hidden_are_refused()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await client.GetAsync("/_blux/img?src=/app.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/_blux/img?src=/img/none.png")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/_blux/img?src=/img/.hidden.png")).StatusCode);
    }

    [Fact]
    public async Task A_remote_host_that_is_not_allowed_is_403_and_nothing_is_fetched()
    {
        var fetched = 0;
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.AllowedRemoteHosts.Add("images.test");
            o.RemoteHandlerFactory = () => new StubHandler(_ => { fetched++; return new HttpResponseMessage(HttpStatusCode.OK); });
        });
        await using var _ = app;

        var response = await client.GetAsync("/_blux/img?src=https://evil.test/a.png");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, fetched);
    }

    [Fact]
    public async Task An_allowed_remote_image_is_fetched_resized_and_cached()
    {
        var fetched = 0;
        var png = EndpointFixture.Png(600, 300);
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.AllowedRemoteHosts.Add("images.test");
            o.RemoteHandlerFactory = () => new StubHandler(_ =>
            {
                fetched++;
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(png) };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return response;
            });
        });
        await using var _ = app;

        using var first = Decode(await client.GetByteArrayAsync("/_blux/img?src=https://images.test/a.png&w=256&f=webp"));
        await client.GetByteArrayAsync("/_blux/img?src=https://images.test/a.png&w=256&f=webp");

        Assert.Equal(256, first.Width);
        Assert.Equal(1, fetched);
    }

    [Fact]
    public async Task Remote_redirects_to_another_host_wrong_types_and_oversized_bodies_are_refused()
    {
        var mode = "redirect";
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.AllowedRemoteHosts.Add("images.test");
            o.MaxRemoteBytes = 1000;
            o.RemoteHandlerFactory = () => new StubHandler(_ =>
            {
                switch (mode)
                {
                    case "redirect":
                        var redirect = new HttpResponseMessage(HttpStatusCode.Found);
                        redirect.Headers.Location = new Uri("http://169.254.169.254/latest/meta-data");
                        return redirect;
                    case "html":
                        var html = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html></html>") };
                        return html;
                    default:
                        var big = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[5000]) };
                        big.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                        return big;
                }
            });
        });
        await using var _ = app;

        Assert.Equal(HttpStatusCode.BadGateway, (await client.GetAsync("/_blux/img?src=https://images.test/a.png")).StatusCode);
        mode = "html";
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await client.GetAsync("/_blux/img?src=https://images.test/b.png")).StatusCode);
        mode = "big";
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.GetAsync("/_blux/img?src=https://images.test/c.png")).StatusCode);
    }

    [Fact]
    public async Task Svg_is_served_as_it_is_with_a_sandboxing_policy_and_rasterized_only_when_asked()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var plain = await client.GetAsync("/_blux/img?src=/img/logo.svg&w=640");
        var raster = await client.GetAsync("/_blux/img?src=/img/logo.svg&w=128&f=png");

        Assert.Equal("image/svg+xml", plain.Content.Headers.ContentType!.MediaType);
        Assert.Contains("sandbox", plain.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Contains("<svg", await plain.Content.ReadAsStringAsync());
        Assert.Equal("image/png", raster.Content.Headers.ContentType!.MediaType);
        Assert.False(raster.Headers.Contains("Content-Security-Policy"));
    }

    [Fact]
    public async Task The_number_of_variants_per_image_is_capped()
    {
        var (app, client) = await fixture.StartAsync(o => o.MaxVariantsPerSource = 2);
        await using var _ = app;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_blux/img?src=/img/photo.png&w=16&f=webp")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_blux/img?src=/img/photo.png&w=32&f=webp")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/_blux/img?src=/img/photo.png&w=48&f=webp")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_blux/img?src=/img/photo.png&w=32&f=webp")).StatusCode);
    }

    [Fact]
    public async Task The_disk_cache_stays_below_its_limit()
    {
        string? cache = null;
        var (app, client) = await fixture.StartAsync(o => { cache = o.CacheDirectory; o.MaxCacheBytes = 3000; });
        await using var _ = app;

        foreach (var width in new[] { 128, 256, 384, 512, 640 })
        {
            await client.GetByteArrayAsync($"/_blux/img?src=/img/photo.png&w={width}&f=png");
        }

        var total = new DirectoryInfo(cache!).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        Assert.True(total <= 3000, $"cache holds {total} bytes");
    }

    [Fact]
    public async Task Requests_beyond_the_processing_slots_give_up_with_503()
    {
        var gate = new TaskCompletionSource();
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.MaxConcurrency = 1;
            o.QueueTimeout = TimeSpan.FromMilliseconds(100);
            o.AllowedRemoteHosts.Add("images.test");
            o.RemoteHandlerFactory = () => new StubHandler(_ =>
            {
                gate.Task.Wait(TimeSpan.FromSeconds(5));
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(EndpointFixture.Png(50, 50)) };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return response;
            });
        });
        await using var _ = app;

        var slow = client.GetAsync("/_blux/img?src=https://images.test/slow.png&w=16");
        await Task.Delay(300);
        var busy = await client.GetAsync("/_blux/img?src=/img/photo.png&w=16&f=webp");
        gate.SetResult();
        await slow;

        Assert.Equal(HttpStatusCode.ServiceUnavailable, busy.StatusCode);
        Assert.Equal("1", busy.Headers.RetryAfter!.Delta!.Value.TotalSeconds.ToString());
    }

    [Fact]
    public void The_provider_urls_registered_by_AddBluxImagesLocal_match_the_endpoint()
    {
        var services = new ServiceCollection();
        services.AddBluxImagesLocal(o => { o.Version = "7"; o.AllowedRemoteHosts.Add("images.test"); });
        using var provider = services.BuildServiceProvider();
        var images = provider.GetRequiredService<Blux.UI.BluxOptions>().Images;

        var resolved = images.Resolve("/img/photo.png", new Blux.UI.BxImageOptions { Format = "webp" }, 300, null);

        Assert.Equal("local", images.DefaultProvider);
        Assert.Contains("images.test", images.Domains);
        Assert.StartsWith("/_blux/img?src=%2Fimg%2Fphoto.png&w=384&f=webp&v=7", resolved.Src);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
