using static Blux.UI.Tools.Query.Tests.TestData;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.Tests;

public class InMemorySortTests
{
    private static int[] Sorted(params DataListSort[] sort) =>
        Schema.ExecuteInMemory(Employees, Request(null, null, sort)).Ids();

    [Fact]
    public void Text_sorts_naturally_ignoring_case_and_accents()
    {
        Assert.Equal([1, 2, 3, 4, 5, 6], Sorted(new DataListSort("name")));
        Assert.Equal([6, 5, 4, 3, 2, 1], Sorted(new DataListSort("name", true)));
    }

    [Fact]
    public void Nulls_sort_last_in_both_directions_and_ties_keep_the_source_order()
    {
        Assert.Equal([6, 2, 1, 5, 4, 3], Sorted(new DataListSort("age")));
        Assert.Equal([4, 1, 5, 2, 6, 3], Sorted(new DataListSort("age", true)));
    }

    [Fact]
    public void Multiple_keys_apply_in_order()
    {
        // status ascending (enum order), then age descending
        Assert.Equal([4, 1, 6, 5, 2, 3], Sorted(new DataListSort("status"), new DataListSort("age", true)));
    }

    [Fact]
    public void Dates_and_decimals_sort_by_value()
    {
        Assert.Equal([3, 1, 4, 2, 5, 6], Sorted(new DataListSort("hired")));
        Assert.Equal([4, 3, 1, 2, 5, 6], Sorted(new DataListSort("salary", true)));
    }

    [Fact]
    public void Sort_is_stable_for_many_equal_keys()
    {
        var rows = Enumerable.Range(1, 40).Select(i => Employees[0] with { Id = i, Age = i % 3 == 0 ? null : 10 }).ToArray();
        var asc = Schema.ExecuteInMemory(rows, Request(null, null, new DataListSort("age"))).Ids();
        var desc = Schema.ExecuteInMemory(rows, Request(null, null, new DataListSort("age", true))).Ids();
        var expected = rows.Where(r => r.Age is not null).Select(r => r.Id).Concat(rows.Where(r => r.Age is null).Select(r => r.Id)).ToArray();
        Assert.Equal(expected, asc);
        Assert.Equal(expected, desc);
    }

    [Fact]
    public void Sort_by_a_second_key_only_reorders_ties()
    {
        var rows = new[]
        {
            Employees[0] with { Id = 1, Age = 10, Name = "b" },
            Employees[0] with { Id = 2, Age = 10, Name = "a" },
            Employees[0] with { Id = 3, Age = 5, Name = "z" },
            Employees[0] with { Id = 4, Age = 10, Name = "a" },
        };
        var result = Schema.ExecuteInMemory(rows, Request(null, null, new DataListSort("age"), new DataListSort("name"))).Ids();
        Assert.Equal([3, 2, 4, 1], result);
    }

    [Theory]
    [InlineData("item2", "item10", -1)]
    [InlineData("item10", "item2", 1)]
    [InlineData("Item1", "item1", 0)]
    [InlineData("item01", "item1", 0)]
    [InlineData("a9", "a10", -1)]
    [InlineData("éclair", "Eclair", 0)]
    [InlineData("abc", "abd", -1)]
    [InlineData("ab", "abc", -1)]
    [InlineData("1", "a", -1)]
    [InlineData("x2y10", "x2y9", 1)]
    public void Natural_comparison(string a, string b, int expected) =>
        Assert.Equal(expected, SortValueComparerProxy.Compare(a, b));

    [Fact]
    public void Natural_order_applies_to_a_sorted_field()
    {
        var rows = new[] { "item10", "item2", "Item1", "item20" }.Select((n, i) => Employees[0] with { Id = i + 1, Name = n }).ToArray();
        var names = Schema.ExecuteInMemory(rows, Request(null, null, new DataListSort("name"))).Items.Select(e => e.Name);
        Assert.Equal(["Item1", "item2", "item10", "item20"], names);
    }

    [Fact]
    public void Default_sort_applies_when_the_request_has_none()
    {
        var schema = DataListSchema.For<Employee>(s => s.DefaultSort(e => e.Name, descending: true));
        Assert.Equal([6, 5, 4, 3, 2, 1], schema.ExecuteInMemory(Employees, Request()).Ids());
        Assert.Equal([1, 2, 3, 4, 5, 6], schema.ExecuteInMemory(Employees, Request(null, null, new DataListSort("name"))).Ids());
    }

