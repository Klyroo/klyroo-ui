using Lux.UI;

namespace Portfolio.Web.Site;

/// <summary>A job or a role: shown as an item of the timeline of the about page.</summary>
public sealed record Experience(string Period, string Title, string Organization, string Summary);

/// <summary>A group of skills or tools: "Design", "Code".</summary>
public sealed record SkillGroup(string Name, IReadOnlyList<string> Items);

/// <summary>
/// The experience and the skills of the about page, as typed C#. The employers are made up. Add a job at the top: the timeline shows the list in the order
/// it is written, newest first.
/// </summary>
public static class Resume
{
    public static IReadOnlyList<Experience> Experiences { get; } =
    [
        new("2024 to now", "Independent design engineer", "Calloway Studio",
            "Design systems, accessibility audits and interface prototypes for product teams in logistics, banking and public services."),
        new("2021 to 2024", "Senior design engineer", "Fjordlight",
            "Led the interface side of a team of nine that built a route planning product. Wrote the component library the four product squads shared, and took on small freelance projects in the evenings."),
        new("2018 to 2021", "Front-end engineer", "Meridian Credit",
            "Built the web app of a regional bank, then moved to the design system team and started its accessibility practice."),
        new("2016 to 2018", "Product designer", "Brightwell Learning",
            "Designed the lesson player of an online school, from user interviews to the final screens, and learned to code it to see it ship."),
    ];

    public static IReadOnlyList<SkillGroup> Skills { get; } =
    [
        new("Design", ["Interface design", "Design tokens", "Prototyping", "User interviews", "Accessibility audits"]),
        new("Code", ["TypeScript", "C# and Blazor", "CSS and Tailwind", "Web components", "Playwright and axe"]),
        new("Practice", ["Design system documentation", "Workshops", "Code review", "Writing for engineers"]),
    ];

    public static IReadOnlyList<LxTimelineItem> Timeline() =>
        [.. Experiences.Select(e => new LxTimelineItem($"{e.Title}, {e.Organization}") { Date = e.Period, Description = e.Summary })];
}
