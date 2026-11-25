using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Klyroo.UI.Tests;

/// <summary>
/// Blazor Hybrid (BlazorWebView): the renderer reports <c>RendererInfo.Name == "WebView"</c>, is always interactive and has no server.
/// These tests render the components under that renderer info and check what degrades.
/// </summary>
public class HybridTests : KlyrooTestContext
{
    private const string Manifest = """{"css":"_klyroo/fonts/klyroo.fonts.css","fonts":[{"preload":["_klyroo/fonts/geist.woff2"]}]}""";

    private void UseWebView() => SetRendererInfo(new RendererInfo("WebView", true));

    [Fact]
    public void Host_detection_reads_the_renderer_name()
    {
        Assert.True(KlyrooHost.IsHybrid(new RendererInfo("WebView", true)));
        Assert.False(KlyrooHost.IsHybrid(new RendererInfo("Server", true)));
        Assert.False(KlyrooHost.IsHybrid(new RendererInfo("Static", false)));
        Assert.False(KlyrooHost.IsHybrid(new RendererInfo("WebAssembly", true)));
    }

    [Fact]
    public void Sidebar_applies_the_remembered_state_with_a_script_on_the_web_but_not_in_a_web_view()
    {
        var web = Render<KlySidebar>(p => p.Add(s => s.Id, "app").Add(s => s.ChildContent, "<p>Nav</p>"));
        Assert.Contains("klyroo-sidebar-", web.Markup.Substring(web.Markup.IndexOf("<script", StringComparison.Ordinal)));

        UseWebView();
        var hybrid = Render<KlySidebar>(p => p.Add(s => s.Id, "app").Add(s => s.ChildContent, "<p>Nav</p>"));
        Assert.DoesNotContain("<script", hybrid.Markup);
        Assert.Equal("app", hybrid.Find("kly-sidebar").GetAttribute("data-kly-sidebar-id"));
    }

    [Fact]
    public void ColorModeScript_renders_in_a_browser_and_nothing_in_a_web_view()
    {
        Assert.Contains("<script>", Render<KlyColorModeScript>().Markup);

        UseWebView();
        Assert.Equal("", Render<KlyColorModeScript>().Markup);
    }

    [Fact]
    public void CultureMeta_renders_nothing_in_a_web_view()
    {
        Assert.Contains("klyroo-culture", Render<KlyCultureMeta>().Markup);

        UseWebView();
        Assert.Equal("", Render<KlyCultureMeta>().Markup);
    }

    [Fact]
    public void Fonts_link_the_plain_route_in_a_web_view_where_there_is_no_asset_map()
    {
        UseWebView();
        var cut = Render<KlyFonts>(p => p.Add(c => c.ManifestJson, Manifest));

        Assert.Equal("_klyroo/fonts/klyroo.fonts.css", cut.Find("link[rel=stylesheet]").GetAttribute("href"));
        Assert.Equal("_klyroo/fonts/geist.woff2", cut.Find("link[rel=preload]").GetAttribute("href"));
    }

    private static void HeadPage(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b)
    {
        b.OpenComponent<HeadOutlet>(0);
        b.CloseComponent();
        b.OpenComponent<KlyHead>(1);
        b.AddAttribute(2, nameof(KlyHead.Fonts), false);
        b.CloseComponent();
    }

    [Fact]
    public void Head_renders_nothing_in_a_browser()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.GetRequiredService<KlyrooOptions>().Colors.Primary = KlyPalette.Blue;

