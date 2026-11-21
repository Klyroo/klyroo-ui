namespace Blux.UI.Tools.Query.Tests;

/// <summary>Every type a cursor accepts, on values one tick, one ulp or one step apart: the walk must visit each row once, in order, and end.</summary>
public class CursorTypeTests
{
    private sealed record Row(
        int Id, TimeOnly Time, TimeSpan Span, DateTime Stamp, DateTimeOffset Moment, DateOnly Day, decimal Amount, double Ratio, float Single, Guid Code,
        char Letter, byte B, sbyte Sb, short S, ushort Us, int I, uint Ui, long L, ulong Ul, bool Flag, Mode Mode, string Text);

    private enum Mode : byte { A = 1, B = 2, C = 200 }

    private static readonly DataListSchema<Row> Schema = DataListSchema.For<Row>(s => s
        .RowKey(r => r.Id)
        .Field(r => r.Time, f => f.Sortable())
        .Field(r => r.Span, f => f.Sortable())
        .Field(r => r.Stamp, f => f.Sortable())
        .Field(r => r.Moment, f => f.Sortable())
        .Field(r => r.Day, f => f.Sortable())
        .Field(r => r.Amount, f => f.Sortable())
        .Field(r => r.Ratio, f => f.Sortable())
        .Field(r => r.Single, f => f.Sortable())
        .Field(r => r.Code, f => f.Sortable())
        .Field(r => r.Letter, f => f.Sortable())
        .Field(r => r.B, f => f.Sortable())
        .Field(r => r.Sb, f => f.Sortable())
        .Field(r => r.S, f => f.Sortable())
        .Field(r => r.Us, f => f.Sortable())
        .Field(r => r.I, f => f.Sortable())
        .Field(r => r.Ui, f => f.Sortable())
        .Field(r => r.L, f => f.Sortable())
        .Field(r => r.Ul, f => f.Sortable())
        .Field(r => r.Flag, f => f.Sortable())
        .Field(r => r.Mode, f => f.Sortable())
        .Field(r => r.Text, f => f.Sortable()));

    private static readonly Row[] Rows = [.. Enumerable.Range(0, 36).Select(Make)];

    private static Row Make(int i)
    {
        var k = i * 7 % 12;
        var ratio = 0.1;
        var single = 0.1f;
        for (var j = 0; j < k; j++)
        {
            ratio = Math.BitIncrement(ratio);
            single = MathF.BitIncrement(single);
        }
        return new Row(
            i + 1,
            new TimeOnly(14, 30, 45).Add(TimeSpan.FromTicks(k)),
            TimeSpan.FromDays(1).Add(TimeSpan.FromTicks(k)),
            new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc).AddTicks(k),
            new DateTimeOffset(2024, 3, 1, 10, 0, 0, TimeSpan.FromHours(2)).AddTicks(k),
            new DateOnly(2024, 3, 1).AddDays(k),
            1.0000000m + k * 0.0000001m,
            ratio,
            single,
            new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)k),
            (char)('a' + k),
            (byte)(244 + k),
            (sbyte)(sbyte.MaxValue - k),
            (short)(short.MaxValue - k),
            (ushort)(ushort.MaxValue - k),
            int.MaxValue - k,
            uint.MaxValue - (uint)k,
            long.MaxValue - k,
            ulong.MaxValue - (ulong)k,
            k % 2 == 0,
            (Mode)new[] { 1, 2, 200 }[k % 3],
            "t" + k.ToString("00"));
    }

    public static TheoryData<string> Keys() =>
        ["time", "span", "stamp", "moment", "day", "amount", "ratio", "single", "code", "letter", "b", "sb", "s", "us", "i", "ui", "l", "ul", "flag", "mode", "text"];

    [Theory]
    [MemberData(nameof(Keys))]
    public void The_cursor_walk_visits_every_row_once_in_order(string key)
    {
        foreach (var descending in new[] { false, true })
        {
            DataListSort[] sort = [new(key, descending)];
            var expected = Schema.ExecuteInMemory(Rows, new DataListRequest(new DataListPaging.Offset(0, 100), sort, DataListFilterGroup.Empty)).Items.Select(r => r.Id).ToList();
            foreach (var take in new[] { 1, 4, 5 })
            {
                var walked = new List<int>();
                string? after = null;
                for (var i = 0; i < 100; i++)
                {
                    var page = Schema.ExecuteInMemory(Rows, new DataListRequest(new DataListPaging.Cursor(after, take), sort, DataListFilterGroup.Empty));
                    walked.AddRange(page.Items.Select(r => r.Id));
                    after = page.NextCursor;
                    if (after is null) break;
                }
                Assert.Null(after);
                Assert.Equal(expected, walked);
                Assert.Equal(36, walked.Distinct().Count());
            }
        }
    }

    [Fact]
    public void A_time_of_day_keeps_its_seconds_and_ticks_in_the_cursor()
    {
        Assert.Equal("14:30:45.0000000", CursorValue.Format(new TimeOnly(14, 30, 45)));
        Assert.Equal("14:30:45.1234567", CursorValue.Format(new TimeOnly(14, 30, 45).Add(TimeSpan.FromTicks(1234567))));
        Assert.True(CursorValue.TryParse("14:30:45.1234567", typeof(TimeOnly), out var parsed));
        Assert.Equal(new TimeOnly(14, 30, 45).Add(TimeSpan.FromTicks(1234567)), parsed);
    }
}
