using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tests;

/// <summary>A provider that makes the generated URLs readable: <c>/cdn{src}?w=..&amp;h=..&amp;q=..&amp;f=..&amp;fit=..</c>.</summary>
internal sealed class EchoProvider(string name = "echo", bool validateDomains = false, bool blur = false) : IBxImageProvider
{
    public string Name => name;
    public bool CanResize => true;
    public bool SupportsBlur => blur;
    public bool ValidateDomains => validateDomains;

    public string GetUrl(string src, BxImageModifiers m)
    {
        var parts = new List<string>();
        if (m.Width is { } w) parts.Add($"w={w}");
        if (m.Height is { } h) parts.Add($"h={h}");
        if (m.Quality is { } q) parts.Add($"q={q}");
        if (m.Format is { } f) parts.Add($"f={f}");
        if (m.Fit is { } fit) parts.Add($"fit={fit.ToString().ToLowerInvariant()}");
        if (m.Blur is { } b) parts.Add($"blur={b}");
        if (m.Extra is { } extra) foreach (var (key, value) in extra) parts.Add($"{key}={value}");
        return $"/cdn{src}" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
    }
}

public class ImageTests : BluxTestContext
{
    public ImageTests() : base(o =>
    {
        o.Images.AddProvider(new EchoProvider());
        o.Images.AddProvider(new EchoProvider("guarded", validateDomains: true));
        o.Images.AddProvider(new EchoProvider("blurry", blur: true));
        o.Images.Domains.Add("images.example.com");
        o.Images.Presets["thumb"] = new BxImageOptions { Format = "webp", Quality = 60, Provider = "echo", Modifiers = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" } };
    })
    {
    }

    private BxImageSettings Images => Services.GetRequiredService<BluxOptions>().Images;

    [Fact]
    public void Sizes_follow_the_algorithm_of_Nuxt_Image_for_breakpoints_and_densities()
    {
        // "sm:50vw" is 50% of 640, "md:400px" is 400. Each entry applies from its breakpoint to the next one.
        var resolved = Images.Resolve("/a.png", new BxImageOptions { Provider = "echo", Sizes = "sm:100vw md:50vw lg:400px", Densities = "x1 x2" });

        Assert.Equal("(max-width: 767px) 100vw, (max-width: 1023px) 50vw, 400px", resolved.Sizes);
        Assert.Equal(
            "/cdn/a.png?w=384 384w, /cdn/a.png?w=400 400w, /cdn/a.png?w=640 640w, /cdn/a.png?w=768 768w, /cdn/a.png?w=800 800w, /cdn/a.png?w=1280 1280w",
            resolved.SrcSet);
        Assert.Equal("/cdn/a.png?w=1280", resolved.Src);
    }

    [Fact]
    public void An_unprefixed_size_is_the_default_and_a_fluid_one_is_measured_against_the_first_breakpoint()
    {
        var resolved = Images.Resolve("/a.png", new BxImageOptions { Provider = "echo", Sizes = "100vw sm:50vw md:400px", Densities = "x1 x2" });

        Assert.Equal("(max-width: 639px) 100vw, (max-width: 767px) 50vw, 400px", resolved.Sizes);
        Assert.Equal(
            "/cdn/a.png?w=320 320w, /cdn/a.png?w=400 400w, /cdn/a.png?w=640 640w, /cdn/a.png?w=800 800w, /cdn/a.png?w=1280 1280w",
            resolved.SrcSet);
        Assert.Equal("/cdn/a.png?w=1280", resolved.Src);
    }

    [Fact]
    public void Sizes_keep_the_ratio_when_width_and_height_are_given_and_forward_format_and_quality()
    {
        var resolved = Images.Resolve("/a.png", new BxImageOptions { Provider = "echo", Sizes = "sm:100px md:200px", Densities = "x1", Format = "webp", Quality = 70 }, 100, 50);

        Assert.Equal("(max-width: 767px) 100px, 200px", resolved.Sizes);
        Assert.Equal("/cdn/a.png?w=100&h=50&q=70&f=webp 100w, /cdn/a.png?w=200&h=100&q=70&f=webp 200w", resolved.SrcSet);
    }

    [Fact]
    public void Densities_without_sizes_scale_the_width_and_the_height()
    {
        var resolved = Images.Resolve("/a.png", new BxImageOptions { Provider = "echo", Densities = "x1 x2" }, 32, 32);

        Assert.Equal("/cdn/a.png?w=32&h=32 1x, /cdn/a.png?w=64&h=64 2x", resolved.SrcSet);
        Assert.Equal("/cdn/a.png?w=32&h=32", resolved.Src);
        Assert.Null(resolved.Sizes);
    }

    [Fact]
    public void Without_a_width_or_a_height_there_is_nothing_to_scale_so_no_srcset()
    {
        var resolved = Images.Resolve("/a.png", new BxImageOptions { Provider = "echo", Format = "webp" });

        Assert.Equal("/cdn/a.png?f=webp", resolved.Src);
        Assert.Null(resolved.SrcSet);
    }

    [Fact]
    public void Default_densities_and_breakpoints_are_nuxts_and_tailwinds()
    {
        Assert.Equal([1, 2], Images.Densities);
        Assert.Equal(640, Images.Screens["sm"]);
        Assert.Equal(1536, Images.Screens["2xl"]);
        Assert.Equal("none", Images.DefaultProvider);
    }

    [Fact]
    public void Entries_with_unusable_sizes_or_densities_are_rejected_or_skipped()
    {
        var skipped = Images.Resolve("/a.png", new BxImageOptions { Provider = "echo", Sizes = "sm:wide md:300px", Densities = "x1" });
        Assert.Equal("300px", skipped.Sizes);
        Assert.Throws<ArgumentException>(() => Images.Resolve("/a.png", new BxImageOptions { Provider = "echo", Densities = "x0" }, 10, 10));
    }

    [Fact]
    public void Sources_without_a_leading_slash_get_one_and_srcset_urls_are_escaped()
    {
        var resolved = Images.Resolve("img/a b.png", new BxImageOptions { Provider = "echo" }, 10, null);

        Assert.Equal("/cdn/img/a%20b.png?w=10 1x, /cdn/img/a%20b.png?w=20 2x", resolved.SrcSet);
    }

    [Fact]
    public void The_none_provider_returns_the_source_unchanged_without_srcset()
    {
        var resolved = Images.Resolve("img/a.png", new BxImageOptions { Sizes = "100vw", Format = "webp" }, 100, 50);

        Assert.Equal("img/a.png", resolved.Src);
        Assert.Null(resolved.SrcSet);
        Assert.Null(resolved.Sizes);
    }

    [Fact]
    public void Remote_sources_outside_the_allowlist_are_passed_through_when_the_provider_validates_domains()
    {
        var options = new BxImageOptions { Provider = "guarded" };
        var other = Images.Resolve("https://other.example.org/a.png", options, 100, null);
        var listed = Images.Resolve("https://IMAGES.example.com/a.png", options, 100, null);
        var local = Images.Resolve("/a.png", options, 100, null);

        Assert.Equal("https://other.example.org/a.png", other.Src);
        Assert.Null(other.SrcSet);
        Assert.Equal("/cdnhttps://IMAGES.example.com/a.png?w=100", listed.Src);
        Assert.Equal("/cdn/a.png?w=100", local.Src);
    }

    [Fact]
    public void Presets_sit_below_explicit_options_and_merge_modifiers_key_by_key()
    {
        var resolved = Images.Resolve("/a.png", new BxImageOptions { Preset = "thumb", Quality = 90, Modifiers = new Dictionary<string, string> { ["b"] = "3", ["c"] = "4" } }, 10, null);

        Assert.Equal("/cdn/a.png?w=10&q=90&f=webp&a=1&b=3&c=4", resolved.Src);
    }

    [Fact]
    public void Unknown_provider_or_preset_is_a_clear_error()
    {
        Assert.Contains("nope", Assert.Throws<InvalidOperationException>(() => Images.Resolve("/a.png", new BxImageOptions { Provider = "nope" })).Message);
        Assert.Contains("nope", Assert.Throws<InvalidOperationException>(() => Images.Resolve("/a.png", new BxImageOptions { Preset = "nope" })).Message);
    }

    [Fact]
    public void Settings_become_read_only_after_the_first_resolution()
    {
        Images.Resolve("/a.png");

        Assert.Throws<InvalidOperationException>(() => Images.Domains.Add("late.example.com"));
        Assert.Throws<InvalidOperationException>(() => Images.Screens["sm"] = 1);
        Assert.Throws<InvalidOperationException>(() => Images.Presets["x"] = new BxImageOptions());
        Assert.Throws<InvalidOperationException>(() => Images.AddProvider(new EchoProvider("late")));
        Assert.Throws<InvalidOperationException>(() => Images.DefaultProvider = "echo");
        Assert.Throws<InvalidOperationException>(() => Images.Densities = [1]);
    }

    [Fact]
    public void Data_urls_and_empty_sources_are_left_alone()
    {
        Assert.Equal("data:image/png;base64,AAAA", Images.Resolve("data:image/png;base64,AAAA", new BxImageOptions { Provider = "echo" }, 10, 10).Src);
        Assert.Equal("", Images.Resolve(null).Src);
    }

    [Fact]
    public void BxImage_renders_one_img_with_the_attributes_of_the_resolution()
    {
        var cut = Render<BxImage>(p => p
            .Add(i => i.Src, "/a.png").Add(i => i.Alt, "A").Add(i => i.Width, 100).Add(i => i.Height, 50)
            .Add(i => i.Provider, "echo").Add(i => i.Format, "webp").Add(i => i.Class, "rounded"));

        var img = cut.Find("img");
        Assert.Equal("/cdn/a.png?w=100&h=50&f=webp", img.GetAttribute("src"));
        Assert.Equal("/cdn/a.png?w=100&h=50&f=webp 1x, /cdn/a.png?w=200&h=100&f=webp 2x", img.GetAttribute("srcset"));
        Assert.Equal("A", img.GetAttribute("alt"));
        Assert.Equal("100", img.GetAttribute("width"));
        Assert.Equal("50", img.GetAttribute("height"));
        Assert.Equal("lazy", img.GetAttribute("loading"));
        Assert.Equal("rounded", img.GetAttribute("class"));
        Assert.Single(cut.FindAll("img"));
        Assert.False(img.HasAttribute("sizes"));
        Assert.False(img.HasAttribute("fetchpriority"));
    }

    [Fact]
    public void BxImage_with_the_default_provider_keeps_the_source_and_sets_size_and_loading()
    {
        var cut = Render<BxImage>(p => p.Add(i => i.Src, "/a.png").Add(i => i.Alt, "").Add(i => i.Width, 100).Add(i => i.Height, 50).Add(i => i.Sizes, "100vw"));

        var img = cut.Find("img");
        Assert.Equal("/a.png", img.GetAttribute("src"));
        Assert.Equal("", img.GetAttribute("alt"));
        Assert.Equal("lazy", img.GetAttribute("loading"));
        Assert.Equal("100", img.GetAttribute("width"));
        Assert.False(img.HasAttribute("srcset"));
        Assert.False(img.HasAttribute("sizes"));
    }

    [Fact]
    public void BxImage_requires_alt()
    {
        var error = Assert.ThrowsAny<Exception>(() => Render<BxImage>(p => p.Add(i => i.Src, "/a.png")));

        Assert.Contains("Alt", error.ToString());
    }

    [Fact]
    public void BxImage_parameters_override_ImageOptions()
    {
        var cut = Render<BxImage>(p => p
            .Add(i => i.Src, "/a.png").Add(i => i.Alt, "").Add(i => i.Width, 10)
            .Add(i => i.ImageOptions, new BxImageOptions { Provider = "echo", Format = "avif", Quality = 40, Loading = BxImageLoading.Eager })
            .Add(i => i.Format, "webp"));

        var img = cut.Find("img");
        Assert.Equal("/cdn/a.png?w=10&q=40&f=webp", img.GetAttribute("src"));
        Assert.Equal("eager", img.GetAttribute("loading"));
    }

    [Fact]
    public void Preload_loads_eagerly_with_high_priority_and_adds_a_preload_link_to_the_head()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render(builder =>
        {
            builder.OpenComponent<HeadOutlet>(0);
            builder.CloseComponent();
            builder.OpenComponent<BxImage>(1);
            builder.AddComponentParameter(2, nameof(BxImage.Src), "/hero.png");
            builder.AddComponentParameter(3, nameof(BxImage.Alt), "Hero");
            builder.AddComponentParameter(4, nameof(BxImage.Provider), "echo");
            builder.AddComponentParameter(5, nameof(BxImage.Sizes), "sm:100vw md:50vw");
            builder.AddComponentParameter(6, nameof(BxImage.Densities), "x1");
            builder.AddComponentParameter(7, nameof(BxImage.Preload), (bool?)true);
            builder.CloseComponent();
        });

        var img = cut.Find("img");
        Assert.Equal("eager", img.GetAttribute("loading"));
        Assert.Equal("high", img.GetAttribute("fetchpriority"));
        var link = cut.Find("link[rel=preload]");
        Assert.Equal("image", link.GetAttribute("as"));
        Assert.Equal(img.GetAttribute("src"), link.GetAttribute("href"));
        Assert.Equal(img.GetAttribute("srcset"), link.GetAttribute("imagesrcset"));
        Assert.Equal(img.GetAttribute("sizes"), link.GetAttribute("imagesizes"));
        Assert.Equal("high", link.GetAttribute("fetchpriority"));
    }

