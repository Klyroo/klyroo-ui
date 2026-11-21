using Microsoft.EntityFrameworkCore;
using static Blux.UI.Tools.Query.EfTests.H;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.EfTests;

/// <summary>
/// What the provider is asked to run: every user value is a parameter, option lists are <c>IN</c>, nothing is lowercased, and a query EF cannot translate fails
/// instead of being evaluated on the client.
/// </summary>
[Collection("Database")]
public class TranslationTests
{
    /// <summary>The SQL of a query without the <c>.param set</c> lines that list the parameter values.</summary>
    private static string Sql(IQueryable query) =>
        string.Join('\n', query.ToQueryString().Split('\n').Select(l => l.TrimEnd('\r')).Where(l => !l.StartsWith(".param", StringComparison.Ordinal)));

    private static string Parameters(IQueryable query) =>
        string.Join('\n', query.ToQueryString().Split('\n').Where(l => l.StartsWith(".param", StringComparison.Ordinal)));

    private static DataListQuery<Employee> Apply(QueryContext db, DataListRequest request) =>
        Schemas.Employees.Apply(db.Employees.AsNoTracking(), request);

    [Fact]
    public void User_values_are_parameters_not_literals()
    {
        using var db = Database.Binary();
        var request = Req(
            filter: DataListFilterGroup.And(
                Rule("name", Op.Contains, Text("Zq9ZqContains")),
                Rule("email", Op.Is, Text("zq9zq@exact.test")),
                Rule("id", Op.IsAnyOf, List("7771", "7772", "7773")),
                Rule("age", Op.Gt, Num(7774)),
                Rule("salary", Op.Lte, Num(7775.5m)),
                Rule("hired", Op.After, Day("2077-07-07")),
                Rule("lastLogin", Op.Is, At("2077-07-07T07:07:07Z")),
                Rule("status", Op.IsAnyOf, List("Pending", "Archived")),
                Rule("tags.name", Op.IsAnyOf, List("zq9zq-tag-a", "zq9zq-tag-b")),
                Rule("tagText", Op.Contains, Text("zq9zq-tag-c")),
                Rule("tags.weight", Op.Gte, Num(7776))),
            search: "Zq9ZqSearch",
            paging: new DataListPaging.Offset(4242, 17));
        var page = Apply(db, request).Page;

        var sql = Sql(page);
        string[] leaks = ["Zq9Zq", "zq9zq", "7771", "7774", "7775", "7776", "2077", "4242", "Pending", "Archived"];
        foreach (var leak in leaks) Assert.DoesNotContain(leak, sql);
        Assert.Matches(@"@p\d*", sql);
        Assert.Contains(" IN (", sql);
        // the values are bound
        var parameters = Parameters(page);
        foreach (var bound in new[] { "Zq9ZqContains", "zq9zq@exact.test", "zq9zq-tag-a", "Zq9ZqSearch", "4242", "7774" }) Assert.Contains(bound, parameters);
    }

    [Fact]
    public void Offset_and_page_size_are_parameters_so_pages_share_a_plan()
    {
        using var db = Database.Binary();
        var sql = Sql(Apply(db, Req(paging: new DataListPaging.Offset(100, 25), sort: Sort(("age", false)))).Page);
        Assert.Matches(@"LIMIT @p\d* OFFSET @p\d*", sql);
    }

