using Blux.UI;

namespace Blux.Playground.Client.Examples;

/// <summary>
/// A simulated remote source of options for the OptionsProvider examples: 64 countries behind a delay, with the two kinds of paging
/// (skip and take, or a cursor), a lookup of selected values, and a switch that makes every call fail. It counts its calls so a
/// page can show how many times the source was really asked.
/// </summary>
public sealed class RemoteDirectory
{
    private static readonly BxSelectItem[] All =
    [
        new("ar", "Argentina"), new("au", "Australia"), new("at", "Austria"), new("be", "Belgium"), new("br", "Brazil"), new("bg", "Bulgaria"),
        new("ca", "Canada"), new("cl", "Chile"), new("cn", "China"), new("co", "Colombia"), new("hr", "Croatia"), new("cz", "Czechia"),
        new("dk", "Denmark"), new("eg", "Egypt"), new("ee", "Estonia"), new("fi", "Finland"), new("fr", "France"), new("de", "Germany"),
        new("gr", "Greece"), new("hu", "Hungary"), new("is", "Iceland"), new("in", "India"), new("id", "Indonesia"), new("ie", "Ireland"),
        new("il", "Israel"), new("it", "Italy"), new("jp", "Japan"), new("ke", "Kenya"), new("lv", "Latvia"), new("lt", "Lithuania"),
        new("lu", "Luxembourg"), new("my", "Malaysia"), new("mx", "Mexico"), new("ma", "Morocco"), new("nl", "Netherlands"), new("nz", "New Zealand"),
        new("ng", "Nigeria"), new("no", "Norway"), new("pk", "Pakistan"), new("pe", "Peru"), new("ph", "Philippines"), new("pl", "Poland"),
        new("pt", "Portugal"), new("ro", "Romania"), new("sa", "Saudi Arabia"), new("rs", "Serbia"), new("sg", "Singapore"), new("sk", "Slovakia"),
        new("si", "Slovenia"), new("za", "South Africa"), new("kr", "South Korea"), new("es", "Spain"), new("se", "Sweden"), new("ch", "Switzerland"),
        new("th", "Thailand"), new("tn", "Tunisia"), new("tr", "Turkey"), new("ua", "Ukraine"), new("ae", "United Arab Emirates"),
        new("gb", "United Kingdom"), new("us", "United States"), new("uy", "Uruguay"), new("vn", "Vietnam"), new("zw", "Zimbabwe"),
    ];

    private static readonly Dictionary<string, string[]> Cities = new()
    {
        ["fr"] = ["Paris", "Lyon", "Marseille", "Toulouse", "Nice", "Nantes", "Strasbourg", "Bordeaux", "Lille", "Rennes"],
        ["de"] = ["Berlin", "Hamburg", "Munich", "Cologne", "Frankfurt", "Stuttgart", "Dusseldorf", "Leipzig", "Dresden", "Bremen"],
        ["it"] = ["Rome", "Milan", "Naples", "Turin", "Palermo", "Genoa", "Bologna", "Florence", "Venice", "Verona"],
    };

    /// <summary>The delay of every call, to see the loading state.</summary>
    public int LatencyMs { get; init; } = 400;

    /// <summary>When true every call throws, to see the error state and its retry button.</summary>
    public bool Fail { get; set; }

    /// <summary>The number of calls to <see cref="ByPage"/>, <see cref="ByCursor"/> and <see cref="Resolve"/>.</summary>
    public int Requests { get; private set; }

    /// <summary>The last request, as text.</summary>
    public string Last { get; private set; } = "none";

    /// <summary>Called after each call so the page can show the counters.</summary>
    public Action? Changed { get; init; }

    private List<BxSelectItem> Matches(string search) =>
        [.. All.Where(c => search.Length == 0 || c.Label.Contains(search, StringComparison.OrdinalIgnoreCase))];

    private async Task Enter(string description, CancellationToken ct)
    {
        Requests++;
        Last = description;
        Changed?.Invoke();
        await Task.Delay(LatencyMs, ct);
        if (Fail) throw new InvalidOperationException("The simulated source is down.");
    }

    /// <summary>Pages with Skip and Take.</summary>
    public async ValueTask<BxOptionsResult> ByPage(BxOptionsRequest request, CancellationToken ct)
    {
        await Enter($"search \"{request.Search}\", skip {request.Skip}, take {request.Take}", ct);
        var matches = Matches(request.Search);
        var page = matches.Skip(request.Skip).Take(request.Take).ToList();
        return new BxOptionsResult(page, HasMore: request.Skip + page.Count < matches.Count);
    }

    /// <summary>Pages with a cursor: the index of the next option, as text.</summary>
    public async ValueTask<BxOptionsResult> ByCursor(BxOptionsRequest request, CancellationToken ct)
    {
        await Enter($"search \"{request.Search}\", cursor {request.Cursor ?? "none"}, take {request.Take}", ct);
        var matches = Matches(request.Search);
        var start = request.Cursor is null ? 0 : int.Parse(request.Cursor);
        var page = matches.Skip(start).Take(request.Take).ToList();
        var next = start + page.Count;
        return new BxOptionsResult(page, NextCursor: next < matches.Count ? next.ToString() : null);
    }

    /// <summary>The options of values that were restored (from the address, a saved form) without ever being listed.</summary>
    public async ValueTask<IReadOnlyList<BxSelectItem>> Resolve(IReadOnlyList<string> values, CancellationToken ct)
    {
        await Enter($"resolve {string.Join(", ", values)}", ct);
        return [.. All.Where(c => values.Contains(c.Value))];
    }

    /// <summary>The cities of one country, paged with Skip and Take: what a source whose options depend on another field looks like (see <c>OptionsKey</c>).</summary>
    public async ValueTask<BxOptionsResult> CitiesOf(string? country, BxOptionsRequest request, CancellationToken ct)
    {
        await Enter($"cities of {country ?? "nowhere"}, search \"{request.Search}\", skip {request.Skip}, take {request.Take}", ct);
        var cities = (country is not null && Cities.TryGetValue(country, out var list) ? list : [])
            .Where(c => request.Search.Length == 0 || c.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();
        var page = cities.Skip(request.Skip).Take(request.Take).Select(c => new BxSelectItem(c.ToLowerInvariant(), c)).ToList();
        return new BxOptionsResult(page, HasMore: request.Skip + page.Count < cities.Count);
    }
}
