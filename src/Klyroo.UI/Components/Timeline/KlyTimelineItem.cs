using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>The state of an event of a <see cref="KlyTimeline"/>.</summary>
public enum KlyTimelineState
{
    /// <summary>The event happened.</summary>
    Done,
    /// <summary>The event is happening now.</summary>
    Current,
    /// <summary>The event has not happened yet.</summary>
    Upcoming,
}

/// <summary>The layout of a <see cref="KlyTimeline"/>.</summary>
public enum KlyTimelineLayout
{
    /// <summary>Events along a line, vertical or horizontal (<see cref="KlyTimeline.Orientation"/>).</summary>
    Default,
    /// <summary>Events in a row that wraps, each with a marker and no connecting line.</summary>
    Pipeline,
    /// <summary>Compact lines that start with the date and time of each event.</summary>
    Log,
    /// <summary>An agenda: events grouped by day, each with a time column, a length, all-day events and a group of events without a date.</summary>
    Schedule,
}

/// <summary>An event of a <see cref="KlyTimeline"/>.</summary>
/// <param name="Title">Title of the event.</param>
public sealed record KlyTimelineItem(string Title)
{
    /// <summary>
    /// Free text: "Mar 2026", "Yesterday". It is displayed as given and takes precedence over <see cref="At"/> for the text; in the log layout it replaces the time part of <see cref="At"/>.
    /// </summary>
    public string? Date { get; init; }
    /// <summary>
    /// When the event happened or starts. It is shown in the short date and time format of the resolved culture when <see cref="Date"/> is not set, set as the <c>datetime</c> of the <c>time</c> element,
    /// and starts each line of the log layout. It is shown in its own offset. In the schedule layout it places the event on its day and orders the events; an event without it is listed last, under the unscheduled heading.
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
    /// <summary>Icon of the event, as markup such as a <see cref="KlyIcon"/>.</summary>
    public RenderFragment? Icon { get; init; }
    /// <summary>The name of the person behind the event.</summary>
    public string? Actor { get; init; }
    /// <summary>
    /// The state of the event. When <see langword="null"/>, <see cref="KlyTimeline.Active"/> decides in the default layout, an event is done in the pipeline and log layouts,
    /// and it is a plain upcoming event in the schedule layout, where only an explicit <see cref="KlyTimelineState.Done"/> is muted and only <see cref="KlyTimelineState.Current"/> is emphasized and gets <c>aria-current</c>.
    /// </summary>
    public KlyTimelineState? State { get; init; }
    /// <summary>A color for the marker of this event, replacing the one of the timeline.</summary>
    public KlyColor? Color { get; init; }
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyTimeline"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record TimelineUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>track</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Track { get; init; }
    /// <summary>Classes merged into the <c>indicator</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Indicator { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>separator</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Separator { get; init; }
    /// <summary>Classes merged into the <c>text</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Text { get; init; }
    /// <summary>Classes merged into the <c>date</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Date { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }

