using System.Globalization;

namespace Klyroo.UI;

/// <summary>One event of the schedule layout, with what the layout derives from its dates.</summary>
/// <param name="Item">The event.</param>
/// <param name="Start">The start, in its own offset, or <see langword="null"/> for an event without a date.</param>
/// <param name="End">The end, in its own offset, or <see langword="null"/> when there is none or it is not after the start.</param>
/// <param name="AllDay">Whether the event lasts whole days: it starts at midnight and has no end, or ends at midnight or at the end of a day.</param>
/// <param name="Days">The number of days an all-day event covers: an end at midnight is excluded, as in calendar files, and an end at the end of a day is included (1 for any other event).</param>
/// <param name="Minutes">The length in minutes of an event with an end that is not all-day.</param>
internal sealed record ScheduleEvent(KlyTimelineItem Item, DateTimeOffset? Start, DateTimeOffset? End, bool AllDay, int Days, int? Minutes)
{
    public DateOnly? Day => Start is { } start ? DateOnly.FromDateTime(start.DateTime) : null;
}

/// <summary>The events of one calendar day, or the events without a date when <paramref name="Day"/> is <see langword="null"/>.</summary>
internal sealed record ScheduleDay(DateOnly? Day, DateTimeOffset? Sample, IReadOnlyList<ScheduleEvent> Events);

// The pure part of the schedule layout: sorting by start, deriving the end, the length and the all-day state, and grouping by day.
internal static class TimelineSchedule
{
    private static readonly TimeSpan EndOfDay = new(23, 59, 0);

    public static ScheduleEvent Describe(KlyTimelineItem item)
    {
        if (item.At is not { } start) return new ScheduleEvent(item, null, null, false, 1, null);

        // The end keeps its own offset: across a change to or from daylight saving time, an event of the 28th to the 30th at
        // midnight still ends at midnight, and a meeting that ends at 10:00 local time reads 10:00. Only the length compares instants.
        DateTimeOffset? end = item.EndAt is { } rawEnd && rawEnd > start ? rawEnd : null;
        var startsAtMidnight = start.TimeOfDay == TimeSpan.Zero;
        var allDay = startsAtMidnight && (end is null || end.Value.TimeOfDay == TimeSpan.Zero || end.Value.TimeOfDay >= EndOfDay);
        if (allDay)
        {
            var days = 1;
            if (end is { } last)
            {
                // An end at midnight is the start of the next day, excluded as in iCal: the 12th to the 13th at midnight is one day.
                var span = DateOnly.FromDateTime(last.DateTime).DayNumber - DateOnly.FromDateTime(start.DateTime).DayNumber;
                days = Math.Max(1, last.TimeOfDay == TimeSpan.Zero ? span : span + 1);
            }
            return new ScheduleEvent(item, start, end, true, days, null);
        }
        int? minutes = end is { } timedEnd ? (int)Math.Min(int.MaxValue, Math.Round((timedEnd - start).TotalMinutes)) : null;
        return new ScheduleEvent(item, start, end, false, 1, minutes);
    }

    // By day, then all-day events first, then by instant. Events without a date come last. Equal events keep their order.
    public static IReadOnlyList<ScheduleEvent> Sort(IReadOnlyList<KlyTimelineItem>? items)
    {
        if (items is null || items.Count == 0) return [];
        return items
            .Select(Describe)
            .OrderBy(e => e.Start is null ? 1 : 0)
            .ThenBy(e => e.Day)
            .ThenBy(e => e.AllDay ? 0 : 1)
            .ThenBy(e => e.Start?.UtcDateTime)
            .ToList();
    }

    // Groups consecutive events of a sorted list by calendar day. An event spanning several days is listed once, on its first day.
    public static IReadOnlyList<ScheduleDay> Group(IReadOnlyList<ScheduleEvent> sorted)
    {
        var days = new List<ScheduleDay>();
        var current = new List<ScheduleEvent>();
        DateOnly? currentDay = null;
        foreach (var e in sorted)
        {
            if (current.Count > 0 && e.Day != currentDay)
            {
                days.Add(new ScheduleDay(currentDay, current[0].Start, current));
                current = [];
            }
            currentDay = e.Day;
            current.Add(e);
        }
        if (current.Count > 0) days.Add(new ScheduleDay(currentDay, current[0].Start, current));
        return days;
    }

    // ISO 8601 duration for the datetime of a time element: PT1H30M, PT45M, P3D.
    public static string IsoDuration(ScheduleEvent e)
    {
        if (e.AllDay) return string.Create(CultureInfo.InvariantCulture, $"P{e.Days}D");
        var minutes = Math.Max(0, e.Minutes ?? 0);
        var hours = minutes / 60;
        var rest = minutes % 60;
        return string.Create(CultureInfo.InvariantCulture, $"PT{(hours > 0 ? $"{hours}H" : "")}{(rest > 0 || hours == 0 ? $"{rest}M" : "")}");
    }
}
