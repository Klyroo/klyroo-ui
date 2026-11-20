using Blux.UI.Images;

namespace Blux.UI.Images.Tests;

/// <summary>Expected URLs are the examples of each vendor's documentation (cited in the XML docs of the providers).</summary>
public class ProviderTests
{
    private static BxImageModifiers M(int? w = null, int? h = null, int? q = null, string? f = null, BxImageFit? fit = null, int? blur = null) => new(w, h, q, f, fit, blur);

    [Fact]
    public void Cloudinary_matches_the_docs_examples()
    {
        var p = new CloudinaryImageProvider("demo");

        Assert.Equal("https://res.cloudinary.com/demo/image/upload/c_fill,h_200,w_200/docs/camera-640.jpg", p.GetUrl("/docs/camera-640.jpg", M(200, 200)));
        Assert.Equal("https://res.cloudinary.com/demo/image/upload/c_fill,h_400,w_250,q_80,f_webp/kitchen-island.png", p.GetUrl("/kitchen-island.png", M(250, 400, 80, "webp")));
        Assert.Equal("https://res.cloudinary.com/demo/image/upload/f_auto/docs/shoes.jpg", p.GetUrl("docs/shoes.jpg", M(f: "auto")));
        Assert.Equal("https://res.cloudinary.com/demo/image/upload/c_fit,w_100/a.jpg", p.GetUrl("/a.jpg", M(100, fit: BxImageFit.Contain)));
        Assert.Equal("https://res.cloudinary.com/demo/image/fetch/c_fill,w_600/https://example.com/a.jpg", p.GetUrl("https://example.com/a.jpg", M(600)));
        Assert.Equal("https://res.cloudinary.com/demo/image/upload/c_fill,w_10,q_50,e_blur:300/a.jpg", p.GetUrl("/a.jpg", M(10, q: 50, blur: 3)));
    }

    [Fact]
    public void Imgix_matches_the_docs_examples()
    {
        var p = new ImgixImageProvider("assets.imgix.net");

        Assert.Equal("https://assets.imgix.net/docs/road-sm.jpg?w=300&h=100&fit=crop", p.GetUrl("/docs/road-sm.jpg", M(300, 100, fit: BxImageFit.Cover)));
        Assert.Equal("https://assets.imgix.net/docs/rendering-api/food-plate.jpg?w=300&fm=bmp", p.GetUrl("/docs/rendering-api/food-plate.jpg", M(300, f: "bmp")));
        Assert.Equal("https://assets.imgix.net/a.jpg?w=100&q=75&fm=webp", p.GetUrl("/a.jpg", M(100, q: 75, f: "webp")));
        Assert.Equal("https://assets.imgix.net/a.jpg?auto=format", p.GetUrl("/a.jpg", M(f: "auto")));
        Assert.Equal("https://assets.imgix.net/a.jpg", p.GetUrl("/a.jpg", M()));
    }

    [Fact]
    public void ImageKit_matches_the_docs_examples()
    {
        var p = new ImageKitImageProvider("https://ik.imagekit.io/ikmedia");

        Assert.Equal("https://ik.imagekit.io/ikmedia/tr:w-300,h-300/docs_images/examples/example_food_3.jpg", p.GetUrl("/docs_images/examples/example_food_3.jpg", M(300, 300)));
        Assert.Equal("https://ik.imagekit.io/ikmedia/tr:q-90/docs_images/examples/example_food_3.jpg", p.GetUrl("/docs_images/examples/example_food_3.jpg", M(q: 90)));
        Assert.Equal("https://ik.imagekit.io/ikmedia/tr:w-300,h-200,c-force/docs_images/examples/example_food_3.jpg", p.GetUrl("/docs_images/examples/example_food_3.jpg", M(300, 200, fit: BxImageFit.Fill)));
        Assert.Equal("https://ik.imagekit.io/ikmedia/tr:w-400,h-300,f-jpg/a.jpg", p.GetUrl("/a.jpg", M(400, 300, f: "jpeg")));
        Assert.Equal("https://ik.imagekit.io/ikmedia/a.jpg", p.GetUrl("/a.jpg", M()));
    }

    [Fact]
    public void Cloudflare_matches_the_docs_examples()
    {
        var p = new CloudflareImagesProvider("https://developers.cloudflare.com");

        Assert.Equal("https://developers.cloudflare.com/cdn-cgi/image/width=1363,height=904,format=webp/_astro/xxy.pMA7L7Ny.png", p.GetUrl("/_astro/xxy.pMA7L7Ny.png", M(1363, 904, f: "webp")));
        Assert.Equal("/cdn-cgi/image/width=300,quality=80,fit=cover/https://example.com/a.jpg", new CloudflareImagesProvider().GetUrl("https://example.com/a.jpg", M(300, q: 80, fit: BxImageFit.Cover)));
        Assert.Equal("/cdn-cgi/image/width=100/a.png", new CloudflareImagesProvider().GetUrl("/a.png", M(100, f: "png")));
        Assert.True(p.ValidateDomains);
    }

