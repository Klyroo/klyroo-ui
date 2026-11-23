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

    [Theory]
    [InlineData("name")]
    [InlineData("customer.name")]
    [InlineData("a b")]
    [InlineData("a!b~c*d")]
    [InlineData("100%+;,\"'<script>")]
    [InlineData("caf\u00e9 \U0001F600")]
    public void Keys_with_any_character_round_trip_and_the_value_holds_only_cookie_safe_characters(string key)
    {
        var prefs = new DataListColumnPreferences();
        prefs.Order.AddRange([key, "other"]);
        prefs.Hidden.Add(key);
        prefs.Widths[key] = 200;

        var encoded = prefs.Encode()!;

        // No percent sign (a server decodes it once, and turns + into a space), no cookie separator, quote, space or comma: the same value on both sides.
        Assert.Matches("^[A-Za-z0-9._~!*'-]+$", encoded);
        var decoded = DataListColumnPreferences.Decode(encoded);
        Assert.Equal([key, "other"], decoded.Order);
        Assert.Contains(key, decoded.Hidden);
        Assert.Equal(200, decoded.Widths[key]);
    }

    [Theory]
    [InlineData("v1!h%3Cscript%3E")]
    [InlineData("v1!ha'zz")]
    [InlineData("v1!ha'4")]
    [InlineData("v1!h'ff'fe")]
    [InlineData("v1!ha b")]
    [InlineData("v1!wa*1e3")]
    [InlineData("v1!wa*-5")]
    [InlineData("v1!wa*999999999999")]
    [InlineData("v1!h")]
    [InlineData("!!!")]
    public void Malformed_or_hostile_values_decode_to_nothing(string raw) => Assert.True(DataListColumnPreferences.Decode(raw).IsEmpty);

    [Fact]
    public void A_value_past_the_bound_is_ignored_and_one_that_would_be_too_long_is_not_written()
    {
        Assert.True(DataListColumnPreferences.Decode("v1!h" + new string('a', DataListColumnPreferences.MaxEncodedLength)).IsEmpty);

        var prefs = new DataListColumnPreferences();
        for (var i = 0; i < 400; i++) prefs.Widths[$"column-number-{i}"] = 100;
        Assert.Null(prefs.Encode());

        // Only the first keys of a section count: a value of a thousand keys cannot cost more than a hundred.
        var many = DataListColumnPreferences.Decode("v1!h" + string.Join('~', Enumerable.Range(0, 120).Select(i => "k" + i)));
        Assert.Equal(100, many.Hidden.Count);

        // Symmetric on the way out: a key past 64 chars or a section past 100 entries refuses to encode.
        var tooLongKey = new DataListColumnPreferences();
        tooLongKey.Hidden.Add(new string('k', 65));
        Assert.Null(tooLongKey.Encode());

        var tooManyKeys = new DataListColumnPreferences();
        for (var i = 0; i < 101; i++) tooManyKeys.Hidden.Add("k" + i);
        Assert.Null(tooManyKeys.Encode());
    }

    [Fact]
    public void Prune_drops_unknown_keys_and_what_cannot_apply()
    {
        var prefs = DataListColumnPreferences.Decode("v1!ogone~name~age!hname~gone~age!vage!sname!ename~age!uage~city!wgone*200~age*120");
        prefs.Prune(new HashSet<string> { "name", "age", "city" }, new HashSet<string> { "name" });

        Assert.Equal(["name", "age"], prefs.Order);
        // A required column is never hidden; a column cannot be hidden and shown, pinned two ways.
        Assert.Equal(["age"], prefs.Hidden);
        Assert.Empty(prefs.Shown);
        Assert.Equal(["name"], prefs.PinnedStart);
        Assert.Equal(["age"], prefs.PinnedEnd);
        Assert.Equal(["city"], prefs.Unpinned);
        Assert.Equal(new Dictionary<string, int> { ["age"] = 120 }, prefs.Widths);
    }

    [Fact]
    public void A_preferences_key_is_reduced_to_cookie_safe_characters()
    {
        Assert.Null(DataListColumnPreferences.NormalizeKey(" "));
        Assert.Equal("a_b_c", DataListColumnPreferences.NormalizeKey("a b;c"));
        Assert.Equal(64, DataListColumnPreferences.NormalizeKey(new string('x', 200))!.Length);
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

    private static string Style(IRenderedComponent<DataListColumnPanelHarness> cut, string key) => cut.Find($"colgroup col[data-col-key={key}]").GetAttribute("style") ?? "";

    private static void Report(IRenderedComponent<DataListColumnPanelHarness> cut, string key, string value) =>
        cut.Find($"[data-slot=col-resize][data-col-key={key}]").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = value });

    [Fact]
    public void A_table_whose_columns_all_have_a_width_is_as_wide_as_their_sum_and_otherwise_fills_its_container()
    {
        var some = Render<DataListColumnPanelHarness>();
        Assert.Contains("min-w-full", some.Find("table").GetAttribute("class"));
        Assert.DoesNotContain("min-w-0", some.Find("table").GetAttribute("class"));

        var all = Render<DataListColumnPanelHarness>(p => p.Add(x => x.CityWidth, 120));
        Assert.Contains("min-w-0", all.Find("table").GetAttribute("class"));
        Assert.DoesNotContain("min-w-full", all.Find("table").GetAttribute("class"));
    }

    [Fact]
    public void A_resize_reported_by_the_handle_sets_the_width_of_the_col_and_the_cells_and_reset_goes_back_to_the_declared_one()
    {
        var cut = Render<DataListColumnPanelHarness>();
        Assert.Equal("width:100px", Style(cut, "age"));

        Report(cut, "age", "116");
        Assert.Equal("width:116px", Style(cut, "age"));
        Assert.Equal("width:116px;min-width:116px;max-width:116px", cut.Find("thead th[data-key=age]").GetAttribute("style"));
        Assert.Contains("box-border", cut.Find("tbody tr td:nth-child(2)").GetAttribute("class"));
        Assert.Equal("Age: 116 pixels wide", cut.Find("[data-slot=col-live]").TextContent);

        Report(cut, "age", "reset");
        Assert.Equal("width:100px", Style(cut, "age"));
        Assert.Equal("Age: default width", cut.Find("[data-slot=col-live]").TextContent);
    }

    [Fact]
    public void A_reported_width_is_clamped_and_a_value_that_is_not_a_number_is_ignored()
    {
        var cut = Render<DataListColumnPanelHarness>();

        Report(cut, "age", "5");
        Assert.Equal("width:64px", Style(cut, "age"));
        Report(cut, "age", "99999");
        Assert.Equal("width:900px", Style(cut, "age"));
        Report(cut, "age", "-30");
        Report(cut, "age", "abc");
        Report(cut, "age", "");
        Assert.Equal("width:900px", Style(cut, "age"));
    }

    [Fact]
    public void The_resize_handle_is_a_named_separator_with_its_limits_and_a_value()
    {
        var cut = Render<DataListColumnPanelHarness>();

        var handle = cut.Find("[data-slot=col-resize][data-col-key=age]");
        Assert.Equal("separator", handle.GetAttribute("role"));
        Assert.Equal("0", handle.GetAttribute("tabindex"));
        Assert.Equal("Resize Age", handle.GetAttribute("aria-label"));
        Assert.Equal("64", handle.GetAttribute("aria-valuemin"));
        Assert.Equal("900", handle.GetAttribute("aria-valuemax"));
        Assert.Equal("100", handle.GetAttribute("aria-valuenow"));
        Assert.Equal("100 pixels", handle.GetAttribute("aria-valuetext"));
        // The browser says the value of a column without a width itself, once the handle has the focus: .NET does not know the width of a column that sizes to its content.
        Assert.False(cut.Find("[data-slot=col-resize][data-col-key=city]").HasAttribute("aria-valuenow"));
    }

    [Fact]
    public void A_pinned_column_without_a_width_gets_one_so_the_next_pinned_column_can_stick_after_it()
    {
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.CityDefaultPin, DataListPin.Start).Add(x => x.NameRequired, false));

        Assert.Equal("width:150px", Style(cut, "city"));
        Assert.Contains("inset-inline-start:0px", cut.Find("thead th[data-key=city]").GetAttribute("style"));

        cut.Find("[data-col-pin-start=name]").Click();
        // Name comes first in the order and is 180 px wide: City sticks where it ends.
        Assert.Contains("inset-inline-start:0px", cut.Find("thead th[data-key=name]").GetAttribute("style"));
        Assert.Contains("inset-inline-start:180px", cut.Find("thead th[data-key=city]").GetAttribute("style"));
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

    [Fact]
    public void Nothing_is_written_without_a_preferences_key()
    {
        var cut = Render<DataListColumnPanelHarness>();

        cut.Find("input[data-col-toggle=city]").Change(false);

        Assert.Empty(JSInterop.Invocations["Blux.tools.writeColumnPrefs"]);
        Assert.Empty(JSInterop.Invocations["Blux.tools.readColumnPrefs"]);
        // The columns are not in the URL either: the list does not sync with it here, and the preferences never write to it.
        Assert.DoesNotContain("city", Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Only_what_differs_from_the_defaults_is_written_and_unknown_keys_of_an_old_cookie_are_dropped()
    {
        JSInterop.Setup<string?>("Blux.tools.readColumnPrefs", _ => true).SetResult("v1!hgone~city!wgone*200!ogone~name~age~city");
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "people"));
        cut.WaitForAssertion(() => Assert.DoesNotContain("data-key=\"city\"", cut.Find("thead").InnerHtml));

        cut.Find("[data-col-pin-end=age]").Click();

        // gone is not a column of this list: it does not travel on. The order that equals the declared one is a default too.
        Assert.Equal("v1!hcity!eage", JSInterop.Invocations["Blux.tools.writeColumnPrefs"].Last().Arguments[1]);
    }

    [Fact]
    public void Show_all_and_Hide_all_keep_the_required_column_and_say_what_they_did()
    {
        var cut = Render<DataListColumnPanelHarness>();

        Assert.True(cut.Find("[data-slot=columns-show-all]").HasAttribute("disabled"));
        cut.Find("[data-slot=columns-hide-all]").Click();

        Assert.Equal(["name"], cut.FindAll("thead th").Select(th => th.GetAttribute("data-key")));
        Assert.Equal("Optional columns hidden", cut.Find("[data-slot=columns-live]").TextContent);
        Assert.True(cut.Find("[data-slot=columns-hide-all]").HasAttribute("disabled"));

        cut.Find("[data-slot=columns-show-all]").Click();

        Assert.Equal(["name", "age", "city"], cut.FindAll("thead th").Select(th => th.GetAttribute("data-key")));
        Assert.Equal("All columns shown", cut.Find("[data-slot=columns-live]").TextContent);
        // Back to the defaults: nothing differs, Reset has nothing to do.
        Assert.True(cut.Find("[data-slot=columns-reset]").HasAttribute("disabled"));
    }

    [Fact]
    public void The_last_visible_column_cannot_be_hidden()
    {
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.NameRequired, false));

        cut.Find("[data-slot=columns-hide-all]").Click();

        // Nothing is required, so the first column stays and its box is disabled, with a hint.
        Assert.Equal(["name"], cut.FindAll("thead th").Select(th => th.GetAttribute("data-key")));
        Assert.True(cut.Find("input[data-col-toggle=name]").HasAttribute("disabled"));
        Assert.Equal("At least one column stays visible.", cut.Find("[data-slot=columns-hint]").TextContent);

        // The context refuses it too (an app can call it).
        cut.Find("input[data-col-toggle=name]").Change(false);
        Assert.Single(cut.FindAll("thead th[data-key]"));
    }

    [Fact]
    public void Each_change_is_said_in_a_polite_live_region_outside_the_popover()
    {
        var cut = Render<DataListColumnPanelHarness>();
        var live = () => cut.Find("[data-slot=columns-live]");
        Assert.Equal("polite", live().GetAttribute("aria-live"));
        Assert.Equal("status", live().GetAttribute("role"));
        Assert.Null(live().Closest("[data-slot=columns-content]"));

        cut.Find("input[data-col-toggle=city]").Change(false);
        Assert.Equal("City hidden", live().TextContent);
        cut.Find("input[data-col-toggle=city]").Change(true);
        Assert.Equal("City shown", live().TextContent);
        cut.Find("[data-col-up=city]").Click();
        Assert.Equal("City moved to position 2 of 3", live().TextContent);
        cut.Find("[data-col-pin-start=city]").Click();
        Assert.Equal("City pinned to the start", live().TextContent);
        cut.Find("[data-col-pin-end=city]").Click();
        Assert.Equal("City pinned to the end", live().TextContent);
        cut.Find("[data-col-pin-end=city]").Click();
        Assert.Equal("City unpinned", live().TextContent);
        cut.Find("[data-slot=columns-reset]").Click();
        Assert.Equal("Columns reset", live().TextContent);
    }

    [Fact]
    public void Pin_buttons_are_toggle_buttons_with_names_and_the_move_buttons_stop_at_the_ends()
    {
        var cut = Render<DataListColumnPanelHarness>();

        var pin = cut.Find("[data-col-pin-start=city]");
        Assert.Equal("Pin City to start", pin.GetAttribute("aria-label"));
        Assert.Equal("false", pin.GetAttribute("aria-pressed"));
        pin.Click();
        Assert.Equal("true", cut.Find("[data-col-pin-start=city]").GetAttribute("aria-pressed"));
        // The name does not change with the state: aria-pressed says it.
        Assert.Equal("Pin City to start", cut.Find("[data-col-pin-start=city]").GetAttribute("aria-label"));

        Assert.Equal("Move Age up", cut.Find("[data-col-up=age]").GetAttribute("aria-label"));
        Assert.Equal("Move Age down", cut.Find("[data-col-down=age]").GetAttribute("aria-label"));
        // Age is the first configurable column in the panel (Name is Required and unlisted), so Move Age up is disabled, and City is the last, so Move City down is disabled.
        Assert.True(cut.Find("[data-col-up=age]").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-col-down=city]").HasAttribute("disabled"));
        // The grip is a shortcut for the pointer, hidden from assistive technology and out of the tab order.
        Assert.Equal("true", cut.Find("[data-col-grip=age]").GetAttribute("aria-hidden"));
    }

    [Fact]
    public async Task Two_lists_on_the_same_page_cannot_share_a_PreferencesKey()
    {
        Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "shared-cols"));
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "shared-cols")));
        Assert.Contains("shared-cols", ex.Message);

        await DisposeComponentsAsync();
        // Once the first list is disposed, the key can be claimed again.
        var afterDispose = Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "shared-cols"));
        Assert.Equal(3, afterDispose.FindAll("thead th[data-key]").Count);
    }

    [Fact]
    public void Dropping_a_dragged_column_on_another_reorders_in_both_directions_and_ignores_a_bad_report()
    {
        var cut = Render<DataListColumnPanelHarness>();
        string Order() => string.Join(",", cut.FindAll("thead th[data-key]").Select(th => th.GetAttribute("data-key")));

        cut.Find("[data-col-grip=city]").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "before:age" });
        Assert.Equal("name,city,age", Order());
        Assert.Equal("City moved to position 2 of 3", cut.Find("[data-slot=columns-live]").TextContent);

        cut.Find("[data-col-grip=city]").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "after:age" });
        Assert.Equal("name,age,city", Order());
        // Back at the declared order: nothing to remember.
        Assert.True(cut.Find("[data-slot=columns-reset]").HasAttribute("disabled"));

        foreach (var bad in new[] { "sideways:age", "before:nothing", "before:", "age", "", "after:city" })
        {
            cut.Find("[data-col-grip=city]").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = bad });
        }
        Assert.Equal("name,age,city", Order());
    }

    [Fact]
    public void The_selection_and_row_actions_columns_stay_where_they_are_whatever_the_panel_does()
    {
        var actions = new DataListRowAction<Person>[] { new() { Key = "open", Label = "Open", OnExecute = _ => Task.CompletedTask } };
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.Selectable, true).Add(x => x.RowActions, actions).Add(x => x.NameRequired, false));

        // They are not in the panel.
        Assert.Equal(["name", "age", "city"], cut.FindAll("[data-slot=columns-item]").Select(i => i.GetAttribute("data-col-key")));
        Assert.Equal("3 of 3 visible", cut.Find("[data-slot=columns-count]").TextContent);

        cut.Find("[data-slot=columns-hide-all]").Click();
        cut.Find("[data-col-pin-end=name]").Click();
        cut.Find("[data-col-pin-start=name]").Click();

        var headers = cut.FindAll("thead th").Select(th => th.GetAttribute("data-key")).ToArray();
        Assert.Equal(["select", "name", "actions"], headers);
        // With a column pinned to the start the selection cell sticks too, and the column sticks after it.
        Assert.Equal("start", cut.Find("thead th[data-key=select]").GetAttribute("data-pinned"));
        Assert.Equal("inset-inline-start:0", cut.Find("thead th[data-key=select]").GetAttribute("style"));
        Assert.Contains("inset-inline-start:calc(2.5rem + 0px)", cut.Find("thead th[data-key=name]").GetAttribute("style"));
        // Their cols have no key, so no resize handle or width belongs to them.
        Assert.Empty(cut.FindAll("thead th[data-key=select] [data-slot=col-resize]"));
        Assert.Empty(cut.FindAll("thead th[data-key=actions] [data-slot=col-resize]"));

        // An end pin leaves room for the actions column, with a deterministic non-zero CSS fallback width before the browser measures it, and max-sm:relative for its resize handle on a phone.
        cut.Find("[data-col-pin-end=name]").Click();
        Assert.Contains("var(--bx-actions-w, 92px)", cut.Find("thead th[data-key=name]").GetAttribute("style"));
        Assert.Contains("max-sm:relative", cut.Find("thead th[data-key=name]").GetAttribute("class"));
        Assert.Equal("end", cut.Find("thead th[data-key=actions]").GetAttribute("data-pinned"));
        Assert.Single(JSInterop.Invocations["Blux.tools.watchActionsWidth"]);
    }

    [Fact]
    public void A_virtualized_table_rebuilds_its_rows_when_a_column_is_hidden()
    {
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.Virtualize, true).Add(x => x.PagingMode, DataListPagingMode.None));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("tbody tr[data-slot=row]")));
        Assert.Equal(3, cut.Find("tbody tr[data-slot=row]").Children.Length);

        cut.Find("input[data-col-toggle=city]").Change(false);

        Assert.All(cut.FindAll("tbody tr[data-slot=row]"), row => Assert.Equal(2, row.Children.Length));
        Assert.Equal(2, cut.FindAll("thead th[data-key]").Count);
    }

    [Fact]
    public void The_panel_search_has_a_name_and_a_negative_threshold_never_shows_it()
    {
        var shown = Render<DataListColumnPanelHarness>(p => p.Add(x => x.SearchThreshold, 0));
        Assert.Equal("Search columns…", shown.Find("[data-slot=columns-search]").GetAttribute("aria-label"));

        var never = Render<DataListColumnPanelHarness>(p => p.Add(x => x.SearchThreshold, -1));
        Assert.Empty(never.FindAll("[data-slot=columns-search]"));

        // The default threshold is 30 configurable columns.
        var normal = Render<DataListColumnPanelHarness>();
        Assert.Empty(normal.FindAll("[data-slot=columns-search]"));
    }

    [Fact]
    public void A_cookie_that_hid_every_column_still_shows_the_first_one()
    {
        JSInterop.Setup<string?>("Blux.tools.readColumnPrefs", _ => true).SetResult("v1!hname~age~city");
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "people").Add(x => x.NameRequired, false));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("thead th[data-key]")));
        Assert.Equal(["name"], cut.FindAll("thead th[data-key]").Select(th => th.GetAttribute("data-key")));
    }

    [Fact]
    public void An_interactive_list_reads_the_browser_when_nothing_was_handed_over()
    {
        JSInterop.Setup<string?>("Blux.tools.readColumnPrefs", _ => true).SetResult("v1!hcity!wage*150");
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "people"));

        cut.WaitForAssertion(() => Assert.Equal(["name", "age"], cut.FindAll("thead th[data-key]").Select(th => th.GetAttribute("data-key"))));
        Assert.Equal("width:150px", Style(cut, "age"));
        Assert.Equal("people", JSInterop.Invocations["Blux.tools.readColumnPrefs"].Single().Arguments[0]);
    }
}

