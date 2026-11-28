namespace Klyroo.UI.Maui;

/// <summary>The list that opens under a menu field: an optional filter field, the options, and a text for the empty list. It takes the place of the popup of the web menus, inline under the field.</summary>
internal sealed class InputsMenuPanel : Border
{
    public InputsMenuPanel(bool withSearch, string searchLabel, string emptyText)
    {
        IsVisible = false;
        StrokeThickness = 1;
        this.SetStroke(KlyTheme.Border);
        this.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        this.SetRadius(KlyTheme.RadiusOf(1));
        HorizontalOptions = LayoutOptions.Fill;

        var stack = new VerticalStackLayout { Spacing = 0 };
        if (withSearch)
        {
            Search = new Entry { Placeholder = searchLabel, FontSize = 14, BackgroundColor = Colors.Transparent, Margin = new Thickness(8, 2), MinimumHeightRequest = 40 };
            Search.SetThemeColor(InputView.TextColorProperty, KlyTheme.Text);
            Search.SetThemeColor(InputView.PlaceholderColorProperty, KlyTheme.TextDimmed);
            Search.HandlerChanged += (_, _) => PlatformTweaks.RemoveNativeFieldChrome(Search);
            SemanticProperties.SetDescription(Search, searchLabel);
            stack.Add(Search);
            var line = new BoxView { HeightRequest = 1 };
            line.SetThemeColor(BoxView.ColorProperty, KlyTheme.Border);
            stack.Add(line);
        }

        Empty = new Label { Text = emptyText, FontSize = 14, Padding = new Thickness(12, 8), IsVisible = false };
        Empty.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
        var body = new VerticalStackLayout { Padding = new Thickness(4), Spacing = 0, Children = { List, Empty } };
        stack.Add(new ScrollView { MaximumHeightRequest = 240, Content = body });
        Content = stack;
    }

    public InputsOptionList List { get; } = new();

    public Label Empty { get; }

    public Entry? Search { get; }

    /// <summary>Lists the options, or the empty text when there are none.</summary>
    public void Show(IReadOnlyList<InputsOption> options, Func<InputsOption, bool> isSelected, KlyColor color)
    {
        List.Show(options, isSelected, color, 14);
        Empty.IsVisible = options.Count == 0;
    }
}
