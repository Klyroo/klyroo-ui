using Blux.UI.Server;
using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>
/// The static filter form of a date filter writes its fields in the zone of the cookie and the GET value comes back through the URL: applying a form must not move the filter, in UTC or in another zone,
/// and a whole-day bound must stay a whole day.
/// </summary>
public class DataListStaticDateFormTests
{
    private sealed class StaticContext : BluxToolsTestContext
    {
        public StaticContext() : base(interactive: false, configureServices: s => s.AddBluxServer().AddSingleton<TimeProvider>(new FixedClock(new DateTimeOffset(2026, 11, 21, 12, 0, 0, TimeSpan.Zero))))
        {
        }

        public void Cookie(string? zone)
        {
            var http = new DefaultHttpContext();
            if (zone is not null) http.Request.Headers.Cookie = "blux-timezone=" + zone;
            Services.GetRequiredService<IHttpContextAccessor>().HttpContext = http;
        }
    }

    private static DataListSchema<Order> SchemaFor(DateGranularity granularity) => DataListSchema.For<Order>(s => s
        .RowKey(o => o.Id)
        .Field(o => o.Id, f => f.Sortable())
        .Field(o => o.Created, f => f.Filter(DataListFilter.Date(granularity).WithLabel("Created")))
        .DefaultSort(o => o.Id));

    // As the app writes it in a URL: the offset has a "+" that must be escaped.
    private static string Iso(DateTimeOffset value) => Uri.EscapeDataString(value.ToString("O", System.Globalization.CultureInfo.InvariantCulture));

    // The URL the app writes for a rule, and the fields of the form the page renders for that URL.
    private static (string Name, string Type, string Value, string? Step)[] Fields(string? zone, DataListSchema<Order> schema, string query)
    {
        // A page of its own for each request, as a browser gets one.
        using var context = new StaticContext();
        context.Cookie(zone);
        context.NavigateTo("/t?" + query);
        using var cut = context.Render<DataListRangeHarness>(p => p.Add(c => c.Schema, schema));
        return [.. cut.FindAll("li[data-filter=created] form input:not([type=hidden])").Select(i => (i.GetAttribute("name")!, i.GetAttribute("type")!, i.GetAttribute("value") ?? "", i.GetAttribute("step")))];
    }

    // What the browser sends when the form is applied: the fields as a GET query string.
    private static string Posted(IEnumerable<(string Name, string Type, string Value, string? Step)> fields) =>
        string.Join('&', fields.Select(f => f.Name + "=" + Uri.EscapeDataString(f.Value)));

    private static IReadOnlyList<FilterRule> Rules(DataListSchema<Order> schema, string query, string? zone) =>
        DataListStateQuery.FromQuery(System.Web.HttpUtility.ParseQueryString(query).AllKeys.Select(k => KeyValuePair.Create(k!, (string?)System.Web.HttpUtility.ParseQueryString(query)[k])), schema, null, zone).Filters;

