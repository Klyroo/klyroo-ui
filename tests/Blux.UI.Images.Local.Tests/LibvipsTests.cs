using NetVips;

namespace Blux.UI.Images.Local.Tests;

public class LibvipsTests(EndpointFixture fixture) : IClassFixture<EndpointFixture>
{
    [Fact]
    public async Task Libvips_only_loads_the_formats_the_endpoint_serves()
    {
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;
        await client.GetAsync("/_blux/img?src=/img/photo.png&w=16&f=webp");   // the processor is created: the loaders are restricted

        var png = EndpointFixture.Png(20, 20);
        foreach (var (bytes, loader) in new[]
        {
            (File.ReadAllBytes(Path.Combine(fixture.WebRoot, "img", "photo.jpg")), "jpegload_buffer"),
            (png, "pngload_buffer"),
            (EndpointFixture.AnimatedGif(), "gifload_buffer"),
            (Image.NewFromBuffer(png).WriteToBuffer(".webp"), "webpload_buffer"),
            (Image.NewFromBuffer(png).WriteToBuffer(".avif"), "heifload_buffer"),
        })
        {
            using var image = Image.NewFromBuffer(bytes);
            Assert.Equal(loader, image.Get("vips-loader"));
        }
        Assert.ThrowsAny<VipsException>(() => Image.NewFromBuffer(EndpointFixture.MultiPageTiff()));
    }

    [Fact]
    public async Task An_avif_source_keeps_its_format_and_converts_to_others()
    {
        File.WriteAllBytes(Path.Combine(fixture.WebRoot, "img", "photo.avif"), Image.NewFromBuffer(EndpointFixture.Png(200, 100)).WriteToBuffer(".avif"));
        var (app, client) = await fixture.StartAsync();
        await using var _ = app;

        var same = await client.GetAsync("/_blux/img?src=/img/photo.avif&w=64");
        var png = await client.GetAsync("/_blux/img?src=/img/photo.avif&w=64&f=png");

        Assert.Equal("image/avif", same.Content.Headers.ContentType!.MediaType);
        Assert.Equal("image/png", png.Content.Headers.ContentType!.MediaType);
        using var image = Image.NewFromBuffer(await png.Content.ReadAsByteArrayAsync());
        Assert.Equal((64, 32), (image.Width, image.Height));
    }
}
