using static Blux.UI.Tools.Query.Tests.TestData;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.Tests;

/// <summary>
/// One rule for a wall clock that names no instant (a gap) or two (an overlap), shared by the server (<see cref="DateUnit"/>), the filter editor and the reading of a posted date-time.
/// </summary>
public class LocalInstantsTests
{
    private static TimeZoneInfo Zone(string id) => TimeZoneInfo.FindSystemTimeZoneById(id);

    private static DateTimeOffset Instant(string text) => DateTimeOffset.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

    // A zone (UTC+1, UTC+1:45 in summer) whose clocks jump 45 minutes at the very start of 2026-06-10: 00:00 to 00:44 do not exist.
    private static TimeZoneInfo SyntheticGap()
    {
        var start = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 0, 0, 0), 6, 10);
        var end = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 1, 0, 0), 6, 20);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), TimeSpan.FromMinutes(45), start, end);
        return TimeZoneInfo.CreateCustomTimeZone("Test/Gap45", TimeSpan.FromHours(1), "Gap45", "Gap45", "Gap45 DST", [rule]);
    }

    [Theory]
    [InlineData("2026-03-29T02:00:00", "2026-03-29T03:00:00+02:00")]
    [InlineData("2026-03-29T02:15:00", "2026-03-29T03:00:00+02:00")]
    [InlineData("2026-03-29T02:59:00", "2026-03-29T03:00:00+02:00")]
    [InlineData("2026-03-29T01:59:00", "2026-03-29T01:59:00+01:00")]
    [InlineData("2026-03-29T03:00:00", "2026-03-29T03:00:00+02:00")]
    public void Paris_spring_a_time_in_the_gap_is_the_instant_the_clocks_resume(string local, string expected) =>
        Assert.Equal(Instant(expected), LocalInstants.At(DateTime.Parse(local), Zone("Europe/Paris")));

    [Theory]
    [InlineData("2026-10-25T02:00:00", "2026-10-25T02:00:00+02:00")]
    [InlineData("2026-10-25T02:30:00", "2026-10-25T02:30:00+02:00")]
    [InlineData("2026-10-25T02:59:00", "2026-10-25T02:59:00+02:00")]
    [InlineData("2026-10-25T03:00:00", "2026-10-25T03:00:00+01:00")]
    public void Paris_autumn_an_ambiguous_time_is_the_earlier_instant(string local, string expected) =>
        Assert.Equal(Instant(expected), LocalInstants.At(DateTime.Parse(local), Zone("Europe/Paris")));

    [Fact]
    public void Apia_skipped_a_whole_day_and_its_midnight_is_the_start_of_the_next_day()
    {
        // Samoa jumped from 2011-12-29 to 2011-12-31 (UTC-10 to UTC+14).
        var apia = Zone("Pacific/Apia");

        Assert.Equal(Instant("2011-12-31T00:00:00+14:00"), LocalInstants.At(new DateOnly(2011, 12, 30), TimeOnly.MinValue, apia));
    }

    [Fact]
    public void A_gap_of_45_minutes_at_midnight_ends_when_the_clocks_resume()
    {
        var zone = SyntheticGap();
        Assert.True(zone.IsInvalidTime(new DateTime(2026, 6, 10, 0, 20, 0)));

        // The old rule stepped 30 minutes and gave 01:00; the clocks resume at 00:45.
        Assert.Equal(Instant("2026-06-10T00:45:00+01:45"), LocalInstants.At(new DateOnly(2026, 6, 10), TimeOnly.MinValue, zone));
        Assert.Equal(Instant("2026-06-10T00:45:00+01:45"), LocalInstants.At(new DateOnly(2026, 6, 10), new TimeOnly(0, 44), zone));
        Assert.Equal(Instant("2026-06-10T00:45:00+01:45"), LocalInstants.At(new DateOnly(2026, 6, 10), new TimeOnly(0, 45), zone));
    }

    // ---- the server and the editor agree: the start of a day is the same instant

    [Fact]
    public void The_start_of_a_day_that_begins_in_a_gap_is_where_the_clocks_resume_on_the_server()
    {
        var zone = SyntheticGap();
        var unit = DateUnit.Of(new FilterValue.Date(new DateOnly(2026, 6, 10)), DateGranularity.Day, zone);

        Assert.Equal(Instant("2026-06-10T00:45:00+01:45"), unit.StartUtc);
        Assert.Equal(LocalInstants.At(new DateOnly(2026, 6, 10), TimeOnly.MinValue, zone), unit.StartUtc);
        Assert.Equal(LocalInstants.At(new DateOnly(2026, 6, 11), TimeOnly.MinValue, zone), unit.NextUtc);
    }

    [Fact]
    public void The_skipped_day_of_Apia_is_an_empty_range_that_ends_where_the_next_day_starts()
    {
        var apia = Zone("Pacific/Apia");
        var skipped = DateUnit.Of(new FilterValue.Date(new DateOnly(2011, 12, 30)), DateGranularity.Day, apia);
        var next = DateUnit.Of(new FilterValue.Date(new DateOnly(2011, 12, 31)), DateGranularity.Day, apia);

        Assert.Equal(Instant("2011-12-31T00:00:00+14:00"), skipped.StartUtc);
        Assert.Equal(skipped.StartUtc, skipped.NextUtc);
        Assert.Equal(next.StartUtc, skipped.NextUtc);
        Assert.Equal(LocalInstants.At(new DateOnly(2011, 12, 30), TimeOnly.MinValue, apia), skipped.StartUtc);
    }

    [Fact]
    public void A_row_at_the_start_of_the_day_after_the_skipped_day_matches_that_day_and_not_the_skipped_one()
    {
        var schema = DataListSchema.For<Event>(s => s.RowKey(e => e.Id).Field(e => e.On, f => f.Filter(DataListFilter.Date())).Field(e => e.When, f => f.Filter(DataListFilter.Date())));
        var rows = new[] { new Event(1, Instant("2011-12-31T00:00:00+14:00"), default, default) };

        int[] Run(DateOnly day)
        {
            var request = new DataListRequest(DataListPaging.None.Instance, [], DataListFilterGroup.And(Rule("when", Op.Is, new FilterValue.Date(day))), null, null, "Pacific/Apia");
            return [.. schema.ExecuteInMemory(rows, request).Items.Select(e => e.Id)];
        }

        Assert.Empty(Run(new DateOnly(2011, 12, 30)));
        Assert.Equal([1], Run(new DateOnly(2011, 12, 31)));
    }

    [Theory]
    [InlineData("Europe/Paris", "2026-03-29")]
    [InlineData("Europe/Paris", "2026-10-25")]
    [InlineData("America/Sao_Paulo", "2018-11-04")]
    [InlineData("Africa/Casablanca", "2026-02-18")]
    [InlineData("Australia/Lord_Howe", "2026-10-04")]
    public void The_bounds_of_a_day_are_those_of_the_shared_rule_in_any_zone(string id, string day)
    {
        var zone = Zone(id);
        var date = DateOnly.Parse(day);
        var unit = DateUnit.Of(new FilterValue.Date(date), DateGranularity.Day, zone);

        Assert.Equal(LocalInstants.At(date, TimeOnly.MinValue, zone), unit.StartUtc);
        Assert.Equal(LocalInstants.At(date.AddDays(1), TimeOnly.MinValue, zone), unit.NextUtc);
    }

    [Fact]
    public void A_day_that_is_23_or_25_hours_long_on_a_Paris_change_keeps_its_length()
    {
        var paris = Zone("Europe/Paris");

        var spring = DateUnit.Of(new FilterValue.Date(new DateOnly(2026, 3, 29)), DateGranularity.Day, paris);
        var autumn = DateUnit.Of(new FilterValue.Date(new DateOnly(2026, 10, 25)), DateGranularity.Day, paris);

        Assert.Equal(TimeSpan.FromHours(23), spring.NextUtc - spring.StartUtc);
        Assert.Equal(TimeSpan.FromHours(25), autumn.NextUtc - autumn.StartUtc);
    }
}
