namespace Klyroo.UI.Maui;

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
