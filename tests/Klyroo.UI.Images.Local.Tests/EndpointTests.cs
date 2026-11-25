using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NetVips;

namespace Klyroo.UI.Images.Local.Tests;

/// <summary>An app with a web root holding a generated 1000x500 PNG, a JPEG, an SVG and a secret file next to the root.</summary>
public sealed class EndpointFixture : IAsyncLifetime
{
    public string Directory { get; } = Path.Combine(Path.GetTempPath(), "klyroo-images-test-" + Guid.NewGuid().ToString("N"));
    public string WebRoot => Path.Combine(Directory, "wwwroot");
    public string Cache => Path.Combine(Directory, "cache");

    public static byte[] Png(int width, int height) =>
        Image.Black(width, height).NewFromImage(30, 120, 200).Copy(interpretation: Enums.Interpretation.Srgb).Cast(Enums.BandFormat.Uchar).WriteToBuffer(".png");

    public const string SvgText = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"40\" height=\"20\"><rect width=\"40\" height=\"20\" fill=\"red\"/></svg>";

    public static byte[] AnimatedGif()
    {
        using var red = Image.Black(10, 10).NewFromImage(200, 10, 10).Cast(Enums.BandFormat.Uchar).Copy(interpretation: Enums.Interpretation.Srgb);
        using var blue = Image.Black(10, 10).NewFromImage(10, 10, 200).Cast(Enums.BandFormat.Uchar).Copy(interpretation: Enums.Interpretation.Srgb);
        using var frames = red.Join(blue, Enums.Direction.Vertical);
        using var tall = frames.Mutate(m => m.Set(GValue.GIntType, "page-height", 10));
        return tall.WriteToBuffer(".gif");
    }

    public static byte[] MultiPageTiff()
    {
        using var frames = Image.Black(50, 100).NewFromImage(200, 10, 10).Cast(Enums.BandFormat.Uchar).Copy(interpretation: Enums.Interpretation.Srgb);
        using var tall = frames.Mutate(m => m.Set(GValue.GIntType, "page-height", 50));
        return tall.WriteToBuffer(".tif[page_height=50]");
    }

    /// <summary>Inserts a tEXt chunk right after IHDR: a raster file that holds markup near its start.</summary>
    public static byte[] WithTextChunk(byte[] png, string text)
    {
        var data = System.Text.Encoding.Latin1.GetBytes("Comment\0" + text);
        var type = System.Text.Encoding.ASCII.GetBytes("tEXt");
        var chunk = new byte[12 + data.Length];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(chunk, (uint)data.Length);
        type.CopyTo(chunk, 4);
        data.CopyTo(chunk, 8);
        var crc = 0xFFFFFFFFu;
        foreach (var b in type.Concat(data))
        {
            crc ^= b;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(8 + data.Length), ~crc);
        const int afterIhdr = 8 + 25;
        return png[..afterIhdr].Concat(chunk).Concat(png[afterIhdr..]).ToArray();
    }

