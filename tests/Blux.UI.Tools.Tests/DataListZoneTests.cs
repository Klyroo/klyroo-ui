using System.Text.Json;
using System.Text.Json.Serialization;
using Blux.UI.Tools.Query;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

[JsonSerializable(typeof(DataListResult<Order>))]
public sealed partial class OrderJson : JsonSerializerContext
{
}

/// <summary>
/// The time zone of a data list outside the browser (the cookie the script of Blux.UI.Tools writes, read by a static render and a prerender), and when the interactive render may adopt the result
/// the prerender handed over: not when its days were cut in another zone than the one of the browser.
/// </summary>
public class DataListZoneTests : BluxToolsTestContext
{
    private static readonly DataListState Base = RangeData.Schema.DefaultState;

    private static FilterRule Moment => new("created", FilterOperator.After, new FilterValue.Moment(new DateTimeOffset(2026, 11, 5, 10, 30, 0, TimeSpan.Zero)));

    private static FilterRule Day => new("day", FilterOperator.Is, new FilterValue.Date(new DateOnly(2026, 11, 5)));

    private static FilterRule MomentRange => new("created", FilterOperator.Between, new FilterValue.Range(new FilterValue.Moment(new DateTimeOffset(2026, 11, 5, 0, 0, 0, TimeSpan.Zero)), null));

    private static FilterRule Number => new("amount", FilterOperator.Gte, new FilterValue.Number(100));

    public DataListZoneTests() : base(interactive: true, persistentState: true)
    {
    }

    // ---- what the render adopts

    private sealed class Calls
    {
        public int Count;
        public string? LastZone;
    }

    // Persists what a prerender of the list would have handed over: the state, the state it loaded, the result, and the zone its days were cut in.
    private void Persist(DataListState state, string? zone)
    {
        var persisted = Persisted!;
        var key = "bx-dl:" + typeof(Order).FullName + ":z";
        var result = new DataListResult<Order>([RangeData.All[0]], 1);
        persisted.Persist(key + ":state", JsonSerializer.Serialize(state, DataListJsonContext.Default.DataListState));
        persisted.Persist(key + ":loaded", JsonSerializer.Serialize(state, DataListJsonContext.Default.DataListState));
        persisted.Persist(key + ":result", JsonSerializer.Serialize(result, OrderJson.Default.DataListResultOrder));
        persisted.Persist(key + ":zone", zone ?? "");
    }

    private (IRenderedComponent<BxDataList<Order>> Cut, Calls Calls) Render(DataListState state, string? browserZone)
    {
        var calls = new Calls();
        DataListProvider<Order> provider = (request, _) =>
        {
            calls.Count++;
            calls.LastZone = request.TimeZone;
            return ValueTask.FromResult(new DataListResult<Order>([RangeData.All[1]], 1));
        };
        var cut = Render<BxDataList<Order>>(p => p
            .Add(c => c.Schema, RangeData.Schema).Add(c => c.Provider, provider).Add(c => c.UrlSync, false).Add(c => c.State, state)
            .Add(c => c.UrlKey, "z").Add(c => c.ResultJson, OrderJson.Default.DataListResultOrder));
        return (cut, calls);
    }

    private (IRenderedComponent<BxDataList<Order>> Cut, Calls Calls) Start(FilterRule rule, string? prerenderZone, string? browserZone)
    {
        JSInterop.Setup<string?>("Blux.tools.timeZone").SetResult(browserZone);
        Persist(Base with { Filters = [rule] }, prerenderZone);
        return Render(Base with { Filters = [rule] }, browserZone);
    }

    [Fact]
    public void A_result_cut_in_utc_is_not_adopted_for_an_instant_in_another_zone()
    {
        var (cut, calls) = Start(Moment, prerenderZone: null, browserZone: "Europe/Paris");

        cut.WaitForAssertion(() => Assert.Equal(1, calls.Count));
        Assert.Equal("Europe/Paris", calls.LastZone);
    }

    [Fact]
    public void A_result_cut_in_utc_is_not_adopted_for_a_range_of_instants_in_another_zone()
    {
        var (cut, calls) = Start(MomentRange, prerenderZone: null, browserZone: "Europe/Paris");

        cut.WaitForAssertion(() => Assert.Equal(1, calls.Count));
    }

    [Fact]
    public void A_result_cut_in_utc_is_not_adopted_for_a_day_in_another_zone()
    {
        var (cut, calls) = Start(Day, prerenderZone: null, browserZone: "Asia/Tokyo");

        cut.WaitForAssertion(() => Assert.Equal(1, calls.Count));
    }

