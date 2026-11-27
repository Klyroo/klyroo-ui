namespace Klyroo.UI.Maui;

/// <summary>
/// A list of in-page anchor links. Give <see cref="ScrollView"/> the scroll view of the page and mark each target view with an
/// <c>AutomationId</c>: a link to <c>#installation</c> scrolls to the view whose id is <c>installation</c>.
/// Which anchor is in view is up to the page; nothing is highlighted.
/// </summary>
public class KlyPageAnchors : ContentView
{
    /// <summary>Identifies <see cref="Links"/>.</summary>
    public static readonly BindableProperty LinksProperty = BindableProperty.Create(nameof(Links), typeof(IReadOnlyList<PageLinkItem>), typeof(KlyPageAnchors), null, propertyChanged: (b, _, _) => ((KlyPageAnchors)b).Rebuild());
    /// <summary>Identifies <see cref="ScrollView"/>.</summary>
    public static readonly BindableProperty ScrollViewProperty = BindableProperty.Create(nameof(ScrollView), typeof(ScrollView), typeof(KlyPageAnchors), null);

    private readonly VerticalStackLayout _list = new() { Spacing = 2 };

    /// <summary>Creates a list of anchors.</summary>
    public KlyPageAnchors()
    {
        Content = _list;
    }

    /// <summary>Raised when the user follows a link, before the control scrolls. Set <see cref="PageLinkEventArgs.Handled"/> to scroll yourself.</summary>
    public event EventHandler<PageLinkEventArgs>? LinkClicked;

    /// <summary>The anchors: <see cref="PageLinkItem.Href"/> is typically a fragment such as <c>#installation</c>.</summary>
    public IReadOnlyList<PageLinkItem>? Links { get => (IReadOnlyList<PageLinkItem>?)GetValue(LinksProperty); set => SetValue(LinksProperty, value); }

    /// <summary>The scroll view that holds the targets. A fragment link scrolls it to the target; without it the link only raises <see cref="LinkClicked"/>.</summary>
    public ScrollView? ScrollView { get => (ScrollView?)GetValue(ScrollViewProperty); set => SetValue(ScrollViewProperty, value); }

    internal VerticalStackLayout List => _list;

    internal void Activate(PageLinkItem link)
    {
        if (PagesLinks.Raise(this, LinkClicked, link.Href, link).Handled) return;
        if (link.Href is { Length: > 0 } href && href[0] == '#')
        {
            if (ScrollView is { } scroll && PagesScroll.Resolve(scroll, href) is { } target) PagesScroll.ScrollTo(scroll, target);
        }
        else
        {
            PagesLinks.Follow(this, null, link.Href, link);
        }
    }

    private void Rebuild()
    {
        _list.Children.Clear();
        if (Links is null) return;
        foreach (var link in Links) _list.Children.Add(Row(link));
    }

    private Border Row(PageLinkItem link)
    {
        var label = PagesUi.Text(14, KlyTheme.TextMuted, FontAttributes.Bold);
        label.Text = link.Label;
        label.IsVisible = true;
        label.LineBreakMode = LineBreakMode.TailTruncation;
        label.VerticalOptions = LayoutOptions.Center;

        var grid = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
        };
        if (link.Icon is not null)
        {
            var leading = new Border { WidthRequest = 24, HeightRequest = 24, StrokeThickness = 0, VerticalOptions = LayoutOptions.Center, Content = PagesUi.Icon(link.Icon, KlySize.Xs, KlyTheme.TextMuted) };
            leading.SetRadius(KlyTheme.RadiusOf(0.5));
            leading.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundElevated);
            if (leading.Content is View icon) icon.HorizontalOptions = LayoutOptions.Center;
            AutomationProperties.SetIsInAccessibleTree(leading, false);
            grid.Add(leading, 0, 0);
        }
        grid.Add(label, 1, 0);
        if (PagesLinks.IsExternal(link.Href, out _)) grid.Add(PagesUi.Icon(KlyIconNames.External, KlySize.Xs, KlyTheme.TextMuted), 2, 0);

        var row = new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(10, 6),
            MinimumHeightRequest = 40,
            BackgroundColor = Colors.Transparent,
            IsEnabled = !link.Disabled,
            Opacity = link.Disabled ? 0.6 : 1,
            Content = grid,
        };
        row.SetRadius(KlyTheme.RadiusOf(1));
        SemanticProperties.SetDescription(row, link.Label);
        PagesUi.Tappable(row, () =>
        {
            if (!link.Disabled) Activate(link);
        });
        return row;
    }
}
