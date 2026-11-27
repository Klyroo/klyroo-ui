namespace Klyroo.UI.Maui;

/// <summary>
/// A responsive grid of <see cref="KlyPricingPlan"/> cards. Horizontal puts as many plans on a row as fit with at least 272 units each; vertical stacks them in one column.
/// Add the plans to <c>Children</c>.
/// </summary>
public class KlyPricingPlans : Grid
{
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyPricingPlans), StackOrientation.Horizontal, propertyChanged: (b, _, _) => ((KlyPricingPlans)b).Refresh());

    private const double MinPlan = 272;
    private const double Gap = 24;

    private readonly PagesColumnFlow _flow;
    private double _width;

    /// <summary>Creates a grid of pricing plans.</summary>
    public KlyPricingPlans()
    {
        _flow = new PagesColumnFlow(this, ColumnsFor, Gap);
        SizeChanged += (_, _) => _width = Width;
    }

    /// <summary>Horizontal places the plans next to each other in a responsive grid, vertical stacks them in one column.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    internal int ColumnCount => _flow.Columns;

    internal void Relayout(double width)
    {
        _width = width;
        _flow.Apply(width);
    }

    private int ColumnsFor(double width) => Orientation == StackOrientation.Vertical ? 1 : (int)Math.Floor((width + Gap) / (MinPlan + Gap));

    private void Refresh() => _flow.Refresh(_width);
}
