namespace Klyroo.UI;

public sealed partial record KlyMessages
{
    /// <summary>Kind of chart, used in the generated description.</summary>
    public string ChartLine { get; init; } = "Line chart";
    /// <summary>Accessible name of an area chart (<see cref="KlyChart{TItem}"/>), read before its generated description.</summary>
    public string ChartArea { get; init; } = "Area chart";
    /// <summary>Accessible name of a bar chart (<see cref="KlyChart{TItem}"/>), read before its generated description.</summary>
    public string ChartBar { get; init; } = "Bar chart";
    /// <summary>Accessible name of a horizontal bar chart (<see cref="KlyChart{TItem}"/>), read before its generated description.</summary>
    public string ChartBarHorizontal { get; init; } = "Horizontal bar chart";
    /// <summary>Accessible name of a donut chart (<see cref="KlyChart{TItem}"/>), read before its generated description.</summary>
    public string ChartDonut { get; init; } = "Donut chart";

    /// <summary>
    /// Generated description of a line, area or bar chart. {0} kind, {1} number of points, {2} first, {3} last, {4} the series
    /// (each formatted with <see cref="ChartRange"/>).
    /// </summary>
    public string ChartSummary { get; init; } = "{0} with {1} data points from {2} to {3}. Series: {4}.";

    /// <summary>Generated description of a donut chart. {0} kind, {1} the slices (each formatted with <see cref="ChartSlice"/>).</summary>
    public string ChartSummaryDonut { get; init; } = "{0}. Slices: {1}.";

    /// <summary>One series in the description. {0} name, {1} lowest value, {2} highest value.</summary>
    public string ChartRange { get; init; } = "{0} ({1} to {2})";

    /// <summary>One slice in the description. {0} name, {1} value, {2} share.</summary>
    public string ChartSlice { get; init; } = "{0} {1} ({2})";

    /// <summary>Accessible name of a chart that can be explored with the keyboard.</summary>
    public string ChartHint { get; init; } = "Chart values. Use the arrow keys to move between points.";

    /// <summary>Added to <see cref="ChartHint"/> when the chart reports a selection (<c>OnSelect</c>): how to choose the point the arrow keys are on.</summary>
    public string ChartHintSelect { get; init; } = "Press Enter to select the point.";

    /// <summary>Summary of the disclosure that holds the chart's data table.</summary>
    public string ChartTable { get; init; } = "Data table";

    /// <summary>Header of the first column of the data table, when the chart has no x axis title.</summary>
    public string ChartIndex { get; init; } = "Category";

    /// <summary>Accessible name of the legend.</summary>
    public string ChartLegend { get; init; } = "Legend";
}
