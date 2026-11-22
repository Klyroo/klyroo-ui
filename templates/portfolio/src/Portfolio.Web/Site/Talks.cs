namespace Portfolio.Web.Site;

/// <summary>What kind of appearance a <see cref="Talk"/> was.</summary>
public enum TalkKind
{
    Talk,
    Workshop,
    Podcast,
}

/// <summary>A talk, a workshop or an episode. <see cref="Slides"/> and <see cref="Recording"/> are optional addresses.</summary>
public sealed record Talk(DateOnly Date, string Title, string Event, string Place, TalkKind Kind)
{
    public string? Slides { get; init; }
    public string? Recording { get; init; }
}

/// <summary>The talks of the speaking page, as typed C#. The events and the places are made up. The page groups them by year, newest first.</summary>
public static class Talks
{
    public static IReadOnlyList<Talk> All { get; } =
    [
        new(new DateOnly(2026, 10, 9), "Focus you can see on every background", "Interface Days", "Porto", TalkKind.Talk)
        {
            Slides = "https://example.com/ines/slides/focus",
            Recording = "https://example.com/ines/video/focus",
        },
        new(new DateOnly(2026, 6, 18), "Auditing a design system before you touch a token", "Systems Summit", "Berlin", TalkKind.Workshop)
        {
            Slides = "https://example.com/ines/slides/audit",
        },
        new(new DateOnly(2026, 3, 2), "Shipping accessibility with a team of nine", "The Shipping Podcast", "Remote", TalkKind.Podcast)
        {
            Recording = "https://example.com/ines/audio/shipping",
        },
        new(new DateOnly(2025, 11, 21), "Offline first is a promise about your data", "Local-First Meetup", "Lisbon", TalkKind.Talk)
        {
            Slides = "https://example.com/ines/slides/offline",
            Recording = "https://example.com/ines/video/offline",
        },
        new(new DateOnly(2025, 5, 15), "Spacing scales with fewer steps", "CSS Day Lisbon", "Lisbon", TalkKind.Talk)
        {
            Slides = "https://example.com/ines/slides/spacing",
        },
    ];
}
