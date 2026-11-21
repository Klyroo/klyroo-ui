namespace Blux.UI.Tools.Query;

/// <summary>
/// The instant of a local day and time in a time zone, with one rule for the two cases a wall clock does not name one instant. The server (the day and unit bounds of <see cref="DateUnit"/>), the filter editor
/// of the data list and the reading of a posted date-time field all use it, so they cannot disagree on a day that starts in a gap.
/// </summary>
internal static class LocalInstants
{
    /// <summary>
    /// The instant of <paramref name="local"/> (the wall clock in <paramref name="zone"/>; its kind is ignored). A time in the gap of a clock change (02:15 when the clocks go from 02:00 to 03:00, or the whole of
    /// 2011-12-30 in Pacific/Apia) is the instant the clocks resume after the gap, so a day that starts in a gap starts when the clocks resume. An ambiguous time (02:30 when the clocks go back from 03:00 to 02:00)
    /// is the earlier instant, so the end bound of a range that falls in the repeated hour stops at its first pass.
    /// </summary>
    public static DateTimeOffset At(DateTime local, TimeZoneInfo zone)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local)) return GapEnd(local, zone);
        var offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Max() : zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset);
    }

    /// <summary>The instant of a local day and time, see <see cref="At(DateTime, TimeZoneInfo)"/>.</summary>
    public static DateTimeOffset At(DateOnly day, TimeOnly time, TimeZoneInfo zone) => At(day.ToDateTime(time), zone);

    // The instant the clocks resume after the gap that holds an invalid local time: the transition is found by bisection between the instant that local time would be before the change (which is after it) and a day earlier.
    private static DateTimeOffset GapEnd(DateTime local, TimeZoneInfo zone)
    {
        var before = zone.GetUtcOffset(local.AddDays(-1));
        var after = zone.GetUtcOffset(local.AddDays(1));
        var end = DateTime.SpecifyKind(local - before, DateTimeKind.Utc);
        long low = -86400, high = 0;
        while (high - low > 1)
        {
            var middle = low + (high - low) / 2;
            if (zone.GetUtcOffset(new DateTimeOffset(end.AddSeconds(middle))) == before) low = middle;
            else high = middle;
        }
        var resumes = end.AddSeconds(high);
        return new DateTimeOffset(DateTime.SpecifyKind(resumes + after, DateTimeKind.Unspecified), after);
    }
}