/// <summary>Static SSR behavior of column preferences: server reads cookie on first paint, panel and resize handles stay hidden.</summary>
public class DataListColumnPanelStaticTests : BluxToolsTestContext
{
    private sealed class FakeCookies(string? value, string pathBase = "") : IBxRequestCookies
    {
        public string PathBase => pathBase;
        public string? Get(string name) => name == BxPathScope.CookieName("blux-dl-cols", pathBase) ? value : null;
    }

    public DataListColumnPanelStaticTests()
        : base(interactive: false, configureServices: s => s.AddSingleton<IBxRequestCookies>(new FakeCookies("other.v1!hname!!people.v1!ocity~name~age!hage!wcity*220!sname")))
    {
    }

    [Fact]
    public void Statically_rendered_table_applies_server_cookie_on_first_paint_and_hides_panel_and_resize_handles()
    {
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "people"));

        // Panel and resize handles are interactive-only.
        Assert.Empty(cut.FindAll("[data-slot=columns-panel]"));
        Assert.Empty(cut.FindAll("[data-slot=col-resize]"));
        Assert.Empty(cut.FindAll("[data-slot=col-live]"));

        // Cookie hid "age", pinned "name" to start, and sized "city" to 220px.
        Assert.Equal(["name", "city"], cut.FindAll("thead th").Select(th => th.GetAttribute("data-key")));
        Assert.Equal("start", cut.Find("thead th[data-key=name]").GetAttribute("data-pinned"));
        Assert.Equal("width:220px", cut.Find("colgroup col[data-col-key=city]").GetAttribute("style"));
        // Every cell of the column has the width, so the first paint has no layout to correct.
        Assert.All(cut.FindAll("tbody tr[data-slot=row] td:nth-child(2)"), cell => Assert.Equal("width:220px;min-width:220px;max-width:220px", cell.GetAttribute("style")));
        Assert.DoesNotContain("<script", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void A_cookie_of_another_list_or_without_a_key_changes_nothing()
    {
        var other = Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "invoices"));
        Assert.Equal(["name", "age", "city"], other.FindAll("thead th").Select(th => th.GetAttribute("data-key")));

        var none = Render<DataListColumnPanelHarness>();
        Assert.Equal(["name", "age", "city"], none.FindAll("thead th").Select(th => th.GetAttribute("data-key")));
    }

    [Fact]
    public void A_hostile_cookie_cannot_put_anything_in_the_markup_or_hide_a_required_column()
    {
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "people"));
        Assert.Equal(["name", "city"], cut.FindAll("thead th").Select(th => th.GetAttribute("data-key")));

        using var hostile = new HostileContext("people.v1!hname~'3cscript'3e~city!wname*99999~'22'3e'3cimg*100!ocity~'3cb'3e");
        var shown = hostile.Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "people"));
        // name is required; city is hidden; the keys that are no column go nowhere; the width is clamped to the limit of the column.
        Assert.Equal(["name", "age"], shown.FindAll("thead th").Select(th => th.GetAttribute("data-key")));
        Assert.Equal("width:900px", shown.Find("colgroup col[data-col-key=name]").GetAttribute("style"));
        Assert.DoesNotContain("script", shown.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", shown.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<b>", shown.Markup, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class HostileContext : BluxToolsTestContext
    {
        public HostileContext(string cookie) : base(interactive: false, configureServices: s => s.AddSingleton<IBxRequestCookies>(new FakeCookies(cookie)))
        {
        }
    }
}

