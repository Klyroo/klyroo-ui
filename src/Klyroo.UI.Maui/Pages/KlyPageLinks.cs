namespace Klyroo.UI.Maui;

/// <summary>A titled list of links. A link marked <see cref="PageLinkItem.Active"/> is the current page and is drawn in the primary color.</summary>
public class KlyPageLinks : ContentView
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyPageLinks), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageLinks)b)._title, (string?)v));
    /// <summary>Identifies <see cref="Links"/>.</summary>
    public static readonly BindableProperty LinksProperty = BindableProperty.Create(nameof(Links), typeof(IReadOnlyList<PageLinkItem>), typeof(KlyPageLinks), null, propertyChanged: (b, _, _) => ((KlyPageLinks)b).Rebuild());
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int?), typeof(KlyPageLinks), null, propertyChanged: (b, _, v) => PagesUi.Heading(((KlyPageLinks)b)._title, (int?)v));

    private readonly Label _title = PagesUi.Text(14, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly VerticalStackLayout _list = new();

    /// <summary>Creates a list of links.</summary>
    public KlyPageLinks()
    {
        _title.Margin = new Thickness(0, 0, 0, 12);
        Content = new VerticalStackLayout { Children = { _title, _list } };
    }

    /// <summary>Raised when the user follows a link.</summary>
    public event EventHandler<PageLinkEventArgs>? LinkClicked;

    /// <summary>Heading above the list.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The links.</summary>
    public IReadOnlyList<PageLinkItem>? Links { get => (IReadOnlyList<PageLinkItem>?)GetValue(LinksProperty); set => SetValue(LinksProperty, value); }

    /// <summary>Gives the title a heading level for a screen reader, 1 to 6; it is plain text when <c>null</c>.</summary>
    public int? HeadingLevel { get => (int?)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    internal VerticalStackLayout List => _list;

    internal void Activate(PageLinkItem link) => PagesLinks.Follow(this, LinkClicked, link.Href, link);

    private void Rebuild()
    {
        _list.Children.Clear();
        if (Links is null) return;
        foreach (var link in Links) _list.Children.Add(Row(link));
    }

    private Grid Row(PageLinkItem link)
    {
        var color = link.Active ? KlyTheme.Resolve(KlyColor.Primary).Text : KlyTheme.TextMuted;
        var label = PagesUi.Text(14, color, link.Active ? FontAttributes.Bold : FontAttributes.None);
        label.Text = link.Label;
        label.IsVisible = true;
        label.LineBreakMode = LineBreakMode.TailTruncation;
        label.VerticalOptions = LayoutOptions.Center;

        var row = new Grid
        {
            ColumnSpacing = 6,
            MinimumHeightRequest = 40,
            HorizontalOptions = LayoutOptions.Fill,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            IsEnabled = !link.Disabled,
            Opacity = link.Disabled ? 0.6 : 1,
        };
        SemanticProperties.SetDescription(row, link.Label);
        if (link.Icon is not null) row.Add(PagesUi.Icon(link.Icon, KlySize.Xs, color), 0, 0);
        row.Add(label, 1, 0);
        if (PagesLinks.IsExternal(link.Href, out _)) row.Add(PagesUi.Icon(KlyIconNames.External, KlySize.Xs, color), 2, 0);
        PagesUi.Tappable(row, () =>
        {
            if (!link.Disabled) Activate(link);
        });
        return row;
    }
}
