using Klyroo.UI;

namespace Dashboard.Shared.Data;

/// <summary>A message of the notifications panel.</summary>
public sealed record AppNotification(int Id, string Title, string Body, DateTimeOffset At, bool Unread);

/// <summary>A member of the team, on the Members settings page.</summary>
public sealed record Member(int Id, string Name, string Email, string Role, bool Active);

/// <summary>The sample notifications and members.</summary>
public static class PeopleData
{
    public static readonly string[] Roles = ["Owner", "Admin", "Member", "Viewer"];

    public static readonly AppNotification[] Notifications = BuildNotifications();

    public static readonly Member[] Members =
    [
        new(1, "Maren Albrecht", "maren@example.com", "Owner", true),
        new(2, "Tobias Eklund", "tobias@example.com", "Admin", true),
        new(3, "Ines Castellan", "ines@example.com", "Member", true),
        new(4, "Kasper Novak", "kasper@example.com", "Member", true),
        new(5, "Lucia Fontaine", "lucia@example.com", "Viewer", false),
        new(6, "Odin Iversen", "odin@example.com", "Member", true),
    ];

    private static AppNotification[] BuildNotifications()
    {
        var now = new DateTimeOffset(DemoClock.Today.ToDateTime(new TimeOnly(9, 40)), TimeSpan.Zero);
        return
        [
            new(1, "Ingrid Holmgren asked for a renewal quote", "The Business plan ends on the 30th. Eight more seats were requested.", now.AddMinutes(-14), true),
            new(2, "Invoice 5139 was paid", "The transfer of Juniper Row arrived and the invoice is now closed.", now.AddHours(-3), true),
            new(3, "Three customers joined this week", "Larkspur Health, Foxglove Studio and Copperleaf started a trial.", now.AddHours(-8), true),
            new(4, "Weekly report is ready", "Revenue is up 6.4 percent compared with the week before.", now.AddDays(-1), false),
            new(5, "Tobias Eklund joined the team", "He has the Admin role and can manage customers and members.", now.AddDays(-3), false),
        ];
    }
}

/// <summary>
/// A directory of people behind a delay, standing for a user directory or an identity provider: what the invite dialog searches while you type. It is a
/// <see cref="KlyOptionsProvider"/> of <c>KlySelectMenu</c>: each call returns one page of the people that match, so a directory of any size can be listed.
/// </summary>
public static class PeopleDirectory
{
    private static readonly string[] Given =
        ["Alma", "Bastian", "Clara", "Dmitri", "Edda", "Felix", "Gwen", "Henrik", "Iris", "Jorge", "Klara", "Lars", "Mina", "Nils", "Olga", "Pavel", "Rosa", "Sven", "Tilda", "Ulrik"];

    private static readonly string[] Family =
        ["Ahlberg", "Bianchi", "Carvalho", "Delacroix", "Esposito", "Falk", "Gustafsson", "Haugen", "Ibarra", "Jansen", "Koenig", "Laurent"];

    private static readonly KlySelectItem[] All =
    [
        .. Given.SelectMany((g, i) => Family.Where((_, j) => (i + j) % 4 == 0)
            .Select(f => new KlySelectItem($"{g}.{f}@example.com".ToLowerInvariant(), $"{g} {f}"))),
    ];

    /// <summary>The delay of every call, in milliseconds.</summary>
    public static int LatencyMs { get; set; } = 150;

    /// <summary>One page of the people whose name contains the search.</summary>
    public static async ValueTask<KlyOptionsResult> Search(KlyOptionsRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(LatencyMs, cancellationToken);
        var matches = All.Where(p => request.Search.Length == 0 || p.Label.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToArray();
        var page = matches.Skip(request.Skip).Take(request.Take).ToArray();
        return new KlyOptionsResult(page, HasMore: request.Skip + page.Length < matches.Length);
    }

    /// <summary>The labels of values that were never listed (a value restored from a saved form): the people with those addresses.</summary>
    public static ValueTask<IReadOnlyList<KlySelectItem>> Resolve(IReadOnlyList<string> values, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<KlySelectItem>>([.. All.Where(p => values.Contains(p.Value))]);
}

/// <summary>The countries of the new customer form, behind a delay like a remote service, paged with Skip and Take.</summary>
public static class CountryDirectory
{
    private static readonly KlySelectItem[] All =
    [
        new("AR", "Argentina"), new("AU", "Australia"), new("AT", "Austria"), new("BE", "Belgium"), new("BR", "Brazil"), new("CA", "Canada"),
        new("CL", "Chile"), new("CO", "Colombia"), new("HR", "Croatia"), new("CZ", "Czechia"), new("DK", "Denmark"), new("EE", "Estonia"),
        new("FI", "Finland"), new("FR", "France"), new("DE", "Germany"), new("GR", "Greece"), new("HU", "Hungary"), new("IS", "Iceland"),
        new("IN", "India"), new("IE", "Ireland"), new("IT", "Italy"), new("JP", "Japan"), new("LV", "Latvia"), new("LT", "Lithuania"),
        new("LU", "Luxembourg"), new("MX", "Mexico"), new("NL", "Netherlands"), new("NZ", "New Zealand"), new("NO", "Norway"), new("PL", "Poland"),
        new("PT", "Portugal"), new("RO", "Romania"), new("SG", "Singapore"), new("SK", "Slovakia"), new("SI", "Slovenia"), new("ZA", "South Africa"),
        new("ES", "Spain"), new("SE", "Sweden"), new("CH", "Switzerland"), new("GB", "United Kingdom"), new("US", "United States"),
    ];

    /// <summary>One page of the countries whose name contains the search.</summary>
    public static async ValueTask<KlyOptionsResult> Search(KlyOptionsRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(120, cancellationToken);
        var matches = All.Where(c => request.Search.Length == 0 || c.Label.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToArray();
        var page = matches.Skip(request.Skip).Take(request.Take).ToArray();
        return new KlyOptionsResult(page, HasMore: request.Skip + page.Length < matches.Length);
    }

    /// <summary>The name of a country by its code, for a value that was restored without its page.</summary>
    public static ValueTask<IReadOnlyList<KlySelectItem>> Resolve(IReadOnlyList<string> values, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<KlySelectItem>>([.. All.Where(c => values.Contains(c.Value))]);

    /// <summary>The name of a country by its code.</summary>
    public static string NameOf(string code) => All.FirstOrDefault(c => c.Value == code)?.Label ?? code;
}
