using Blux.UI.Build;

namespace Blux.UI.Tests.Icons;

public class IconifySetTests
{
    [Fact]
    public void Reads_an_api_answer_with_the_default_size_of_the_set()
    {
        var set = IconifySet.Parse("""
            {"prefix":"ph","width":256,"height":256,"icons":{"x":{"body":"<path d=\"M1 1\"/>"}},"aliases":{},"not_found":["nope"]}
            """);

        Assert.Equal("ph", set.Prefix);
        Assert.True(set.TryResolve("x", out var icon));
        Assert.Equal("<path d=\"M1 1\"/>", icon.Body);
        Assert.Equal("0 0 256 256", icon.ViewBox);
        Assert.Equal(["nope"], set.NotFound);
        Assert.False(set.TryResolve("nope", out _));
    }

    [Fact]
    public void A_set_without_a_size_is_16_by_16_like_iconify()
    {
        var set = IconifySet.Parse("""{"prefix":"p","icons":{"a":{"body":"<g/>"}}}""");
        Assert.True(set.TryResolve("a", out var icon));
        Assert.Equal("0 0 16 16", icon.ViewBox);
    }

    [Fact]
    public void An_icon_can_override_the_size_and_offset_of_the_set()
    {
        var set = IconifySet.Parse("""{"prefix":"p","width":24,"height":24,"icons":{"a":{"body":"<g/>","width":32,"height":20,"left":-2,"top":1.5}}}""");
        Assert.True(set.TryResolve("a", out var icon));
        Assert.Equal("-2 1.5 32 20", icon.ViewBox);
    }

    [Fact]
    public void Aliases_resolve_to_their_parent_and_chains_are_followed()
    {
        var set = IconifySet.Parse("""
            {"prefix":"lucide","width":24,"height":24,"icons":{"circle-exclamation-point":{"body":"<g/>"}},
             "aliases":{"alert-circle":{"parent":"circle-exclamation-point"},"warn":{"parent":"alert-circle"}}}
            """);
        Assert.True(set.TryResolve("alert-circle", out var a));
        Assert.True(set.TryResolve("warn", out var b));
        Assert.Equal("<g/>", a.Body);
        Assert.Equal(a.Body, b.Body);
        Assert.Equal("0 0 24 24", b.ViewBox);
    }

    [Fact]
    public void An_alias_that_points_nowhere_is_not_resolved()
    {
        var set = IconifySet.Parse("""{"prefix":"p","icons":{},"aliases":{"a":{"parent":"missing"},"loop":{"parent":"loop"}}}""");
        Assert.False(set.TryResolve("a", out _));
        Assert.False(set.TryResolve("loop", out _));
    }

    [Fact]
    public void A_horizontal_flip_of_an_alias_is_drawn_with_a_transform()
    {
        var set = IconifySet.Parse("""
            {"prefix":"p","width":24,"height":24,"icons":{"arrow-right":{"body":"<path d=\"M1 1\"/>"}},
             "aliases":{"arrow-left":{"parent":"arrow-right","hFlip":true}}}
            """);
        Assert.True(set.TryResolve("arrow-left", out var icon));
        Assert.Equal("<g transform=\"translate(24 0) scale(-1 1)\"><path d=\"M1 1\"/></g>", icon.Body);
        Assert.Equal("0 0 24 24", icon.ViewBox);
    }

    [Fact]
    public void Flips_cancel_out_and_rotations_add_up()
    {
        var set = IconifySet.Parse("""
            {"prefix":"p","width":24,"height":24,"icons":{"a":{"body":"<g/>","hFlip":true}},
             "aliases":{"b":{"parent":"a","hFlip":true},"c":{"parent":"a","rotate":1},"d":{"parent":"c","rotate":1}}}
            """);
        Assert.True(set.TryResolve("b", out var flippedBack));
        Assert.Equal("<g/>", flippedBack.Body);
        Assert.True(set.TryResolve("c", out var quarter));
        Assert.StartsWith("<g transform=\"rotate(90 12 12) translate(24 0) scale(-1 1)\">", quarter.Body);
        Assert.True(set.TryResolve("d", out var half));
        Assert.StartsWith("<g transform=\"rotate(180 12 12) translate(24 0) scale(-1 1)\">", half.Body);
    }

    [Fact]
    public void A_quarter_turn_swaps_width_and_height()
    {
        var set = IconifySet.Parse("""{"prefix":"p","icons":{"a":{"body":"<g/>","width":30,"height":10,"rotate":1}}}""");
        Assert.True(set.TryResolve("a", out var icon));
        Assert.Equal("0 0 10 30", icon.ViewBox);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html>Not Found</html>")]
    [InlineData("[1,2]")]
    [InlineData("{\"prefix\":")]
    public void An_answer_that_is_not_an_icon_set_is_a_diagnostic_not_a_crash(string text)
    {
        Assert.Throws<BuildToolException>(() => IconifySet.Parse(text));
    }
}
