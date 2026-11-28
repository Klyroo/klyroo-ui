using Microsoft.Maui.Layouts;

namespace Klyroo.UI.Maui;

/// <summary>
/// A column of views, centered, with a maximum width and a horizontal padding that grows with the width (16, 24, then 32 units from 640 and 1024 wide), as the web container.
/// <see cref="Layout.Padding"/> is added to it, so use it for a vertical padding.
/// </summary>
public class KlyContainer : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyContainer), KlySize.Md, propertyChanged: (b, _, _) => ((KlyContainer)b).InvalidateMeasure());

    /// <summary>Maximum width: <c>Xs</c> 672, <c>Sm</c> 896, <c>Md</c> 1280 (default), <c>Lg</c> 1536, <c>Xl</c> none.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    internal static double MaxWidthOf(KlySize size) => size switch
    {
        KlySize.Xs => 672,
        KlySize.Sm => 896,
        KlySize.Md => 1280,
        KlySize.Lg => 1536,
        KlySize.Xl => double.PositiveInfinity,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    /// <summary>The area the content takes inside <paramref name="bounds"/>: at most the maximum width, centered, minus the horizontal padding.</summary>
    internal static Rect ContentArea(Rect bounds, KlySize size)
    {
        var width = Math.Min(bounds.Width, MaxWidthOf(size));
        var inset = LayoutBreakpoints.HorizontalPadding(width);
        var x = bounds.X + ((bounds.Width - width) / 2) + inset;
        return new Rect(x, bounds.Y, Math.Max(0, width - (2 * inset)), bounds.Height);
    }

    /// <inheritdoc />
    protected override ILayoutManager CreateLayoutManager() => new LayoutContainerManager(this);

    private sealed class LayoutContainerManager(KlyContainer container) : VerticalStackLayoutManager(container)
    {
        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            var width = Math.Min(widthConstraint, MaxWidthOf(container.Size));
            var inset = LayoutBreakpoints.HorizontalPadding(width);
            var inner = base.Measure(Math.Max(0, width - (2 * inset)), heightConstraint);
            return new Size(inner.Width + (2 * inset), inner.Height);
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            var area = ContentArea(bounds, container.Size);
            var arranged = base.ArrangeChildren(area);
            return new Size(bounds.Width, arranged.Height);
        }
    }
}
