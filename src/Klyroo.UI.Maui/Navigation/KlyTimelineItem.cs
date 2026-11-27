namespace Klyroo.UI.Maui;

/// <summary>An event of a <see cref="KlyTimeline"/>.</summary>
/// <param name="Title">Title of the event.</param>
public sealed record KlyTimelineItem(string Title)
{
    /// <summary>
    /// Free text: "Mar 2026", "Yesterday". It is displayed as given and takes precedence over <see cref="At"/> for the text; in the log layout it replaces the time part of <see cref="At"/>.
    /// </summary>
    public string? Date { get; init; }

    /// <summary>
    /// When the event happened or starts. It is shown in the short date and time format of the culture when <see cref="Date"/> is not set, and starts each line of the log layout.
    /// It is shown in its own offset. In the schedule layout it places the event on its day and orders the events; an event without it is listed last, under the unscheduled heading.
    /// </summary>
    public DateTimeOffset? At { get; init; }

    /// <summary>
    /// When the event ends. Only the schedule layout reads it: it shows the time range and the length of the event. It is ignored when <see cref="At"/> is not set or when it is not after <see cref="At"/>,
    /// and it is shown in its own offset. An event that starts at midnight and has no end, or ends at midnight or at the end of a day, is an all-day event. An end at midnight is excluded, as in calendar files (iCal):
    /// an <see cref="At"/> of the 12th and an end of the 14th at midnight is two days, and one of the 14th at 23:59 is three.
    /// </summary>
    public DateTimeOffset? EndAt { get; init; }

    /// <summary>Text under the title.</summary>
    public string? Description { get; init; }

    /// <summary>Icon of the event: a Lucide name such as <see cref="KlyIconNames.Star"/>.</summary>
    public string? Icon { get; init; }

    /// <summary>The name of the person behind the event.</summary>
    public string? Actor { get; init; }

    /// <summary>
    /// The state of the event. When <see langword="null"/>, <see cref="KlyTimeline.Active"/> decides in the default layout, an event is done in the pipeline and log layouts,
    /// and it is a plain upcoming event in the schedule layout, where only an explicit <see cref="KlyTimelineState.Done"/> is muted and only <see cref="KlyTimelineState.Current"/> is emphasized.
    /// </summary>
    public KlyTimelineState? State { get; init; }

    /// <summary>A color for the marker of this event, replacing the one of the timeline.</summary>
    public KlyColor? Color { get; init; }
}
