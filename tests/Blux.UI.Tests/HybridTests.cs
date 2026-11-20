using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tests;

/// <summary>
/// Blazor Hybrid (BlazorWebView): the renderer reports <c>RendererInfo.Name == "WebView"</c>, is always interactive and has no server.
/// These tests render the components under that renderer info and check what degrades.
/// </summary>
public class HybridTests : BluxTestContext
{
    private const string Manifest = """{"css":"_blux/fonts/blux.fonts.css","fonts":[{"preload":["_blux/fonts/geist.woff2"]}]}""";

    private void UseWebView() => SetRendererInfo(new RendererInfo("WebView", true));

    [Fact]
    public void Host_detection_reads_the_renderer_name()
    {
        Assert.True(BluxHost.IsHybrid(new RendererInfo("WebView", true)));
        Assert.False(BluxHost.IsHybrid(new RendererInfo("Server", true)));
        Assert.False(BluxHost.IsHybrid(new RendererInfo("Static", false)));
        Assert.False(BluxHost.IsHybrid(new RendererInfo("WebAssembly", true)));
    }

    [Fact]
    public void Sidebar_applies_the_remembered_state_with_a_script_on_the_web_but_not_in_a_web_view()
    {
        var web = Render<BxSidebar>(p => p.Add(s => s.Id, "app").Add(s => s.ChildContent, "<p>Nav</p>"));
        Assert.Contains("blux-sidebar-", web.Markup.Substring(web.Markup.IndexOf("<script", StringComparison.Ordinal)));

        UseWebView();
        var hybrid = Render<BxSidebar>(p => p.Add(s => s.Id, "app").Add(s => s.ChildContent, "<p>Nav</p>"));
        Assert.DoesNotContain("<script", hybrid.Markup);
        Assert.Equal("app", hybrid.Find("bx-sidebar").GetAttribute("data-bx-sidebar-id"));
    }

    [Fact]
    public void ColorModeScript_renders_in_a_browser_and_nothing_in_a_web_view()
    {
        Assert.Contains("<script>", Render<BxColorModeScript>().Markup);

        UseWebView();
        Assert.Equal("", Render<BxColorModeScript>().Markup);
    }

    [Fact]
    public void CultureMeta_renders_nothing_in_a_web_view()
    {
        Assert.Contains("blux-culture", Render<BxCultureMeta>().Markup);

        UseWebView();
        Assert.Equal("", Render<BxCultureMeta>().Markup);
    }

    [Fact]
    public void Fonts_link_the_plain_route_in_a_web_view_where_there_is_no_asset_map()
    {
        UseWebView();
        var cut = Render<BxFonts>(p => p.Add(c => c.ManifestJson, Manifest));

        Assert.Equal("_blux/fonts/blux.fonts.css", cut.Find("link[rel=stylesheet]").GetAttribute("href"));
        Assert.Equal("_blux/fonts/geist.woff2", cut.Find("link[rel=preload]").GetAttribute("href"));
    }

    private static void HeadPage(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b)
    {
        b.OpenComponent<HeadOutlet>(0);
        b.CloseComponent();
        b.OpenComponent<BxHead>(1);
        b.AddAttribute(2, nameof(BxHead.Fonts), false);
        b.CloseComponent();
    }

    [Fact]
    public void Head_renders_nothing_in_a_browser()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.GetRequiredService<BluxOptions>().Colors.Primary = BxPalette.Blue;

