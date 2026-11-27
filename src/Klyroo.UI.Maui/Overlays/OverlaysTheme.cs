namespace Klyroo.UI.Maui;

/// <summary>Colors of the overlay surfaces that are not in <see cref="KlyTheme"/>: the web <c>--klyroo-bg-overlay</c> token and the fixed scrim of the web theme.</summary>
internal static class OverlaysTheme
{
    /// <summary>Background of panels, menus and toasts (the <c>bg-overlay</c> token of the web library).</summary>
    public static KlyThemeColor Surface { get; } = KlyThemeColor.FromHex("#ffffff", "#121820");

    /// <summary>The backdrop behind a modal: zinc-950 at 60%, the same in both themes, as <c>backdrop:bg-zinc-950/60</c> on the web.</summary>
    public static KlyThemeColor Scrim { get; } = KlyThemeColor.FromHex("#09090b", "#09090b").WithAlpha(0.6f);

    /// <summary>Colors of the drop shadow of a floating surface.</summary>
    public static KlyThemeColor Shadow { get; } = KlyThemeColor.FromHex("#09090b", "#09090b");

    /// <summary>A raised surface: background, border and shadow of panels, menus and toasts.</summary>
    public static void ApplySurface(this Border border, double radius)
    {
        border.SetThemeColor(VisualElement.BackgroundColorProperty, Surface);
        border.SetStroke(KlyTheme.Border);
        border.StrokeThickness = 1;
        border.SetRadius(radius);
        border.Shadow = new Shadow { Brush = new SolidColorBrush(Shadow.Light), Offset = new Point(0, 4), Radius = 12, Opacity = 0.25f };
    }
}
