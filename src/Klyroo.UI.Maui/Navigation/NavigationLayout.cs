namespace Klyroo.UI.Maui;

/// <summary>Small view builders shared by the navigation controls: themed labels and icons, and the chevron turn.</summary>
internal static class NavigationLayout
{
    /// <summary>A label in a font size and a theme color. Truncates by default, like the web <c>truncate</c> utility.</summary>
    public static Label TextLabel(double fontSize, KlyThemeColor color, FontAttributes attributes = FontAttributes.None, LineBreakMode lineBreak = LineBreakMode.TailTruncation)
    {
        var label = new Label { FontSize = fontSize, FontAttributes = attributes, LineBreakMode = lineBreak, VerticalTextAlignment = TextAlignment.Center };
        label.SetThemeColor(Label.TextColorProperty, color);
        return label;
    }

    /// <summary>A Lucide icon of a size and a theme color.</summary>
    public static KlyIcon Icon(string name, KlySize size, KlyThemeColor color) =>
        new() { Name = name, Size = size, IconColor = color, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };

    /// <summary>Turns a view to an angle: animated while the view is on screen, at once otherwise.</summary>
    public static void Rotate(VisualElement view, double degrees)
    {
        if (view.Handler is null)
        {
            view.Rotation = degrees;
            return;
        }
        view.AbortAnimation("KlyRotate");
        _ = view.RotateToAsync(degrees, 200);
    }

    /// <summary>A line in a theme color, with an optional fixed width or height.</summary>
    public static BoxView Line(KlyThemeColor color, double? width = null, double? height = null)
    {
        var line = new BoxView { CornerRadius = 0 };
        line.SetThemeColor(BoxView.ColorProperty, color);
        if (width is { } w) line.WidthRequest = w;
        if (height is { } h) line.HeightRequest = h;
        return line;
    }

    /// <summary>The index of the first enabled item of a list, or -1.</summary>
    public static int FirstEnabled<T>(IReadOnlyList<T> items, Func<T, bool> disabled)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (!disabled(items[i])) return i;
        }
        return -1;
    }

    /// <summary>The semantic heading level of a level from 1 to 6; <c>None</c> for anything else.</summary>
    public static SemanticHeadingLevel HeadingLevelOf(int? level) => level switch
    {
        1 => SemanticHeadingLevel.Level1,
        2 => SemanticHeadingLevel.Level2,
        3 => SemanticHeadingLevel.Level3,
        4 => SemanticHeadingLevel.Level4,
        5 => SemanticHeadingLevel.Level5,
        6 => SemanticHeadingLevel.Level6,
        _ => SemanticHeadingLevel.None,
    };
}
