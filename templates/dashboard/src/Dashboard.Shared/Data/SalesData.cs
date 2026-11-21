namespace Dashboard.Shared.Data;

/// <summary>An order of the "Recent sales" list of the Home page.</summary>
public sealed record Sale(int Id, Customer Customer, decimal Amount, string Status, DateTimeOffset At);

/// <summary>The latest sample orders, taken from the customers of the Customers page.</summary>
public static class SalesData
{
    public static readonly Sale[] Recent = Build();

    private static Sale[] Build()
    {
        var random = new DemoRandom(4242);
        var customers = CustomerData.All.Where(c => c.Status is "Active" or "Trial").Take(60).ToArray();
        var now = new DateTimeOffset(DemoClock.Today.ToDateTime(new TimeOnly(15, 0)), TimeSpan.Zero);
        return
        [
            .. Enumerable.Range(0, 8).Select(i => new Sale(
                5200 - i,
                customers[random.Next(customers.Length)],
                random.Between(24, 640) + random.Between(0, 99) / 100m,
                random.Next(8) switch { 0 => "Refunded", 1 or 2 => "Pending", _ => "Paid" },
                now.AddMinutes(-(i * 37 + random.Between(3, 30))))),
        ];
    }
}