    [Fact]
    public void A_result_cut_in_the_zone_of_the_browser_is_adopted_and_loads_nothing()
    {
        var (cut, calls) = Start(Moment, prerenderZone: "Europe/Paris", browserZone: "Europe/Paris");

        cut.WaitForAssertion(() => Assert.Contains("Order 01", cut.Markup));
        Assert.Equal(0, calls.Count);
    }

    [Fact]
    public void A_result_cut_in_another_zone_than_the_one_of_the_browser_is_loaded_again()
    {
        var (cut, calls) = Start(Moment, prerenderZone: "Asia/Tokyo", browserZone: "Europe/Paris");

        cut.WaitForAssertion(() => Assert.Equal(1, calls.Count));
    }

    [Fact]
    public void Utc_on_both_sides_is_the_same_zone()
    {
        var (cut, calls) = Start(Moment, prerenderZone: null, browserZone: null);

        cut.WaitForAssertion(() => Assert.Contains("Order 01", cut.Markup));
        Assert.Equal(0, calls.Count);
    }

    [Fact]
    public void A_result_that_does_not_depend_on_a_zone_is_adopted_whatever_the_zones()
    {
        var (cut, calls) = Start(Number, prerenderZone: null, browserZone: "Europe/Paris");

        cut.WaitForAssertion(() => Assert.Contains("Order 01", cut.Markup));
        Assert.Equal(0, calls.Count);
    }

    // ---- the cookie of a static render

    private sealed class StaticContext : BluxToolsTestContext
    {
        public StaticContext() : base(interactive: false)
        {
        }

        public void Request(string? cookie, string? pathBase = null)
        {
            var http = new DefaultHttpContext();
            if (cookie is not null) http.Request.Headers.Cookie = cookie;
            if (pathBase is not null) http.Request.PathBase = pathBase;
            Services.GetRequiredService<IHttpContextAccessor>().HttpContext = http;
        }

        public string? ZoneOfTheRequest()
        {
            string? zone = null;
            DataListProvider<Order> provider = (request, _) =>
            {
                zone = request.TimeZone;
                return ValueTask.FromResult(new DataListResult<Order>([], 0));
            };
            Render<BxDataList<Order>>(p => p.Add(c => c.Schema, RangeData.Schema).Add(c => c.Provider, provider).Add(c => c.UrlSync, false));
            return zone;
        }
    }

    [Fact]
    public void A_static_render_cuts_days_in_the_zone_of_the_cookie()
    {
        using var context = new StaticContext();
        context.Request("blux-timezone=Asia/Tokyo");

        Assert.Equal("Asia/Tokyo", context.ZoneOfTheRequest());
    }

    [Fact]
    public void A_static_render_without_the_cookie_cuts_days_in_utc()
    {
        using var context = new StaticContext();
        context.Request("other=1");

        Assert.Null(context.ZoneOfTheRequest());
    }

    [Theory]
    [InlineData("blux-timezone=Nowhere/Land")]
    [InlineData("blux-timezone=")]
    [InlineData("blux-timezone=%3Cscript%3E")]
    public void A_cookie_with_an_unknown_zone_is_ignored(string cookie)
    {
        using var context = new StaticContext();
        context.Request(cookie);

        Assert.Null(context.ZoneOfTheRequest());
    }

    [Fact]
    public void Below_a_path_base_the_cookie_is_the_one_of_that_base()
    {
        using var context = new StaticContext();
        context.Request("blux-timezone=Asia/Tokyo; blux-timezone.sub~~=Europe/Paris", "/sub");

        Assert.Equal("Europe/Paris", context.ZoneOfTheRequest());
    }

    [Fact]
    public void Below_a_path_base_the_cookie_of_the_root_is_not_read()
    {
        using var context = new StaticContext();
        context.Request("blux-timezone=Asia/Tokyo", "/sub");

        Assert.Null(context.ZoneOfTheRequest());
    }

    [Fact]
    public void Without_a_request_there_is_no_zone()
    {
        using var context = new StaticContext();

        Assert.Null(context.ZoneOfTheRequest());
    }

    [Fact]
    public void A_static_tag_shows_an_instant_in_the_zone_of_the_cookie()
    {
        using var context = new StaticContext();
        context.Request("blux-timezone=Pacific/Auckland");
        context.NavigateTo("/t?f.created.after=2026-11-05T10:30:00Z");

        var cut = context.Render<DataListRangeHarness>();

        // 10:30 UTC is 23:30 on the 5th in Auckland (UTC+13).
        Assert.Contains("11/5/2026", cut.Find("[data-slot=filter-tag-button]").TextContent);
        Assert.Contains("11:30", cut.Find("[data-slot=filter-tag-button]").TextContent);
    }
}

internal static class StaticContextExtensions
{
    public static void NavigateTo(this BunitContext context, string url) =>
        ((BunitNavigationManager)context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()).NavigateTo(url);
}
