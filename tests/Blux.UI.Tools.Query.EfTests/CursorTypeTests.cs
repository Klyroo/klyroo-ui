using Microsoft.EntityFrameworkCore;
using static Blux.UI.Tools.Query.EfTests.H;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.EfTests;

/// <summary>
/// A cursor walk over every type a cursor can carry, on values that differ only in the last unit of precision (a tick, one ulp): a lossy cursor format would
/// repeat or skip rows. SQLite and the in-memory executor must walk the same pages, and every row exactly once, in both directions.
/// </summary>
[Collection("Database")]
public class CursorTypeTests
{
    public static TheoryData<string> Keys() =>
        ["time", "span", "stamp", "moment", "day", "amount", "ratio", "single", "letter", "tiny", "small", "big", "level", "flag", "text", "shiftStart"];

    [Theory]
    [MemberData(nameof(Keys))]
    public void Walking_a_cursor_on_the_type_visits_every_row_once_in_the_same_order_as_memory(string key)
    {
        using var db = Database.Binary();
        var rows = db.Readings.AsNoTracking().OrderBy(r => r.Id).ToList();
        foreach (var descending in new[] { false, true })
        {
            DataListSort[] sort = [new(key, descending)];
            var offset = Run(Schemas.Readings, db.Readings.AsNoTracking(), new DataListRequest(new DataListPaging.Offset(0, 100), sort, DataListFilterGroup.Empty));
            var memory = Schemas.Readings.ExecuteInMemory(rows, new DataListRequest(new DataListPaging.Offset(0, 100), sort, DataListFilterGroup.Empty));
            Assert.Equal(memory.Items.Select(r => r.Id), offset.Items.Select(r => r.Id));

            foreach (var take in new[] { 1, 4, 5 })
            {
                var walkedDb = Walk(r => Run(Schemas.Readings, db.Readings.AsNoTracking(), r), sort, take);
                var walkedMemory = Walk(r => Schemas.Readings.ExecuteInMemory(rows, r), sort, take);
                Assert.Equal(memory.Items.Select(r => r.Id), walkedMemory);
                Assert.Equal(memory.Items.Select(r => r.Id), walkedDb);
                Assert.Equal(36, walkedDb.Distinct().Count());
            }
        }
    }

    private static List<int> Walk(Func<DataListRequest, DataListResult<Reading>> run, DataListSort[] sort, int take)
    {
        var ids = new List<int>();
        string? after = null;
        for (var i = 0; i < 100; i++)
        {
            var page = run(new DataListRequest(new DataListPaging.Cursor(after, take), sort, DataListFilterGroup.Empty));
            ids.AddRange(page.Items.Select(r => r.Id));
            after = page.NextCursor;
            if (after is null) return ids;
        }
        throw new Xunit.Sdk.XunitException("the cursor never ended: the walk loops");
    }

    [Fact]
    public void The_values_really_differ_only_in_the_last_unit()
    {
        var rows = Seed.Readings();
        Assert.Equal(12, rows.Select(r => r.Time).Distinct().Count());
        Assert.True(rows.Max(r => r.Time) - rows.Min(r => r.Time) < TimeSpan.FromTicks(12));
        Assert.True(rows.Max(r => r.Stamp) - rows.Min(r => r.Stamp) < TimeSpan.FromTicks(12));
        Assert.Equal(12, rows.Select(r => r.Ratio).Distinct().Count());
        Assert.Equal(12, rows.Select(r => r.Single).Distinct().Count());
        Assert.Equal(12, rows.Select(r => r.Amount).Distinct().Count());
    }
}

/// <summary>StoresLocalTime in practice: wall-clock DateTime columns and the bounds of the request's time zone.</summary>
[Collection("Database")]
public class LocalTimeTests
{
    private static int[] Ids(DataListResult<Reading> r) => [.. r.Items.Select(x => x.Id)];

    private static (int[] Memory, int[] Database) Both(QueryContext db, FilterRule rule, string? zone)
    {
        var request = Req(rule: rule, zone: zone);
        var rows = db.Readings.AsNoTracking().OrderBy(r => r.Id).ToList();
        return (Ids(Schemas.Readings.ExecuteInMemory(rows, request)), Ids(Run(Schemas.Readings, db.Readings.AsNoTracking(), request)));
    }