    [Fact]
    public void Netlify_matches_the_docs_examples()
    {
        var p = new NetlifyImageProvider();

        Assert.Equal("/.netlify/images?url=/owl.jpeg&w=50", p.GetUrl("/owl.jpeg", M(50)));
        Assert.Equal("/.netlify/images?url=/owl.jpeg&fit=cover&w=50&h=50", p.GetUrl("/owl.jpeg", M(50, 50, fit: BxImageFit.Cover)));
        Assert.Equal("/.netlify/images?url=/owl.jpeg&fm=avif&q=50", p.GetUrl("/owl.jpeg", M(q: 50, f: "avif")));
        Assert.Equal("/.netlify/images?url=https://my-images.com/owl.jpeg", p.GetUrl("https://my-images.com/owl.jpeg", M()));
        Assert.Equal("/.netlify/images?url=/a.jpg&fm=jpg", p.GetUrl("/a.jpg", M(f: "jpeg")));
    }

    [Fact]
    public void Vercel_follows_the_url_template_and_snaps_the_width_to_the_configured_sizes()
    {
        var p = new VercelImageProvider([640, 750, 828, 1080, 1200]);

        Assert.Equal("/_vercel/image?url=/a.png&w=750&q=75", p.GetUrl("/a.png", M(700)));
        Assert.Equal("/_vercel/image?url=/a.png&w=1200&q=50", p.GetUrl("/a.png", M(5000, q: 50)));
        Assert.Equal("/_vercel/image?url=/a.png&w=1200&q=75", p.GetUrl("/a.png", M()));
        Assert.Equal("/_vercel/image?url=https://x.org/a.png&w=3840&q=75", new VercelImageProvider().GetUrl("https://x.org/a.png", M(3840)));
    }

    [Fact]
    public void Bunny_matches_the_docs_examples()
    {
        var p = new BunnyImageProvider("https://yourzone.b-cdn.net");

        Assert.Equal("https://yourzone.b-cdn.net/image.jpg?width=500", p.GetUrl("/image.jpg", M(500)));
        Assert.Equal("https://yourzone.b-cdn.net/image.jpg?height=300", p.GetUrl("/image.jpg", M(h: 300)));
        Assert.Equal("https://yourzone.b-cdn.net/image.jpg?quality=75&format=webp", p.GetUrl("/image.jpg", M(q: 75, f: "webp")));
    }

    [Fact]
    public void Template_fills_the_placeholders()
    {
        var p = new TemplateImageProvider("acme", "https://img.acme.dev/{width}x{height}/q{quality}.{format}?u={encodedSrc}&raw={src}");

        Assert.Equal("https://img.acme.dev/100x50/q80.webp?u=%2Fa%20b.png&raw=/a b.png", p.GetUrl("/a b.png", M(100, 50, 80, "webp")));
        Assert.Equal("https://img.acme.dev/x/q.?u=%2Fa.png&raw=/a.png", p.GetUrl("/a.png", M()));
    }

    [Fact]
    public void Local_urls_are_canonical_snapped_and_encoded()
    {
        var p = new BxLocalImageProvider("/_blux/img", version: "1.2");

        Assert.Equal("/_blux/img?src=%2Fimg%2Fa.png&w=384&h=216&q=80&f=webp&fit=cover&v=1.2", p.GetUrl("/img/a.png", M(320, 180, 78, "webp", BxImageFit.Cover)));
        Assert.Equal("/_blux/img?src=%2Fa.png", new BxLocalImageProvider().GetUrl("/a.png", M()));
        Assert.Equal(3840, BxLocalImageProvider.SnapWidth(9000));
        Assert.Equal(16, BxLocalImageProvider.SnapWidth(1));
        Assert.Equal(5, BxLocalImageProvider.SnapQuality(1));
        Assert.Equal(100, BxLocalImageProvider.SnapQuality(99));
    }

    [Fact]
    public void Extension_methods_register_and_can_make_the_provider_the_default()
    {
        var settings = new BxImageSettings();
        settings.AddCloudinary("demo").AddImgix("a.imgix.net").AddImageKit("https://ik.imagekit.io/x").AddCloudflareImages().AddNetlify()
            .AddVercel().AddBunny("https://z.b-cdn.net").AddTemplate("t", "{src}").AddLocal(makeDefault: true);

        Assert.Equal("local", settings.DefaultProvider);
        Assert.Equal(10, settings.Providers.Count);
        var resolved = settings.Resolve("/a.png", null, 100, 100);
        Assert.Equal("/_blux/img?src=%2Fa.png&w=128&h=128", resolved.Src.Split(" ")[0]);
    }