    [Fact]
    public void Placeholder_true_asks_the_provider_for_a_tiny_blurred_version_and_a_url_is_used_as_is()
    {
        var auto = Render<BxImage>(p => p.Add(i => i.Src, "/a.png").Add(i => i.Alt, "").Add(i => i.Width, 200).Add(i => i.Height, 100)
            .Add(i => i.Provider, "blurry").Add(i => i.Placeholder, true));
        var url = Render<BxImage>(p => p.Add(i => i.Src, "/a.png").Add(i => i.Alt, "").Add(i => i.Placeholder, "/tiny.png"));

        Assert.Contains("url(\"/cdn/a.png?w=10&h=5&q=50&blur=3\")", auto.Find("img").GetAttribute("style"));
        Assert.Contains("url(\"/tiny.png\")", url.Find("img").GetAttribute("style"));
    }

    [Fact]
    public void A_placeholder_url_cannot_break_out_of_the_style_and_a_style_of_the_caller_is_kept()
    {
        var cut = Render<BxImage>(p => p.Add(i => i.Src, "/a.png").Add(i => i.Alt, "").Add(i => i.Placeholder, "/t.png\");color:red;x(\"")
            .AddUnmatched("style", "opacity:.5"));

        var style = cut.Find("img").GetAttribute("style")!;
        Assert.DoesNotContain("\");color:red", style);
        Assert.EndsWith(";opacity:.5", style);
    }