/// <summary>The cookie of an app below a path base carries the slug of the base in its name, like the other Blux cookies.</summary>
public class DataListColumnPanelPathBaseTests : BluxToolsTestContext
{
    private sealed class FakeCookies(string pathBase) : IBxRequestCookies
    {
        public List<string> Asked { get; } = [];
        public string PathBase => pathBase;
        public string? Get(string name)
        {
            Asked.Add(name);
            return name == "blux-dl-cols.app~~" ? "people.v1!hcity" : null;
        }
    }

    private static readonly FakeCookies Cookies = new("/app");

    public DataListColumnPanelPathBaseTests()
        : base(interactive: false, configureServices: s => s.AddSingleton<IBxRequestCookies>(Cookies))
    {
    }

    [Fact]
    public void The_name_of_the_cookie_is_scoped_to_the_path_base()
    {
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "people"));

        Assert.Contains("blux-dl-cols.app~~", Cookies.Asked);
        Assert.DoesNotContain("blux-dl-cols", Cookies.Asked);
        Assert.Equal(["name", "age"], cut.FindAll("thead th").Select(th => th.GetAttribute("data-key")));
    }
}

/// <summary>An interactive list that follows a prerender has no request: the prerender hands the cookie over in the persistent state, and the page pays for it in its budget.</summary>
public class DataListColumnPanelHandoffTests : BluxToolsTestContext
{
    public DataListColumnPanelHandoffTests() : base(interactive: true, persistentState: true)
    {
    }

