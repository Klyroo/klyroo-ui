using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.Tests;

public sealed record Appointment(int Id, DateTimeOffset At, DateOnly Day);

/// <summary>
/// A date-time of a URL without an offset is the wall clock of the zone of the request (what a <c>datetime-local</c> field of a static form posts), never UTC by default for a request that has a zone.
/// </summary>
public class StateQueryZoneTests
{
    private static DataListSchema<Appointment> SchemaFor(DateGranularity granularity) => DataListSchema.For<Appointment>(s => s
        .RowKey(a => a.Id)
        .Field(a => a.At, f => f.Filter(DataListFilter.Date(granularity)))
        .Field(a => a.Day, f => f.Filter(DataListFilter.Date())));

    private static DateTimeOffset Instant(string text) => DateTimeOffset.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

    private static FilterRule? ParseAt(string key, Op op, string raw, string? zone, DateGranularity granularity = DateGranularity.Minute)
    {
        var state = DataListStateQuery.FromQuery([KeyValuePair.Create(key, (string?)raw)], SchemaFor(granularity), null, zone);
        return state.Filters.OfType<FilterRule>().SingleOrDefault();
    }

    [Theory]
    [InlineData("Europe/Paris", "2026-11-05T13:00:00+00:00")]
    [InlineData("UTC", "2026-11-05T14:00:00+00:00")]
    [InlineData(null, "2026-11-05T14:00:00+00:00")]
    [InlineData("Nowhere/Land", "2026-11-05T14:00:00+00:00")]
    [InlineData("Asia/Tokyo", "2026-11-05T05:00:00+00:00")]
    public void A_date_time_without_an_offset_is_the_wall_clock_of_the_zone(string? zone, string expected)
    {
        var rule = ParseAt("f.at.after", Op.After, "2026-11-05T14:00", zone);

        Assert.Equal(new FilterValue.Moment(Instant(expected)), rule!.Value);
    }

    [Fact]
    public void Without_a_zone_the_reading_is_the_one_of_the_overload_that_has_none()
    {
        var pairs = new[] { KeyValuePair.Create("f.at.after", (string?)"2026-11-05T14:00") };

        Assert.Equal(DataListStateQuery.FromQuery(pairs, SchemaFor(DateGranularity.Minute)), DataListStateQuery.FromQuery(pairs, SchemaFor(DateGranularity.Minute), null, null));
    }

    [Theory]
    [InlineData("2026-11-05T12:00:00Z")]
    [InlineData("2026-11-05T13:00:00+01:00")]
    public void A_value_with_an_offset_is_that_instant_in_any_zone(string raw)
    {
        var rule = ParseAt("f.at.after", Op.After, raw, "Asia/Tokyo");

        Assert.Equal(new FilterValue.Moment(Instant("2026-11-05T12:00:00Z")), rule!.Value);
    }

    [Fact]
    public void A_day_is_a_day_whatever_the_zone()
    {
        var rule = ParseAt("f.day", Op.Is, "2026-11-05", "Asia/Tokyo");

        Assert.Equal(new FilterValue.Date(new DateOnly(2026, 11, 5)), rule!.Value);
    }

    [Fact]
    public void A_range_sent_as_two_fields_reads_both_ends_in_the_zone()
    {
        var state = DataListStateQuery.FromQuery(
            [KeyValuePair.Create("f.at.from", (string?)"2026-11-05T13:00"), KeyValuePair.Create("f.at.to", (string?)"2026-11-05T15:00")],
            SchemaFor(DateGranularity.Minute), null, "Europe/Paris");

        var rule = Assert.IsType<FilterRule>(Assert.Single(state.Filters));
        Assert.Equal(new FilterValue.Range(new FilterValue.Moment(Instant("2026-11-05T12:00:00Z")), new FilterValue.Moment(Instant("2026-11-05T14:00:00Z"))), rule.Value);
    }

    [Fact]
    public void A_range_in_one_field_reads_both_ends_in_the_zone_and_a_date_end_stays_a_date()
    {
        var rule = ParseAt("f.at.between", Op.Between, "2026-11-05T13:00..2026-11-07", "Europe/Paris");

        Assert.Equal(new FilterValue.Range(new FilterValue.Moment(Instant("2026-11-05T12:00:00Z")), new FilterValue.Date(new DateOnly(2026, 11, 7))), rule!.Value);
    }

    [Fact]
    public void An_hour_filter_cuts_the_minutes_of_a_posted_time()
    {
        var rule = ParseAt("f.at.after", Op.After, "2026-11-05T13:45", "Europe/Paris", DateGranularity.Hour);

        Assert.Equal(new FilterValue.Moment(Instant("2026-11-05T12:00:00Z")), rule!.Value);
    }

    [Fact]
    public void A_posted_time_in_a_clock_gap_is_the_instant_the_clocks_resume()
    {
        var rule = ParseAt("f.at.after", Op.After, "2026-03-29T02:30", "Europe/Paris");

        Assert.Equal(new FilterValue.Moment(Instant("2026-03-29T03:00:00+02:00")), rule!.Value);
    }

    [Theory]
    [InlineData("Europe/Paris", DateGranularity.Minute)]
    [InlineData("Europe/Paris", DateGranularity.Hour)]
    [InlineData("UTC", DateGranularity.Minute)]
    [InlineData("UTC", DateGranularity.Hour)]
    public void A_state_written_to_a_url_reads_back_to_itself_in_the_zone_of_the_request(string zone, DateGranularity granularity)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(zone);
        var from = new FilterValue.Moment(LocalInstants.At(new DateOnly(2026, 11, 5), new TimeOnly(13, 0), tz));
        var to = new FilterValue.Moment(LocalInstants.At(new DateOnly(2026, 11, 5), new TimeOnly(15, 0), tz));
        var schema = SchemaFor(granularity);
        foreach (var rule in new[]
                 {
                     new FilterRule("at", Op.Is, from), new FilterRule("at", Op.Before, from), new FilterRule("at", Op.After, from), new FilterRule("at", Op.Between, new FilterValue.Range(from, to)),
                 })
        {
            var state = schema.DefaultState with { Filters = [rule] };
            var query = DataListStateQuery.ToQuery(state, schema).Select(p => KeyValuePair.Create(p.Key, (string?)p.Value));

            Assert.Equal(state.Filters, DataListStateQuery.FromQuery(query, schema, null, zone).Filters);
        }
    }
}
