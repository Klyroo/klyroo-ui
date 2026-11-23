using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>Column preferences cookie codec, <see cref="BxDataListColumnPanel{TItem}"/>, pinning, resizing and SSR first-paint cookie reading.</summary>
public class DataListColumnPanelTests : BluxToolsTestContext
{
    [Fact]
    public void Preferences_codec_round_trips_and_clamps_widths_and_ignores_malformed_input()
    {
        var prefs = new DataListColumnPreferences();
        prefs.Order.AddRange(["city", "name", "age"]);
        prefs.Hidden.Add("age");
        prefs.Shown.Add("notes");
        prefs.PinnedStart.Add("name");
        prefs.PinnedEnd.Add("city");
        prefs.Unpinned.Add("id");
        prefs.Widths["name"] = 240;
        prefs.Widths["tiny"] = 10;
        prefs.Widths["huge"] = 5000;

        var encoded = prefs.Encode();
        Assert.StartsWith("v1!", encoded);

        var decoded = DataListColumnPreferences.Decode(encoded);
        Assert.Equal(["city", "name", "age"], decoded.Order);
        Assert.Contains("age", decoded.Hidden);
        Assert.Contains("notes", decoded.Shown);
        Assert.Contains("name", decoded.PinnedStart);
        Assert.Contains("city", decoded.PinnedEnd);
        Assert.Contains("id", decoded.Unpinned);
        Assert.Equal(240, decoded.Widths["name"]);
        Assert.Equal(64, decoded.Widths["tiny"]);
        Assert.Equal(900, decoded.Widths["huge"]);

        // Malformed / unknown version returns empty without throwing.
        Assert.True(DataListColumnPreferences.Decode(null).IsEmpty);
        Assert.True(DataListColumnPreferences.Decode("v2!hage").IsEmpty);
        Assert.True(DataListColumnPreferences.Decode("v1!wbad*notanumber!w%ZZ*100").IsEmpty);
    }

    [Fact]
    public void Column_panel_omits_required_columns_and_toggles_visibility_and_resets()
    {
        var cut = Render<DataListColumnPanelHarness>();

        // Name is Required, so only Age and City appear in the panel.
        var items = cut.FindAll("[data-slot=columns-item]");
        Assert.Equal(["age", "city"], items.Select(i => i.GetAttribute("data-col-key")));
        Assert.Equal("3 of 3 visible", cut.Find("[data-slot=columns-count]").TextContent);
        Assert.True(cut.Find("[data-slot=columns-reset]").HasAttribute("disabled"));

        // Hide City -> header disappears from the table, count updates, Reset enables.
        cut.Find("input[data-col-toggle=city]").Change(false);

        Assert.Equal(["name", "age"], cut.FindAll("thead th").Select(th => th.GetAttribute("data-key")));
        Assert.Equal("2 of 3 visible", cut.Find("[data-slot=columns-count]").TextContent);
        Assert.False(cut.Find("[data-slot=columns-reset]").HasAttribute("disabled"));

        // Reset restores City.
        cut.Find("[data-slot=columns-reset]").Click();
        Assert.Equal(["name", "age", "city"], cut.FindAll("thead th").Select(th => th.GetAttribute("data-key")));
        Assert.True(cut.Find("[data-slot=columns-reset]").HasAttribute("disabled"));
    }

    [Fact]
    public void Reordering_and_pinning_columns_updates_table_headers_and_sticky_attributes()
    {
        var cut = Render<DataListColumnPanelHarness>();

        // Move City up (from index 2 to 1: name, city, age).
        cut.Find("[data-col-up=city]").Click();
        Assert.Equal(["name", "city", "age"], cut.FindAll("thead th").Select(th => th.GetAttribute("data-key")));

        // Pin City to Start -> Start-pinned columns render first with data-pinned="start".
        cut.Find("[data-col-pin-start=city]").Click();
        Assert.Equal(["city", "name", "age"], cut.FindAll("thead th").Select(th => th.GetAttribute("data-key")));
        var cityTh = cut.Find("thead th[data-key=city]");
        Assert.Equal("start", cityTh.GetAttribute("data-pinned"));
        Assert.Contains("inset-inline-start:0px", cityTh.GetAttribute("style") ?? "");

        // Pin Age to End -> End-pinned column renders last with data-pinned="end".
        cut.Find("[data-col-pin-end=age]").Click();
        Assert.Equal(["city", "name", "age"], cut.FindAll("thead th").Select(th => th.GetAttribute("data-key")));
        var ageTh = cut.Find("thead th[data-key=age]");
        Assert.Equal("end", ageTh.GetAttribute("data-pinned"));
        Assert.Contains("inset-inline-end:0px", ageTh.GetAttribute("style") ?? "");

        // Clicking Pin start again unpins City.
        cut.Find("[data-col-pin-start=city]").Click();
        Assert.False(cut.Find("thead th[data-key=city]").HasAttribute("data-pinned"));
    }

