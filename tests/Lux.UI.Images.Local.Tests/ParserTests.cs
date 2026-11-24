using System.Net;
using Lux.UI.Images.Local;

namespace Lux.UI.Images.Local.Tests;

public class ParserTests
{
    private static BxImageParseResult Parse(Dictionary<string, string> query, Action<BxLocalImagesOptions>? configure = null)
    {
        var options = new BxLocalImagesOptions();
        configure?.Invoke(options);
        return BxImageRequestParser.Parse(key => query.GetValueOrDefault(key), options);
    }

    [Fact]
    public void A_plain_request_is_normalized()
    {
        var result = Parse(new() { ["src"] = "img/a.png", ["w"] = "300", ["h"] = "200", ["q"] = "77", ["f"] = "JPG", ["fit"] = "contain", ["v"] = "1" });

        var request = result.Request!;
        Assert.Equal("/img/a.png", request.Source);
        Assert.Equal(384, request.Width);
        Assert.Equal(256, request.Height);
        Assert.Equal(80, request.Quality);
        Assert.Equal("jpeg", request.Format);
        Assert.Equal(BxImageFit.Contain, request.Fit);
        Assert.True(request.Versioned);
    }

    [Theory]
    [InlineData("1", 16)]
    [InlineData("17", 32)]
    [InlineData("3840", 3840)]
    [InlineData("3841", 3840)]
    [InlineData("99999", 3840)]
    public void Widths_are_clamped_then_rounded_up_to_the_configured_steps(string width, int expected)
    {
        Assert.Equal(expected, Parse(new() { ["src"] = "/a.png", ["w"] = width }).Request!.Width);
    }

    [Fact]
    public void Heights_and_qualities_are_clamped()
    {
        var request = Parse(new() { ["src"] = "/a.png", ["h"] = "999999", ["q"] = "5000" }).Request!;

        Assert.Equal(4096, request.Height);
        Assert.Equal(100, request.Quality);
        Assert.Equal(5, Parse(new() { ["src"] = "/a.png", ["q"] = "0" }).Request!.Quality);
    }

    [Theory]
    [InlineData("/../secret.png")]
    [InlineData("/a/../../secret.png")]
    [InlineData("/a/./b.png")]
    [InlineData("..\\secret.png")]
    [InlineData("/a\\b.png")]
    [InlineData("//evil.example/a.png")]
    [InlineData("C:/windows/a.png")]
    [InlineData("/a.png?x=1")]
    [InlineData("/a\0.png")]
    [InlineData("/a\n.png")]
    public void Traversal_and_odd_paths_are_rejected_with_400(string src)
    {
        var result = Parse(new() { ["src"] = src });

        Assert.Null(result.Request);
        Assert.Equal(400, result.Status);
    }

    [Fact]
    public void Only_image_extensions_are_served_locally()
    {
        Assert.Equal(415, Parse(new() { ["src"] = "/appsettings.json" }).Status);
        Assert.Equal(415, Parse(new() { ["src"] = "/site.css" }).Status);
        Assert.NotNull(Parse(new() { ["src"] = "/Photo.JPG" }).Request);
    }

    [Theory]
    [InlineData("w", "0")]
    [InlineData("w", "-5")]
    [InlineData("w", "abc")]
    [InlineData("h", "1.5")]
    [InlineData("q", "high")]
    [InlineData("f", "bmp")]
    [InlineData("fit", "sideways")]
    [InlineData("fit", "99")]
    [InlineData("blur", "x")]
    public void Bad_values_are_400(string key, string value)
    {
        var result = Parse(new() { ["src"] = "/a.png", [key] = value });

        Assert.Equal(400, result.Status);
    }

    [Fact]
    public void Missing_or_oversized_src_is_400()
    {
        Assert.Equal(400, Parse([]).Status);
        Assert.Equal(400, Parse(new() { ["src"] = "/" + new string('a', 3000) + ".png" }).Status);
    }

    [Fact]
    public void Remote_sources_need_an_allowed_host_and_no_credentials()
    {
        Assert.Equal(403, Parse(new() { ["src"] = "https://images.example.com/a.png" }).Status);
        Assert.Equal(403, Parse(new() { ["src"] = "http://localhost/a.png" }).Status);
        Assert.Equal(403, Parse(new() { ["src"] = "https://images.example.com.evil.org/a.png" }, o => o.AllowedRemoteHosts.Add("images.example.com")).Status);
        Assert.Equal(400, Parse(new() { ["src"] = "https://user:pw@images.example.com/a.png" }, o => o.AllowedRemoteHosts.Add("images.example.com")).Status);

        var allowed = Parse(new() { ["src"] = "https://IMAGES.example.com/a.png?x=1" }, o => o.AllowedRemoteHosts.Add("images.example.com")).Request!;
        Assert.True(allowed.IsRemote);
        Assert.Equal("https://images.example.com/a.png?x=1", allowed.Source);
    }

    [Fact]
    public void Equal_requests_have_equal_keys_whatever_the_spelling()
    {
        var a = Parse(new() { ["src"] = "/a.png", ["w"] = "300", ["q"] = "78", ["f"] = "jpg" }).Request!;
        var b = Parse(new() { ["src"] = "a.png", ["w"] = "384", ["q"] = "80", ["f"] = "jpeg" }).Request!;

        Assert.Equal(a.CanonicalKey("s"), b.CanonicalKey("s"));
        Assert.NotEqual(a.CanonicalKey("s"), a.CanonicalKey("t"));
    }

    [Theory]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("172.32.0.1", true)]
    [InlineData("192.168.1.1", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("0.0.0.0", false)]
    [InlineData("::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("fd00::1", false)]
    [InlineData("::ffff:10.0.0.1", false)]
    [InlineData("64:ff9b::7f00:1", false)]
    [InlineData("64:ff9b::a00:1", false)]
    [InlineData("64:ff9b::808:808", true)]
    [InlineData("64:ff9b:1::1", false)]
    [InlineData("2002:7f00:1::", false)]
    [InlineData("2002:a9fe:a9fe::1", false)]
    [InlineData("2002:808:808::1", true)]
    [InlineData("::127.0.0.1", false)]
    [InlineData("::10.0.0.1", false)]
    [InlineData("::192.168.0.1", false)]
    [InlineData("::8.8.8.8", true)]
    [InlineData("::", false)]
    [InlineData("::ffff:169.254.169.254", false)]
    [InlineData("::ffff:8.8.8.8", true)]
    [InlineData("100.127.255.255", false)]
    [InlineData("100.128.0.1", true)]
    [InlineData("198.18.0.1", false)]
    [InlineData("198.19.255.255", false)]
    [InlineData("192.0.2.1", false)]
    [InlineData("198.51.100.1", false)]
    [InlineData("203.0.113.1", false)]
    [InlineData("192.0.0.8", false)]
    [InlineData("240.0.0.1", false)]
    [InlineData("255.255.255.255", false)]
    [InlineData("224.0.0.1", false)]
    [InlineData("2001:db8::1", false)]
    [InlineData("2001::1", false)]
    [InlineData("ff02::1", false)]
    [InlineData("fec0::1", false)]
    [InlineData("5efe::1", false)]
    [InlineData("93.184.216.34", true)]
    [InlineData("2606:2800:220:1:248:1893:25c8:1946", true)]
    public void Only_public_addresses_are_fetched(string address, bool expected)
    {
        Assert.Equal(expected, BxImageProcessor.IsPublic(IPAddress.Parse(address)));
    }
}