        Assert.DoesNotContain("data-bx-theme", Render(HeadPage).Markup);
    }

    [Fact]
    public void Head_sends_the_theme_variables_to_the_head_in_a_web_view()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.GetRequiredService<BluxOptions>().Colors.Primary = BxPalette.Blue;
        UseWebView();

        Assert.Contains("data-bx-theme", Render(HeadPage).Markup);
    }

    private static void ImagePage(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b)
    {
        b.OpenComponent<HeadOutlet>(0);
        b.CloseComponent();
        b.OpenComponent<BxImage>(1);
        b.AddAttribute(2, nameof(BxImage.Src), "/img/hero.png");
        b.AddAttribute(3, nameof(BxImage.Alt), "Hero");
        b.AddAttribute(4, nameof(BxImage.Preload), true);
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
        var cut = Render<BxLocaleSelect>(p => p.Add(l => l.Locales, [BxLocales.En, BxLocales.Fr]));

        Assert.True(cut.Find("[data-bx-locale-select]").HasAttribute("data-bx-reload"));
        Assert.False(cut.Find("bx-select").HasAttribute("data-bx-seq"));
    }

    [Fact]
    public async Task LocaleSelect_in_a_web_view_saves_the_choice_in_localStorage_and_does_not_reload()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        UseWebView();
        string? raised = null;
        var cut = Render<BxLocaleSelect>(p => p
            .Add(l => l.Locales, [BxLocales.En, BxLocales.Fr])
            .Add(l => l.Value, "en")
            .Add(l => l.OnLocaleChange, EventCallback.Factory.Create<string>(this, code => raised = code)));

        Assert.False(cut.Find("[data-bx-locale-select]").HasAttribute("data-bx-reload"));
        await cut.InvokeAsync(() => cut.Find("bx-select").TriggerEventAsync("onbxchange", new BxChangeEventArgs { Value = "fr", Sequence = 1 }));

        var call = JSInterop.VerifyInvoke("Blux.setStoredCulture");
        Assert.Equal(["blux-culture", "fr"], call.Arguments.Select(a => a?.ToString()));
        Assert.Equal("fr", raised);
        Assert.Equal("fr", cut.Find("bx-select").GetAttribute("value"));
    }

    [Fact]
    public async Task LocaleSelect_in_a_web_view_keeps_the_choice_when_the_parent_renders_again()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        UseWebView();
        var cut = Render<BxLocaleSelect>(p => p.Add(l => l.Locales, [BxLocales.En, BxLocales.Fr, BxLocales.De]).Add(l => l.Value, "en"));
        await cut.InvokeAsync(() => cut.Find("bx-select").TriggerEventAsync("onbxchange", new BxChangeEventArgs { Value = "fr", Sequence = 1 }));

        // An OnLocaleChange handler that renders the parent again hands the same parameters back: the culture in effect is
        // still the old one (the choice applies at the next start), but the select must not snap back to it.
        cut.Render(p => p.Add(l => l.Locales, [BxLocales.En, BxLocales.Fr, BxLocales.De]).Add(l => l.Value, "en"));
        Assert.Equal("fr", cut.Find("bx-select").GetAttribute("value"));

        // A new Value from the parent is a new decision of the app: it wins.
        cut.Render(p => p.Add(l => l.Locales, [BxLocales.En, BxLocales.Fr, BxLocales.De]).Add(l => l.Value, "de"));
        Assert.Equal("de", cut.Find("bx-select").GetAttribute("value"));
    }

    [Fact]
    public void App_spatial_navigation_renders_the_element_that_turns_tv_mode_on_only_when_asked()
    {
        Assert.Empty(Render<BxApp>().FindAll("bx-spatial"));
        Assert.Single(Render<BxApp>(p => p.Add(a => a.SpatialNavigation, true)).FindAll("bx-spatial"));
    }

    [Fact]
    public void Forms_with_a_form_name_stay_interactive_in_a_web_view()
    {
        UseWebView();
        var cut = Render<BxChatPrompt>(p => p.Add(c => c.FormName, "prompt"));

        // In a Blazor Web App without interactivity the prompt would post (a form with a name and an antiforgery token).
        Assert.Empty(cut.FindAll("input[name='__RequestVerificationToken']"));
    }

    [Fact]
    public void The_stylesheet_keeps_content_inside_the_safe_area()
    {
        var css = File.ReadAllText(Path.Combine(LibraryRoot(), "wwwroot", "blux.css"));

        Assert.Contains("env(safe-area-inset-top", css);
        Assert.Contains("env(safe-area-inset-bottom", css);
        Assert.Contains("pointer:coarse", css.Replace(" ", ""));
        Assert.Contains("data-bx-tv", css);
    }

    [Fact]
    public void The_bundle_exports_the_initializers_of_every_host()
    {
        var bundle = File.ReadAllText(Path.Combine(LibraryRoot(), "wwwroot", "Blux.UI.lib.module.js"));
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
            var candidate = Path.Combine(dir.FullName, "src", "Blux.UI");
            if (Directory.Exists(candidate)) return candidate;
        }
        throw new DirectoryNotFoundException("src/Blux.UI not found above the test output directory.");
    }
}

[CollectionDefinition("Culture", DisableParallelization = true)]
public sealed class CultureCollection;

/// <summary>Applying a culture sets the process wide default: these tests run alone so no other test sees it.</summary>
[Collection("Culture")]
public class HybridCultureTests : BluxTestContext
{
    private void UseWebView() => SetRendererInfo(new RendererInfo("WebView", true));

    [Fact]
    public void App_applies_the_stored_culture_before_it_renders_in_a_web_view()
    {
        var previous = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture, CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en");
            JSInterop.Setup<string?>("Blux.getStoredCulture", "blux-culture").SetResult("fr");
            UseWebView();

            var cut = Render<BxApp>(p => p.Add(a => a.ChildContent, "<p>Content</p>"));

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
            JSInterop.Setup<string?>("Blux.getStoredCulture", "blux-culture").SetResult("fr");

            Assert.Equal("en", Render<BxApp>().Find("div").GetAttribute("lang"));
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
            JSInterop.Setup<string?>("Blux.getStoredCulture", "blux-culture").SetResult("xx-NOPE");
            UseWebView();

            Assert.Equal("en", Render<BxApp>().Find("div").GetAttribute("lang"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
