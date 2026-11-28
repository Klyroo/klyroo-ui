namespace Klyroo.UI.Maui;

/// <summary>What a radio group and a checkbox group share: a legend over the choices, which stack or wrap by orientation, and an error frame around the group while it is invalid.</summary>
internal sealed class InputsChoiceLayout
{
    private readonly Border _root;

    public InputsChoiceLayout(Border root)
    {
        _root = root;
        Legend = new Label { FontAttributes = FontAttributes.Bold, IsVisible = false, Margin = new Thickness(0, 0, 0, 4) };
        Legend.SetThemeColor(Label.TextColorProperty, KlyTheme.Text);
        SemanticProperties.SetHeadingLevel(Legend, SemanticHeadingLevel.Level2);
        Items = new FlexLayout { Direction = Microsoft.Maui.Layouts.FlexDirection.Column, Wrap = Microsoft.Maui.Layouts.FlexWrap.NoWrap, AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Start };
        root.Content = new VerticalStackLayout { Spacing = 0, Children = { Legend, Items } };
        root.StrokeThickness = 0;
        root.SetStroke(KlyThemeColor.Transparent);
        root.SetRadius(KlyTheme.RadiusOf(1));
        root.HorizontalOptions = LayoutOptions.Fill;
    }

    public Label Legend { get; }

    public FlexLayout Items { get; }

    public void ShowLegend(string? text, double fontSize)
    {
        Legend.Text = text;
        Legend.IsVisible = !string.IsNullOrEmpty(text);
        Legend.FontSize = fontSize;
    }

    /// <summary>Stacks the choices (vertical), or lays them side by side and wraps them (horizontal).</summary>
    public void ShowOrientation(StackOrientation orientation)
    {
        var vertical = orientation == StackOrientation.Vertical;
        Items.Direction = vertical ? Microsoft.Maui.Layouts.FlexDirection.Column : Microsoft.Maui.Layouts.FlexDirection.Row;
        Items.Wrap = vertical ? Microsoft.Maui.Layouts.FlexWrap.NoWrap : Microsoft.Maui.Layouts.FlexWrap.Wrap;
        foreach (var child in Items.Children)
        {
            if (child is View view) view.Margin = ChoiceMargin(vertical);
        }
    }

    /// <summary>The space around a choice: below it in a column, and at its end in a row.</summary>
    public static Thickness ChoiceMargin(bool vertical) => vertical ? new Thickness(0, 0, 0, 6) : new Thickness(0, 0, 16, 6);

    public void ShowInvalid(bool invalid)
    {
        _root.SetStroke(invalid ? KlyTheme.Resolve(KlyColor.Error).Fill : KlyThemeColor.Transparent);
        _root.StrokeThickness = invalid ? 2 : 0;
        _root.Padding = invalid ? new Thickness(8) : new Thickness(0);
    }
}
