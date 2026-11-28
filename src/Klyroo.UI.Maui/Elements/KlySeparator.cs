namespace Klyroo.UI.Maui;

/// <summary>A thin line between two groups of content.</summary>
public class KlySeparator : Border
{
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlySeparator), StackOrientation.Horizontal, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlySeparator), KlyColor.Neutral, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlySeparator), KlySize.Xs, propertyChanged: Restyle);

    /// <summary>Creates a horizontal separator.</summary>
    public KlySeparator()
    {
        // A decorative line: a screen reader has nothing to say about it.
        AutomationProperties.SetIsInAccessibleTree(this, false);
        StrokeThickness = 0;
        ApplyStyle();
    }

    /// <summary>Direction of the line: <c>Horizontal</c> spans the width, <c>Vertical</c> the height.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Semantic color of the line, drawn at 30% like the web separator.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Thickness of the line: 1, 2, 3, 4 or 5 units from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlySeparator)bindable).ApplyStyle();

    private void ApplyStyle()
    {
        this.SetThemeColor(BackgroundColorProperty, KlyTheme.Resolve(Color).Fill.WithAlpha(0.3f));
        var thickness = (double)(int)Size + 1;
        if (Orientation == StackOrientation.Horizontal)
        {
            HeightRequest = thickness;
            WidthRequest = -1;
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Center;
        }
        else
        {
            WidthRequest = thickness;
            HeightRequest = -1;
            VerticalOptions = LayoutOptions.Fill;
            HorizontalOptions = LayoutOptions.Center;
        }
    }
}
