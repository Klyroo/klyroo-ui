namespace Klyroo.UI.Maui;

/// <summary>The kind of chart <see cref="KlyChart{TItem, TIndex}"/> draws.</summary>
public enum KlyChartType
{
    /// <summary>Values joined by a line.</summary>
    Line,
    /// <summary>Values joined by a line with the area under it filled.</summary>
    Area,
    /// <summary>One bar per value, vertical or, with <c>Horizontal</c>, horizontal.</summary>
    Bar,
    /// <summary>One slice per row, sized by the first category.</summary>
    Donut,
    /// <summary>One horizontal bar per row, sized by the first category relative to the largest one, with the share kept from the previous step: a conversion funnel. Rows are the steps in order.</summary>
    Funnel,
}
