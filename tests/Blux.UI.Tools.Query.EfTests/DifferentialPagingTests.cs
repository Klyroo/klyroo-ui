using static Blux.UI.Tools.Query.EfTests.H;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.EfTests;

/// <summary>Sort (nulls last in both directions, row key tie-break), offset paging and keyset cursor paging, against SQLite and in memory, on 130 rows full of ties and nulls.</summary>
[Collection("Database")]
public class DifferentialPagingTests
{
    private static readonly Dictionary<string, DataListSort[]> Sorts = new()
    {
        ["none"] = [],
        ["age asc"] = Sort(("age", false)),
        ["age desc"] = Sort(("age", true)),
        ["salary desc"] = Sort(("salary", true)),
        ["score asc then hired desc"] = Sort(("score", false), ("hired", true)),
        ["active then status desc then age"] = Sort(("active", false), ("status", true), ("age", false)),
        ["lastLogin asc"] = Sort(("lastLogin", false)),
        ["lastLogin desc"] = Sort(("lastLogin", true)),
        ["department then age desc"] = Sort(("department.name", false), ("age", true)),
        ["hired desc"] = Sort(("hired", true)),
    };

    public static TheoryData<string> SortNames() => [.. Sorts.Keys];

    [Theory]
    [MemberData(nameof(SortNames))]
    public void Offset_pages_are_the_same(string sort)
    {
        using var db = Database.Binary();
        var rows = Load(db);
        foreach (var (skip, take) in new[] { (0, 10), (10, 10), (35, 20), (120, 50), (500, 10) })
            AssertSame(db, rows, Req(paging: new DataListPaging.Offset(skip, take), sort: Sorts[sort]));
        AssertSame(db, rows, Req(sort: Sorts[sort]));
    }

    [Theory]
    [MemberData(nameof(SortNames))]
    public void Filtered_sorted_offset_pages_are_the_same(string sort)
    {
        using var db = Database.Binary();
        var rows = Load(db);
        var filter = DataListFilterGroup.And(Rule("status", Op.IsNot, Text("Archived")), Rule("age", Op.Gte, Num(20), includeEmpty: true));
        foreach (var skip in new[] { 0, 20, 60 })
            AssertSame(db, rows, Req(filter: filter, paging: new DataListPaging.Offset(skip, 20), sort: Sorts[sort], search: "n"));
    }

    [Fact]
    public void Nulls_come_last_whatever_the_direction()
    {
        using var db = Database.Binary();
        foreach (var descending in new[] { false, true })
        {
            var ages = Run(db, Req(sort: Sort(("age", descending)))).Items.Select(e => e.Age).ToList();
            var firstNull = ages.IndexOf(null);
            Assert.True(firstNull > 0);
            Assert.All(ages.Skip(firstNull), a => Assert.Null(a));
            var values = ages.Take(firstNull).Select(a => a!.Value).ToList();
            Assert.Equal(descending ? values.OrderByDescending(a => a) : values.Order(), values);
        }
    }

    [Fact]
    public void Ties_are_broken_by_the_key_by_its_type_so_ten_comes_after_two()
    {
        using var db = Database.Binary();
        var ids = Run(db, Req(rule: Rule("age", Op.Is, Num(18)), sort: Sort(("age", false)))).Ids();
        Assert.True(ids.Length > 3);
        Assert.Equal(ids.Order(), ids);
        Assert.Contains(ids, id => id >= 10);
    }

    // ---- cursor

    private static List<DataListResult<Employee>> Walk(Func<DataListRequest, DataListResult<Employee>> run, DataListSort[] sort, DataListFilterGroup? filter, int take, bool count)
    {
        var pages = new List<DataListResult<Employee>>();
        string? after = null;
        do
        {
            var page = run(Req(filter: filter, paging: new DataListPaging.Cursor(after, take, count), sort: sort));
            pages.Add(page);
            after = page.NextCursor;
        }
        while (after is not null && pages.Count < 200);
        return pages;
    }

