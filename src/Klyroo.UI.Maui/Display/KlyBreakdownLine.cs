namespace Klyroo.UI.Maui;

/// <summary>One line of a <see cref="KlyBreakdown"/>, also used for its total.</summary>
/// <param name="Label">The line label.</param>
public sealed record KlyBreakdownLine(string Label)
{
    /// <summary>The amount, formatted with <see cref="KlyBreakdown.Format"/> and <see cref="KlyBreakdown.Culture"/>. A missing amount shows a dash.</summary>
    public decimal? Amount { get; init; }

    /// <summary>A second amount, such as the previous period, shown in a second column. Setting it on any line, or setting <see cref="KlyBreakdown.CompareColumns"/>, switches the block to two columns, with a dash where a line has no second amount.</summary>
    public decimal? CompareAmount { get; init; }

    /// <summary>A line of smaller text under the label.</summary>
    public string? Detail { get; init; }

    /// <summary>Shows the line as a subtotal, with a top border and bold text.</summary>
    public bool IsSubtotal { get; init; }

    /// <summary>Shows the line in a dimmed color.</summary>
    public bool IsMuted { get; init; }

    /// <summary>A color for the amount, such as <see cref="KlyColor.Success"/> for a discount.</summary>
    public KlyColor? Color { get; init; }

    /// <summary>A badge shown after the label.</summary>
    public KlyBadgeItem? Badge { get; init; }
}