    [Theory]
    [InlineData("Europe/Paris")]
    [InlineData("America/New_York")]
    [InlineData("Asia/Kolkata")]
    [InlineData(null)]
    public void A_local_time_field_compares_wall_clock_bounds_in_memory_and_in_the_database(string? zone)
    {
        using var db = Database.Binary();
        foreach (var op in new[] { Op.Is, Op.IsNot, Op.Before, Op.After })
        {
            var (memory, database) = Both(db, Rule("shiftStart", op, Day("2024-03-31")), zone);
            Assert.Equal(memory, database);
            var (memoryAt, databaseAt) = Both(db, Rule("shiftStart", op, At("2024-03-30T23:30:00Z")), zone);
            Assert.Equal(memoryAt, databaseAt);
        }
        var (m, d) = Both(db, Rule("shiftStart", Op.Between, Range(Day("2024-03-30"), Day("2024-03-30"))), zone);
        Assert.Equal(m, d);
    }

    [Fact]
    public void The_day_is_the_local_midnight_to_midnight_whatever_the_daylight_saving_change()
    {
        using var db = Database.Binary();
        // wall-clock stamps run from 22:30 on 03-30 to 04:00 on 03-31 (k = 0..11); Paris changes clocks on 03-31, but a wall-clock day is still midnight to midnight
        var (memory, database) = Both(db, Rule("shiftStart", Op.Is, Day("2024-03-31")), "Europe/Paris");
        var expected = Seed.Readings().Where(r => r.ShiftStart.Date == new DateTime(2024, 3, 31)).Select(r => r.Id).Order().ToArray();
        Assert.NotEmpty(expected);
        Assert.Equal(expected, memory.Order());
        Assert.Equal(expected, database.Order());
    }

    [Fact]
    public void An_hour_is_cut_in_the_request_zone_and_read_as_wall_clock()
    {
        using var db = Database.Binary();
        // 2024-03-30T23:30Z is 00:30 on 03-31 in Paris (UTC+1 that evening), so the hour is [00:00, 01:00) of the wall clock
        var (_, paris) = Both(db, Rule("shiftStart", Op.Is, At("2024-03-30T23:30:00Z")), "Europe/Paris");
        Assert.Equal(Seed.Readings().Where(r => r.ShiftStart >= new DateTime(2024, 3, 31, 0, 0, 0) && r.ShiftStart < new DateTime(2024, 3, 31, 1, 0, 0)).Select(r => r.Id).Order(), paris.Order());
        // the same instant is 19:30 on 03-30 in New York: no stamp is there
        var (_, newYork) = Both(db, Rule("shiftStart", Op.Is, At("2024-03-30T23:30:00Z")), "America/New_York");
        Assert.Empty(newYork);
    }

    [Fact]
    public void The_bounds_are_unspecified_kind_parameters_not_utc_instants()
    {
        using var db = Database.Binary();
        var request = Req(rule: Rule("shiftStart", Op.Is, Day("2024-03-31")), zone: "Europe/Paris");
        var sql = Schemas.Readings.Apply(db.Readings.AsNoTracking(), request).Matching.ToQueryString();
        // Paris midnight is 2024-03-30T23:00Z: a UTC bound would be 2024-03-30 23:00:00, the wall-clock one is 2024-03-31 00:00:00
        Assert.Contains("2024-03-31 00:00:00", sql);
        Assert.DoesNotContain("2024-03-30 23:00:00", sql);
    }

    [Fact]
    public void Without_the_option_a_datetime_field_is_read_as_utc()
    {
        using var db = Database.Binary();
        var schema = DataListSchema.For<Reading>(s => s.RowKey(e => e.Id).Field(e => e.ShiftStart, f => f.Filter(DataListFilter.Date())));
        var request = Req(rule: Rule("shiftStart", Op.Is, Day("2024-03-31")), zone: "Europe/Paris");
        var utc = Run(schema, db.Readings.AsNoTracking(), request).Items.Select(r => r.Id).Order().ToArray();
        var local = Ids(Run(Schemas.Readings, db.Readings.AsNoTracking(), request)).Order().ToArray();
        // Paris midnight is 23:00 UTC the evening before: the UTC reading starts one hour earlier
        Assert.NotEqual(local, utc);
        Assert.Equal(
            Seed.Readings().Where(r => r.ShiftStart >= new DateTime(2024, 3, 30, 23, 0, 0) && r.ShiftStart < new DateTime(2024, 3, 31, 22, 0, 0)).Select(r => r.Id).Order(),
            utc);
    }

    [Fact]
    public void The_option_is_refused_on_a_field_that_is_not_a_datetime()
    {
        var error = Assert.Throws<InvalidOperationException>(() => DataListSchema.For<Reading>(s => s.Field(e => e.Day, f => f.StoresLocalTime())));
        Assert.Contains("DateTime", error.Message);
    }
}