    [Fact]
    public void The_interactive_render_takes_what_the_prerender_handed_over_and_does_not_ask_the_browser()
    {
        Persisted!.Persist("bx-dl-cols:people", "v1!hcity!wage*150");

        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "people"));

        Assert.Equal(["name", "age"], cut.FindAll("thead th[data-key]").Select(th => th.GetAttribute("data-key")));
        Assert.Equal("width:150px", cut.Find("colgroup col[data-col-key=age]").GetAttribute("style"));
        Assert.Empty(JSInterop.Invocations["Blux.tools.readColumnPrefs"]);
    }

    [Fact]
    public void Without_a_handover_the_browser_is_asked_once()
    {
        var cut = Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "people"));

        Assert.Single(JSInterop.Invocations["Blux.tools.readColumnPrefs"]);
        Assert.Equal(["name", "age", "city"], cut.FindAll("thead th[data-key]").Select(th => th.GetAttribute("data-key")));
    }
}

/// <summary>The prerender of an interactive page persists the cookie it read, only when there is something to hand over.</summary>
public class DataListColumnPanelPrerenderTests : BluxToolsTestContext
{
    private sealed class FakeCookies(string? value) : IBxRequestCookies
    {
        public string? PathBase => null;
        public string? Get(string name) => name == "blux-dl-cols" && value is not null ? "people." + value : null;
    }

