using static Blux.UI.Tools.Query.Tests.TestData;

namespace Blux.UI.Tools.Query.Tests;

/// <summary>The row key breaks ties by its own type (2 before 10), in every paging mode, so a database that orders the key the same way gives the same pages.</summary>
public class RowKeyOrderTests
{
    private sealed record Item(int Id, int Group);

    private static readonly DataListSchema<Item> ItemSchema = DataListSchema.For<Item>(s => s
        .RowKey(e => e.Id)
        .Field(e => e.Group, f => f.Sortable()));

    private static readonly Item[] Items = [.. Enumerable.Range(1, 25).Select(i => new Item(i, i % 2)).Reverse()];

    [Fact]
    public void Offset_paging_breaks_ties_by_the_typed_key_whatever_the_input_order()
    {
        var request = new DataListRequest(new DataListPaging.Offset(0, 100), [new DataListSort("group")], DataListFilterGroup.Empty);
        var ids = ItemSchema.ExecuteInMemory(Items, request).Items.Select(i => i.Id).ToArray();
        Assert.Equal([2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25], ids);
    }

    [Fact]
    public void The_cursor_walks_keys_past_ten_in_numeric_order()
    {
        var sort = new[] { new DataListSort("group") };
        var seen = new List<int>();
        string? after = null;
        do
        {
            var page = ItemSchema.ExecuteInMemory(Items, new DataListRequest(new DataListPaging.Cursor(after, 4), sort, DataListFilterGroup.Empty));
            seen.AddRange(page.Items.Select(i => i.Id));
            after = page.NextCursor;
        }
        while (after is not null);
        Assert.Equal([2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25], seen);
    }

    [Fact]
    public void A_schema_without_a_key_keeps_the_input_order_of_ties()
    {
        var schema = DataListSchema.For<Item>(s => s.Field(e => e.Group, f => f.Sortable()));
        var request = new DataListRequest(DataListPaging.None.Instance, [new DataListSort("group")], DataListFilterGroup.Empty);
        var ids = schema.ExecuteInMemory(Items, request).Items.Select(i => i.Id).Take(3).ToArray();
        Assert.Equal([24, 22, 20], ids);
    }

    [Fact]
    public void A_key_the_cursor_cannot_carry_is_refused_with_a_clear_message()
    {
        var schema = DataListSchema.For<Keyed>(s => s.RowKey(e => e.Key));
        var error = Assert.Throws<InvalidOperationException>(() =>
            schema.ExecuteInMemory([new Keyed(new object())], new DataListRequest(new DataListPaging.Cursor(null, 5), [], DataListFilterGroup.Empty)));
        Assert.Contains("row key", error.Message);
    }

    private sealed record Keyed(object Key);
}
