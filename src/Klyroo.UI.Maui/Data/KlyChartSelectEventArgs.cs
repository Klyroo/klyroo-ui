namespace Klyroo.UI.Maui;

/// <summary>What <see cref="KlyChart{TItem, TIndex}.Selected"/> reports: the row of the bar, point or slice that was tapped.</summary>
/// <typeparam name="TItem">The row type of the chart.</typeparam>
/// <typeparam name="TIndex">The type of the x value of a row (<see cref="KlyChart{TItem, TIndex}.Index"/>).</typeparam>
/// <param name="Row">The position of the row in <see cref="KlyChart{TItem, TIndex}.Data"/>.</param>
/// <param name="Index">The value of the row on the x axis: a date, a number, a label. The label of the slice with a donut.</param>
/// <param name="SeriesKey">The <see cref="KlyChartCategory{TItem}.Key"/> of the series that was picked (the only category of a donut or of a chart with one series), or null when the pick named none.</param>
/// <param name="SeriesIndex">The position of that series in <see cref="KlyChart{TItem, TIndex}.Categories"/>, or -1 when the pick named none.</param>
/// <param name="Value">The value of that series in the row, or null for a gap or when no series was named.</param>
/// <param name="Item">The row of <see cref="KlyChart{TItem, TIndex}.Data"/>.</param>
public sealed record KlyChartSelectEventArgs<TItem, TIndex>(int Row, TIndex Index, string? SeriesKey, int SeriesIndex, double? Value, TItem Item);
