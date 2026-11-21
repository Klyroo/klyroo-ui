using Blux.UI.Tools.Query;

namespace Blux.UI.Tools.Tests;

public sealed record Order(int Id, string Name, decimal Amount, decimal Weight, DateOnly Day, DateTimeOffset Created);

/// <summary>30 orders (amount 10 to 300 by 10, one per day of November 2026, created at 10:30 UTC) and a schema with number filters (amount with a slider, weight plain) and date filters (day, created to the minute).</summary>
public static class RangeData
{
    public static readonly Order[] All =
    [
        .. Enumerable.Range(1, 30).Select(i => new Order(
            i, $"Order {i:00}", i * 10m, i / 2m,
            new DateOnly(2026, 11, 1).AddDays(i - 1),
            new DateTimeOffset(2026, 11, 1, 10, 30, 0, TimeSpan.Zero).AddDays(i - 1))),
    ];

    public static DataListSchema<Order> Schema { get; } = DataListSchema.For<Order>(s => s
        .RowKey(o => o.Id)
        .Field(o => o.Id, f => f.Sortable())
        .Field(o => o.Name, f => f.Sortable())
        .Field(o => o.Amount, f => f.Sortable().Filter(DataListFilter.Number(0, 10, 500).WithLabel("Amount").WithNumberDisplay(DataListNumberDisplay.InputAndSlider)))
        .Field(o => o.Weight, f => f.Filter(DataListFilter.Number().WithLabel("Weight")))
        .Field(o => o.Day, f => f.Sortable().Filter(DataListFilter.Date().WithLabel("Day").WithMaxRangeDays(10)))
        .Field(o => o.Created, f => f.Filter(DataListFilter.Date(DateGranularity.Minute).WithLabel("Created")))
        .DefaultSort(o => o.Id)
        .DefaultPageSize(10));
}

/// <summary>A clock that stands still, for the presets of the date editors.</summary>
public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
