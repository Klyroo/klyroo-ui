namespace Dashboard.Shared.Data;

/// <summary>
/// The clock and the random numbers of the sample data. Every number is computed from a fixed seed, so the server and the WebAssembly client build the
/// same rows (a prerendered page and its interactive render agree), and the dates are relative to today, so the date filters and the periods of the charts
/// always have something to show. Replace the sample data with your own source: nothing else in the app depends on how it is made.
/// </summary>
public static class DemoClock
{
    /// <summary>Today, fixed when the process starts (the server and each browser tab agree within a day).</summary>
    public static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
}

/// <summary>A small deterministic generator (a linear congruential one): the same sequence on every run and on every platform.</summary>
public sealed class DemoRandom(uint seed)
{
    private uint _state = seed;

    /// <summary>The next number in [0, <paramref name="max"/>).</summary>
    public int Next(int max)
    {
        _state = unchecked(_state * 1664525u + 1013904223u);
        return (int)((_state >> 8) % (uint)max);
    }

    /// <summary>The next number in [min, max].</summary>
    public int Between(int min, int max) => min + Next(max - min + 1);

    /// <summary>One element of <paramref name="items"/>.</summary>
    public T Pick<T>(IReadOnlyList<T> items) => items[Next(items.Count)];
}

/// <summary>
/// Amounts of money. The sample data is in dollars whatever the language of the user: a neutral culture has no currency symbol (the "C" format would write a generic
/// sign), so the symbol is written here and the number follows the culture. Use your own currency code or <c>RegionInfo</c> in a real app.
/// </summary>
public static class Money
{
    public static string Format(decimal value, int decimals = 0) => "$" + value.ToString("N" + decimals, System.Globalization.CultureInfo.CurrentCulture);
}
