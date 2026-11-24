using Lux.UI.Build;

namespace Lux.UI.Tests.Icons;

/// <summary>Icon bodies are third-party data (Iconify, a shared cache, a self-hosted server, SVG files) rendered as raw markup: the sanitizer is the only thing between them and the page.</summary>
public class SvgSanitizerTests
{
    private static string Clean(string body) => SvgSanitizer.Sanitize(body);

    /// <summary>Either the payload is refused outright (not well-formed XML: it cannot be read, so it is never guessed at) or what comes out is harmless.</summary>
    private static string CleanOrRefused(string payload)
    {
        try
        {
            return Clean(payload);
        }
        catch (FormatException)
        {
            return "";
        }
    }

    private static void AssertHarmless(string output)
    {
        Assert.DoesNotMatch(@"(?i)\son\w+\s*=", output);
        Assert.DoesNotMatch(@"(?i)<\s*/?\s*(script|foreignObject|iframe|image|style|object|embed|a|svg|html|body)\b", output);
        Assert.DoesNotContain("javascript", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data:", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("http", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"(?i)(?:href\s*=\s*""(?!#))", output);
    }

    public static TheoryData<string> Payloads => new()
    {
        // The bypasses of the old regex filter.
        """<image/onerror="alert(1)" href="x"/>""",
        """<image href="x" onerror="alert(1)"/>""",
        """<path d="M0 0" onerror=alert(1)/>""",
        """<path d="M0 0" onclick=alert(1) />""",
        "<scr<script></script>ipt>alert(1)</scr<script></script>ipt>",
        "<scr<script>x</script>ipt>alert(1)</script>",
        """<a href="&#106;avascript:alert(1)"><path d="M0 0"/></a>""",
        """<a xlink:href="&#x6A;avascript&#58;alert(1)"><path d="M0 0"/></a>""",
        """<a href="java&#x09;script:alert(1)"><path d="M0 0"/></a>""",
        """<a href=" &#9;javascript:alert(1)"><path d="M0 0"/></a>""",
        """<animate attributeName="href" to="javascript:alert(1)"/>""",
        """<a><animate attributeName="href" values="javascript:alert(1)"/><text>x</text></a>""",
        """<animate attributeName="xlink:href" to="javascript:alert(1)"/>""",
        """<animate attributeName=" HREF " to="javascript:alert(1)"/>""",
        """<set attributeName="onmouseover" to="alert(1)"/>""",
        """<set attributeName="onclick" to="alert(1)"/>""",
        """<use href="https://evil.example/x.svg#a"/>""",
        """<use xlink:href="https://evil.example/x.svg#a"/>""",
        """<use href="//evil.example/x.svg#a"/>""",
        """<use href="data:image/svg+xml;base64,PHN2Zz48L3N2Zz4="/>""",
        // OWASP SVG vectors.
        "<svg onload=alert(1)></svg>",
        """<svg onload="alert(1)"></svg>""",
        "<svg/onload=alert(1)></svg>",
        """<g onload="alert(1)"><path d="M0 0"/></g>""",
        """<script>alert(1)</script>""",
        """<script xlink:href="data:text/javascript,alert(1)"/>""",
        """<script href="https://evil.example/x.js"/>""",
        """<svg:script xmlns:svg="http://www.w3.org/2000/svg">alert(1)</svg:script>""",
        """<x:script xmlns:x="http://evil.example/">alert(1)</x:script>""",
        """<foreignObject><iframe src="javascript:alert(1)"/></foreignObject>""",
        """<foreignObject><body onload="alert(1)"/></foreignObject>""",
        """<style>@import 'https://evil.example/a.css';</style>""",
        """<style>path{fill:url(https://evil.example/a)}</style><path d="M0 0"/>""",
        """<image href="https://evil.example/x.png"/>""",
        """<image xlink:href="javascript:alert(1)"/>""",
        """<mask id="m"><image href="https://evil.example/x.png"/></mask>""",
        """<path d="M0 0" fill="url(https://evil.example/a#b)"/>""",
        """<path d="M0 0" fill="url('javascript:alert(1)')"/>""",
        """<path d="M0 0" fill="url(data:image/svg+xml;base64,AAAA)"/>""",
        """<path d="M0 0" style="fill:url(https://evil.example/a)"/>""",
        """<path d="M0 0" style="background:url(javascript:alert(1))"/>""",
        """<path d="M0 0" style="fill:red;behavior:url(x.htc)"/>""",
        """<path d="M0 0" style="width:expression(alert(1))"/>""",
        """<path d="M0 0" fill="javascript:alert(1)"/>""",
        """<path d="M0 0" fill="data:text/html,alert(1)"/>""",
        """<set attributeName="fill" to="url(https://evil.example/a)"/>""",
        """<animate attributeName="fill" values="url(https://evil.example/a);red"/>""",
        """<linearGradient id="g" xlink:href="https://evil.example/a#b"/>""",
        """<text><tspan onclick="alert(1)">x</tspan></text>""",
        """<!DOCTYPE svg [<!ENTITY x "javascript:alert(1)">]><path d="&x;"/>""",
        """<path d="&x;"/>""",
        """</svg><script>alert(1)</script><svg>""",
        """<g></g></svg><svg onload="alert(1)">""",
        """<g><![CDATA[x]]></g><a href="javascript:alert(1)">x</a>""",
        """<path d="M0 0" onClick="alert(1)" ONCLICK="alert(1)" OnLoad="alert(1)"/>""",
        """<path d="M0 0" xlink:onclick="alert(1)" xml:onload="alert(1)"/>""",
        """<path d="M0 0" xmlns:x="http://evil.example" x:onload="alert(1)"/>""",
        """<animateMotion><mpath href="https://evil.example/x#p"/></animateMotion>""",
        """<animate attributeName="style" to="background:url(https://evil.example/a)"/>""",
        """<animate attributeName="d" begin="click" to="M0 0" onbegin="alert(1)"/>""",
        """<switch><foreignObject requiredExtensions="x"><div onmouseover="alert(1)"/></foreignObject><text>x</text></switch>""",
        """<feImage href="https://evil.example/x.png"/>""",
        """<filter id="f"><feImage xlink:href="javascript:alert(1)"/></filter>""",
    };

    [Theory]
    [MemberData(nameof(Payloads))]
    public void Hostile_markup_never_reaches_the_output(string payload)
    {
        AssertHarmless(CleanOrRefused(payload));
    }

    [Fact]
    public void Quoteless_handlers_and_reconstituted_tags_cannot_be_read_so_they_are_refused()
    {
        Assert.Throws<FormatException>(() => Clean("<path d=\"M0 0\" onerror=alert(1)/>"));
        Assert.Throws<FormatException>(() => Clean("<image/onerror=\"alert(1)\"/>"));
        Assert.Throws<FormatException>(() => Clean("<scr<script></script>ipt>alert(1)</script>"));
        Assert.Throws<FormatException>(() => Clean("</svg><script>alert(1)</script><svg>"));
    }

    [Fact]
    public void A_dtd_is_never_processed()
    {
        Assert.Throws<FormatException>(() => Clean("<!DOCTYPE svg [<!ENTITY a \"aaaaaaaaaa\"><!ENTITY b \"&a;&a;&a;&a;&a;&a;&a;&a;\">]><path d=\"&b;\"/>"));
    }

    [Fact]
    public void Entity_encoded_script_links_are_dropped_after_decoding_and_the_drawing_stays()
    {
        var output = Clean("""<a href="&#106;avascript:alert(1)"><path d="M0 0h1"/></a>""");

        Assert.Equal("<path d=\"M0 0h1\"/>", output);
    }

    [Fact]
    public void An_animation_that_could_change_a_link_or_a_handler_is_dropped()
    {
        Assert.Equal("", Clean("""<animate attributeName="href" to="javascript:alert(1)"/>"""));
        Assert.Equal("", Clean("""<animate attributeName="xlink:href" to="#a"/>"""));
        Assert.Equal("", Clean("""<set attributeName="onclick" to="alert(1)"/>"""));
        Assert.Equal("", Clean("""<animate attributeName="style" to="x"/>"""));
        Assert.Equal("<set attributeName=\"fill\" to=\"red\"/>", Clean("""<set attributeName="fill" to="red"/>"""));
    }

    [Fact]
    public void A_use_keeps_only_a_local_reference()
    {
        Assert.Equal("", Clean("""<use href="https://evil.example/a.svg#x"/>"""));
        Assert.Equal("", Clean("""<use xlink:href="a.svg#x"/>"""));
        Assert.Equal("<use href=\"#a\"/>", Clean("""<use href="#a"/>"""));
        Assert.Equal("<use xlink:href=\"#a\"/>", Clean("""<use xlink:href="#a"/>"""));
    }

    [Fact]
    public void Urls_must_point_to_an_id_of_the_same_image_and_are_written_in_one_form()
    {
        Assert.Equal("<path fill=\"url(#a)\"/>", Clean("""<path fill="url('#a')"/>"""));
        Assert.Equal("<path fill=\"url(#a)\"/>", Clean("""<path fill="url( &quot;#a&quot; )"/>"""));
        Assert.Equal("<path/>", Clean("""<path fill="url(https://evil.example/a)"/>"""));
        Assert.Equal("<path/>", Clean("""<path fill="url(#a) url(x.png)"/>"""));
    }

    [Fact]
    public void Handlers_classes_data_attributes_and_unknown_attributes_are_dropped_with_any_case()
    {
        var output = Clean("""<path d="M0 0" id="p" class="x" data-a="b" onclick="x" ONLOAD="x" tabindex="0" title="t"/>""");

        Assert.Equal("<path d=\"M0 0\" id=\"p\"/>", output);
    }

    [Fact]
    public void Style_keeps_only_presentation_properties_with_safe_values()
    {
        Assert.Equal("<path style=\"fill:red;stroke:#00f\"/>", Clean("""<path style="fill:red; position:fixed; stroke:#00f"/>"""));
        Assert.Equal("<path style=\"fill:url(#g)\"/>", Clean("""<path style="fill:url('#g')"/>"""));
        Assert.Equal("<path/>", Clean("""<path style="position:fixed;top:0"/>"""));
        Assert.Equal("<path/>", Clean("""<path style="fill:\72 ed"/>"""));
    }

    [Fact]
    public void Dropped_elements_take_their_content_with_them_except_links_which_keep_it()
    {
        Assert.Equal("<path d=\"M1 1\"/>", Clean("""<foreignObject><path d="M0 0"/></foreignObject><path d="M1 1"/>"""));
        Assert.Equal("<path d=\"M1 1\"/>", Clean("""<script><path d="M0 0"/></script><path d="M1 1"/>"""));
        Assert.Equal("<path d=\"M1 1\"/>", Clean("""<a href="https://example.com"><path d="M1 1"/></a>"""));
        Assert.Equal("<path d=\"M1 1\"/>", Clean("""<title>Logo</title><desc>x</desc><metadata>m</metadata><path d="M1 1"/>"""));
    }

    [Fact]
    public void Text_is_escaped()
    {
        Assert.Equal("<text x=\"1\">a &lt;b&gt; &amp; c</text>", Clean("""<text x="1">a &lt;b&gt; &amp; c</text>"""));
        Assert.Equal("<text>&lt;script&gt;</text>", Clean("<text><![CDATA[<script>]]></text>"));
    }

    [Fact]
    public void Extreme_nesting_is_refused_instead_of_overflowing_the_stack()
    {
        var deep = string.Concat(Enumerable.Repeat("<g>", 5000)) + string.Concat(Enumerable.Repeat("</g>", 5000));

        Assert.Throws<FormatException>(() => Clean(deep));
    }

    [Fact]
    public void Namespaces_of_the_file_only_make_the_body_readable()
    {
        var output = SvgSanitizer.Sanitize("""<path d="M0 0" inkscape:label="x"/><sodipodi:namedview id="v"/>""",
            [new KeyValuePair<string, string>("xmlns:inkscape", "http://www.inkscape.org/namespaces/inkscape"), new KeyValuePair<string, string>("xmlns:sodipodi", "http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd")]);

        Assert.Equal("<path d=\"M0 0\"/>", output);
    }

    // Real icons: what the sanitizer reads and writes again is the same markup.
    public static TheoryData<string> RealIcons => new()
    {
        // lucide:search
        """<g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="m21 21l-4.34-4.34"/><circle cx="11" cy="11" r="8"/></g>""",
        // svg-spinners:90-ring-with-bg (animateTransform)
        """<path d="M12 1a11 11 0 1 0 11 11A11 11 0 0 0 12 1Zm0 19a8 8 0 1 1 8-8A8 8 0 0 1 12 20Z" opacity=".25"/><path d="M10.14,1.16a11,11,0,0,0-9,8.92A1.59,1.59,0,0,0,2.46,12,1.52,1.52,0,0,0,4.11,10.7a8,8,0,0,1,6.66-6.61A1.42,1.42,0,0,0,12,2.69h0A1.57,1.57,0,0,0,10.14,1.16Z"><animateTransform attributeName="transform" dur="0.75s" repeatCount="indefinite" type="rotate" values="0 12 12;360 12 12"/></path>""",
        // svg-spinners:bars-scale (animations that begin when another ends)
        """<rect width="2.8" height="12" x="1" y="6"><animate id="svgSpinnersBarsScale0" attributeName="y" begin="0;svgSpinnersBarsScale1.end-0.1s" calcMode="spline" dur="0.6s" keySplines=".36,.61,.3,.98;.36,.61,.3,.98" values="6;1;6"/></rect><rect width="2.8" height="12" x="5.8" y="6"><animate id="svgSpinnersBarsScale1" attributeName="y" begin="svgSpinnersBarsScale0.begin+0.1s" calcMode="spline" dur="0.6s" keySplines=".36,.61,.3,.98;.36,.61,.3,.98" values="6;1;6"/></rect>""",
        // a logo with gradients, a clip path and a mask
        """<defs><linearGradient id="a" x1="0" x2="1" y1="0" y2="1" gradientUnits="objectBoundingBox"><stop offset="0" stop-color="#f00"/><stop offset="1" stop-color="#00f" stop-opacity=".5"/></linearGradient><radialGradient id="b" cx=".5" cy=".5" r=".5" href="#a"/><clipPath id="c"><rect width="24" height="24" rx="4"/></clipPath><mask id="d"><rect width="24" height="24" fill="#fff"/><circle cx="12" cy="12" r="4"/></mask></defs><g clip-path="url(#c)" mask="url(#d)"><path d="M0 0h24v24H0z" fill="url(#a)"/><use href="#e"/></g>""",
        // a Figma export: ids like clip0_1_2, a filter
        """<g clip-path="url(#clip0_1_2)"><path d="M3 3h18v18H3z" fill="#4285F4" fill-rule="evenodd" clip-rule="evenodd"/></g><defs><clipPath id="clip0_1_2"><rect width="24" height="24" fill="white"/></clipPath><filter id="f" x="0" y="0" width="24" height="24" color-interpolation-filters="sRGB"><feFlood flood-opacity="0" result="a"/><feGaussianBlur stdDeviation="2"/></filter></defs>""",
        // text
        """<text x="2" y="16" font-family="Arial" font-size="12" text-anchor="start">Hi<tspan dx="1">!</tspan></text>""",
    };

    [Theory]
    [MemberData(nameof(RealIcons))]
    public void Real_icons_come_out_as_they_went_in(string body)
    {
        Assert.Equal(body, Clean(body));
    }

    [Fact]
    public void Single_quoted_attributes_and_white_space_between_elements_are_normalized()
    {
        Assert.Equal("<g fill=\"none\"><path d=\"M1 1\"/><circle r=\"2\"/></g>", Clean("<g fill='none'>\n  <path d='M1 1'/>\n  <circle r='2'></circle>\n</g>"));
    }

    [Fact]
    public void The_output_of_the_sanitizer_is_a_fixed_point()
    {
        foreach (var payload in new[] { "<a href=\"#x\"><path fill=\"url('#a')\" style=\"fill:red;position:fixed\" d=\"M0 0\"/></a>", "<text>a &amp; b</text>" })
        {
            var once = Clean(payload);
            Assert.Equal(once, Clean(once));
        }
    }
}

public class SvgImportSanitizingTests
{
    [Fact]
    public void An_svg_file_is_sanitized_whatever_its_shape()
    {
        var icon = SvgImport.Parse("""
            <svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 24 24" onload="alert(1)" fill="none">
              <image href="https://evil.example/x.png"/>
              <a xlink:href="javascript:alert(1)"><path d="M0 0" onclick="x()"/></a>
              <use href="https://evil.example/a.svg#b"/>
            </svg>
            """);

        Assert.Equal("<g fill=\"none\"><path d=\"M0 0\"/></g>", icon.Body);
    }

    [Fact]
    public void An_svg_file_with_a_hostile_shape_that_cannot_be_read_is_refused()
    {
        Assert.Throws<FormatException>(() => SvgImport.Parse("<svg viewBox=\"0 0 24 24\"><path d=\"M0 0\" onerror=alert(1)/></svg>"));
        Assert.Throws<FormatException>(() => SvgImport.Parse("<svg viewBox=\"0 0 24 24\"><scr<script></script>ipt>alert(1)</script></svg>"));
    }

    [Fact]
    public void A_file_with_nothing_drawable_left_is_an_empty_icon()
    {
        var e = Assert.Throws<FormatException>(() => SvgImport.Parse("<svg viewBox=\"0 0 24 24\"><script>alert(1)</script></svg>"));
        Assert.Contains("empty", e.Message);
    }

    [Fact]
    public void An_inkscape_file_with_its_own_namespaces_is_read()
    {
        var icon = SvgImport.Parse("""
            <svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" viewBox="0 0 24 24">
              <sodipodi:namedview id="base" inkscape:zoom="1"/>
              <path d="M0 0h4" inkscape:label="x"/>
            </svg>
            """);

        Assert.Equal("<path d=\"M0 0h4\"/>", icon.Body);
    }
}
