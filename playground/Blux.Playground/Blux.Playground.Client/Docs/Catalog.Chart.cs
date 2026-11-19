using Blux.UI;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    private static readonly ComponentDoc[] Chart =
    [
        new("chart", "Chart", "Data", "A line, area, bar or donut chart drawn as SVG by the server, with a tooltip, a legend and a data table.",
            [new("Lines and areas", typeof(Examples.ChartLineExample), "Hover a column, or focus a chart and use the arrow keys, Home and End. The values are announced as the focus moves."),
             new("Bars", typeof(Examples.ChartBarExample), "Grouped, stacked and horizontal. The data table is behind a disclosure; by default it is visually hidden but still read by screen readers."),
             new("Donut", typeof(Examples.ChartDonutExample), "One slice per row, sized by the first category. Series differ by fill pattern as well as by color.")],
            [typeof(BxChart<object>)]),
    ];
}
