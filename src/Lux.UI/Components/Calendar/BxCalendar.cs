using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>What a <see cref="BxCalendar"/> selects.</summary>
public enum BxCalendarMode
{
    /// <summary>One date, bound with <c>Value</c>.</summary>
    Single,

    /// <summary>Any number of dates, bound with <c>Values</c>.</summary>
    Multiple,

    /// <summary>A start and an end date, bound with <c>Range</c>.</summary>
    Range,
}

/// <summary>A range of dates. <see cref="End"/> is null while only the start has been chosen.</summary>
public sealed record BxDateRange(DateOnly? Start, DateOnly? End);

public sealed record CalendarUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Header { get; init; }
    public string? Heading { get; init; }
    public string? Nav { get; init; }
    public string? Grid { get; init; }
    public string? Weekday { get; init; }
    public string? Cell { get; init; }
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
