using Microsoft.EntityFrameworkCore;
using static Blux.UI.Tools.Query.EfTests.H;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.EfTests;

/// <summary>The facet bucket cap, and cursors whose sort values come from the database instead of the loaded entity.</summary>
[Collection("Database")]
public class CapAndNavigationTests
{
    private static DataListSchema<Employee> Capped(int cap) => DataListSchema.For<Employee>(s => s
        .RowKey(e => e.Id)
        .Field(e => e.Status, f => f.Filter(DataListFilter.Option()))
        .Field(e => e.Department!.Name, f => f.Filter(DataListFilter.Option()))
        .Collection(e => e.Tags, t => t.Name, f => f.Filter(DataListFilter.Option()))
        .MaxFacetBuckets(cap));

    private static DataListFacetResult[] Facets(DataListSchema<Employee> schema, IQueryable<Employee> source, DataListRequest request) =>
        [.. Run(schema, source, request).Facets!];

    [Theory]
    [InlineData("status", 1)]
    [InlineData("status", 2)]
    [InlineData("status", 3)]
    [InlineData("department.name", 2)]
    [InlineData("department.name", 5)]
    [InlineData("tags.name", 3)]
    [InlineData("tags.name", 4)]
    [InlineData("tags.name", 50)]
    public void A_capped_facet_keeps_the_most_counted_values_like_the_executor(string key, int cap)
    {
        using var db = Database.Binary();
        var schema = Capped(cap);
        foreach (var facet in new[]
        {
            new DataListFacetRequest(key, IncludeMissing: true),
            new DataListFacetRequest(key, Skip: 1, Limit: 1),
            new DataListFacetRequest(key, Search: "e"),
        })
        {
            var request = Req(facets: [facet]);
            var expected = schema.ExecuteInMemory(Load(db), request).Facets!.Single();
            var actual = Facets(schema, db.Employees.AsNoTracking(), request).Single();
            Assert.Equal(expected, actual);
            Assert.True(actual.Total <= cap);
        }
    }

    [Fact]
    public void A_facet_over_more_values_than_the_cap_is_marked_truncated()
    {
        using var db = Database.Binary();
        var distinct = Load(db).SelectMany(e => e.Tags).Select(t => t.Name).Where(n => !string.IsNullOrEmpty(n)).Distinct().Count();
        Assert.True(distinct > 3);
        var request = Req(facets: [new DataListFacetRequest("tags.name")]);

        var cut = Facets(Capped(3), db.Employees.AsNoTracking(), request).Single();
        Assert.True(cut.Truncated);
        Assert.Equal(3, cut.Options.Count);
        Assert.Equal(3, cut.Total);

        var whole = Facets(Capped(distinct), db.Employees.AsNoTracking(), request).Single();
        Assert.False(whole.Truncated);
        Assert.Equal(distinct, whole.Options.Count);
        // the kept options are the head of the whole list
        Assert.Equal(whole.Options.Take(3), cut.Options);
    }

    [Fact]
    public void The_cap_is_a_take_on_the_group_by_so_the_database_never_returns_more_than_the_cap_plus_one()
    {
        using var db = Database.Binary();
        var schema = Capped(7);
        var query = schema.FacetQueries(db.Employees.AsNoTracking(), Req(facets: [new DataListFacetRequest("status")]))[0];
        var sql = query.Buckets.ToQueryString();
        Assert.Contains("ORDER BY COUNT(*) DESC", sql);
        Assert.Contains("LIMIT @p", sql);
        Assert.Contains("8", sql); // the parameter: cap + 1
        var many = DataListSchema.For<Employee>(s => s.RowKey(e => e.Id).Field(e => e.Id, f => f.Filter(DataListFilter.Option())).MaxFacetBuckets(10));
        var loaded = many.FacetQueries(db.Employees.AsNoTracking(), Req(facets: [new DataListFacetRequest("id")]))[0].Buckets.ToList();
        Assert.Equal(11, loaded.Count);
    }