    [Fact]
    public void Unknown_and_unsortable_keys_are_dropped()
    {
        Assert.Equal([1, 2, 3, 4, 5, 6], Sorted(new DataListSort("nope"), new DataListSort("email")));
    }

    /// <summary>The comparer is internal; the test reaches it through the sorted result of a one-field schema.</summary>
    private static class SortValueComparerProxy
    {
        public static int Compare(string a, string b)
        {
            var rows = new[] { Employees[0] with { Id = 1, Name = a }, Employees[0] with { Id = 2, Name = b } };
            var asc = Schema.ExecuteInMemory(rows, Request(null, null, new DataListSort("name"))).Ids();
            var desc = Schema.ExecuteInMemory(rows, Request(null, null, new DataListSort("name", true))).Ids();
            // equal keys keep the source order in both directions; different keys reverse
            if (asc.SequenceEqual(desc)) return 0;
            return asc[0] == 1 ? -1 : 1;
        }
    }
}

public class InMemoryPagingTests
{
    private static DataListRequest Offset(int skip, int take, params DataListSort[] sort) =>
        new(new DataListPaging.Offset(skip, take), sort, DataListFilterGroup.Empty);

    [Fact]
    public void Offset_paging_takes_a_slice_of_the_sorted_filtered_rows_and_counts_all_matches()
    {
        var result = Schema.ExecuteInMemory(Employees, Offset(2, 2, new DataListSort("name")));
        Assert.Equal([3, 4], result.Ids());
        Assert.Equal(6, result.TotalCount);
        Assert.Null(result.NextCursor);
        Assert.Empty(Schema.ExecuteInMemory(Employees, Offset(60, 2)).Items);
    }

    [Fact]
    public void Offset_number_is_the_one_based_page()
    {
        Assert.Equal(3, new DataListPaging.Offset(40, 20).Number);
        Assert.Equal(1, new DataListPaging.Offset(0, 20).Number);
        Assert.Equal(new DataListPaging.Offset(40, 20), DataListPaging.Offset.ForPage(3, 20));
    }

    [Fact]
    public void Page_size_is_clamped_to_the_schema_maximum_and_a_negative_skip_to_zero()
    {
        var schema = DataListSchema.For<Employee>(s => s.Field(e => e.Name, f => f.Sortable()).MaxPageSize(3).DefaultPageSize(2));
        Assert.Equal(3, schema.ExecuteInMemory(Employees, Offset(0, 100)).Items.Count);
        Assert.Equal(2, schema.ExecuteInMemory(Employees, Offset(0, 0)).Items.Count);
        Assert.Equal([1, 2], schema.ExecuteInMemory(Employees, Offset(-5, 2)).Ids());
    }

    [Fact]
    public void No_paging_returns_everything()
    {
        var result = Schema.ExecuteInMemory(Employees, Request());
        Assert.Equal(6, result.Items.Count);
        Assert.Equal(6, result.TotalCount);
    }

    [Fact]
    public void Cursor_pages_walk_the_whole_sorted_set_once()
    {
        var sort = new[] { new DataListSort("name") };
        var seen = new List<int>();
        string? cursor = null;
        var pages = 0;
        do
        {
            var result = Schema.ExecuteInMemory(Employees, new DataListRequest(new DataListPaging.Cursor(cursor, 2, true), sort, DataListFilterGroup.Empty));
            seen.AddRange(result.Ids());
            Assert.Equal(6, result.TotalCount);
            cursor = result.NextCursor;
            pages++;
        }
        while (cursor is not null && pages < 10);
        Assert.Equal([1, 2, 3, 4, 5, 6], seen);
        Assert.Equal(3, pages);
    }

