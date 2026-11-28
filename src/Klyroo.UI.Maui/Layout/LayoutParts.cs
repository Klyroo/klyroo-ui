namespace Klyroo.UI.Maui;

/// <summary>Small building blocks shared by the layout controls: the hairlines between areas, the slot that hosts an optional view, the scrim behind a panel.</summary>
internal static class LayoutParts
{
    /// <summary>The dark veil behind a panel, built from the page tokens so it is dark in both themes.</summary>
    public static KlyThemeColor Scrim { get; } = new KlyThemeColor(KlyTheme.BackgroundInverted.Light, KlyTheme.Background.Dark).WithAlpha(0.6f);

    /// <summary>A one unit line in the border color, horizontal by default.</summary>
    public static Border Line(bool vertical = false)
    {
        var line = new Border { StrokeThickness = 0, InputTransparent = true };
        line.SetThemeColor(VisualElement.BackgroundColorProperty, KlyTheme.Border);
        AutomationProperties.SetIsInAccessibleTree(line, false);
        if (vertical)
        {
            line.WidthRequest = 1;
            line.HorizontalOptions = LayoutOptions.End;
            line.VerticalOptions = LayoutOptions.Fill;
        }
        else
        {
            line.HeightRequest = 1;
            line.VerticalOptions = LayoutOptions.End;
            line.HorizontalOptions = LayoutOptions.Fill;
        }
        return line;
    }

    /// <summary>A view that hosts one optional child and is gone while it has none.</summary>
    public static ContentView Slot() => new() { IsVisible = false };

    public static void Fill(ContentView slot, View? view)
    {
        slot.Content = view;
        slot.IsVisible = view is not null;
    }

    // A view is both a BindableObject and an IView, so the attached property setters of Grid are ambiguous for it.
    public static void Column(View view, int column) => Grid.SetColumn((BindableObject)view, column);

    public static void ColumnSpan(View view, int span) => Grid.SetColumnSpan((BindableObject)view, span);

    public static void Row(View view, int row) => Grid.SetRow((BindableObject)view, row);

    public static SemanticHeadingLevel Heading(int level) => Math.Clamp(level, 1, 6) switch
    {
        1 => SemanticHeadingLevel.Level1,
        2 => SemanticHeadingLevel.Level2,
        3 => SemanticHeadingLevel.Level3,
        4 => SemanticHeadingLevel.Level4,
        5 => SemanticHeadingLevel.Level5,
        _ => SemanticHeadingLevel.Level6,
    };
}