    [Theory]
    [InlineData(BxSize.Xs, 24)]
    [InlineData(BxSize.Md, 32)]
    [InlineData(BxSize.Xl, 48)]
    public void Avatar_passes_its_size_in_pixels_and_forwards_the_image_options(BxSize size, int pixels)
    {
        var cut = Render<BxAvatar>(p => p.Add(a => a.Src, "/a.png").Add(a => a.Alt, "Ada").Add(a => a.Size, size)
            .Add(a => a.ImageOptions, new BxImageOptions { Provider = "echo", Format = "webp" }));

        var img = cut.Find("img");
        Assert.Equal($"/cdn/a.png?w={pixels}&h={pixels}&f=webp", img.GetAttribute("src"));
        Assert.Equal($"/cdn/a.png?w={pixels}&h={pixels}&f=webp 1x, /cdn/a.png?w={pixels * 2}&h={pixels * 2}&f=webp 2x", img.GetAttribute("srcset"));
        Assert.Equal(pixels.ToString(), img.GetAttribute("width"));
        Assert.Equal(pixels.ToString(), img.GetAttribute("height"));
        Assert.Equal("", img.GetAttribute("alt"));
        Assert.True(img.HasAttribute("data-bx-avatar-image"));
        Assert.Equal("Ada", cut.Find("span").GetAttribute("aria-label"));
        Assert.Contains("A", cut.Find("[aria-hidden=true]").TextContent);
    }