        Assert.DoesNotContain("data-kly-theme", Render(HeadPage).Markup);
    }

    [Fact]
    public void Head_sends_the_theme_variables_to_the_head_in_a_web_view()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.GetRequiredService<KlyrooOptions>().Colors.Primary = KlyPalette.Blue;
        UseWebView();

        Assert.Contains("data-kly-theme", Render(HeadPage).Markup);
    }

    private static void ImagePage(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b)
    {
        b.OpenComponent<HeadOutlet>(0);
        b.CloseComponent();
        b.OpenComponent<KlyImage>(1);
        b.AddAttribute(2, nameof(KlyImage.Src), "/img/hero.png");
        b.AddAttribute(3, nameof(KlyImage.Alt), "Hero");
        b.AddAttribute(4, nameof(KlyImage.Preload), true);
        b.CloseComponent();
    }

    [Fact]
    public void Image_preload_adds_a_head_link_in_a_browser()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Assert.NotEmpty(Render(ImagePage).FindAll("link[rel=preload]"));
    }

    [Fact]
    public void Image_preload_adds_no_head_link_in_a_web_view_but_still_loads_eagerly()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        UseWebView();

        var cut = Render(ImagePage);

        Assert.Empty(cut.FindAll("link[rel=preload]"));
        Assert.Equal("eager", cut.Find("img").GetAttribute("loading"));
        Assert.Equal("high", cut.Find("img").GetAttribute("fetchpriority"));
    }

    [Fact]
    public void LocaleSelect_on_the_web_describes_the_cookie_and_reloads()
    {
        var cut = Render<KlyLocaleSelect>(p => p.Add(l => l.Locales, [KlyLocales.En, KlyLocales.Fr]));

        Assert.True(cut.Find("[data-kly-locale-select]").HasAttribute("data-kly-reload"));
        Assert.False(cut.Find("kly-select").HasAttribute("data-kly-seq"));
    }

    [Fact]
    public async Task LocaleSelect_in_a_web_view_saves_the_choice_in_localStorage_and_does_not_reload()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        UseWebView();
        string? raised = null;
        var cut = Render<KlyLocaleSelect>(p => p
            .Add(l => l.Locales, [KlyLocales.En, KlyLocales.Fr])
            .Add(l => l.Value, "en")
            .Add(l => l.OnLocaleChange, EventCallback.Factory.Create<string>(this, code => raised = code)));

        Assert.False(cut.Find("[data-kly-locale-select]").HasAttribute("data-kly-reload"));
        await cut.InvokeAsync(() => cut.Find("kly-select").TriggerEventAsync("onklychange", new KlyChangeEventArgs { Value = "fr", Sequence = 1 }));

        var call = JSInterop.VerifyInvoke("Klyroo.setStoredCulture");
        Assert.Equal(["klyroo-culture", "fr"], call.Arguments.Select(a => a?.ToString()));
        Assert.Equal("fr", raised);
        Assert.Equal("fr", cut.Find("kly-select").GetAttribute("value"));
    }

    [Fact]
    public async Task LocaleSelect_in_a_web_view_keeps_the_choice_when_the_parent_renders_again()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        UseWebView();
        var cut = Render<KlyLocaleSelect>(p => p.Add(l => l.Locales, [KlyLocales.En, KlyLocales.Fr, KlyLocales.De]).Add(l => l.Value, "en"));
        await cut.InvokeAsync(() => cut.Find("kly-select").TriggerEventAsync("onklychange", new KlyChangeEventArgs { Value = "fr", Sequence = 1 }));

        // An OnLocaleChange handler that renders the parent again hands the same parameters back: the culture in effect is
        // still the old one (the choice applies at the next start), but the select must not snap back to it.
        cut.Render(p => p.Add(l => l.Locales, [KlyLocales.En, KlyLocales.Fr, KlyLocales.De]).Add(l => l.Value, "en"));
        Assert.Equal("fr", cut.Find("kly-select").GetAttribute("value"));

        // A new Value from the parent is a new decision of the app: it wins.
        cut.Render(p => p.Add(l => l.Locales, [KlyLocales.En, KlyLocales.Fr, KlyLocales.De]).Add(l => l.Value, "de"));
        Assert.Equal("de", cut.Find("kly-select").GetAttribute("value"));
    }

    [Fact]
    public void App_spatial_navigation_renders_the_element_that_turns_tv_mode_on_only_when_asked()
    {
        Assert.Empty(Render<KlyApp>().FindAll("kly-spatial"));
        Assert.Single(Render<KlyApp>(p => p.Add(a => a.SpatialNavigation, true)).FindAll("kly-spatial"));
    }

    [Fact]
    public void Forms_with_a_form_name_stay_interactive_in_a_web_view()
    {
        UseWebView();
        var cut = Render<KlyChatPrompt>(p => p.Add(c => c.FormName, "prompt"));

        // In a Blazor Web App without interactivity the prompt would post (a form with a name and an antiforgery token).
        Assert.Empty(cut.FindAll("input[name='__RequestVerificationToken']"));
    }

    [Fact]
    public void The_stylesheet_keeps_content_inside_the_safe_area()
    {
        var css = File.ReadAllText(Path.Combine(LibraryRoot(), "wwwroot", "klyroo.css"));

        Assert.Contains("env(safe-area-inset-top", css);
        Assert.Contains("env(safe-area-inset-bottom", css);
        Assert.Contains("pointer:coarse", css.Replace(" ", ""));
        Assert.Contains("data-kly-tv", css);
    }

    [Fact]
    public void The_bundle_exports_the_initializers_of_every_host()
    {
        var bundle = File.ReadAllText(Path.Combine(LibraryRoot(), "wwwroot", "Klyroo.UI.lib.module.js"));
        var exports = Regex.Match(bundle, @"export\s*\{([^}]*)\}\s*;?\s*$").Groups[1].Value;
        var names = exports.Split(',').Select(e => e.Split(" as ").Last().Trim()).ToHashSet();

        // A Web App runs afterWebStarted; WebAssembly, Server and BlazorWebView run beforeStart and afterStarted.
        Assert.Contains("afterWebStarted", names);
        Assert.Contains("beforeStart", names);
        Assert.Contains("afterStarted", names);
    }

    private static string LibraryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Klyroo.UI");
            if (Directory.Exists(candidate)) return candidate;
        }
        throw new DirectoryNotFoundException("src/Klyroo.UI not found above the test output directory.");
    }
}

[CollectionDefinition("Culture", DisableParallelization = true)]
public sealed class CultureCollection;

/// <summary>Applying a culture sets the process wide default: these tests run alone so no other test sees it.</summary>
[Collection("Culture")]
public class HybridCultureTests : KlyrooTestContext
{
    private void UseWebView() => SetRendererInfo(new RendererInfo("WebView", true));

    [Fact]
    public void App_applies_the_stored_culture_before_it_renders_in_a_web_view()
    {
        var previous = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture, CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en");
            JSInterop.Setup<string?>("Klyroo.getStoredCulture", "klyroo-culture").SetResult("fr");
            UseWebView();

            var cut = Render<KlyApp>(p => p.Add(a => a.ChildContent, "<p>Content</p>"));

            Assert.Equal("fr", cut.Find("div").GetAttribute("lang"));
            Assert.Equal("fr", CultureInfo.DefaultThreadCurrentUICulture?.Name);
            Assert.Contains("Content", cut.Markup);
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture, CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture) = previous;
        }
    }

    [Fact]
    public void App_ignores_the_store_in_a_browser()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            JSInterop.Setup<string?>("Klyroo.getStoredCulture", "klyroo-culture").SetResult("fr");

            Assert.Equal("en", Render<KlyApp>().Find("div").GetAttribute("lang"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void App_ignores_a_stored_culture_the_app_does_not_support()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            JSInterop.Setup<string?>("Klyroo.getStoredCulture", "klyroo-culture").SetResult("xx-NOPE");
            UseWebView();

            Assert.Equal("en", Render<KlyApp>().Find("div").GetAttribute("lang"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
