using Klyroo.UI.Build;

namespace Klyroo.UI.Tests.Icons;

public class SvgImportTests
{
    [Fact]
    public void Takes_the_content_and_the_view_box_of_the_root()
    {
        var icon = SvgImport.Parse("""
            <?xml version="1.0" encoding="UTF-8"?>
            <!-- made by hand -->
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 20">
              <path d="M1 1h30"/>
              <circle cx="4" cy="4" r="2"/>
            </svg>
            """);

        Assert.Equal("<path d=\"M1 1h30\"/><circle cx=\"4\" cy=\"4\" r=\"2\"/>", icon.Body);
        Assert.Equal("0 0 32 20", icon.ViewBox);
    }

    [Fact]
    public void Presentation_attributes_of_the_root_move_to_a_group()
    {
        var icon = SvgImport.Parse("""<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" width="24"><path d="M0 0"/></svg>""");

        Assert.Equal("<g fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\"><path d=\"M0 0\"/></g>", icon.Body);
    }

    [Theory]
    [InlineData("""<svg width="48px" height="24"><g/></svg>""", "0 0 48 24")]
    [InlineData("""<svg><g/></svg>""", "0 0 24 24")]
    [InlineData("""<svg viewBox="0,0,16,16"><g/></svg>""", "0 0 16 16")]
    [InlineData("""<svg viewBox="-2 -2 20 20"><g/></svg>""", "-2 -2 20 20")]
    public void Finds_a_view_box_when_the_file_has_none_or_writes_it_differently(string svg, string viewBox)
    {
        Assert.Equal(viewBox, SvgImport.Parse(svg).ViewBox);
    }

    [Fact]
    public void Removes_scripts_handlers_and_foreign_content()
    {
        var icon = SvgImport.Parse("""
            <svg viewBox="0 0 24 24"><title>Logo</title><desc>x</desc><script>alert(1)</script>
            <path d="M0 0" onclick="alert(1)" onload='x()'/><foreignObject><div/></foreignObject>
            <a href="javascript:alert(1)"><path d="M1 1"/></a></svg>
            """);

        Assert.DoesNotContain("script", icon.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", icon.Body);
        Assert.DoesNotContain("onload", icon.Body);
        Assert.DoesNotContain("foreignObject", icon.Body);
        Assert.DoesNotContain("javascript", icon.Body);
        Assert.DoesNotContain("<title", icon.Body);
        Assert.Contains("<path d=\"M0 0\"", icon.Body);
    }

    [Theory]
    [InlineData("<div/>", "no <svg>")]
    [InlineData("<svg viewBox=\"0 0 24 24\"></svg>", "empty")]
    [InlineData("<svg viewBox=\"0 0 24 24\"/>", "empty")]
    [InlineData("<svg viewBox=\"0 0 24 24\"><style>path{fill:red}</style><path d=\"M0 0\"/></svg>", "<style>")]
    [InlineData("<svg viewBox=\"0 0 0 24\"><path d=\"M0 0\"/></svg>", "viewBox")]
    [InlineData("<svg viewBox=\"a b c d\"><path d=\"M0 0\"/></svg>", "viewBox")]
    public void Refuses_what_cannot_be_inlined_and_says_why(string svg, string reason)
    {
        var e = Assert.Throws<FormatException>(() => SvgImport.Parse(svg));
        Assert.Contains(reason, e.Message, StringComparison.OrdinalIgnoreCase);
    }
}
