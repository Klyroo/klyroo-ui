namespace Klyroo.UI;

// Texts of the read-only blocks (KlyFacts, KlyStat, KlyMeter, KlyDateTile, KlyText, KlyAlert details, schedule timeline...).
// A text that holds a count is a function of that count, so a language chooses its own plural forms and word order.
public sealed partial record KlyMessages
{
    /// <summary>The button that reveals more items of a list-like block, given the number of items the next click reveals (<c>"Show more · 3"</c>).</summary>
    public Func<int, string> ShowMore { get; init; } = n => $"Show more · {n}";

    /// <summary>The toggle that expands a clamped <see cref="KlyText"/> (<c>"Read more"</c>).</summary>
    public string ReadMore { get; init; } = "Read more";

    /// <summary>The toggle that clamps an expanded <see cref="KlyText"/> again (<c>"Show less"</c>).</summary>
    public string ShowLess { get; init; } = "Show less";

    /// <summary>The toggle that opens the details of a <see cref="KlyAlert"/> (<c>"Show details"</c>).</summary>
    public string ShowDetails { get; init; } = "Show details";

    /// <summary>The toggle that closes the details of a <see cref="KlyAlert"/> or the lines of a collapsible <see cref="KlyBreakdown"/> (<c>"Hide details"</c>).</summary>
    public string HideDetails { get; init; } = "Hide details";

    /// <summary>The toggle that opens the lines of a collapsible <see cref="KlyBreakdown"/> (<c>"Details"</c>).</summary>
    public string Details { get; init; } = "Details";

    /// <summary>The accessible name of a copy button. <c>{0}</c> is the label or value it copies (<c>"Copy Email"</c>).</summary>
    public string CopyValue { get; init; } = "Copy {0}";

    /// <summary>The name of the series of a funnel <see cref="KlyChart{TItem, TIndex}"/> or a chart without a name, used in the tooltip and the data table (<c>"Value"</c>).</summary>
    public string ChartValue { get; init; } = "Value";

    /// <summary>The accessible name of a <see cref="KlyMeter"/> without a title (<c>"Meter"</c>).</summary>
    public string Meter { get; init; } = "Meter";

    /// <summary>The countdown text of a <see cref="KlyDateTile"/> for the current day, and the heading of today in a schedule (<c>"Today"</c>).</summary>
    public string Today { get; init; } = "Today";

    /// <summary>The countdown text of a <see cref="KlyDateTile"/> for the next day, and the heading of tomorrow in a schedule (<c>"Tomorrow"</c>).</summary>
    public string Tomorrow { get; init; } = "Tomorrow";

    /// <summary>The countdown text of a <see cref="KlyDateTile"/> for a date two days away or more, given the number of days (<c>"In 5 days"</c>).</summary>
    public Func<int, string> InDays { get; init; } = n => $"In {n} days";

    /// <summary>The countdown text of a <see cref="KlyDateTile"/> for a past date, given the number of days (<c>"3 days overdue"</c>).</summary>
    public Func<int, string> DaysOverdue { get; init; } = n => n == 1 ? "1 day overdue" : $"{n} days overdue";

    /// <summary>The length of a range <see cref="KlyDateTile"/> or of an all-day event of a schedule, given the number of days (<c>"7 days"</c>).</summary>
    public Func<int, string> DayCount { get; init; } = n => n == 1 ? "1 day" : $"{n} days";

    /// <summary>The time of a schedule event that lasts whole days (<c>"All day"</c>).</summary>
    public string AllDay { get; init; } = "All day";

    /// <summary>The heading of the events of a schedule that have no date (<c>"Unscheduled"</c>).</summary>
    public string Unscheduled { get; init; } = "Unscheduled";

    /// <summary>The length of a schedule event, given its total number of minutes (<c>"1 h 30 min"</c>, <c>"45 min"</c>, <c>"2 h"</c>). The length is never negative.</summary>
    public Func<int, string> Duration { get; init; } = m => Internal.Durations.Format(m, "h", "min");

    /// <summary>The alternative text of an image of a <see cref="KlyImageGallery"/> that has none. <c>{0}</c> is its position (<c>"Image 2"</c>).</summary>
    public string ImageAlt { get; init; } = "Image {0}";
}
