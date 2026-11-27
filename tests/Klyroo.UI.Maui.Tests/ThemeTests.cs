namespace Klyroo.UI.Maui.Tests;

public class ThemeTests
{
    [Theory]
    [InlineData("primary")]
    [InlineData("secondary")]
    [InlineData("success")]
    [InlineData("info")]
    [InlineData("warning")]
    [InlineData("error")]
    [InlineData("neutral")]
    public void Every_built_in_color_resolves(string name)
    {
        var set = KlyTheme.Resolve(name);

        Assert.NotEqual(set.Fill.Light, Colors.Transparent);
        Assert.NotEqual(set.Contrast.Light, set.Fill.Light);
    }

    [Fact]
    public void Primary_is_the_lime_of_the_web_theme_in_both_modes()
    {
        var primary = KlyTheme.Resolve(KlyColor.Primary);

        Assert.Equal(Color.FromArgb("#b6ff00"), primary.Fill.Light);
        Assert.Equal(Color.FromArgb("#b6ff00"), primary.Fill.Dark);
        Assert.Equal(Color.FromArgb("#3b5900"), primary.Text.Light);
    }

    [Fact]
    public void An_unregistered_color_says_how_to_register_it()
    {
        var error = Assert.Throws<InvalidOperationException>(() => KlyTheme.Resolve("brand-unregistered"));

        Assert.Contains("KlyTheme.Register", error.Message);
        Assert.Contains("brand-unregistered", error.Message);
    }

    [Fact]
    public void A_registered_color_resolves_and_is_used_by_a_control()
    {
        var set = new KlyColorSet(KlyThemeColor.FromHex("#ff00ff", "#aa00aa"), KlyThemeColor.FromHex("#880088", "#ff88ff"), KlyThemeColor.FromHex("#ffffff", "#000000"));
        KlyTheme.Register("brand-registered", set);

        Assert.Equal(set, KlyTheme.Resolve("brand-registered"));
        var badge = new KlyBadge { Text = "x", Color = "brand-registered" };
        Assert.Equal("brand-registered", badge.Color.Value);
    }

    [Fact]
    public void A_color_with_no_name_is_refused()
    {
        Assert.Throws<ArgumentException>(() => new KlyColor(" "));
        Assert.Throws<ArgumentException>(() => KlyTheme.Register(default, default));
    }

    [Fact]
    public void WithAlpha_changes_both_modes_and_keeps_the_hue()
    {
        var tinted = KlyThemeColor.FromHex("#ff0000", "#00ff00").WithAlpha(0.1f);

        Assert.Equal(0.1f, tinted.Light.Alpha, 3);
        Assert.Equal(0.1f, tinted.Dark.Alpha, 3);
        Assert.Equal(1f, tinted.Light.Red);
        Assert.Equal(1f, tinted.Dark.Green);
    }

    [Fact]
    public void Surfaces_follow_the_web_variants()
    {
        var c = KlyTheme.Resolve(KlyColor.Success);

        var solid = Surface.For(ButtonVariant.Solid, c);
        Assert.Equal(c.Fill, solid.Background);
        Assert.Equal(c.Contrast, solid.Text);
        Assert.Equal(1, solid.StrokeThickness);

        var soft = Surface.For(ButtonVariant.Soft, c);
        Assert.Equal(0.1f, soft.Background.Light.Alpha, 3);
        Assert.Equal(c.Text, soft.Text);
        Assert.Equal(0, soft.StrokeThickness);

        var outline = Surface.For(ButtonVariant.Outline, c);
        Assert.Equal(KlyThemeColor.Transparent, outline.Background);
        Assert.Equal(0.75f, outline.Stroke.Light.Alpha, 3);

        var subtle = Surface.For(ButtonVariant.Subtle, c);
        Assert.Equal(soft.Background, subtle.Background);
        Assert.Equal(outline.Stroke, subtle.Stroke);

        Assert.Equal(Surface.For(ButtonVariant.Ghost, c), Surface.For(ButtonVariant.Link, c));
        Assert.Equal(0, Surface.For(ButtonVariant.Ghost, c).StrokeThickness);
    }

    [Fact]
    public void Badge_and_alert_variants_map_onto_the_same_surfaces()
    {
        var c = KlyTheme.Resolve(KlyColor.Info);

        Assert.Equal(Surface.For(ButtonVariant.Solid, c), Surface.For(KlyVariant.Solid, c));
        Assert.Equal(Surface.For(ButtonVariant.Outline, c), Surface.For(KlyVariant.Outline, c));
        Assert.Equal(Surface.For(ButtonVariant.Soft, c), Surface.For(KlyVariant.Soft, c));
        Assert.Equal(Surface.For(ButtonVariant.Subtle, c), Surface.For(KlyVariant.Subtle, c));
    }
}
