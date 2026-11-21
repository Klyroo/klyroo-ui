using System.Globalization;

namespace Dashboard.Shared.Data;

/// <summary>The periods of the Home page.</summary>
public enum MetricsPeriod
{
    Week,
    Month,
    Quarter,
    Year,
}

/// <summary>One point of the revenue chart: the label of a bucket (a day, a week, a month), the revenue of the period and of the one before it.</summary>
public sealed record RevenuePoint(string Label, double Current, double Previous);

/// <summary>The four numbers of the cards of the Home page, for a period and for the one before it.</summary>
public sealed record MetricsSummary(decimal Revenue, decimal PreviousRevenue, int Orders, int PreviousOrders, int NewCustomers, int PreviousNewCustomers, double Conversion, double PreviousConversion);

/// <summary>A share of the revenue, for the donut.</summary>
public sealed record ChannelShare(string Channel, double Revenue);

/// <summary>
/// Two years of sample figures, one row per day (revenue, orders, visits), and what the Home page derives from them. The new customers are counted from the
/// customers of the Customers page, so the two pages agree.
/// </summary>
public static class MetricsData
{
    private sealed record Day(DateOnly Date, decimal Revenue, int Orders, int Visits);

    private static readonly Day[] Days = Build();

    private static Day[] Build()
    {
        var random = new DemoRandom(77);
        var days = new Day[730];
        for (var i = 0; i < days.Length; i++)
        {
            var date = DemoClock.Today.AddDays(i - days.Length + 1);
            var trend = 1m + i / 900m;
            var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0.62m : 1m;
            var season = 1m + (decimal)Math.Sin(i / 58.0) * 0.12m;
            var revenue = Math.Round((2300m + random.Between(0, 1400)) * trend * weekend * season, 0);
            var orders = (int)(revenue / random.Between(62, 96));
            var visits = (int)(orders * random.Between(24, 38));
            days[i] = new Day(date, revenue, orders, visits);
        }
        return days;
    }

    /// <summary>The number of days a period covers.</summary>
    public static int Length(MetricsPeriod period) => period switch
    {
        MetricsPeriod.Week => 7,
        MetricsPeriod.Month => 30,
        MetricsPeriod.Quarter => 84,
        _ => 364,
    };

    private static IEnumerable<Day> Range(int daysBack, int length) => Days.Skip(Days.Length - daysBack - length).Take(length);

    /// <summary>The revenue per bucket for the period and for the one before it: a day for a week, three days for a month, a week for a quarter, four weeks for a year.</summary>
    public static IReadOnlyList<RevenuePoint> Revenue(MetricsPeriod period)
    {
        var length = Length(period);
        var bucket = period switch { MetricsPeriod.Week => 1, MetricsPeriod.Month => 3, MetricsPeriod.Quarter => 7, _ => 28 };
        var current = Range(0, length).ToArray();
        var previous = Range(length, length).ToArray();
        var points = new List<RevenuePoint>();
        for (var start = 0; start < length; start += bucket)
        {
            var now = current.Skip(start).Take(bucket).ToArray();
            var before = previous.Skip(start).Take(bucket).ToArray();
            // The first day of the bucket names it, and two buckets never start on the same day.
            var label = now[0].Date.ToString(period == MetricsPeriod.Week ? "ddd d" : "MMM d", CultureInfo.InvariantCulture);
            points.Add(new RevenuePoint(label, (double)now.Sum(d => d.Revenue), (double)before.Sum(d => d.Revenue)));
        }
        return points;
    }

    /// <summary>The numbers of the cards for a period.</summary>
    public static MetricsSummary Summary(MetricsPeriod period)
    {
        var length = Length(period);
        var current = Range(0, length).ToArray();
        var previous = Range(length, length).ToArray();
        var from = DemoClock.Today.AddDays(-length + 1);
        var before = from.AddDays(-length);
        int Joined(DateOnly start, DateOnly end) => CustomerData.All.Count(c => c.Joined >= start && c.Joined <= end);
        return new MetricsSummary(
            current.Sum(d => d.Revenue), previous.Sum(d => d.Revenue),
            current.Sum(d => d.Orders), previous.Sum(d => d.Orders),
            Joined(from, DemoClock.Today), Joined(before, from.AddDays(-1)),
            Rate(current), Rate(previous));
    }

    private static double Rate(Day[] days) => days.Sum(d => d.Visits) is var visits and > 0 ? days.Sum(d => d.Orders) * 100.0 / visits : 0;

    /// <summary>The revenue of the period by acquisition channel: fixed shares of the total, which is what the sample data can offer.</summary>
    public static IReadOnlyList<ChannelShare> Channels(MetricsPeriod period)
    {
        var total = (double)Range(0, Length(period)).Sum(d => d.Revenue);
        return
        [
            new("Search", total * 0.38),
            new("Direct", total * 0.27),
            new("Referral", total * 0.17),
            new("Email", total * 0.11),
            new("Social", total * 0.07),
        ];
    }

    /// <summary>The orders of each day of the last two weeks, for the bar chart.</summary>
    public static IReadOnlyList<(string Day, int Orders)> OrdersByDay() =>
        [.. Range(0, 14).Select(d => (d.Date.ToString("ddd d", CultureInfo.InvariantCulture), d.Orders))];
}
