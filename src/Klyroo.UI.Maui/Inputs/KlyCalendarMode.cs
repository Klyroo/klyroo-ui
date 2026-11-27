namespace Klyroo.UI.Maui;

/// <summary>What a <see cref="KlyCalendar"/> selects.</summary>
public enum KlyCalendarMode
{
    /// <summary>One date, bound with <c>Value</c>.</summary>
    Single,

    /// <summary>Any number of dates, bound with <c>Values</c>.</summary>
    Multiple,

    /// <summary>A start and an end date, bound with <c>Range</c>.</summary>
    Range,
}
