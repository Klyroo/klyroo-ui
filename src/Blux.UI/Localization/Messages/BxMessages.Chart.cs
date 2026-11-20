namespace Blux.UI;

public sealed partial record BxMessages
{
    /// <summary>Kind of chart, used in the generated description.</summary>
    public string ChartLine { get; init; } = "Line chart";
    public string ChartArea { get; init; } = "Area chart";
    public string ChartBar { get; init; } = "Bar chart";
    public string ChartBarHorizontal { get; init; } = "Horizontal bar chart";
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

    /// <summary>Summary of the disclosure that holds the chart's data table.</summary>
    public string ChartTable { get; init; } = "Data table";

    /// <summary>Header of the first column of the data table, when the chart has no x axis title.</summary>
    public string ChartIndex { get; init; } = "Category";

    /// <summary>Accessible name of the legend.</summary>
    public string ChartLegend { get; init; } = "Legend";
}
