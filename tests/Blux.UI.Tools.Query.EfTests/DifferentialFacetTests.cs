using Microsoft.EntityFrameworkCore;
using static Blux.UI.Tools.Query.EfTests.H;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.EfTests;

/// <summary>Facets: the group-by queries, run against SQLite and turned into results, equal the in-memory counts (scalar, boolean, enum, path and collection fields).</summary>
[Collection("Database")]
public class DifferentialFacetTests
{
    private static readonly string[] Keys = ["status", "department.name", "active", "tags.name", "id"];

    private static DataListFacetRequest[] Facet(string key, FacetMode mode = FacetMode.ExcludeSelf, int? limit = null, int skip = 0, string? search = null, bool missing = false) =>
        [new DataListFacetRequest(key, mode, limit, skip, search, missing)];

    [Theory]
    [InlineData("status")]
    [InlineData("department.name")]
    [InlineData("active")]
    [InlineData("tags.name")]
    [InlineData("id")]
    public void Plain_facets_with_the_missing_count(string key)
    {
        using var db = Database.Binary();
        AssertSame(db, Load(db), Req(facets: Facet(key, missing: true)));
    }

    [Theory]
    [InlineData("status")]
    [InlineData("department.name")]
    [InlineData("tags.name")]
    public void A_facet_leaves_out_its_own_filter_unless_asked(string key)
    {
        using var db = Database.Binary();
        var rows = Load(db);
        FilterRule own = key switch
        {
            "status" => Rule("status", Op.Is, Text("Active")),
            "department.name" => Rule("department.name", Op.IsAnyOf, List("Sales", "Support")),
            _ => Rule("tags.name", Op.IsAnyOf, List("dotnet")),
        };
        var filter = DataListFilterGroup.And(own, Rule("age", Op.Gte, Num(25), includeEmpty: true));
        foreach (var mode in new[] { FacetMode.ExcludeSelf, FacetMode.IncludeSelf })
            AssertSame(db, rows, Req(filter: filter, facets: Facet(key, mode, missing: true), search: "a", paging: new DataListPaging.Offset(0, 5)));
    }

    [Fact]
    public void Several_facets_with_a_window_and_a_search()
    {
        using var db = Database.Binary();
        var rows = Load(db);
        DataListFacetRequest[] facets =
        [
            new("tags.name", Limit: 2, Skip: 1),
            new("department.name", Search: "ng", IncludeMissing: true),
            new("status", Mode: FacetMode.IncludeSelf, Limit: 1),
            new("id", Limit: 10, Skip: 5),
        ];
        AssertSame(db, rows, Req(rule: Rule("active", Op.Is, Bool(true)), facets: facets));
        var facetsOnly = Run(db, Req(facets: facets));
        Assert.Equal(4, facetsOnly.Facets!.Count);
        Assert.True(facetsOnly.Facets[0].Total > 2);
    }

    [Fact]
    public void A_row_is_counted_once_per_value_even_when_several_elements_carry_it()
    {
        using var db = Database.Binary();
        var rows = Load(db);
        Assert.Contains(rows, e => e.Tags.GroupBy(t => t.Name).Any(g => g.Key is not null && g.Count() > 1));
        AssertSame(db, rows, Req(facets: Facet("tags.name")));
    }

    [Fact]
    public void Facet_queries_are_group_by_queries_with_parameters()
    {
        using var db = Database.Binary();
        var queries = Schemas.Employees.FacetQueries(
            db.Employees.AsNoTracking(),
            Req(rule: Rule("name", Op.Contains, Text("Zq9ZqFacet")), facets: [new DataListFacetRequest("status", IncludeMissing: true), new DataListFacetRequest("tags.name")]));
        Assert.Equal(["status", "tags.name"], queries.Select(q => q.Key));

        var scalar = queries[0].Buckets.ToQueryString();
        Assert.Contains("GROUP BY", scalar);
        Assert.Contains("COUNT(*)", scalar);
        Assert.DoesNotContain("Zq9ZqFacet", string.Join('\n', scalar.Split('\n').Where(l => !l.StartsWith(".param", StringComparison.Ordinal))));
        Assert.NotNull(queries[0].Missing);

        var collection = queries[1].Buckets.ToQueryString();
        Assert.Contains("GROUP BY", collection);
        Assert.Contains("COUNT(DISTINCT", collection);
        Assert.Null(queries[1].Missing);
    }

    [Fact]
    public void Facets_the_schema_does_not_allow_are_dropped()
    {
        using var db = Database.Binary();
        var queries = Schemas.Employees.FacetQueries(db.Employees, Req(facets: [new DataListFacetRequest("name"), new DataListFacetRequest("nope"), new DataListFacetRequest("status")]));
        Assert.Equal(["status"], queries.Select(q => q.Key));
    }
}
