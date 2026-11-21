using System.Collections.Concurrent;

namespace Blux.UI.Tools.Query;

/// <summary>
/// The half-open range <c>[start, next)</c> that a date filter value stands for, in the user's time zone: a <see cref="FilterValue.Date"/> is that local day
/// (23 or 25 hours long on a daylight saving change), a <see cref="FilterValue.Moment"/> is the hour, minute or second that contains it (or the local day for day
/// granularity). Day boundaries that fall in a daylight saving gap start at the first valid time after the gap, and in an overlap at the earlier instant.
/// Hour, minute and second units are cut using the zone's offset at the instant, so a zone with a half-hour offset gets local hours.
/// </summary>
internal readonly record struct DateUnit(DateTimeOffset StartUtc, DateTimeOffset NextUtc, DateOnly StartDay, DateOnly? NextDay)
{
    public static DateUnit Of(FilterValue value, DateGranularity granularity, TimeZoneInfo zone)
    {
        switch (value)
        {
            case FilterValue.Date date:
                return ForDay(date.Value, zone);
            case FilterValue.Moment moment:
                var offset = zone.GetUtcOffset(moment.Value);
                var local = ToLocal(moment.Value, offset);
                if (granularity == DateGranularity.Day) return ForDay(DateOnly.FromDateTime(local), zone);
                var unit = granularity switch
                {
                    DateGranularity.Hour => TimeSpan.TicksPerHour,
                    DateGranularity.Minute => TimeSpan.TicksPerMinute,
                    _ => TimeSpan.TicksPerSecond,
                };
                var startLocalTicks = local.Ticks - local.Ticks % unit;
                var start = FromLocal(new DateTime(startLocalTicks), offset);
                var next = start.Ticks > DateTimeOffset.MaxValue.Ticks - unit ? DateTimeOffset.MaxValue : start.AddTicks(unit);
                var day = DateOnly.FromDateTime(local);
                return new DateUnit(start, next, day, day == DateOnly.MaxValue ? null : day.AddDays(1));
            default:
                throw new InvalidOperationException("A date filter value must be a Date or a Moment.");
        }
    }

    private static DateUnit ForDay(DateOnly day, TimeZoneInfo zone)
    {
        var nextDay = day == DateOnly.MaxValue ? (DateOnly?)null : day.AddDays(1);
        return new DateUnit(
            LocalStart(day, zone),
            nextDay is { } n ? LocalStart(n, zone) : DateTimeOffset.MaxValue,
            day,
            nextDay);
    }

    private static DateTime ToLocal(DateTimeOffset instant, TimeSpan offset)
    {
        try { return instant.UtcDateTime + offset; }
        catch (ArgumentOutOfRangeException) { return instant.UtcDateTime; }
    }

    private static DateTimeOffset FromLocal(DateTime local, TimeSpan offset)
    {
        try { return new DateTimeOffset(local, offset); }
        catch (ArgumentOutOfRangeException) { return DateTimeOffset.MinValue; }
    }

    /// <summary>Midnight of a local day as an instant (the instant the clocks resume when midnight is in a gap, see <see cref="LocalInstants"/>).</summary>
    private static DateTimeOffset LocalStart(DateOnly day, TimeZoneInfo zone)
    {
        try { return LocalInstants.At(day, TimeOnly.MinValue, zone); }
        catch (ArgumentOutOfRangeException) { return DateTimeOffset.MinValue; }
    }
}

/// <summary>Resolves the IANA time zone ids of requests.</summary>
internal static class TimeZones
{
    private static readonly ConcurrentDictionary<string, TimeZoneInfo?> Known = new(StringComparer.Ordinal);

    /// <summary>The zone with this id (IANA, or whatever the platform accepts), or <see langword="null"/> when there is none.</summary>
    public static TimeZoneInfo? Resolve(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 64) return null;
        if (Known.TryGetValue(id, out var zone)) return zone;
        TimeZoneInfo.TryFindSystemTimeZoneById(id, out zone);
        if (Known.Count < 256) Known[id] = zone;
        return zone;
    }
}
