using Klyroo.UI.Theming;

namespace Klyroo.UI;

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

/// <summary>A range of dates. <see cref="End"/> is null while only the start has been chosen.</summary>
/// <param name="Start">First date of the range, or null when none was chosen.</param>
/// <param name="End">Last date of the range, or null while only the start has been chosen.</param>
public sealed record KlyDateRange(DateOnly? Start, DateOnly? End);

/// <summary>Per-instance class overrides of the slots of <see cref="KlyCalendar"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record CalendarUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyCalendar"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyCalendar"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>heading</c> slot of <see cref="KlyCalendar"/>, after the theme and the app configuration.</summary>
    public string? Heading { get; init; }
    /// <summary>Classes merged into the <c>nav</c> slot of <see cref="KlyCalendar"/>, after the theme and the app configuration.</summary>
    public string? Nav { get; init; }
    /// <summary>Classes merged into the <c>grid</c> slot of <see cref="KlyCalendar"/>, after the theme and the app configuration.</summary>
    public string? Grid { get; init; }
    /// <summary>Classes merged into the <c>weekday</c> slot of <see cref="KlyCalendar"/>, after the theme and the app configuration.</summary>
    public string? Weekday { get; init; }
    /// <summary>Classes merged into the <c>cell</c> slot of <see cref="KlyCalendar"/>, after the theme and the app configuration.</summary>
    public string? Cell { get; init; }
    /// <summary>Classes merged into the <c>day</c> slot of <see cref="KlyCalendar"/>, after the theme and the app configuration.</summary>
    public string? Day { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "header" => Header,
        "heading" => Heading,
        "nav" => Nav,
        "grid" => Grid,
        "weekday" => Weekday,
        "cell" => Cell,
        "day" => Day,
        _ => null,
    };
}