    [Fact]
    public void Avatar_without_a_provider_keeps_its_source_and_gets_the_size()
    {
        var cut = Render<BxAvatar>(p => p.Add(a => a.Src, "/a.png").Add(a => a.Alt, "Ada"));

        var img = cut.Find("img");
        Assert.Equal("/a.png", img.GetAttribute("src"));
        Assert.Equal("32", img.GetAttribute("width"));
        Assert.False(img.HasAttribute("srcset"));
    }

    [Fact]
    public void ColorModeImage_renders_two_images_that_share_the_options_and_the_size_attributes()
    {
        var cut = Render<BxColorModeImage>(p => p.Add(i => i.Light, "/l.png").Add(i => i.Dark, "/d.png").Add(i => i.Alt, "Logo")
            .Add(i => i.ImageOptions, new BxImageOptions { Provider = "echo", Format = "webp" }).AddUnmatched("width", "40"));

        var images = cut.FindAll("img");
        Assert.Equal(2, images.Count);
        Assert.Equal("/cdn/l.png?w=40&f=webp", images[0].GetAttribute("src"));
        Assert.Equal("/cdn/d.png?w=40&f=webp", images[1].GetAttribute("src"));
        Assert.All(images, i => Assert.Equal("40", i.GetAttribute("width")));
    }

