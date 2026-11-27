namespace Klyroo.UI.Maui;

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
