using static Blux.UI.Tools.Query.Tests.TestData;

namespace Blux.UI.Tools.Query.Tests;

[Flags]
public enum Perm { None = 0, Read = 1, Write = 2, Admin = 4 }

public sealed record Item(int Id, string? Name = "x", double? Score = 0, Perm Perm = Perm.None, Status Status = Status.Active);

/// <summary>Cursor paging must visit every row once at the awkward sort values: "", null, undefined and flag enums, NaN and the infinities.</summary>
public class CursorBoundaryTests
{
    private static readonly DataListSchema<Item> ItemSchema = DataListSchema.For<Item>(s => s
        .RowKey(i => i.Id)
        .Field(i => i.Name, f => f.Sortable())
        .Field(i => i.Score, f => f.Sortable())
        .Field(i => i.Perm, f => f.Sortable())
        .Field(i => i.Status, f => f.Sortable()));

    private static int[] Walk(Item[] rows, DataListSort sort, int pageSize)
    {
        var seen = new List<int>();
        string? cursor = null;
        for (var guard = 0; guard < rows.Length + 3; guard++)
        {
            var page = ItemSchema.ExecuteInMemory(rows, new DataListRequest(new DataListPaging.Cursor(cursor, pageSize), [sort], DataListFilterGroup.Empty));
            seen.AddRange(page.Items.Select(i => i.Id));
            cursor = page.NextCursor;
            if (cursor is null) return [.. seen];
        }
        throw new InvalidOperationException("The cursor never ended.");
    }

    private static int[] Offset(Item[] rows, DataListSort sort) =>
        [.. ItemSchema.ExecuteInMemory(rows, new DataListRequest(DataListPaging.None.Instance, [sort], DataListFilterGroup.Empty)).Items.Select(i => i.Id)];

    private static void AssertWalkMatchesOffsetOrder(Item[] rows, string key)
    {
        foreach (var descending in new[] { false, true })
            foreach (var size in new[] { 1, 2, 3 })
            {
                var sort = new DataListSort(key, descending);
                Assert.Equal(Offset(rows, sort), Walk(rows, sort, size));
                Assert.Equal(rows.Length, Walk(rows, sort, size).Distinct().Count());
            }
    }

    [Fact]
    public void An_empty_string_is_not_null()
    {
        var rows = new[] { new Item(1, ""), new Item(2, "b"), new Item(3, "c"), new Item(4, null), new Item(5, "a") };
        Assert.Equal([1, 5, 2, 3, 4], Walk(rows, new DataListSort("name"), 1));
        Assert.Equal([3, 2, 5, 1, 4], Walk(rows, new DataListSort("name", true), 1));
        AssertWalkMatchesOffsetOrder(rows, "name");
    }

    [Fact]
    public void Several_empty_strings_and_nulls_in_a_row()
    {
        var rows = new[] { new Item(1, null), new Item(2, ""), new Item(3, null), new Item(4, ""), new Item(5, "z") };
        AssertWalkMatchesOffsetOrder(rows, "name");
        Assert.Equal([2, 4, 5, 1, 3], Walk(rows, new DataListSort("name"), 1));
    }

    [Fact]
    public void Undefined_and_flag_enums_round_trip()
    {
        var rows = new[]
        {
            new Item(1, Perm: Perm.Read | Perm.Admin), new Item(2, Perm: (Perm)64), new Item(3, Perm: Perm.None),
            new Item(4, Perm: Perm.Write), new Item(5, Perm: (Perm)(-1)), new Item(6, Status: (Status)99), new Item(7, Status: Status.Archived),
        };
        AssertWalkMatchesOffsetOrder(rows, "perm");
        AssertWalkMatchesOffsetOrder(rows, "status");
    }

    [Fact]
    public void NaN_infinities_and_null_round_trip()
    {
        var rows = new[]
        {
            new Item(1, Score: double.NaN), new Item(2, Score: 1.5), new Item(3, Score: null), new Item(4, Score: double.PositiveInfinity),
            new Item(5, Score: -1), new Item(6, Score: double.NegativeInfinity), new Item(7, Score: double.NaN), new Item(8, Score: 0.1 + 0.2),
        };
        AssertWalkMatchesOffsetOrder(rows, "score");
    }

    [Fact]
    public void A_cursor_of_an_unknown_shape_ends_the_walk_instead_of_restarting()
    {
        var rows = new[] { new Item(1, "a"), new Item(2, "b") };
        var first = ItemSchema.ExecuteInMemory(rows, new DataListRequest(new DataListPaging.Cursor(null, 1), [new DataListSort("name")], DataListFilterGroup.Empty));
        Assert.NotNull(first.NextCursor);
        var dead = ItemSchema.ExecuteInMemory(rows, new DataListRequest(new DataListPaging.Cursor("zzz", 1), [new DataListSort("name")], DataListFilterGroup.Empty));
        Assert.Empty(dead.Items);
        Assert.Null(dead.NextCursor);
    }
}