    [Theory]
    [MemberData(nameof(SortNames))]
    public void Walking_the_cursor_gives_the_same_pages_and_every_row_once(string sort)
    {
        using var db = Database.Binary();
        var rows = Load(db);
        var expected = Walk(r => Memory(rows, r), Sorts[sort], null, 7, count: true);
        var actual = Walk(r => Run(db, r), Sorts[sort], null, 7, count: true);

        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Ids(), actual[i].Ids());
            Assert.Equal(expected[i].NextCursor, actual[i].NextCursor);
            Assert.Equal(expected[i].TotalCount, actual[i].TotalCount);
        }
        var all = actual.SelectMany(p => p.Ids()).ToList();
        Assert.Equal(rows.Count, all.Count);
        Assert.Equal(rows.Count, all.Distinct().Count());
    }

    [Theory]
    [InlineData("age asc")]
    [InlineData("lastLogin desc")]
    [InlineData("department then age desc")]
    public void Walking_a_filtered_cursor_with_a_page_size_of_one_matches(string sort)
    {
        using var db = Database.Binary();
        var rows = Load(db);
        var filter = DataListFilterGroup.And(Rule("active", Op.Is, Bool(true)), Rule("tags.name", Op.IsNot, List("css")));
        var expected = Walk(r => Memory(rows, r), Sorts[sort], filter, 1, count: false).SelectMany(p => p.Ids()).ToList();
        var actual = Walk(r => Run(db, r), Sorts[sort], filter, 1, count: false).SelectMany(p => p.Ids()).ToList();
        Assert.Equal(expected, actual);
        Assert.NotEmpty(actual);
    }

    [Fact]
    public void A_cursor_has_no_total_unless_asked()
    {
        using var db = Database.Binary();
        var withoutCount = Run(db, Req(paging: new DataListPaging.Cursor(null, 5)));
        Assert.Null(withoutCount.TotalCount);
        Assert.NotNull(withoutCount.NextCursor);
        Assert.Equal(130, Run(db, Req(paging: new DataListPaging.Cursor(null, 5, Count: true))).TotalCount);
    }

    [Fact]
    public void A_cursor_made_for_another_sort_or_not_decoding_ends_the_walk()
    {
        using var db = Database.Binary();
        var rows = Load(db);
        var cursor = Run(db, Req(paging: new DataListPaging.Cursor(null, 5), sort: Sort(("age", false)))).NextCursor;
        foreach (var after in new[] { cursor!, "not a cursor", new string('x', 5000) })
        {
            var request = Req(paging: new DataListPaging.Cursor(after, 5, Count: true), sort: Sort(("age", true)));
            var actual = Run(db, request);
            Assert.Empty(actual.Items);
            Assert.Null(actual.NextCursor);
            AssertSame(db, rows, request);
        }
    }

    [Fact]
    public void The_last_page_has_no_next_cursor_and_a_full_last_page_is_not_followed_by_an_empty_one()
    {
        using var db = Database.Binary();
        var rows = Load(db);
        var pages = Walk(r => Run(db, r), Sort(("age", false)), DataListFilterGroup.And(Rule("age", Op.Lte, Num(18))), 1000, count: false);
        Assert.Single(pages);
        Assert.Null(pages[0].NextCursor);
        var exact = rows.Count(e => e.Age <= 18);
        var oneToOne = Walk(r => Run(db, r), Sort(("age", false)), DataListFilterGroup.And(Rule("age", Op.Lte, Num(18))), exact, count: false);
        Assert.Single(oneToOne);
        Assert.Equal(exact, oneToOne[0].Items.Count);
    }

    [Fact]
    public void Text_sort_follows_the_database_order_with_the_key_as_tie_break()
    {
        // The in-memory executor sorts text naturally (case and accents ignored, 2 before 10); a database sorts by its collation, here SQLite's binary one.
        using var db = Database.Binary();
        var rows = Load(db);
        var expected = rows.OrderBy(e => e.Name, StringComparer.Ordinal).ThenBy(e => e.Id).Select(e => e.Id).ToArray();
        Assert.Equal(expected, Run(db, Req(sort: Sort(("name", false)))).Ids());

        var walked = Walk(r => Run(db, r), Sort(("name", false)), null, 9, count: false).SelectMany(p => p.Ids()).ToArray();
        Assert.Equal(expected, walked);

        var descending = rows.OrderByDescending(e => e.Name, StringComparer.Ordinal).ThenBy(e => e.Id).Select(e => e.Id).ToArray();
        Assert.Equal(descending, Walk(r => Run(db, r), Sort(("name", true)), null, 9, count: false).SelectMany(p => p.Ids()).ToArray());
    }

    [Fact]
    public void Default_sort_applies_when_the_request_has_none()
    {
        using var db = Database.Binary();
        var schema = DataListSchema.For<Employee>(s => s.RowKey(e => e.Id).Field(e => e.Age, f => f.Sortable()).DefaultSort(e => e.Age, descending: true));
        var request = Req(paging: new DataListPaging.Offset(0, 20));
        Assert.Equal(
            schema.ExecuteInMemory(Load(db), request).Items.Select(e => e.Id),
            Run(schema, db.Employees, request).Items.Select(e => e.Id));
    }
}
