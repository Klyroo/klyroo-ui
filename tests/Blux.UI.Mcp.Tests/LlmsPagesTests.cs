namespace Blux.UI.Mcp.Tests;

public class LlmsPagesTests
{
    [Fact]
    public void A_configured_site_url_is_used_whatever_the_request_says()
    {
        var pages = new LlmsPages(TestSupport.Bundle, "https://docs.example.com/");

        var text = pages.Index("http://evil.test");

        Assert.Contains("](https://docs.example.com/components/button)", text);
        Assert.DoesNotContain("evil.test", text);
        Assert.DoesNotContain("evil.test", pages.Full("http://evil.test"));
    }

    [Fact]
    public void With_a_site_url_the_text_is_generated_once()
    {
        var pages = new LlmsPages(TestSupport.Bundle, "https://docs.example.com");

        Assert.Same(pages.Full("http://a"), pages.Full("http://b"));
        Assert.Same(pages.Index("http://a"), pages.Index("http://b"));
    }

    [Fact]
    public void Without_a_site_url_the_request_origin_is_used_and_cached_for_a_few_origins_only()
    {
        var pages = new LlmsPages(TestSupport.Bundle, null);

        Assert.Contains("](https://one.test/components/button)", pages.Index("https://one.test"));
        Assert.Same(pages.Full("https://one.test"), pages.Full("https://one.test"));
        for (var i = 0; i < 50; i++) pages.Full($"https://host{i}.test");
        Assert.True(pages.CachedOrigins <= LlmsPages.MaxCachedOrigins);
    }

    [Theory]
    [InlineData("docs.example.com")]
    [InlineData("ftp://docs.example.com")]
    [InlineData("javascript:alert(1)")]
    public void A_site_url_that_is_not_an_http_address_is_rejected(string value) =>
        Assert.Throws<ArgumentException>(() => new LlmsPages(TestSupport.Bundle, value));

    [Fact]
    public void An_empty_site_url_means_not_configured() =>
        Assert.Null(new LlmsPages(TestSupport.Bundle, "  ").SiteUrl);
}
