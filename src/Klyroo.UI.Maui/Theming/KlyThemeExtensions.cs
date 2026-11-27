using Microsoft.Maui.Controls.Shapes;

namespace Klyroo.UI.Maui;

internal static class KlyThemeExtensions
{
    /// <summary>Binds a color property to the light and dark values: it follows the app theme without any code of the control.</summary>
    public static void SetThemeColor(this BindableObject target, BindableProperty property, KlyThemeColor color) =>
        target.SetAppThemeColor(property, color.Light, color.Dark);

    public static void SetStroke(this Border border, KlyThemeColor color) =>
        border.SetAppTheme<Brush>(Border.StrokeProperty, new SolidColorBrush(color.Light), new SolidColorBrush(color.Dark));

    public static void SetRadius(this Border border, double radius) =>
        border.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(radius) };

    /// <summary>Applies a surface (background, border, border width) to a border.</summary>
    public static void Apply(this Border border, Surface surface)
    {
        border.SetThemeColor(VisualElement.BackgroundColorProperty, surface.Background);
        border.SetStroke(surface.Stroke);
        border.StrokeThickness = surface.StrokeThickness;
    }
}
