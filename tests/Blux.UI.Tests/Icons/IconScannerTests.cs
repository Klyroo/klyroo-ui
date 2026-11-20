using Blux.UI.Build;

namespace Blux.UI.Tests.Icons;

public class IconScannerTests
{
    private static List<IconMention> Scan(string text, Func<string, bool>? extra = null) => IconScanner.Scan("Test.razor", text, extra);

    private static string[] Keys(IEnumerable<IconMention> mentions) => [.. mentions.Select(m => m.Icon.Key).Order(StringComparer.Ordinal)];

    [Fact]
    public void Finds_the_name_of_a_BxIcon_in_every_form()
    {
        var found = Scan("""
            <BxIcon Name="ph:house" />
            <BxIcon Name="i-mdi-home" Size="BxSize.Lg" />
            <BxIcon Name="book-open" />
            """);

        Assert.Equal(["lucide:book-open", "mdi:home", "ph:house"], Keys(found));
        Assert.All(found, m => Assert.Equal(IconDetection.Context, m.Detection));
        Assert.Equal(IconNameForm.Bare, found.Single(m => m.Icon.Name == "book-open").Form);
    }

    [Fact]
    public void Finds_the_name_of_a_BxIcon_split_over_lines_and_after_other_attributes()
    {
        var found = Scan("<BxIcon\n    Class=\"size-6\"\n    Label=\"Home\"\n    Name=\"mdi:menu\" />");
        Assert.Equal(["mdi:menu"], Keys(found));
        Assert.Equal(4, found[0].Line);
    }

    [Fact]
    public void Finds_icon_typed_attributes_and_properties()
    {
        var found = Scan("""
            <BxButton LeadingIcon="ph:arrow-right" TrailingIcon="i-ph-arrow-up" />
            <BxPageFeature Icon="simple-icons:github" Title="x" />
            @code {
                private static readonly Item[] Items = [new("Home") { Icon = "mdi:home", ProviderIcon = "i-simple-icons-nuget" }];
            }
            """);

        Assert.Equal(["mdi:home", "ph:arrow-right", "ph:arrow-up", "simple-icons:github", "simple-icons:nuget"], Keys(found));
        Assert.All(found, m => Assert.Equal(IconDetection.Context, m.Detection));
    }

    [Fact]
    public void Finds_BxIcons_Named_and_app_icon_assignments_in_code()
    {
        var found = Scan("""
            var star = BxIcons.Named("ph:star");
            var heart = BxIcons.Named(name: "mdi:heart");
            builder.Services.AddBluxUi(o => { o.Icons.Close = "ph:x"; o.Icons.Loading = "i-mdi-loading"; });
            """, null);

        Assert.Equal(["mdi:heart", "mdi:loading", "ph:star", "ph:x"], Keys(found));
        Assert.All(found, m => Assert.Equal(IconDetection.Context, m.Detection));
    }

    [Fact]
    public void A_standalone_prefixed_string_is_found_loosely()
    {
        var found = Scan("""
            var a = "simple-icons:github";
            var b = "i-simple-icons-nuget";
            var c = cond ? "ph:sun" : "ph:moon";
            """);

        Assert.Equal(["ph:moon", "ph:sun", "simple-icons:github", "simple-icons:nuget"], Keys(found));
        Assert.All(found, m => Assert.Equal(IconDetection.Loose, m.Detection));
    }

    [Fact]
    public void A_strict_match_is_not_reported_twice_as_a_loose_one()
    {
        var found = Scan("""<BxIcon Name="ph:house" />""");
        var only = Assert.Single(found);
        Assert.Equal(IconDetection.Context, only.Detection);
    }

    [Fact]
    public void Custom_prefixes_are_known_to_the_scan_when_the_project_declares_the_collection()
    {
        var found = Scan("""
            <BxIcon Name="custom:logo" /> <BxIcon Name="i-custom-mark" /> var s = "custom:other";
            """, p => p == "custom");

        Assert.Equal(["custom:logo", "custom:mark", "custom:other"], Keys(found));
    }

    [Theory]
    [InlineData("var t = \"hh:mm\";")]
    [InlineData("var t = \"12:30\";")]
    [InlineData("var u = \"https://example.com/a:b\";")]
    [InlineData("var n = \"i-was-here\";")]
    [InlineData("var s = \"Name: value\";")]
    [InlineData("var s = \"unknown:thing\";")]
    [InlineData("<div class=\"flex items-center\">ph x</div>")]
    [InlineData("<BxButton Icon=\"@icon\" LeadingIcon=\"@(open ? Close : Open)\" />")]
    [InlineData("var show = options.ShowIcon == \"ph:x\" ? 1 : 0; var Icon = x;")]
    [InlineData("<BxButton ShowIcon=\"true\" HideIcon=\"false\" IconSize=\"20\" />")]
    [InlineData("<BxButton Icon=\"Home\" />")]
    [InlineData("<span data-icon=\"x\" aria-label=\"search\"></span>")]
    [InlineData("<BxIcon Name=\"@name\" />")]
    [InlineData("var path = $\"i-lucide-{name}\";")]
    [InlineData("var x = Icons.Close; var y = Icons.Close == \"ph:x\";")]
    public void Does_not_take_ordinary_strings_for_icons(string text)
    {
        // "ph:x" in the comparison above is a standalone string, the one case allowed through: it is a real icon name.
        var found = Scan(text).Where(m => m.Icon.Key != "ph:x").ToList();
        Assert.Empty(found);
    }

    [Fact]
    public void Bare_names_in_flag_like_properties_are_ignored()
    {
        Assert.Empty(Scan("<BxButton LeadingIcon=\"true\" TrailingIcon=\"none\" Icon=\"123\" />"));
    }

    [Fact]
    public void A_bare_name_outside_an_icon_typed_place_is_not_an_icon()
    {
        Assert.Empty(Scan("""<BxButton Label="search" Href="settings" Class="inbox" /> var x = "calendar";"""));
    }

    [Fact]
    public void Reports_the_line_of_each_mention()
    {
        var found = Scan("a\nb\n<BxIcon Name=\"ph:one\" />\n\n<BxIcon Name=\"ph:two\" />");
        Assert.Equal([3, 5], found.OrderBy(m => m.Line).Select(m => m.Line));
        Assert.All(found, m => Assert.Equal("Test.razor", m.File));
    }

    [Fact]
    public void Icons_registered_in_code_are_listed_so_they_are_never_downloaded()
    {
        var registered = IconScanner.RegisteredNames("""
            o.Icons.Register("custom:logo", "<path d='M0 0'/>");
            o.Icons.RegisterSvg("brand", svg);
            """).ToList();

        Assert.Equal(["custom:logo", "lucide:brand"], registered.Order(StringComparer.Ordinal));
    }
}
