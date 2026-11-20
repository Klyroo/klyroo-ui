namespace Blux.UI.E2E;

/// <summary>
/// The docs site declares Geist, Geist Mono and Doto with BluxFont items: the build downloads and self-hosts them, so a page
/// never asks a font provider for anything.
/// </summary>
public class FontsTests(PlaygroundServer server) : PlaygroundTest(server)
{
    [Theory]
    [InlineData("/")]
    [InlineData("/components/button")]
    [InlineData("/docs/fonts")]
    public async Task Fonts_come_from_the_site_and_never_from_a_provider(string path)
    {
        var hosts = new HashSet<string>();
        Page.Request += (_, request) => hosts.Add(new Uri(request.Url).Host);

        await Page.GotoAsync(path);
        await Page.WaitForFunctionAsync("document.fonts.ready.then(() => true)");
        await Page.WaitForFunctionAsync("[...document.fonts].some(f => f.family.replace(/\"/g, '') === 'Geist' && f.status === 'loaded')");

        Assert.DoesNotContain(hosts, h => h.Contains("googleapis") || h.Contains("gstatic") || h.Contains("bunny") || h.Contains("fontsource") || h.Contains("jsdelivr"));
        Assert.Equal([new Uri(BaseUrl).Host], hosts);
    }

    [Fact]
    public async Task Geist_is_loaded_and_the_role_tokens_point_at_the_self_hosted_faces()
    {
        await Page.GotoAsync("/");
        await Page.WaitForFunctionAsync("[...document.fonts].some(f => f.family.replace(/\"/g, '') === 'Geist' && f.status === 'loaded')");

        Assert.True(await Page.EvaluateAsync<bool>("document.fonts.check('16px Geist')"));
        var sans = await Page.EvaluateAsync<string>("getComputedStyle(document.documentElement).getPropertyValue('--blux-font-sans')");
        Assert.StartsWith("\"Geist\", \"Geist Fallback\"", sans.Trim());
        var mono = await Page.EvaluateAsync<string>("getComputedStyle(document.documentElement).getPropertyValue('--blux-font-mono')");
        Assert.StartsWith("\"Geist Mono\", \"Geist Mono Fallback\"", mono.Trim());
        // The page text really is set in it (Tailwind's font-sans and the preflight follow the token).
        var body = await Page.EvaluateAsync<string>("getComputedStyle(document.body).fontFamily");
        Assert.StartsWith("Geist,", body);
    }

    [Fact]
    public async Task The_main_faces_are_preloaded_and_served_with_a_long_cache()
    {
        await Page.GotoAsync("/");
        var preloads = await Page.EvaluateAsync<string[]>("[...document.querySelectorAll('link[rel=preload][as=font]')].map(l => l.getAttribute('href'))");
        Assert.Equal(2, preloads.Length);
        Assert.All(preloads, href => Assert.StartsWith("_blux/fonts/", href));
        Assert.Contains(preloads, href => href.Contains("Geist-400-700-normal-latin"));

        var response = await Page.APIRequest.GetAsync(preloads[0]);
        Assert.True(response.Ok);
        Assert.Equal("font/woff2", response.Headers["content-type"]);
        // Development servers send no-cache; a published app sends the year-long immutable header.
        if (Environment.GetEnvironmentVariable("BLUX_PLAYGROUND_PUBLISHED") is { Length: > 0 })
        {
            Assert.Equal("max-age=31536000, immutable", response.Headers["cache-control"]);
        }
    }

    [Fact]
    public async Task The_stylesheet_is_linked_before_the_site_stylesheet_and_declares_a_fallback_face()
    {
        await Page.GotoAsync("/");
        var hrefs = await Page.EvaluateAsync<string[]>("[...document.querySelectorAll('link[rel=stylesheet]')].map(l => l.getAttribute('href'))");
        var fonts = Array.FindIndex(hrefs, h => h.Contains("blux.fonts"));
        var site = Array.FindIndex(hrefs, h => h.StartsWith("site"));
        Assert.InRange(fonts, 0, site - 1);

        var css = await Page.APIRequest.GetAsync(hrefs[fonts]);
        var text = await css.TextAsync();
        Assert.Contains("\"Geist Fallback\"", text);
        Assert.Contains("size-adjust:", text);
        Assert.DoesNotContain("https://", text);
    }
}
