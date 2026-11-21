using Microsoft.EntityFrameworkCore;
using static Blux.UI.Tools.Query.EfTests.H;

namespace Blux.UI.Tools.Query.EfTests;

public sealed class EmployeeRow
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int? Age { get; set; }

    public string? DepartmentName { get; set; }

    public int TagCount { get; set; }
}

/// <summary>
/// The recommended shape for an API: a schema over a flat row type that the query projects to. The page rows then carry every value a sort or a cursor needs,
/// whatever the entity graph looks like, and nothing has to be loaded with <c>Include</c>.
/// </summary>
[Collection("Database")]
public class ProjectionTests
{
    private static readonly DataListSchema<EmployeeRow> Schema = DataListSchema.For<EmployeeRow>(s => s
        .RowKey(e => e.Id)
        .Search(e => e.Name, e => e.DepartmentName)
        .Field(e => e.Age, f => f.Sortable().Filter(DataListFilter.Number(0, 1)))
        .Field(e => e.DepartmentName, f => f.Sortable().Filter(DataListFilter.Option()))
        .Field(e => e.TagCount, f => f.Sortable().Filter(DataListFilter.Number(0, 1))));

    private static IQueryable<EmployeeRow> Rows(QueryContext db) => db.Employees.AsNoTracking().Select(e => new EmployeeRow
    {
        Id = e.Id,
        Name = e.Name,
        Age = e.Age,
        DepartmentName = e.Department!.Name,
        TagCount = e.Tags.Count,
    });

    [Fact]
    public void A_projected_row_type_walks_a_cursor_sorted_by_a_joined_column_and_a_count()
    {
        using var db = Database.Binary();
        var memory = Rows(db).ToList();
        DataListSort[] sort = [new("departmentName", true), new("tagCount"), new("age", true)];
        var filter = DataListFilterGroup.And(Rule("age", FilterOperator.Gte, Num(20), includeEmpty: true));

        var expected = new List<int>();
        var actual = new List<int>();
        string? afterMemory = null;
        string? afterDb = null;
        for (var i = 0; i < 100; i++)
        {
            var m = Schema.ExecuteInMemory(memory, new DataListRequest(new DataListPaging.Cursor(afterMemory, 11), sort, filter));
            var d = Run(Schema, Rows(db), new DataListRequest(new DataListPaging.Cursor(afterDb, 11), sort, filter));
            Assert.Equal(m.Items.Select(r => r.Id), d.Items.Select(r => r.Id));
            Assert.Equal(m.NextCursor, d.NextCursor);
            expected.AddRange(m.Items.Select(r => r.Id));
            actual.AddRange(d.Items.Select(r => r.Id));
            afterMemory = m.NextCursor;
            afterDb = d.NextCursor;
            if (afterDb is null) break;
        }
        Assert.Equal(expected, actual);
        Assert.Equal(actual.Count, actual.Distinct().Count());
        Assert.True(actual.Count > 100);
    }

    [Fact]
    public void Facets_and_search_work_on_a_projection()
    {
        using var db = Database.Binary();
        var request = new DataListRequest(
            new DataListPaging.Offset(0, 20),
            [new DataListSort("age")],
            DataListFilterGroup.Empty,
            new DataListSearch("ng"),
            [new DataListFacetRequest("departmentName", IncludeMissing: true)]);
        var expected = Schema.ExecuteInMemory(Rows(db).ToList(), request);
        var actual = Run(Schema, Rows(db), request);
        Assert.Equal(expected.Items.Select(r => r.Id), actual.Items.Select(r => r.Id));
        Assert.Equal(expected.TotalCount, actual.TotalCount);
        Assert.Equal(expected.Facets, actual.Facets);
    }
}
