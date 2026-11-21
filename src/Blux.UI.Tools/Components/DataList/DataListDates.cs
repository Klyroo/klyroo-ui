using System.Globalization;
using Blux.UI.Tools.Query;

namespace Blux.UI.Tools;

/// <summary>The presets of a date filter editor.</summary>
internal enum DatePreset
{
    Today,
    Yesterday,
    StartOfMonth,
    SevenDaysAgo,
    StartOfYear,
    Last7Days,
    Last30Days,
    ThisMonth,
    LastMonth,
    YearToDate,
}

/// <summary>
/// The date arithmetic of the date filter editors: the day it is in the time zone of the user, the presets, and the conversions between a local day and time
/// (what the editor shows) and the instant a <see cref="FilterValue.Moment"/> carries.
/// </summary>
internal static class DataListDates
{
    /// <summary>The presets an editor lists for an operator: three for a single date (by operator), six for a range.</summary>
    public static IReadOnlyList<DatePreset> PresetsFor(FilterOperator op) => op switch
    {
        FilterOperator.Between => [DatePreset.Today, DatePreset.Last7Days, DatePreset.Last30Days, DatePreset.ThisMonth, DatePreset.LastMonth, DatePreset.YearToDate],
        FilterOperator.Before => [DatePreset.Today, DatePreset.SevenDaysAgo, DatePreset.StartOfMonth],
        FilterOperator.After => [DatePreset.Today, DatePreset.SevenDaysAgo, DatePreset.StartOfYear],
        _ => [DatePreset.Today, DatePreset.Yesterday, DatePreset.StartOfMonth],
    };

    /// <summary>The day of a single date preset.</summary>
    public static DateOnly Day(DatePreset preset, DateOnly today) => preset switch
    {
        DatePreset.Yesterday => today.AddDays(-1),
        DatePreset.StartOfMonth => new DateOnly(today.Year, today.Month, 1),
        DatePreset.SevenDaysAgo => today.AddDays(-7),
        DatePreset.StartOfYear => new DateOnly(today.Year, 1, 1),
        _ => today,
    };

    /// <summary>The first and last day of a range preset, both included: the last seven (thirty) days end today and count it, a month is whole, the year runs to today.</summary>
    public static (DateOnly From, DateOnly To) Range(DatePreset preset, DateOnly today)
    {
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
        return preset switch
        {
            DatePreset.Last7Days => (today.AddDays(-6), today),
            DatePreset.Last30Days => (today.AddDays(-29), today),
            DatePreset.ThisMonth => (firstOfMonth, firstOfMonth.AddMonths(1).AddDays(-1)),
            DatePreset.LastMonth => (firstOfMonth.AddMonths(-1), firstOfMonth.AddDays(-1)),
            DatePreset.YearToDate => (new DateOnly(today.Year, 1, 1), today),
            _ => (today, today),
        };
    }

    /// <summary>The text of a preset.</summary>
    public static string Label(DatePreset preset, BxToolsMessages messages) => preset switch
    {
        DatePreset.Today => messages.DatePresetToday,
        DatePreset.Yesterday => messages.DatePresetYesterday,
        DatePreset.StartOfMonth => messages.DatePresetStartOfMonth,
        DatePreset.SevenDaysAgo => messages.DatePreset7DaysAgo,
        DatePreset.StartOfYear => messages.DatePresetStartOfYear,
        DatePreset.Last7Days => messages.DatePresetLast7Days,
        DatePreset.Last30Days => messages.DatePresetLast30Days,
        DatePreset.ThisMonth => messages.DatePresetThisMonth,
        DatePreset.LastMonth => messages.DatePresetLastMonth,
        _ => messages.DatePresetYearToDate,
    };

    /// <summary>The id if this machine knows a zone by that name (at most 64 characters), else <see langword="null"/>.</summary>
    public static string? ValidId(string? id) =>
        !string.IsNullOrWhiteSpace(id) && id.Length <= 64 && TimeZoneInfo.TryFindSystemTimeZoneById(id, out _) ? id : null;

    /// <summary>Whether two ids stand for the same zone (an absent or unknown id is UTC).</summary>
    public static bool SameZone(string? a, string? b) => Zone(a).Id == Zone(b).Id;

    /// <summary>The zone with this IANA id, or UTC when there is none (a render with no browser, an id the platform does not know).</summary>
    public static TimeZoneInfo Zone(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id) && id.Length <= 64 && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)) return zone;
        return TimeZoneInfo.Utc;
    }

    /// <summary>The calendar day it is in <paramref name="zone"/> at <paramref name="now"/>.</summary>
    public static DateOnly Today(DateTimeOffset now, TimeZoneInfo zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);

    /// <summary>
    /// The instant of a local day and time (cut to the hour for <see cref="DateGranularity.Hour"/>). A time in the gap of a clock change is the instant the clocks resume, and an ambiguous time the earlier instant
    /// (see <see cref="LocalInstants"/>, which the server uses for the same cases, so the editor and the filter agree).
    /// </summary>
    public static DateTimeOffset Moment(DateOnly day, TimeOnly time, TimeZoneInfo zone, DateGranularity granularity) =>
        LocalInstants.At(day, new TimeOnly(time.Hour, granularity == DateGranularity.Hour ? 0 : time.Minute), zone);

    /// <summary>The local day and time of an instant in <paramref name="zone"/>.</summary>
    public static (DateOnly Day, TimeOnly Time) Local(DateTimeOffset moment, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTime(moment, zone).DateTime;
        return (DateOnly.FromDateTime(local), TimeOnly.FromDateTime(local));
    }

    /// <summary>The day of a bound of a date rule (<see cref="FilterValue.Date"/> or <see cref="FilterValue.Moment"/> in <paramref name="zone"/>), or null.</summary>
    public static (DateOnly Day, TimeOnly? Time)? Bound(FilterValue? value, TimeZoneInfo zone)
    {
        switch (value)
        {
            case FilterValue.Date d:
                return (d.Value, null);
            case FilterValue.Moment m:
                var local = Local(m.Value, zone);
                return (local.Day, local.Time);
            default:
                return null;
        }
    }

    /// <summary>The text of a date value in <paramref name="culture"/>: the day for a date, the day and time in <paramref name="zone"/> for a moment (the hour alone for hour granularity).</summary>
    public static string Format(FilterValue? value, DateGranularity granularity, CultureInfo culture, TimeZoneInfo zone)
    {
        switch (value)
        {
            case FilterValue.Date d:
                return d.Value.ToString("d", culture);
            case FilterValue.Moment m:
                var (day, time) = Local(m.Value, zone);
                var pattern = granularity switch
                {
                    DateGranularity.Hour => culture.DateTimeFormat.ShortTimePattern,
                    DateGranularity.Second => culture.DateTimeFormat.LongTimePattern,
                    _ => culture.DateTimeFormat.ShortTimePattern,
                };
                var clock = granularity == DateGranularity.Hour ? new DateTime(day.Year, day.Month, day.Day, time.Hour, 0, 0) : day.ToDateTime(time);
                return day.ToString("d", culture) + " " + clock.ToString(pattern, culture);
            default:
                return "";
        }
    }
}