    [Fact]
    public void Cursor_total_is_only_computed_on_request()
    {
        var result = Schema.ExecuteInMemory(Employees, new DataListRequest(new DataListPaging.Cursor(null, 2), [], DataListFilterGroup.Empty));
        Assert.Null(result.TotalCount);
        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public void Cursor_pages_break_ties_and_nulls_by_the_row_key()
    {
        var sort = new[] { new DataListSort("age") };
        var seen = new List<int>();
        string? cursor = null;
        var guard = 0;
        do
        {
            var result = Schema.ExecuteInMemory(Employees, new DataListRequest(new DataListPaging.Cursor(cursor, 2), sort, DataListFilterGroup.Empty));
            seen.AddRange(result.Ids());
            cursor = result.NextCursor;
        }
        while (cursor is not null && ++guard < 10);
        // ages 18, 25, 30, 30 (ties by row key), 41, then the null
        Assert.Equal([6, 2, 1, 5, 4, 3], seen);
    }

    [Fact]
    public void Cursor_descending_walks_the_reverse_order()
    {
        var sort = new[] { new DataListSort("salary", true) };
        var page1 = Schema.ExecuteInMemory(Employees, new DataListRequest(new DataListPaging.Cursor(null, 4), sort, DataListFilterGroup.Empty));
        Assert.Equal([4, 3, 1, 2], page1.Ids());
        var page2 = Schema.ExecuteInMemory(Employees, new DataListRequest(new DataListPaging.Cursor(page1.NextCursor, 4), sort, DataListFilterGroup.Empty));
        Assert.Equal([5, 6], page2.Ids());
        Assert.Null(page2.NextCursor);
    }

    [Fact]
    public void Cursor_survives_a_removed_row_because_it_is_keyset_not_positional()
    {
        var sort = new[] { new DataListSort("name") };
        var first = Schema.ExecuteInMemory(Employees, new DataListRequest(new DataListPaging.Cursor(null, 2), sort, DataListFilterGroup.Empty));
        var without3 = Employees.Where(e => e.Id != 3).ToArray();
        var second = Schema.ExecuteInMemory(without3, new DataListRequest(new DataListPaging.Cursor(first.NextCursor, 2), sort, DataListFilterGroup.Empty));
        Assert.Equal([4, 5], second.Ids());
    }

    [Theory]
    [InlineData("not base64 !!")]
    [InlineData("e30")]
    [InlineData("W10")]
    [InlineData("WyIxIl0")]
    public void A_cursor_that_does_not_decode_ends_the_walk_with_an_empty_page(string cursor)
    {
        var sort = new[] { new DataListSort("name") };
        var result = Schema.ExecuteInMemory(Employees, new DataListRequest(new DataListPaging.Cursor(cursor, 2, true), sort, DataListFilterGroup.Empty));
        Assert.Empty(result.Items);
        Assert.Null(result.NextCursor);
        Assert.Equal(6, result.TotalCount);
    }

    [Fact]
    public void An_empty_cursor_is_the_first_page()
    {
        var result = Schema.ExecuteInMemory(Employees, new DataListRequest(new DataListPaging.Cursor("", 2), [new DataListSort("name")], DataListFilterGroup.Empty));
        Assert.Equal([1, 2], result.Ids());
    }

    [Fact]
    public void A_cursor_made_for_another_sort_ends_the_walk_with_an_empty_page()
    {
        var byName = Schema.ExecuteInMemory(Employees, new DataListRequest(new DataListPaging.Cursor(null, 2), [new DataListSort("name")], DataListFilterGroup.Empty));
        var byAge = Schema.ExecuteInMemory(Employees, new DataListRequest(new DataListPaging.Cursor(byName.NextCursor, 2), [new DataListSort("age")], DataListFilterGroup.Empty));
        Assert.Empty(byAge.Items);
        Assert.Null(byAge.NextCursor);
    }

    [Fact]
    public void A_tampered_cursor_cannot_leave_the_filtered_set()
    {
        // The cursor carries a position, not rows: whatever it says, only rows matching the filter come back.
        var filter = DataListFilterGroup.And(Rule("active", Op.Is, new FilterValue.Boolean(true)));
        var sort = new[] { new DataListSort("name") };
        var first = Schema.ExecuteInMemory(Employees, new DataListRequest(new DataListPaging.Cursor(null, 1), sort, filter));
        var forged = first.NextCursor![..^1] + "A";
        var result = Schema.ExecuteInMemory(Employees, new DataListRequest(new DataListPaging.Cursor(forged, 10), sort, filter));
        Assert.All(result.Items, e => Assert.True(e.Active));
    }

    [Fact]
    public void Cursor_paging_needs_a_row_key()
    {
        var schema = DataListSchema.For<Employee>(s => s.Field(e => e.Name, f => f.Sortable()));
        var request = new DataListRequest(new DataListPaging.Cursor(null, 2), [], DataListFilterGroup.Empty);
        Assert.Throws<InvalidOperationException>(() => schema.ExecuteInMemory(Employees, request));
    }

    [Fact]
    public async Task In_memory_provider_answers_requests()
    {
        var provider = Schema.InMemoryProvider(Employees);
        var result = await provider(Offset(0, 2, new DataListSort("name", true)), CancellationToken.None);
        Assert.Equal([6, 5], result.Ids());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await provider(Offset(0, 2), new CancellationToken(true)));
    }
}

