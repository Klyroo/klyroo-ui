namespace Klyroo.UI.Maui;

/// <summary>Where the table with the chart's data goes.</summary>
public enum KlyChartTable
{
    /// <summary>No table: a screen reader gets the generated description of the chart instead.</summary>
    Hidden,
    /// <summary>Shown under the chart.</summary>
    Visible,
    /// <summary>Behind a button under the chart.</summary>
    Toggle,
}