    /// <summary>Classes merged into the <c>actor</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Actor { get; init; }
    /// <summary>Classes merged into the <c>number</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Number { get; init; }
    /// <summary>Classes merged into the <c>pipelineList</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? PipelineList { get; init; }
    /// <summary>Classes merged into the <c>pipelineItem</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? PipelineItem { get; init; }
    /// <summary>Classes merged into the <c>logList</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? LogList { get; init; }
    /// <summary>Classes merged into the <c>logItem</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? LogItem { get; init; }
    /// <summary>Classes merged into the <c>logTimestamp</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? LogTimestamp { get; init; }
    /// <summary>Classes merged into the <c>logMarker</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? LogMarker { get; init; }
    /// <summary>Classes merged into the <c>logDot</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? LogDot { get; init; }
    /// <summary>Classes merged into the <c>logIcon</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? LogIcon { get; init; }
    /// <summary>Classes merged into the <c>logBody</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? LogBody { get; init; }
    /// <summary>Classes merged into the <c>logTitle</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? LogTitle { get; init; }
    /// <summary>Classes merged into the <c>logDescription</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? LogDescription { get; init; }
    /// <summary>Classes merged into the <c>logActor</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? LogActor { get; init; }
    /// <summary>Classes merged into the <c>schedule</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Schedule { get; init; }
    /// <summary>Classes merged into the <c>scheduleDay</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleDay { get; init; }
    /// <summary>Classes merged into the <c>scheduleDayTitle</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleDayTitle { get; init; }
    /// <summary>Classes merged into the <c>scheduleUnscheduled</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleUnscheduled { get; init; }
    /// <summary>Classes merged into the <c>scheduleList</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleList { get; init; }
    /// <summary>Classes merged into the <c>scheduleCurrent</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleCurrent { get; init; }
    /// <summary>Classes merged into the <c>scheduleCurrentTone</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleCurrentTone { get; init; }
    /// <summary>Classes merged into the <c>scheduleDone</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleDone { get; init; }
    /// <summary>Classes merged into the <c>scheduleDoneTone</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleDoneTone { get; init; }
    /// <summary>Classes merged into the <c>scheduleUpcoming</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleUpcoming { get; init; }
    /// <summary>Classes merged into the <c>scheduleUpcomingTone</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleUpcomingTone { get; init; }
    /// <summary>Classes merged into the <c>scheduleTimes</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleTimes { get; init; }
    /// <summary>Classes merged into the <c>scheduleTime</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleTime { get; init; }
    /// <summary>Classes merged into the <c>scheduleDuration</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleDuration { get; init; }
    /// <summary>Classes merged into the <c>scheduleBody</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleBody { get; init; }
    /// <summary>Classes merged into the <c>scheduleHeader</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleHeader { get; init; }
    /// <summary>Classes merged into the <c>scheduleIcon</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleIcon { get; init; }
    /// <summary>Classes merged into the <c>scheduleTitle</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleTitle { get; init; }
    /// <summary>Classes merged into the <c>scheduleDescription</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleDescription { get; init; }
    /// <summary>Classes merged into the <c>scheduleActor</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleActor { get; init; }
    /// <summary>Classes merged into the <c>more</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? More { get; init; }
    /// <summary>Classes merged into the <c>moreIcon</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? MoreIcon { get; init; }

    /// <summary>Classes merged into the <c>scheduleEvent</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? ScheduleEvent { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "item" => Item,
        "track" => Track,
        "indicator" => Indicator,
        "icon" => Icon,
        "separator" => Separator,
        "text" => Text,
        "date" => Date,
        "title" => Title,
        "description" => Description,
        "actor" => Actor,
        "number" => Number,
        "pipelineList" => PipelineList,
        "pipelineItem" => PipelineItem,
        "logList" => LogList,
        "logItem" => LogItem,
        "logTimestamp" => LogTimestamp,
        "logMarker" => LogMarker,
        "logDot" => LogDot,
        "logIcon" => LogIcon,
        "logBody" => LogBody,
        "logTitle" => LogTitle,
        "logDescription" => LogDescription,
        "logActor" => LogActor,
        "schedule" => Schedule,
        "scheduleDay" => ScheduleDay,
        "scheduleDayTitle" => ScheduleDayTitle,
        "scheduleUnscheduled" => ScheduleUnscheduled,
        "scheduleList" => ScheduleList,
        "scheduleCurrent" => ScheduleCurrent,
        "scheduleCurrentTone" => ScheduleCurrentTone,
        "scheduleDone" => ScheduleDone,
        "scheduleDoneTone" => ScheduleDoneTone,
        "scheduleUpcoming" => ScheduleUpcoming,
        "scheduleUpcomingTone" => ScheduleUpcomingTone,
        "scheduleTimes" => ScheduleTimes,
        "scheduleTime" => ScheduleTime,
        "scheduleDuration" => ScheduleDuration,
        "scheduleBody" => ScheduleBody,
        "scheduleHeader" => ScheduleHeader,
        "scheduleIcon" => ScheduleIcon,
        "scheduleTitle" => ScheduleTitle,
        "scheduleDescription" => ScheduleDescription,
        "scheduleActor" => ScheduleActor,
        "more" => More,
        "moreIcon" => MoreIcon,
        "scheduleEvent" => ScheduleEvent,
        _ => null,
    };
}