    public ValueTask InitializeAsync()
    {
        System.IO.Directory.CreateDirectory(Path.Combine(WebRoot, "img"));
        File.WriteAllBytes(Path.Combine(WebRoot, "img", "photo.png"), Png(1000, 500));
        File.WriteAllBytes(Path.Combine(WebRoot, "img", "photo.jpg"), Image.NewFromBuffer(Png(800, 400)).WriteToBuffer(".jpg"));
        File.WriteAllText(Path.Combine(WebRoot, "img", "logo.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"40\" height=\"20\"><rect width=\"40\" height=\"20\" fill=\"red\"/></svg>");
        File.WriteAllText(Path.Combine(WebRoot, "app.json"), "{}");
        File.WriteAllBytes(Path.Combine(Directory, "secret.png"), Png(10, 10));
        File.WriteAllBytes(Path.Combine(WebRoot, "img", ".hidden.png"), Png(10, 10));
        File.WriteAllBytes(Path.Combine(WebRoot, "img", "portrait.png"), Png(1000, 3000));
        File.WriteAllBytes(Path.Combine(WebRoot, "img", "landscape.png"), Png(3000, 1000));
        File.WriteAllBytes(Path.Combine(WebRoot, "img", "small.png"), Png(100, 50));
        File.WriteAllBytes(Path.Combine(WebRoot, "img", "thin.png"), Png(2, 1000));
        File.WriteAllBytes(Path.Combine(WebRoot, "img", "texty.png"), WithTextChunk(Png(40, 20), "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"));
        File.WriteAllText(Path.Combine(WebRoot, "img", "trap.png"), SvgText);
        File.WriteAllText(Path.Combine(WebRoot, "img", "prolog.svg"), "<?xml version=\"1.0\"?>\n<!--" + new string('x', 3000) + "-->\n<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\" [ <!ENTITY a \"b\"> ]>\n<?pi x?>\n" + SvgText);
        File.WriteAllBytes(Path.Combine(WebRoot, "img", "anim.gif"), AnimatedGif());
        File.WriteAllBytes(Path.Combine(WebRoot, "img", "multi.png"), MultiPageTiff());
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        try { System.IO.Directory.Delete(Directory, recursive: true); } catch (IOException) { }
        return ValueTask.CompletedTask;
    }

    public async Task<(WebApplication App, HttpClient Client)> StartAsync(Action<KlyLocalImagesOptions>? configure = null, string? pathBase = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = WebRoot, ContentRootPath = Directory, EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Services.AddKlyrooImagesLocal(o =>
        {
            o.CacheDirectory = Path.Combine(Cache, Guid.NewGuid().ToString("N"));
            configure?.Invoke(o);
        });
        var app = builder.Build();
        if (pathBase is not null)
        {
            // WebApplication adds UseRouting first on its own: a path base only applies to routing when UseRouting follows UsePathBase.
            app.UsePathBase(pathBase);
            app.UseRouting();
        }
        app.MapKlyrooImages();
        await app.StartAsync();
        return (app, app.GetTestClient());
    }
}

public class EndpointTests(EndpointFixture fixture) : IClassFixture<EndpointFixture>
{
    private static Image Decode(byte[] bytes) => Image.NewFromBuffer(bytes);

    [Fact]
    public async Task Under_a_path_base_the_endpoint_answers_below_it_and_the_default_url_leads_there()
    {
        var (app, client) = await fixture.StartAsync(pathBase: "/sub");
        await using var _ = app;
        var url = new KlyLocalImageProvider().GetUrl("/img/photo.png", new KlyImageModifiers(64, null, null, "webp", null, null));

        // What the browser does with <img src> under <base href="/sub/">.
        var resolved = new Uri(new Uri("http://localhost/sub/"), url);
        var response = await client.GetAsync(resolved.PathAndQuery);

        Assert.Equal("/sub/_klyroo/img", resolved.AbsolutePath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/webp", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task A_png_is_resized_and_converted_to_webp()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var response = await client.GetAsync("/_klyroo/img?src=/img/photo.png&w=384&f=webp&q=80");

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

        using var cover = Decode(await client.GetByteArrayAsync("/_klyroo/img?src=/img/photo.png&w=128&h=128&f=png"));
        using var byHeight = Decode(await client.GetByteArrayAsync("/_klyroo/img?src=/img/photo.png&h=100&f=png"));

        Assert.Equal((128, 128), (cover.Width, cover.Height));
        Assert.Equal((200, 100), (byHeight.Width, byHeight.Height));
    }

    [Fact]
    public async Task A_jpeg_keeps_its_format_without_f_and_is_never_enlarged()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var response = await client.GetAsync("/_klyroo/img?src=/img/photo.jpg&w=2048");

        Assert.Equal("image/jpeg", response.Content.Headers.ContentType!.MediaType);
        using var image = Decode(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(800, image.Width);
    }

    [Fact]
    public async Task The_etag_gives_a_304_and_a_version_token_makes_the_response_immutable()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var first = await client.GetAsync("/_klyroo/img?src=/img/photo.png&w=256&f=webp&v=abc");
        var request = new HttpRequestMessage(HttpMethod.Get, "/_klyroo/img?src=/img/photo.png&w=256&f=webp&v=abc");
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

        await client.GetByteArrayAsync("/_klyroo/img?src=/img/photo.png&w=256&f=webp");
        var files = Directory.GetFiles(cache!, "*", SearchOption.AllDirectories);
        var written = File.GetLastWriteTimeUtc(files.Single());
        await Task.Delay(30);
        await client.GetByteArrayAsync("/_klyroo/img?src=/img/photo.png&w=250&f=webp&q=78");

        Assert.Equal(written, File.GetLastWriteTimeUtc(Directory.GetFiles(cache!, "*", SearchOption.AllDirectories).Single()));
    }