    [Fact]
    public void BlogPost_ChangelogVersion_and_PageLogos_forward_the_image_options()
    {
        var options = new BxImageOptions { Provider = "echo", Format = "webp", Sizes = "100vw sm:50vw", Densities = "x1" };

        var post = Render<BxBlogPost>(p => p.Add(b => b.Title, "T").Add(b => b.Image, "/p.png").Add(b => b.ImageOptions, options));
        var version = Render<BxChangelogVersion>(p => p.Add(b => b.Title, "v1").Add(b => b.Image, "/p.png").Add(b => b.ImageOptions, options));
        var logos = Render<BxPageLogos>(p => p.Add(b => b.Items, new[] { new PageLogoItem("P") { Src = "/p.png" } }).Add(b => b.ImageOptions, options));

        foreach (var cut in new[] { post.Find("img"), version.Find("img"), logos.Find("img") })
        {
            Assert.Equal("(max-width: 639px) 100vw, 50vw", cut.GetAttribute("sizes"));
            Assert.Contains("f=webp", cut.GetAttribute("srcset"));
        }
    }

    [Fact]
    public void ChatAttachment_card_preview_has_the_size_of_its_slot()
    {
        var cut = Render<BxChatAttachment>(p => p.Add(a => a.Name, "a.png").Add(a => a.Type, "image/png").Add(a => a.Url, "/a.png")
            .Add(a => a.ImageOptions, new BxImageOptions { Provider = "echo" }));

        var img = cut.Find("img");
        Assert.Equal("/cdn/a.png?w=36&h=36", img.GetAttribute("src"));
        Assert.True(img.HasAttribute("data-bx-avatar-image"));
    }

    [Fact]
    public void Unmatched_attributes_reach_the_img()
    {
        var cut = Render<BxImage>(p => p.Add(i => i.Src, "/a.png").Add(i => i.Alt, "").AddUnmatched("data-x", "1").AddUnmatched("title", "T"));

        Assert.Equal("1", cut.Find("img").GetAttribute("data-x"));
        Assert.Equal("T", cut.Find("img").GetAttribute("title"));
    }
}