    [Fact]
    public void Srcset_descriptors_are_the_widths_the_local_endpoint_will_really_produce()
    {
        var settings = new BxImageSettings();
        settings.AddLocal(makeDefault: true);

        var resolved = settings.Resolve("/a.png", new BxImageOptions { Sizes = "400px", Densities = "x1" }, 400, null);

        Assert.Contains(" 512w", resolved.SrcSet);
        Assert.DoesNotContain(" 400w", resolved.SrcSet);
        Assert.Contains("w=512", resolved.SrcSet);
    }

    [Fact]
    public void Candidates_that_snap_to_the_same_width_collapse_into_one()
    {
        var settings = new BxImageSettings();
        settings.AddLocal(makeDefault: true);

        var resolved = settings.Resolve("/a.png", new BxImageOptions { Sizes = "sm:360px md:370px" }, 360, null);

        Assert.Equal(["384w", "750w"], resolved.SrcSet!.Split(", ").Select(c => c.Split(' ')[^1]).ToArray());
    }

    [Fact]
    public void The_descriptor_of_a_provider_that_snaps_widths_follows_its_sizes()
    {
        var settings = new BxImageSettings();
        settings.AddVercel([640, 750, 1080], makeDefault: true);

        var resolved = settings.Resolve("/a.png", new BxImageOptions { Sizes = "700px", Densities = "x1 x2" }, 700, null);

        Assert.Equal(["750w", "1080w"], resolved.SrcSet!.Split(", ").Select(c => c.Split(' ')[^1]).ToArray());
    }

    [Fact]
    public void Providers_that_do_not_snap_keep_the_requested_width_as_the_descriptor()
    {
        var settings = new BxImageSettings();
        settings.AddCloudinary("demo", makeDefault: true);

        var resolved = settings.Resolve("/a.png", new BxImageOptions { Sizes = "400px", Densities = "x1" }, 400, null);

        Assert.Contains(" 400w", resolved.SrcSet);
    }

    [Fact]
    public void A_protocol_relative_source_is_sent_to_the_endpoint_as_https()
    {
        var settings = new BxImageSettings();
        settings.Domains.Add("img.test");
        settings.AddLocal(makeDefault: true);

        var resolved = settings.Resolve("//img.test/a.png", null, 100, null);

        Assert.StartsWith("/_blux/img?src=https%3A%2F%2Fimg.test%2Fa.png&w=128", resolved.Src);
        Assert.Equal("/_blux/img?src=https%3A%2F%2Fimg.test%2Fa.png", new BxLocalImageProvider().GetUrl("//img.test/a.png", M()));
    }

    [Fact]
    public void Domains_follow_the_server_rule_for_ports()
    {
        string Src(string entry, string src)
        {
            var settings = new BxImageSettings();
            settings.Domains.Add(entry);
            settings.AddLocal(makeDefault: true);
            return settings.Resolve(src, null, 100, null).Src;
        }

        Assert.StartsWith("/_blux/img", Src("img.test", "https://img.test/a.png"));
        Assert.StartsWith("/_blux/img", Src("img.test", "https://img.test:443/a.png"));
        Assert.Equal("https://img.test:8443/a.png", Src("img.test", "https://img.test:8443/a.png"));
        Assert.StartsWith("/_blux/img", Src("img.test:8443", "https://img.test:8443/a.png"));
        Assert.Equal("https://img.test/a.png", Src("img.test:8443", "https://img.test/a.png"));
        Assert.Equal("https://img.test:9000/a.png", Src("img.test:8443", "https://img.test:9000/a.png"));
    }

    [Fact]
    public void The_placeholder_shares_the_source_and_preset_rules_of_resolve()
    {
        var settings = new BxImageSettings();
        settings.AddLocal(makeDefault: true);

        Assert.Null(settings.ResolvePlaceholder("blob:https://x/1", null, 10, null));
        Assert.Null(settings.ResolvePlaceholder("data:image/png;base64,AAAA", null, 10, null));
        Assert.Throws<InvalidOperationException>(() => settings.ResolvePlaceholder("/a.png", new BxImageOptions { Preset = "nope" }, 10, null));
        Assert.Throws<InvalidOperationException>(() => settings.Resolve("/a.png", new BxImageOptions { Preset = "nope" }, 10, null));
        Assert.StartsWith("/_blux/img?src=%2Fa.png&w=16", settings.ResolvePlaceholder("/a.png", null, 10, null));
    }
}