    // ---- cursor from the database

    [Fact]
    public void A_cursor_walk_sorted_by_a_navigation_without_include_equals_the_offset_order()
    {
        using var db = Database.Binary();
        var schema = Schemas.Employees;
        DataListSort[] sort = [new("department.name", true), new("age"), new("lastLogin", true)];

        // the entities are loaded without their Department: reading the sort value from them would give null
        var all = Run(schema, db.Employees.AsNoTracking(), Req(sort: sort, paging: new DataListPaging.Offset(0, 500)));
        Assert.All(all.Items, e => Assert.Null(e.Department));

        var walked = new List<int>();
        string? after = null;
        var pages = 0;
        do
        {
            var page = Run(schema, db.Employees.AsNoTracking(), Req(sort: sort, paging: new DataListPaging.Cursor(after, 9)));
            walked.AddRange(page.Ids());
            after = page.NextCursor;
            pages++;
        }
        while (after is not null && pages < 100);

        Assert.Equal(all.Ids(), walked);
        Assert.Equal(130, walked.Distinct().Count());
        Assert.True(pages > 10);
    }

    [Fact]
    public void The_rows_carry_the_sort_values_and_the_key_read_by_the_database()
    {
        using var db = Database.Binary();
        var query = Schemas.Employees.Apply(db.Employees.AsNoTracking(), Req(sort: Sort(("department.name", false), ("lastLogin", true)), paging: new DataListPaging.Cursor(null, 5)));
        var rows = query.Rows.ToList();
        Assert.Equal(6, rows.Count);
        Assert.All(rows, r => Assert.Equal(2, r.SortValues.Length));
        Assert.All(rows, r => Assert.Equal((object)r.Item.Id, r.Key));
        Assert.Contains(rows, r => r.SortValues[0] is string);
        Assert.Contains(rows, r => r.SortValues[1] is DateTimeOffset);
    }

    [Fact]
    public void A_cursor_request_cannot_be_turned_into_a_result_from_loaded_entities()
    {
        using var db = Database.Binary();
        var request = Req(paging: new DataListPaging.Cursor(null, 5), sort: Sort(("age", false)));
        var rows = Schemas.Employees.Apply(db.Employees.AsNoTracking(), request).Page.ToList();
        var error = Assert.Throws<InvalidOperationException>(() => Schemas.Employees.ToResult(request, rows));
        Assert.Contains("Rows", error.Message);
    }

    [Fact]
    public void Offset_requests_still_take_the_entities_directly()
    {
        using var db = Database.Binary();
        var request = Req(paging: new DataListPaging.Offset(0, 5), sort: Sort(("age", false)));
        var query = Schemas.Employees.Apply(db.Employees.AsNoTracking(), request);
        var viaEntities = Schemas.Employees.ToResult(request, query.Page.ToList(), query.Matching.Count());
        var viaRows = Schemas.Employees.ToResult(request, query.Rows.ToList(), query.Matching.Count());
        Assert.Equal(viaEntities.Ids(), viaRows.Ids());
        Assert.Equal(130, viaEntities.TotalCount);
    }

    [Fact]
    public void An_offset_result_without_the_total_throws_instead_of_reporting_null()
    {
        using var db = Database.Binary();
        var request = Req(paging: new DataListPaging.Offset(0, 5));
        var query = Schemas.Employees.Apply(db.Employees.AsNoTracking(), request);
        Assert.True(query.WantsTotal);
        var entities = Assert.Throws<InvalidOperationException>(() => Schemas.Employees.ToResult(request, query.Page.ToList()));
        Assert.Contains("total", entities.Message);
        Assert.Throws<InvalidOperationException>(() => Schemas.Employees.ToResult(request, query.Rows.ToList()));
        // unpaged requests derive the total from the rows
        var all = Req();
        Assert.Equal(130, Schemas.Employees.ToResult(all, Schemas.Employees.Apply(db.Employees.AsNoTracking(), all).Page.ToList()).TotalCount);
    }
}