    private static IRenderedComponent<DataListColumnPanelHarness> Prerender(BluxToolsTestContext context) =>
        context.Render<DataListColumnPanelHarness>(p => p.Add(x => x.PreferencesKey, "people")
            .SetAssignedRenderMode(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode(prerender: true)));

    private sealed class Context : BluxToolsTestContext
    {
        public Context(string? cookie) : base(interactive: false, configureServices: s => s.AddSingleton<IBxRequestCookies>(new FakeCookies(cookie)), persistentState: true)
        {
        }

        public Bunit.TestDoubles.BunitPersistentComponentState State => Persisted!;
    }

    [Fact]
    public void The_prerender_keeps_a_disabled_button_and_reads_the_cookie()
    {
        using var context = new Context("v1!hcity");
        var cut = Prerender(context);

        Assert.Equal(["name", "age"], cut.FindAll("thead th[data-key]").Select(th => th.GetAttribute("data-key")));
        Assert.True(cut.Find("[data-slot=columns-trigger]").HasAttribute("disabled"));
    }

    [Fact]
    public void The_cookie_is_persisted_for_the_interactive_render_and_nothing_is_when_it_is_empty()
    {
        using var with = new Context("v1!hcity!hunknown");
        Prerender(with);
        with.State.TriggerOnPersisting();
        Assert.True(with.State.TryTake("bx-dl-cols:people", out string? text));
        // Pruned like a write: the key of no column does not travel.
        Assert.Equal("v1!hcity", text);

        using var without = new Context(null);
        Prerender(without);
        without.State.TriggerOnPersisting();
        Assert.False(without.State.TryTake("bx-dl-cols:people", out string? none));
    }
}