    public static TheoryData<string?, string, DateGranularity> Cases()
    {
        var data = new TheoryData<string?, string, DateGranularity>();
        foreach (var zone in new string?[] { null, "Europe/Paris" })
            foreach (var op in new[] { "is", "before", "after", "between" })
                foreach (var granularity in new[] { DateGranularity.Minute, DateGranularity.Hour })
                    data.Add(zone, op, granularity);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Applying_a_timed_form_keeps_the_instant_of_the_filter(string? zone, string op, DateGranularity granularity)
    {
        var tz = DataListDates.Zone(zone);
        var schema = SchemaFor(granularity);
        var from = DataListDates.Moment(new DateOnly(2026, 11, 5), new TimeOnly(13, 0), tz, granularity);
        var to = DataListDates.Moment(new DateOnly(2026, 11, 5), new TimeOnly(15, 0), tz, granularity);
        var query = op == "between" ? $"f.created.between={Iso(from)}..{Iso(to)}" : op == "is" ? $"f.created={Iso(from)}" : $"f.created.{op}={Iso(from)}";
        var expected = Rules(schema, query, zone);

        var first = Fields(zone, schema, query);
        // The field shows the wall clock of the zone: 13:00 in Paris is 12:00Z, so the form says 13:00 and not 12:00 or 14:00.
        Assert.All(first, f => Assert.Equal("datetime-local", f.Type));
        Assert.Equal(op == "between" ? ["2026-11-05T13:00", "2026-11-05T15:00"] : ["2026-11-05T13:00"], first.Select(f => f.Value));

        // Applying the form three times changes neither the fields nor the rule: no drift.
        var current = first;
        for (var i = 0; i < 3; i++)
        {
            var posted = Posted(current);
            Assert.Equal(expected, Rules(schema, posted, zone));
            current = Fields(zone, schema, posted);
            Assert.Equal(first.Select(f => f.Value), current.Select(f => f.Value));
        }
    }

    [Fact]
    public void A_posted_time_without_an_offset_in_paris_is_not_read_as_utc()
    {
        const string zone = "Europe/Paris";
        var schema = SchemaFor(DateGranularity.Minute);

        // The page is rendered from a URL the form wrote: 14:00 local in November is 13:00Z, so the tag and the field say 14:00 and the rule holds 13:00Z.
        var fields = Fields(zone, schema, "f.created.after=2026-11-05T14:00");

        Assert.Equal("2026-11-05T14:00", Assert.Single(fields).Value);
        var rule = Assert.IsType<FilterRule>(Assert.Single(Rules(schema, "f.created.after=2026-11-05T14:00", "Europe/Paris")));
        Assert.Equal(new FilterValue.Moment(new DateTimeOffset(2026, 11, 5, 13, 0, 0, TimeSpan.Zero)), rule.Value);
    }

    [Theory]
    [InlineData(null, "is", DateGranularity.Minute)]
    [InlineData("Europe/Paris", "is", DateGranularity.Minute)]
    [InlineData("Europe/Paris", "before", DateGranularity.Hour)]
    [InlineData("Europe/Paris", "after", DateGranularity.Minute)]
    [InlineData("Europe/Paris", "between", DateGranularity.Hour)]
    public void A_whole_day_bound_comes_back_as_a_whole_day(string? zone, string op, DateGranularity granularity)
    {
        var schema = SchemaFor(granularity);
        var query = op == "between" ? "f.created.between=2026-11-05..2026-11-07" : op == "is" ? "f.created=2026-11-05" : $"f.created.{op}=2026-11-05";
        var expected = Rules(schema, query, zone);

        var fields = Fields(zone, schema, query);

        // A date-time field cannot say "all day": the bound gets a date field, whose value reads back as the same day.
        Assert.All(fields, f => Assert.Equal("date", f.Type));
        Assert.All(fields, f => Assert.Null(f.Step));
        Assert.Equal(op == "between" ? ["2026-11-05", "2026-11-07"] : ["2026-11-05"], fields.Select(f => f.Value));
        var posted = Posted(fields);
        Assert.Equal(expected, Rules(schema, posted, zone));
        Assert.Equal(fields, Fields(zone, schema, posted));
        Assert.All(Assert.IsType<FilterRule>(Assert.Single(Rules(schema, posted, zone))).Value switch
        {
            FilterValue.Range range => new[] { range.From, range.To },
            var one => [one],
        }, bound => Assert.IsType<FilterValue.Date>(bound));
    }

    [Fact]
    public void A_range_with_a_day_and_a_time_keeps_each_bound_as_it_is()
    {
        const string zone = "Europe/Paris";
        var schema = SchemaFor(DateGranularity.Minute);
        var to = DataListDates.Moment(new DateOnly(2026, 11, 7), new TimeOnly(18, 30), DataListDates.Zone("Europe/Paris"), DateGranularity.Minute);
        var query = $"f.created.between=2026-11-05..{Iso(to)}";

        var fields = Fields(zone, schema, query);

        Assert.Equal(["date", "datetime-local"], fields.Select(f => f.Type));
        Assert.Equal(["2026-11-05", "2026-11-07T18:30"], fields.Select(f => f.Value));
        Assert.Equal(Rules(schema, query, "Europe/Paris"), Rules(schema, Posted(fields), "Europe/Paris"));
    }
}
