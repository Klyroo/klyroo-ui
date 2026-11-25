using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Klyroo.UI.Tests;

/// <summary>What the browser remembers is scoped to the path base of the app (see js/base-path.js for the other half).</summary>
public class PathScopeTests : KlyrooTestContext
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("/", "")]
    [InlineData("/sub", "sub")]
    [InlineData("/sub/", "sub")]
    [InlineData("/templates/blog/demo", "templates.blog.demo")]
    [InlineData("/a-b", "a-b")]
    [InlineData("/a/b", "a.b")]
    [InlineData("/a b", "a~20b")]
    [InlineData("/café", "caf~c3~a9")]
    public void The_slug_of_a_path_base_is_safe_in_a_cookie_name(string? pathBase, string expected) => Assert.Equal(expected, KlyPathScope.Slug(pathBase));

    [Fact]
    public void Sidebar_cookie_names_keep_the_root_name_and_differ_below_a_path_base()
    {
        Assert.Equal("klyroo-sidebar-app", KlyPathScope.SidebarCookieName("app"));
        Assert.Equal("klyroo-sidebar-app", KlyPathScope.SidebarCookieName("app", "/"));
        Assert.Equal("klyroo-sidebar.sub~~app", KlyPathScope.SidebarCookieName("app", "/sub"));
        Assert.NotEqual(KlyPathScope.SidebarCookieName("app", "/a"), KlyPathScope.SidebarCookieName("app", "/b"));
        // An app at the root can never produce the name of an app below a base: the names start differently.
        Assert.NotEqual(KlyPathScope.SidebarCookieName("sub~~app"), KlyPathScope.SidebarCookieName("app", "/sub"));
        Assert.DoesNotContain(KlyPathScope.SidebarCookieName("x", "/we;ird/pa th"), c => c is ';' or '/' or ' ');
    }

    [Theory]
    [InlineData("http://localhost/", "")]
    [InlineData("http://localhost/sub/", "/sub")]
    [InlineData("https://h.test/templates/blog/demo/", "/templates/blog/demo")]
    [InlineData("https://h.test/caf%C3%A9/", "/café")]
    public void The_path_base_comes_from_the_base_uri(string baseUri, string expected) => Assert.Equal(expected, KlyPathScope.PathBaseOf(baseUri));

    [Fact]
    public void The_first_paint_script_of_the_sidebar_keeps_the_root_cookie_name_at_the_root() =>
        Assert.Contains("var k='klyroo-sidebar-'+", Render<KlySidebar>(p => p.Add(s => s.Id, "app")).Markup);

    [Fact]
    public void The_first_paint_script_of_the_sidebar_reads_the_cookie_of_its_path_base()
    {
        // The fake navigation manager of bUnit is created with the context: only its base address can still be changed.
        typeof(NavigationManager).GetProperty(nameof(NavigationManager.BaseUri))!.SetValue(Services.GetRequiredService<NavigationManager>(), "http://localhost/sub/");
        var below = Render<KlySidebar>(p => p.Add(s => s.Id, "app")).Markup;

        Assert.Contains("var k='klyroo-sidebar.sub~~'+", below);
        Assert.DoesNotContain("var k='klyroo-sidebar-'+", below);
    }

    [Fact]
    public void Header_and_error_link_home_relative_to_the_base_href()
    {
        Assert.Equal("./", Render<KlyHeader>(p => p.Add(h => h.Title, "App")).Find("a").GetAttribute("href"));
        Assert.Contains("href=\"./\"", Render<KlyError>().Markup);
    }

    [Fact]
    public void A_named_cookie_keeps_its_name_at_the_root_and_carries_the_slug_below_a_path_base()
    {
        Assert.Equal("klyroo-timezone", KlyPathScope.CookieName("klyroo-timezone"));
        Assert.Equal("klyroo-timezone", KlyPathScope.CookieName("klyroo-timezone", "/"));
        Assert.Equal("klyroo-timezone.sub~~", KlyPathScope.CookieName("klyroo-timezone", "/sub"));
        Assert.Equal("klyroo-timezone.a.b~20c~~", KlyPathScope.CookieName("klyroo-timezone", "/a/b c"));
    }
}