public class InMemoryFacetTests
{
    private static DataListResult<Employee> Run(DataListFilterGroup? filter, DataListSearch? search, params DataListFacetRequest[] facets) =>
        Schema.ExecuteInMemory(Employees, new DataListRequest(DataListPaging.None.Instance, [], filter ?? DataListFilterGroup.Empty, search, facets));

    private static DataListFacetResult Facet(DataListResult<Employee> r, string key) => r.Facets!.Single(f => f.Key == key);

    [Fact]
    public void Counts_per_option_by_count_then_value()
    {
        var facet = Facet(Run(null, null, new DataListFacetRequest("status")), "status");
        Assert.Equal([new DataListFacetOption("Active", 3), new DataListFacetOption("Pending", 2), new DataListFacetOption("Archived", 1)], facet.Options);
        Assert.Equal(3, facet.Total);
        Assert.Null(facet.Missing);
    }

    [Fact]
    public void A_facet_leaves_out_its_own_filter_by_default_and_keeps_it_on_request()
    {
        var filter = DataListFilterGroup.And(
            Rule("active", Op.Is, new FilterValue.Boolean(true)),
            Rule("status", Op.IsAnyOf, List("Active")));
        var exclude = Facet(Run(filter, null, new DataListFacetRequest("status")), "status");
        Assert.Equal([new DataListFacetOption("Active", 3), new DataListFacetOption("Pending", 1)], exclude.Options);

        var include = Facet(Run(filter, null, new DataListFacetRequest("status", FacetMode.IncludeSelf)), "status");
        Assert.Equal([new DataListFacetOption("Active", 3)], include.Options);
    }

    [Fact]
    public void Rows_are_still_filtered_by_every_rule_including_the_facets_own()
    {
        var filter = DataListFilterGroup.And(Rule("status", Op.IsAnyOf, List("Pending")));
        var result = Run(filter, null, new DataListFacetRequest("status"));
        Assert.Equal([2, 5], result.Ids().Order());
        Assert.Equal(3, Facet(result, "status").Total);
    }

    [Fact]
    public void Search_applies_to_the_counts()
    {
        var facet = Facet(Run(null, new DataListSearch("acme"), new DataListFacetRequest("status")), "status");
        Assert.Equal([new DataListFacetOption("Active", 2)], facet.Options);
    }

    [Fact]
    public void Missing_counts_null_and_empty_values()
    {
        var facet = Facet(Run(null, null, new DataListFacetRequest("department.name", IncludeMissing: true)), "department.name");
        Assert.Equal([new DataListFacetOption("Engineering", 2), new DataListFacetOption("Sales", 2)], facet.Options);
        Assert.Equal(2, facet.Missing);
    }

    [Fact]
    public void Limit_skip_and_search_window_the_options()
    {
        var limited = Facet(Run(null, null, new DataListFacetRequest("department.name", Limit: 1)), "department.name");
        Assert.Equal([new DataListFacetOption("Engineering", 2)], limited.Options);
        Assert.Equal(2, limited.Total);

        var skipped = Facet(Run(null, null, new DataListFacetRequest("department.name", Limit: 1, Skip: 1)), "department.name");
        Assert.Equal([new DataListFacetOption("Sales", 2)], skipped.Options);

        var searched = Facet(Run(null, null, new DataListFacetRequest("department.name", Search: "SAL")), "department.name");
        Assert.Equal([new DataListFacetOption("Sales", 2)], searched.Options);
        Assert.Equal(1, searched.Total);
    }

    [Fact]
    public void Boolean_values_are_true_and_false()
    {
        var facet = Facet(Run(null, null, new DataListFacetRequest("active")), "active");
        Assert.Equal([new DataListFacetOption("true", 4), new DataListFacetOption("false", 2)], facet.Options);
    }

    [Fact]
    public void Facets_on_other_fields_are_dropped_and_no_facets_gives_null()
    {
        Assert.Null(Run(null, null, new DataListFacetRequest("age"), new DataListFacetRequest("nope")).Facets);
        Assert.Null(Run(null, null).Facets);
    }

    [Fact]
    public void Query_only_filters_do_not_change_counts()
    {
        var filter = DataListFilterGroup.And(Rule("archived", Op.Is, new FilterValue.Boolean(true)));
        var facet = Facet(Run(filter, null, new DataListFacetRequest("status")), "status");
        Assert.Equal(3, facet.Total);
    }
}
