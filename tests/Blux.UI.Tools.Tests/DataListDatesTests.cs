using System.Globalization;
using Blux.UI.Tools.Query;

namespace Blux.UI.Tools.Tests;

/// <summary>The date arithmetic of the date filter editors: presets, the day it is in a time zone, local times and instants.</summary>
public class DataListDatesTests
{
    private static readonly DateOnly Today = new(2026, 11, 21);

    [Fact]
    public void Single_date_presets_count_from_today()
    {
        Assert.Equal(new DateOnly(2026, 11, 21), DataListDates.Day(DatePreset.Today, Today));
        Assert.Equal(new DateOnly(2026, 11, 20), DataListDates.Day(DatePreset.Yesterday, Today));
        Assert.Equal(new DateOnly(2026, 11, 1), DataListDates.Day(DatePreset.StartOfMonth, Today));
        Assert.Equal(new DateOnly(2026, 11, 14), DataListDates.Day(DatePreset.SevenDaysAgo, Today));
        Assert.Equal(new DateOnly(2026, 1, 1), DataListDates.Day(DatePreset.StartOfYear, Today));
    }

    [Fact]
    public void Range_presets_include_both_ends_and_the_months_are_whole()
    {
        Assert.Equal((Today, Today), DataListDates.Range(DatePreset.Today, Today));
        Assert.Equal((new DateOnly(2026, 11, 15), Today), DataListDates.Range(DatePreset.Last7Days, Today));
        Assert.Equal((new DateOnly(2026, 10, 23), Today), DataListDates.Range(DatePreset.Last30Days, Today));
        Assert.Equal((new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30)), DataListDates.Range(DatePreset.ThisMonth, Today));
        Assert.Equal((new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)), DataListDates.Range(DatePreset.LastMonth, Today));
        Assert.Equal((new DateOnly(2026, 1, 1), Today), DataListDates.Range(DatePreset.YearToDate, Today));
    }

    [Fact]
    public void Last_month_of_january_is_december_of_the_year_before_and_a_leap_february_has_29_days()
    {
        Assert.Equal((new DateOnly(2025, 12, 1), new DateOnly(2025, 12, 31)), DataListDates.Range(DatePreset.LastMonth, new DateOnly(2026, 1, 15)));
        Assert.Equal((new DateOnly(2028, 2, 1), new DateOnly(2028, 2, 29)), DataListDates.Range(DatePreset.ThisMonth, new DateOnly(2028, 2, 10)));
    }

    [Theory]
    [InlineData(FilterOperator.Is, "Today,Yesterday,StartOfMonth")]
    [InlineData(FilterOperator.IsNot, "Today,Yesterday,StartOfMonth")]
    [InlineData(FilterOperator.Before, "Today,SevenDaysAgo,StartOfMonth")]
    [InlineData(FilterOperator.After, "Today,SevenDaysAgo,StartOfYear")]
    [InlineData(FilterOperator.Between, "Today,Last7Days,Last30Days,ThisMonth,LastMonth,YearToDate")]
    public void The_presets_of_an_operator(FilterOperator op, string expected)
    {
        Assert.Equal(expected, string.Join(',', DataListDates.PresetsFor(op)));
    }

    [Fact]
    public void Today_is_the_day_in_the_zone()
    {
        var now = new DateTimeOffset(2026, 11, 21, 23, 30, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2026, 11, 21), DataListDates.Today(now, TimeZoneInfo.Utc));
        Assert.Equal(new DateOnly(2026, 11, 22), DataListDates.Today(now, DataListDates.Zone("Europe/Paris")));
        Assert.Equal(new DateOnly(2026, 11, 21), DataListDates.Today(now, DataListDates.Zone("America/New_York")));
    }

    [Fact]
    public void An_unknown_zone_is_utc()
    {
        Assert.Equal(TimeZoneInfo.Utc, DataListDates.Zone(null));
        Assert.Equal(TimeZoneInfo.Utc, DataListDates.Zone("Nowhere/Land"));
        Assert.Equal(TimeZoneInfo.Utc, DataListDates.Zone(new string('x', 100)));
    }

    [Fact]
    public void A_moment_is_the_instant_of_the_local_day_and_time_and_reads_back()
    {
        var zone = DataListDates.Zone("Europe/Paris");

        var moment = DataListDates.Moment(new DateOnly(2026, 7, 1), new TimeOnly(14, 45), zone, DateGranularity.Minute);

        Assert.Equal(new DateTimeOffset(2026, 7, 1, 12, 45, 0, TimeSpan.Zero), moment);
        Assert.Equal(TimeSpan.FromHours(2), moment.Offset);
        Assert.Equal((new DateOnly(2026, 7, 1), new TimeOnly(14, 45)), DataListDates.Local(moment, zone));
    }

    [Fact]
    public void An_hour_granularity_drops_the_minutes()
    {
        var moment = DataListDates.Moment(new DateOnly(2026, 11, 5), new TimeOnly(14, 45), TimeZoneInfo.Utc, DateGranularity.Hour);

        Assert.Equal(new DateTimeOffset(2026, 11, 5, 14, 0, 0, TimeSpan.Zero), moment);
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(2, 15)]
    [InlineData(2, 59)]
    public void Any_time_in_the_spring_gap_of_Paris_is_the_first_time_after_it(int hour, int minute)
    {
        var zone = DataListDates.Zone("Europe/Paris");

        // The clocks go from 02:00 to 03:00 on 2026-03-29: every time in between is 03:00 (+02:00).
        var moment = DataListDates.Moment(new DateOnly(2026, 3, 29), new TimeOnly(hour, minute), zone, DateGranularity.Minute);

        Assert.Equal(new DateTimeOffset(2026, 3, 29, 3, 0, 0, TimeSpan.FromHours(2)), moment);
    }

    [Fact]
    public void The_times_around_the_spring_gap_are_not_moved()
    {
        var zone = DataListDates.Zone("Europe/Paris");

        Assert.Equal(new DateTimeOffset(2026, 3, 29, 1, 59, 0, TimeSpan.FromHours(1)), DataListDates.Moment(new DateOnly(2026, 3, 29), new TimeOnly(1, 59), zone, DateGranularity.Minute));
        Assert.Equal(new DateTimeOffset(2026, 3, 29, 3, 0, 0, TimeSpan.FromHours(2)), DataListDates.Moment(new DateOnly(2026, 3, 29), new TimeOnly(3, 0), zone, DateGranularity.Minute));
    }

    [Fact]
    public void A_gap_that_is_not_an_hour_long_ends_where_the_clocks_resume()
    {
        // Lord Howe Island moves its clocks by 30 minutes: 02:00 becomes 02:30 on 2026-10-04.
        var zone = DataListDates.Zone("Australia/Lord_Howe");

        var moment = DataListDates.Moment(new DateOnly(2026, 10, 4), new TimeOnly(2, 10), zone, DateGranularity.Minute);

        Assert.Equal(new DateTimeOffset(2026, 10, 4, 2, 30, 0, TimeSpan.FromHours(11)), moment);
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(2, 30)]
    [InlineData(2, 59)]
    public void Any_time_in_the_repeated_hour_of_the_autumn_change_in_Paris_is_the_earlier_instant(int hour, int minute)
    {
        var zone = DataListDates.Zone("Europe/Paris");

        // The clocks go back from 03:00 to 02:00 on 2026-10-25: 02:xx happens at +02:00 and again at +01:00, and the earlier one is taken.
        var moment = DataListDates.Moment(new DateOnly(2026, 10, 25), new TimeOnly(hour, minute), zone, DateGranularity.Minute);

        Assert.Equal(new DateTimeOffset(2026, 10, 25, hour, minute, 0, TimeSpan.FromHours(2)), moment);
    }

    [Fact]
    public void The_times_around_the_autumn_change_are_not_moved()
    {
        var zone = DataListDates.Zone("Europe/Paris");

        Assert.Equal(new DateTimeOffset(2026, 10, 25, 1, 59, 0, TimeSpan.FromHours(2)), DataListDates.Moment(new DateOnly(2026, 10, 25), new TimeOnly(1, 59), zone, DateGranularity.Minute));
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 3, 0, 0, TimeSpan.FromHours(1)), DataListDates.Moment(new DateOnly(2026, 10, 25), new TimeOnly(3, 0), zone, DateGranularity.Minute));
    }

    [Fact]
    public void A_bound_is_a_day_without_a_time_or_a_day_and_time_in_the_zone()
    {
        var zone = DataListDates.Zone("Pacific/Auckland");

        Assert.Equal((new DateOnly(2026, 11, 5), (TimeOnly?)null), DataListDates.Bound(new FilterValue.Date(new DateOnly(2026, 11, 5)), zone));
        Assert.Equal((new DateOnly(2026, 11, 5), (TimeOnly?)new TimeOnly(23, 30)), DataListDates.Bound(new FilterValue.Moment(new DateTimeOffset(2026, 11, 5, 10, 30, 0, TimeSpan.Zero)), zone));
        Assert.Null(DataListDates.Bound(null, zone));
        Assert.Null(DataListDates.Bound(new FilterValue.Number(3), zone));
    }

    [Fact]
    public void Dates_are_written_in_the_culture_with_the_hour_alone_for_an_hour_filter()
    {
        var zone = TimeZoneInfo.Utc;
        var moment = new FilterValue.Moment(new DateTimeOffset(2026, 11, 5, 14, 45, 30, TimeSpan.Zero));
        var en = new CultureInfo("en-US");
        var fr = new CultureInfo("fr-FR");

        Assert.Equal("11/5/2026", DataListDates.Format(new FilterValue.Date(new DateOnly(2026, 11, 5)), DateGranularity.Day, en, zone));
        Assert.Equal("11/5/2026 2:45 PM", Plain(DataListDates.Format(moment, DateGranularity.Minute, en, zone)));
        Assert.Equal("11/5/2026 2:00 PM", Plain(DataListDates.Format(moment, DateGranularity.Hour, en, zone)));
        Assert.Equal("11/5/2026 2:45:30 PM", Plain(DataListDates.Format(moment, DateGranularity.Second, en, zone)));
        Assert.Equal("05/11/2026 14:45", DataListDates.Format(moment, DateGranularity.Minute, fr, zone));
        Assert.Equal("", DataListDates.Format(null, DateGranularity.Day, en, zone));
    }

    // ICU writes a narrow no-break space before AM and PM.
    private static string Plain(string text) => text.Replace('\u202f', ' ');
}
