namespace Klyroo.UI.Maui;

/// <summary>
/// Links to the previous and the next page of a documentation site, as two cards side by side from 640 units wide and stacked below. It shows nothing when it has neither.
/// A tap raises <see cref="LinkClicked"/>.
/// </summary>
public class KlyContentSurround : ContentView
{
    /// <summary>Identifies <see cref="Previous"/>.</summary>
    public static readonly BindableProperty PreviousProperty = BindableProperty.Create(nameof(Previous), typeof(KlyContentSurroundLink), typeof(KlyContentSurround), null, propertyChanged: (b, _, _) => ((KlyContentSurround)b).Rebuild());
    /// <summary>Identifies <see cref="Next"/>.</summary>
    public static readonly BindableProperty NextProperty = BindableProperty.Create(nameof(Next), typeof(KlyContentSurroundLink), typeof(KlyContentSurround), null, propertyChanged: (b, _, _) => ((KlyContentSurround)b).Rebuild());
    /// <summary>Identifies <see cref="PreviousLabel"/>.</summary>
    public static readonly BindableProperty PreviousLabelProperty = BindableProperty.Create(nameof(PreviousLabel), typeof(string), typeof(KlyContentSurround), "Previous", propertyChanged: (b, _, _) => ((KlyContentSurround)b).Rebuild());
    /// <summary>Identifies <see cref="NextLabel"/>.</summary>
    public static readonly BindableProperty NextLabelProperty = BindableProperty.Create(nameof(NextLabel), typeof(string), typeof(KlyContentSurround), "Next", propertyChanged: (b, _, _) => ((KlyContentSurround)b).Rebuild());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyContentSurround), "Previous and next pages", propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyContentSurround)b, (string?)v));

    private readonly Grid _grid = new() { ColumnSpacing = 16, RowSpacing = 16 };
    private double _width;

    /// <summary>Creates the links.</summary>
    public KlyContentSurround()
    {
        Content = _grid;
        SemanticProperties.SetDescription(this, AccessibleName);
        SizeChanged += (_, _) => Relayout(Width);
        Rebuild();
    }

    /// <summary>Raised when the user taps the card of the previous or the next page. <see cref="PageLinkEventArgs.Item"/> is the <see cref="KlyContentSurroundLink"/>.</summary>
    public event EventHandler<PageLinkEventArgs>? LinkClicked;

    /// <summary>The page before this one. Without it the next page stays in the second column.</summary>
    public KlyContentSurroundLink? Previous { get => (KlyContentSurroundLink?)GetValue(PreviousProperty); set => SetValue(PreviousProperty, value); }

    /// <summary>The page after this one.</summary>
    public KlyContentSurroundLink? Next { get => (KlyContentSurroundLink?)GetValue(NextProperty); set => SetValue(NextProperty, value); }

    /// <summary>Caption of the previous page, in the language of the app.</summary>
    public string PreviousLabel { get => (string)GetValue(PreviousLabelProperty); set => SetValue(PreviousLabelProperty, value); }

    /// <summary>Caption of the next page, in the language of the app.</summary>
    public string NextLabel { get => (string)GetValue(NextLabelProperty); set => SetValue(NextLabelProperty, value); }

    /// <summary>What a screen reader says for the group of links, in the language of the app.</summary>
    public string AccessibleName { get => (string)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal Grid LayoutGrid => _grid;

    internal void Relayout(double width)
    {
        _width = width;
        Rebuild();
    }

    private void Rebuild()
    {
        _grid.Children.Clear();
        _grid.ColumnDefinitions = new ColumnDefinitionCollection();
        _grid.RowDefinitions = new RowDefinitionCollection();
        IsVisible = Previous is not null || Next is not null;
        if (!IsVisible) return;

        var wide = _width >= PagesUi.Sm;
        if (wide)
        {
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }
        else
        {
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        if (Previous is { } previous) _grid.Add(Card(previous, false), 0, 0);
        if (Next is { } next) _grid.Add(Card(next, true), wide ? 1 : 0, wide ? 0 : (Previous is null ? 0 : 1));
    }

    private Border Card(KlyContentSurroundLink link, bool next)
    {
        var caption = PagesUi.Text(12, KlyTheme.TextMuted);
        caption.Text = next ? NextLabel : PreviousLabel;
        caption.IsVisible = true;
        caption.TextTransform = TextTransform.Uppercase;
        caption.CharacterSpacing = 0.6;
        caption.VerticalOptions = LayoutOptions.Center;

        var icon = PagesUi.Icon(next ? KlyIconNames.ArrowRight : KlyIconNames.ArrowLeft, KlySize.Xs, KlyTheme.TextMuted);
        var captionRow = new HorizontalStackLayout { Spacing = 6, HorizontalOptions = next ? LayoutOptions.End : LayoutOptions.Start };
        if (next)
        {
            captionRow.Children.Add(caption);
            captionRow.Children.Add(icon);
        }
        else
        {
            captionRow.Children.Add(icon);
            captionRow.Children.Add(caption);
        }

        var title = PagesUi.Text(16, KlyTheme.TextHighlighted);
        title.Text = link.Title;
        title.IsVisible = true;
        title.HorizontalTextAlignment = next ? TextAlignment.End : TextAlignment.Start;
        var description = PagesUi.Text(14, KlyTheme.TextMuted);
        PagesUi.Show(description, link.Description);
        description.MaxLines = 2;
        description.LineBreakMode = LineBreakMode.TailTruncation;
        description.HorizontalTextAlignment = next ? TextAlignment.End : TextAlignment.Start;

        var card = new Border
        {
            Padding = new Thickness(16),
            StrokeThickness = 1,
            MinimumHeightRequest = 40,
            Content = new VerticalStackLayout { Spacing = 4, Children = { captionRow, title, description } },
        };
        card.SetRadius(KlyTheme.RadiusOf(1));
        card.SetStroke(KlyTheme.Border);
        card.SetThemeColor(BackgroundColorProperty, KlyThemeColor.Transparent);
        SemanticProperties.SetDescription(card, $"{caption.Text}: {link.Title}");
        PagesUi.Tappable(card, () => Activate(link));
        return card;
    }

    internal void Activate(KlyContentSurroundLink link) => PagesLinks.Follow(this, LinkClicked, link.Href, link);
}
