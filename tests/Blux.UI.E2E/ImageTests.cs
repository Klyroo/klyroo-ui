namespace Blux.UI.E2E;

/// <summary>
/// The docs site runs Blux.UI.Images.Local at /_blux/img, so the BxImage page loads real resized images in every render mode.
/// </summary>
public class ImageTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private async Task ScrollAndWaitForImage(string testId)
    {
        var image = Page.GetByTestId(testId);
        await image.ScrollIntoViewIfNeededAsync();
        await Page.WaitForFunctionAsync("id => { const i = document.querySelector(`[data-testid=${id}]`); return i && i.complete && i.naturalWidth > 0; }", testId, new() { Timeout = 30_000 });
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Image_page_has_no_accessibility_violations(string mode)
    {
        await Open(mode, "image");
        foreach (var id in new[] { "image-basic", "image-cover", "image-contain", "image-responsive", "image-placeholder", "image-eager" })
        {
            await ScrollAndWaitForImage(id);
        }

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sized_image_is_a_resized_webp_from_the_local_optimizer(string mode)
    {
        await Open(mode, "image");
        await ScrollAndWaitForImage("image-basic");
        var image = Page.GetByTestId("image-basic");

        var source = await image.EvaluateAsync<string>("i => i.currentSrc");
        var response = await Page.APIRequest.GetAsync(source);

        Assert.Contains("/_blux/img?src=%2Fimg%2Fsample-gradient.png", source);
        Assert.Equal("image/webp", response.Headers["content-type"]);
        // 320 CSS pixels at density 1 is served as the next step of the optimizer, 384.
        Assert.Equal(384, await image.EvaluateAsync<int>("i => i.naturalWidth"));
        Assert.Equal(216, await image.EvaluateAsync<int>("i => i.naturalHeight"));
        await Assertions.Expect(image).ToHaveAttributeAsync("width", "320");
        await Assertions.Expect(image).ToHaveAttributeAsync("loading", "lazy");
        var srcset = await image.GetAttributeAsync("srcset");
        Assert.Contains(" 1x,", srcset);
        Assert.EndsWith(" 2x", srcset);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Cover_crops_to_the_requested_shape(string mode)
    {
        await Open(mode, "image");
        await ScrollAndWaitForImage("image-cover");
        var image = Page.GetByTestId("image-cover");

        Assert.Equal(256, await image.EvaluateAsync<int>("i => i.naturalWidth"));
        Assert.Equal(256, await image.EvaluateAsync<int>("i => i.naturalHeight"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Responsive_image_carries_sizes_and_width_candidates(string mode)
    {
        await Open(mode, "image");
        await ScrollAndWaitForImage("image-responsive");
        var image = Page.GetByTestId("image-responsive");

        var sizes = await image.GetAttributeAsync("sizes");
        var srcset = await image.GetAttributeAsync("srcset");
        var source = await image.EvaluateAsync<string>("i => i.currentSrc");
        var response = await Page.APIRequest.GetAsync(source);

        Assert.Equal("(max-width: 639px) 100vw, (max-width: 767px) 50vw, 400px", sizes);
        Assert.Matches(@"w=\d+.* \d+w", srcset);
        Assert.Equal("image/webp", response.Headers["content-type"]);
        Assert.True(await image.EvaluateAsync<int>("i => i.naturalWidth") > 0);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Placeholder_paints_a_tiny_version_and_eager_images_have_high_priority(string mode)
    {
        await Open(mode, "image");
        await ScrollAndWaitForImage("image-placeholder");
        await ScrollAndWaitForImage("image-eager");

        var style = await Page.GetByTestId("image-placeholder").GetAttributeAsync("style");

        Assert.Contains("background-image:url(", style);
        Assert.Contains("w=16", style);
        await Assertions.Expect(Page.GetByTestId("image-eager")).ToHaveAttributeAsync("loading", "eager");
        await Assertions.Expect(Page.GetByTestId("image-eager")).ToHaveAttributeAsync("fetchpriority", "high");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Avatar_still_shows_its_initials_when_the_picture_is_broken(string mode)
    {
        await Open(mode, "avatar");
        var avatar = Page.Locator("[role=img][aria-label='Ada Lovelace']");
        await Assertions.Expect(avatar).ToBeVisibleAsync();

        // /missing.png goes through the optimizer, which answers 404: the image is hidden and the fallback shows.
        var image = avatar.Locator("img[data-bx-avatar-image]");
        await Assertions.Expect(image).ToBeHiddenAsync();
        await Assertions.Expect(avatar.Locator("[aria-hidden=true]")).ToHaveTextAsync("AL");
        var source = await image.GetAttributeAsync("src");
        Assert.StartsWith("/_blux/img?src=%2Fmissing.png", source);
        await Assertions.Expect(image).ToHaveAttributeAsync("width", "40");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Remote_avatars_are_not_sent_to_the_optimizer(string mode)
    {
        await Open(mode, "avatar");

        var remote = Page.Locator("img[data-bx-avatar-image][src^='https://i.pravatar.cc']");

        Assert.Equal(1, await remote.CountAsync());
    }

    [Fact]
    public async Task Images_guide_has_no_accessibility_violations()
    {
        await Page.GotoAsync("/docs/images");
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Images", Level = 1 })).ToBeVisibleAsync();

        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task The_optimizer_refuses_path_traversal_and_unlisted_hosts()
    {
        await Page.GotoAsync("/");

        Assert.Equal(400, (await Page.APIRequest.GetAsync("/_blux/img?src=/../Program.cs")).Status);
        Assert.Equal(400, (await Page.APIRequest.GetAsync("/_blux/img?src=%2e%2e%2fappsettings.json")).Status);
        Assert.Equal(403, (await Page.APIRequest.GetAsync("/_blux/img?src=https://example.com/a.png")).Status);
        Assert.Equal(415, (await Page.APIRequest.GetAsync("/_blux/img?src=/site.css")).Status);
        Assert.Equal(200, (await Page.APIRequest.GetAsync("/_blux/img?src=/img/sample-waves.png&w=64&f=webp")).Status);
    }
}