    [Theory]
    [InlineData("/_klyroo/img?src=/../secret.png")]
    [InlineData("/_klyroo/img?src=/img/../../secret.png")]
    [InlineData("/_klyroo/img?src=%2e%2e%2fsecret.png")]
    [InlineData("/_klyroo/img?src=..%5csecret.png")]
    [InlineData("/_klyroo/img?src=%2F%2E%2E%2Fsecret.png")]
    [InlineData("/_klyroo/img?src=//evil.example/a.png")]
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

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await client.GetAsync("/_klyroo/img?src=/app.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/_klyroo/img?src=/img/none.png")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/_klyroo/img?src=/img/.hidden.png")).StatusCode);
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

        var response = await client.GetAsync("/_klyroo/img?src=https://evil.test/a.png");

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

        using var first = Decode(await client.GetByteArrayAsync("/_klyroo/img?src=https://images.test/a.png&w=256&f=webp"));
        await client.GetByteArrayAsync("/_klyroo/img?src=https://images.test/a.png&w=256&f=webp");

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

        Assert.Equal(HttpStatusCode.BadGateway, (await client.GetAsync("/_klyroo/img?src=https://images.test/a.png")).StatusCode);
        mode = "html";
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await client.GetAsync("/_klyroo/img?src=https://images.test/b.png")).StatusCode);
        mode = "big";
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.GetAsync("/_klyroo/img?src=https://images.test/c.png")).StatusCode);
    }

    [Fact]
    public async Task Svg_is_served_as_it_is_with_a_sandboxing_policy_and_rasterized_only_when_asked()
    {
        var (app, client) = await fixture.StartAsync(o => o.RasterizeSvg = true);
        await using var _ = app;

        var plain = await client.GetAsync("/_klyroo/img?src=/img/logo.svg&w=640");
        var raster = await client.GetAsync("/_klyroo/img?src=/img/logo.svg&w=128&f=png");

        Assert.Equal("image/svg+xml", plain.Content.Headers.ContentType!.MediaType);
        Assert.Contains("sandbox", plain.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Contains("<svg", await plain.Content.ReadAsStringAsync());
        Assert.Equal("image/png", raster.Content.Headers.ContentType!.MediaType);
        Assert.False(raster.Headers.Contains("Content-Security-Policy"));
    }

    [Fact]
    public async Task Requests_beyond_the_processing_slots_give_up_with_503()
    {
        var gate = new TaskCompletionSource();
        var (app, client) = await fixture.StartAsync(o =>
        {
            o.MaxConcurrency = 1;
            o.QueueTimeout = TimeSpan.FromMilliseconds(100);
            o.OnProcess = (request, _) => { if (request.Width == 16) gate.Task.Wait(TimeSpan.FromSeconds(5)); };
        });
        await using var _ = app;

        var slow = client.GetAsync("/_klyroo/img?src=/img/photo.png&w=16&f=webp");
        await Task.Delay(300);
        var busy = await client.GetAsync("/_klyroo/img?src=/img/photo.png&w=32&f=webp");
        gate.SetResult();
        await slow;

        Assert.Equal(HttpStatusCode.ServiceUnavailable, busy.StatusCode);
        Assert.Equal("1", busy.Headers.RetryAfter!.Delta!.Value.TotalSeconds.ToString());
    }

    [Fact]
    public void The_provider_urls_registered_by_AddKlyrooImagesLocal_match_the_endpoint()
    {
        var services = new ServiceCollection();
        services.AddKlyrooImagesLocal(o => { o.Version = "7"; o.AllowedRemoteHosts.Add("images.test"); });
        using var provider = services.BuildServiceProvider();
        var images = provider.GetRequiredService<Klyroo.UI.KlyrooOptions>().Images;

        var resolved = images.Resolve("/img/photo.png", new Klyroo.UI.KlyImageOptions { Format = "webp" }, 300, null);

        Assert.Equal("local", images.DefaultProvider);
        Assert.Contains("images.test", images.Domains);
        Assert.StartsWith("_klyroo/img?src=%2Fimg%2Fphoto.png&w=384&f=webp&v=7", resolved.Src);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
