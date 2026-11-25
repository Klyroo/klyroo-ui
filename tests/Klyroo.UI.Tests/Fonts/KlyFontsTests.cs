namespace Klyroo.UI.Tests.Fonts;

public class KlyFontsTests : KlyrooTestContext
{
    private const string Manifest = """
        {"v":1,"css":"_klyroo/fonts/klyroo.fonts.css","fonts":[
          {"family":"Geist","role":"sans","provider":"google","fallback":true,"preload":["_klyroo/fonts/Geist-400-700-normal-latin.ab12cd34.woff2"]},
          {"family":"Geist Mono","role":"mono","provider":"google","fallback":true,"preload":["_klyroo/fonts/GeistMono-400-600-normal-latin.0f0f0f0f.woff2"]},
          {"family":"Doto","role":"","provider":"google","fallback":false,"preload":[]}]}
        """;

    [Fact]
    public void Renders_the_stylesheet_and_one_preload_per_preloaded_face()
    {
        var cut = Render<KlyFonts>(p => p.Add(c => c.ManifestJson, Manifest));
        var links = cut.FindAll("link");
        Assert.Equal(3, links.Count);
        var preloads = links.Where(l => l.GetAttribute("rel") == "preload").ToList();
        Assert.Equal(2, preloads.Count);
        Assert.All(preloads, l =>
        {
            Assert.Equal("font", l.GetAttribute("as"));
            Assert.Equal("font/woff2", l.GetAttribute("type"));
            Assert.True(l.HasAttribute("crossorigin"));
        });
        Assert.Equal("_klyroo/fonts/Geist-400-700-normal-latin.ab12cd34.woff2", preloads[0].GetAttribute("href"));
        var sheet = links.Single(l => l.GetAttribute("rel") == "stylesheet");
        Assert.Equal("_klyroo/fonts/klyroo.fonts.css", sheet.GetAttribute("href"));
    }

    [Fact]
    public void Preloads_come_before_the_stylesheet()
    {
        var cut = Render<KlyFonts>(p => p.Add(c => c.ManifestJson, Manifest));
        Assert.Equal(["preload", "preload", "stylesheet"], cut.FindAll("link").Select(l => l.GetAttribute("rel")!));
    }

    [Fact]
    public void Renders_nothing_when_no_font_is_declared()
    {
        // The test assembly has no generated manifest, which is what an app without KlyrooFont items looks like.
        var cut = Render<KlyFonts>();
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void A_manifest_without_preload_still_links_the_stylesheet()
    {
        var cut = Render<KlyFonts>(p => p.Add(c => c.ManifestJson, """{"v":1,"css":"_klyroo/fonts/klyroo.fonts.css","fonts":[]}"""));
        Assert.Single(cut.FindAll("link"));
    }
}