    [Fact]
    public void Keyboard_resize_adjusts_col_width_and_Home_resets_it()
    {
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.Resizable, true));

        var col = () => cut.Find("colgroup col[data-col-key=age]");
        Assert.Equal("width:100px", col().GetAttribute("style"));

        cut.Find("[data-slot=col-resize][data-col-key=age]").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("width:116px", col().GetAttribute("style"));

        cut.Find("[data-slot=col-resize][data-col-key=age]").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal("width:100px", col().GetAttribute("style"));

        cut.Find("[data-slot=col-resize][data-col-key=age]").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        cut.Find("[data-slot=col-resize][data-col-key=age]").KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.Equal("width:100px", col().GetAttribute("style"));
    }

    [Fact]
    public void Panel_search_filters_configurable_columns_when_threshold_is_met()
    {
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.SearchThreshold, 2));

        cut.Find("[data-slot=columns-search]").Input("cit");
        var visibleItems = cut.FindAll("[data-slot=columns-item]");
        Assert.Single(visibleItems);
        Assert.Equal("city", visibleItems[0].GetAttribute("data-col-key"));
    }

    [Fact]
    public void PreferencesKey_persists_changes_to_cookie_via_JS()
    {
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "people"));

        cut.Find("input[data-col-toggle=city]").Change(false);

        var write = JSInterop.Invocations["Blux.tools.writeColumnPrefs"].Last();
        Assert.Equal("people", write.Arguments[0]);
        Assert.Equal("v1!hcity", write.Arguments[1]);

        // Reset writes empty string to delete the cookie.
        cut.Find("[data-slot=columns-reset]").Click();
        var clear = JSInterop.Invocations["Blux.tools.writeColumnPrefs"].Last();
        Assert.Equal("people", clear.Arguments[0]);
        Assert.Equal("", clear.Arguments[1]);
    }
}

/// <summary>Static SSR behavior of column preferences: server reads cookie on first paint, panel and resize handles stay hidden.</summary>
public class DataListColumnPanelStaticTests : BluxToolsTestContext
{
    private sealed class FakeCookies(string? value, string pathBase = "") : IBxRequestCookies
    {
        public string PathBase => pathBase;
        public string? Get(string name) => name == "blux-dl-cols-people" ? value : null;
    }

    public DataListColumnPanelStaticTests()
        : base(interactive: false, configureServices: s => s.AddSingleton<IBxRequestCookies>(new FakeCookies("v1!ocity~name~age!hage!wcity*220!sname")))
    {
    }

    [Fact]
    public void Statically_rendered_table_applies_server_cookie_on_first_paint_and_hides_panel_and_resize_handles()
    {
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "people"));

        // Panel and resize handles are interactive-only.
        Assert.Empty(cut.FindAll("[data-slot=columns-panel]"));
        Assert.Empty(cut.FindAll("[data-slot=col-resize]"));

        // Cookie hid "age", pinned "name" to start, and sized "city" to 220px.
        Assert.Equal(["name", "city"], cut.FindAll("thead th").Select(th => th.GetAttribute("data-key")));
        Assert.Equal("start", cut.Find("thead th[data-key=name]").GetAttribute("data-pinned"));
        Assert.Equal("width:220px", cut.Find("colgroup col[data-col-key=city]").GetAttribute("style"));
    }
}