    [Fact]
    public void Text_is_not_lowercased_so_the_collation_decides()
    {
        using var db = Database.Binary();
        var sql = Sql(Apply(db, Req(rule: Rule("name", Op.Contains, Text("Young")), search: "Young")).Page);
        Assert.DoesNotContain("lower(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("upper(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("collate", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Nulls_sort_last_in_both_directions_with_a_leading_flag()
    {
        using var db = Database.Binary();
        foreach (var descending in new[] { false, true })
        {
            var sql = Sql(Apply(db, Req(sort: Sort(("age", descending)))).Page);
            var orderBy = sql[sql.IndexOf("ORDER BY", StringComparison.Ordinal)..];
            Assert.Matches(@"ORDER BY CASE\s+WHEN .* IS NULL THEN 1\s+ELSE 0\s+END, .*" + (descending ? "DESC" : "") + @".*, .*\.""Id""", orderBy.Replace("\n", " "));
        }
    }

    [Fact]
    public void The_count_query_is_not_ordered_or_paged()
    {
        using var db = Database.Binary();
        var sql = Sql(Apply(db, Req(rule: Rule("age", Op.Gt, Num(20)), paging: new DataListPaging.Offset(10, 10), sort: Sort(("age", true)))).Matching);
        Assert.DoesNotContain("ORDER BY", sql);
        Assert.DoesNotContain("LIMIT", sql);
    }

    [Fact]
    public void Collections_become_exists_subqueries()
    {
        using var db = Database.Binary();
        var sql = Sql(Apply(db, Req(rule: Rule("tags.name", Op.IsAnyOf, List("dotnet", "css")))).Page);
        Assert.Contains("EXISTS", sql);
        Assert.Contains("FROM \"Tag\"", sql);
    }

    [Fact]
    public void Paths_through_a_navigation_join_in_the_database()
    {
        using var db = Database.Binary();
        var sql = Sql(Apply(db, Req(rule: Rule("department.name", Op.Is, Text("Sales")), sort: Sort(("department.name", false)))).Page);
        Assert.Contains("LEFT JOIN \"Depts\"", sql);
    }

    // ---- fail fast

    [Fact]
    public void An_in_memory_queryable_is_refused_with_a_pointer_to_the_executor()
    {
        var rows = Seed.Employees().AsQueryable();
        var apply = Assert.Throws<InvalidOperationException>(() => Schemas.Employees.Apply(rows, Req()));
        Assert.Contains("ExecuteInMemory", apply.Message);
        var facets = Assert.Throws<InvalidOperationException>(() => Schemas.Employees.FacetQueries(rows, Req()));
        Assert.Contains("ExecuteInMemory", facets.Message);
    }

    [Fact]
    public void Cursor_paging_without_a_row_key_throws_when_the_query_is_built()
    {
        using var db = Database.Binary();
        var schema = DataListSchema.For<Employee>(s => s.Field(e => e.Age, f => f.Sortable()));
        var error = Assert.Throws<InvalidOperationException>(() => schema.Apply(db.Employees, Req(paging: new DataListPaging.Cursor(null, 10))));
        Assert.Contains("row key", error.Message);
    }

    [Fact]
    public void A_collection_facet_without_a_row_key_throws()
    {
        using var db = Database.Binary();
        var schema = DataListSchema.For<Employee>(s => s.Collection(e => e.Tags, t => t.Name, f => f.Filter(DataListFilter.Option())));
        var request = Req(facets: [new DataListFacetRequest("tags.name")]);
        var error = Assert.Throws<InvalidOperationException>(() => schema.FacetQueries(db.Employees, request));
        Assert.Contains("row key", error.Message);
    }

    [Fact]
    public void A_guid_key_cannot_carry_a_keyset_cursor_on_a_database()
    {
        using var db = Database.Binary();
        var schema = DataListSchema.For<Document>(s => s.RowKey(e => e.Id).Field(e => e.Title, f => f.Sortable()));
        var error = Assert.Throws<InvalidOperationException>(() => schema.Apply(db.Documents, Req(paging: new DataListPaging.Cursor(null, 10))));
        Assert.Contains("Guid", error.Message);
        // offset paging is fine: the key only breaks ties
        Assert.Empty(schema.Apply(db.Documents, Req(paging: new DataListPaging.Offset(0, 10))).Page.ToList());
    }

    [Fact]
    public void Without_a_converter_sqlite_refuses_to_compare_instants_instead_of_evaluating_on_the_client()
    {
        using var db = Database.Plain();
        var comparing = Apply(db, Req(rule: Rule("lastLogin", Op.After, At("2024-03-02T00:00:00Z")))).Page;
        var error = Assert.Throws<InvalidOperationException>(() => comparing.ToList());
        Assert.Contains("could not be translated", error.Message);

        var ordering = Apply(db, Req(sort: Sort(("lastLogin", false)))).Page;
        var orderError = Assert.Throws<NotSupportedException>(() => ordering.ToList());
        Assert.Contains("ORDER BY", orderError.Message);
    }

    [Fact]
    public void Decimals_need_no_converter_on_sqlite()
    {
        using var db = Database.Plain();
        var page = Apply(db, Req(rule: Rule("salary", Op.Between, Range(Num(4000), Num(6100))), sort: Sort(("salary", true)))).Page.ToList();
        Assert.NotEmpty(page);
        Assert.Equal(page.OrderByDescending(e => e.Salary).Select(e => e.Id), page.Select(e => e.Id));
        Assert.All(page, e => Assert.InRange(e.Salary, 4000m, 6100m));
    }
}
