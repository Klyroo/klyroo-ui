using Klyroo.UI.Internal;
using Xunit;

namespace Klyroo.UI.Tests;

public class HtmlTextTests
{
    [Fact]
    public void The_linear_fallback_keeps_the_text_and_drops_tags_scripts_and_styles()
    {
        var text = HtmlText.StripTags("<p>Hello <img src=x onerror=alert(1)></p><script>alert(2)</script><STYLE>p{}</STYLE><p>World</p><scripted>kept</scripted>");

        Assert.Equal("Hello Worldkept", text);
    }

    [Fact]
    public void The_linear_fallback_stops_at_an_unclosed_tag_or_script()
    {
        Assert.Equal("a", HtmlText.StripTags("a<b"));
        Assert.Equal("a", HtmlText.StripTags("a<script>never closed"));
        Assert.Equal("", HtmlText.StripTags(""));
    }
}
