using Microsoft.EntityFrameworkCore;
using static Blux.UI.Tools.Query.EfTests.H;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.EfTests;

/// <summary>Time zones and day bounds (computed instants are parameters), then the documented differences: text case and accents follow the database.</summary>
[Collection("Database")]
public class ZoneTests
{
    private static List<int> Ids(DataListResult<Activity> r) => [.. r.Items.Select(a => a.Id)];

    private static void Same(QueryContext db, List<Activity> rows, string key, Op op, FilterValue value, string? zone)
    {
        var request = Req(rule: Rule(key, op, value), zone: zone);
        var expected = Schemas.Activities.ExecuteInMemory(rows, request);
        var actual = Run(Schemas.Activities, db.Activities, request);
        Assert.Equal(Ids(expected), Ids(actual));
        Assert.Equal(expected.TotalCount, actual.TotalCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Europe/Paris")]
    [InlineData("America/New_York")]
    [InlineData("Asia/Kolkata")]
    [InlineData("Pacific/Kiritimati")]
    [InlineData("Not/AZone")]
    public void Day_filters_use_the_local_day_for_instants_and_the_plain_day_for_dates(string? zone)
    {
        using var db = Database.Binary();
        var rows = LoadActivities(db);
        foreach (var key in new[] { "when", "at", "on" })
            foreach (var op in new[] { Op.Is, Op.IsNot, Op.Before, Op.After })
                foreach (var day in new[] { "2024-03-15", "2024-03-30", "2024-03-31", "2024-04-01" })
                    Same(db, rows, key, op, Day(day), zone);
        foreach (var key in new[] { "when", "at", "on" })
            Same(db, rows, key, Op.Between, Range(Day("2024-03-30"), Day("2024-03-31")), zone);
    }

    [Theory]
    [InlineData("Europe/Paris")]
    [InlineData("Asia/Kolkata")]
    public void Hour_granularity_cuts_hours_in_the_zone(string zone)
    {
        using var db = Database.Binary();
        var rows = LoadActivities(db);
        foreach (var key in new[] { "when", "at" })
            foreach (var op in new[] { Op.Is, Op.Before, Op.After })
                Same(db, rows, key, op, At("2024-03-15T11:30:00Z"), zone);
    }

    [Fact]
    public void The_daylight_saving_day_is_23_hours_long_in_the_query()
    {
        using var db = Database.Binary();
        // 2024-03-31 in Paris runs from 2024-03-30T23:00Z to 2024-03-31T22:00Z
        var ids = Ids(Run(Schemas.Activities, db.Activities, Req(rule: Rule("when", Op.Is, Day("2024-03-31")), zone: "Europe/Paris")));
        Assert.Equal([2, 3, 4], ids);
    }

    [Fact]
    public void Day_bounds_are_parameters_computed_before_the_query()
    {
        using var db = Database.Binary();
        var query = Schemas.Activities.Apply(db.Activities, Req(rule: Rule("when", Op.Is, Day("2077-07-07")), zone: "Europe/Paris"));
        var sql = string.Join('\n', query.Page.ToQueryString().Split('\n').Where(l => !l.StartsWith(".param", StringComparison.Ordinal)));
        Assert.DoesNotContain("2077", sql);
        Assert.DoesNotContain("AT TIME ZONE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Matches(@">= @p\d*", sql);
        Assert.Matches(@"< @p\d*", sql);
    }
}

/// <summary>What the database decides instead of the executor: text case and accents, and the order of text.</summary>
[Collection("Database")]
public class CollationTests
{
    [Fact]
    public void Contains_is_case_and_accent_insensitive_in_memory_and_follows_the_database_on_sqlite()
    {
        using var db = Database.Binary();
        var rows = Load(db);
        var request = Req(rule: Rule("name", Op.Contains, Text("ALICE")));
        Assert.Equal([1], Memory(rows, request).Ids());
        // SQLite translates Contains to instr(), which is case sensitive whatever the collation of the column
        Assert.Empty(Run(db, request).Ids());

        var accent = Req(rule: Rule("name", Op.Contains, Text("eric")));
        Assert.Equal([5], Memory(rows, accent).Ids());
        Assert.Empty(Run(db, accent).Ids());
    }

    [Fact]
    public void Is_is_exact_in_memory_and_follows_the_collation_of_the_column()
    {
        var request = Req(rule: Rule("name", Op.Is, Text("ALICE JOHNSON")));
        using (var binary = Database.Binary())
        {
            Assert.Empty(Memory(Load(binary), request).Ids());
            Assert.Empty(Run(binary, request).Ids());
        }
        using var noCase = Database.NoCase();
        Assert.Empty(Memory(Load(noCase), request).Ids());
        Assert.Equal([1], Run(noCase, request).Ids());
    }

    [Fact]
    public void Option_filters_and_facets_merge_values_the_collation_treats_as_equal()
    {
        using var db = Database.NoCase();
        var rows = Load(db);
        var request = Req(rule: Rule("tags.name", Op.IsAnyOf, List("BLAZOR")), facets: [new DataListFacetRequest("tags.name")]);
        var memory = Memory(rows, request);
        var actual = Run(db, request);
        Assert.Empty(memory.Ids());
        Assert.NotEmpty(actual.Ids());

        var facet = actual.Facets!.Single();
        var blazor = facet.Options.Where(o => string.Equals(o.Value, "blazor", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Single(blazor);
        var inMemoryBlazor = Memory(rows, Req(facets: [new DataListFacetRequest("tags.name")])).Facets!.Single().Options
            .Where(o => string.Equals(o.Value, "blazor", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Equal(2, inMemoryBlazor.Count);
    }

    [Fact]
    public void Text_sort_uses_the_collation_of_the_column()
    {
        using var db = Database.NoCase();
        var rows = Load(db);
        var ids = Run(db, Req(sort: Sort(("name", false)))).Ids();
        Assert.Equal(rows.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Id).Select(e => e.Id), ids);
        // NOCASE folds ASCII only: the accented name sorts after z, where the executor puts it between D and F
        Assert.Equal(5, ids[^1]);
        Assert.NotEqual(5, Memory(rows, Req(sort: Sort(("name", false)))).Ids()[^1]);
    }
}
