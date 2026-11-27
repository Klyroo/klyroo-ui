namespace Klyroo.UI.Maui;

/// <summary>
/// A documentation search laid out in the page: a native <see cref="SearchBar"/> with its results listed under it, over a list of pages and the headings inside them.
/// The results show while <see cref="IsOpen"/>, which the search bar opens when it takes the focus or gets text. Choosing a result raises <see cref="ResultSelected"/>,
/// clears the search and closes the list. It draws no overlay: put it where the results can unfold.
/// </summary>
public class KlyContentSearch : ContentView
{
    /// <summary>Identifies <see cref="Pages"/>.</summary>
    public static readonly BindableProperty PagesProperty = BindableProperty.Create(nameof(Pages), typeof(IReadOnlyList<KlyContentSearchPage>), typeof(KlyContentSearch), null, propertyChanged: (b, _, _) => ((KlyContentSearch)b).ShowResults());
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyContentSearch), null, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlyContentSearch)b).ShowText((string?)v));
    /// <summary>Identifies <see cref="Placeholder"/>.</summary>
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(KlyContentSearch), "Search the documentation...", propertyChanged: (b, _, v) => ((KlyContentSearch)b)._search.Placeholder = (string?)v);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyContentSearch), "Search documentation", propertyChanged: (b, _, v) => SemanticProperties.SetDescription(((KlyContentSearch)b)._search, (string?)v));
    /// <summary>Identifies <see cref="EmptyText"/>.</summary>
    public static readonly BindableProperty EmptyTextProperty = BindableProperty.Create(nameof(EmptyText), typeof(string), typeof(KlyContentSearch), "No results", propertyChanged: (b, _, v) => ((KlyContentSearch)b)._empty.Text = (string?)v);
    /// <summary>Identifies <see cref="IsOpen"/>.</summary>
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(KlyContentSearch), false, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlyContentSearch)b).ShowOpen((bool)v));
    /// <summary>Identifies <see cref="MaxResults"/>.</summary>
    public static readonly BindableProperty MaxResultsProperty = BindableProperty.Create(nameof(MaxResults), typeof(int), typeof(KlyContentSearch), 50, propertyChanged: (b, _, _) => ((KlyContentSearch)b).ShowResults());

    private readonly SearchBar _search = new();
    private readonly VerticalStackLayout _results = new();
    private readonly Label _empty = PagesUi.Text(14, KlyTheme.TextMuted);
    private readonly Border _panel = new() { IsVisible = false, StrokeThickness = 1, Padding = new Thickness(0, 4) };

    /// <summary>Creates a search.</summary>
    public KlyContentSearch()
    {
        _search.Placeholder = Placeholder;
        _search.SetThemeColor(SearchBar.TextColorProperty, KlyTheme.Text);
        _search.SetThemeColor(SearchBar.PlaceholderColorProperty, KlyTheme.TextDimmed);
        _search.SetThemeColor(SearchBar.CancelButtonColorProperty, KlyTheme.TextMuted);
        SemanticProperties.SetDescription(_search, AccessibleName);
        _search.TextChanged += (_, e) => SetValue(TextProperty, e.NewTextValue);
        _search.Focused += (_, _) => IsOpen = true;

        _empty.Text = EmptyText;
        _empty.Padding = new Thickness(16, 12);
        _panel.SetRadius(KlyTheme.RadiusOf(1));
        _panel.SetStroke(KlyTheme.Border);
        _panel.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        _panel.Content = new ScrollView { MaximumHeightRequest = 360, Content = _results };

        Content = new VerticalStackLayout { Spacing = 4, Children = { _search, _panel } };
        ShowResults();
    }

    /// <summary>Raised when the user chooses a result. <see cref="PageLinkEventArgs.Item"/> is the <see cref="KlyContentSearchPage"/>, or the <see cref="KlyContentSearchHeading"/> of a heading; the address of a heading is the page address with <c>#</c> and the heading id.</summary>
    public event EventHandler<PageLinkEventArgs>? ResultSelected;

    /// <summary>Raised when the results open or close, by the user or by code.</summary>
    public event EventHandler? OpenChanged;

    /// <summary>Raised when the text of the search changes, by the user or by code.</summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged;

    /// <summary>The pages to search. A page is listed with its headings, under the heading of its <see cref="KlyContentSearchPage.Group"/>.</summary>
    public IReadOnlyList<KlyContentSearchPage>? Pages { get => (IReadOnlyList<KlyContentSearchPage>?)GetValue(PagesProperty); set => SetValue(PagesProperty, value); }

    /// <summary>The text searched. Two-way bindable.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Hint of the search field, in the language of the app.</summary>
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>What a screen reader says for the search field, in the language of the app.</summary>
    public string AccessibleName { get => (string)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>Text shown when nothing matches, in the language of the app.</summary>
    public string? EmptyText { get => (string?)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    /// <summary>Whether the results are shown. Two-way bindable.</summary>
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>The most results the list shows: every result is a view, so a long index would build thousands of them. 50 by default.</summary>
    public int MaxResults { get => (int)GetValue(MaxResultsProperty); set => SetValue(MaxResultsProperty, value); }

    internal SearchBar Bar => _search;

    internal VerticalStackLayout ResultList => _results;

    internal Border Panel => _panel;

    /// <summary>One line of the results: a page or one of its headings.</summary>
    internal sealed record Result(string Label, string? Description, string Href, string? Group, string? Icon, object Item);

    /// <summary>Closes the results.</summary>
    public void Close() => IsOpen = false;

    /// <summary>Chooses a result: raises <see cref="ResultSelected"/>, clears the text and closes the results.</summary>
    internal void Select(Result entry)
    {
        PagesLinks.Follow(this, ResultSelected, entry.Href, entry.Item);
        Text = null;
        Close();
    }

    /// <summary>The pages and headings that match the query: every word of the query is found in the title, description and keywords of a page, or in the text of a heading and the title of its page. The pages come before their headings, in the order of the list.</summary>
    internal IReadOnlyList<Result> Search(string? query)
    {
        var words = (query ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        bool Matches(string text) => words.All(w => text.Contains(w, StringComparison.OrdinalIgnoreCase));
        var found = new List<Result>();
        foreach (var page in Pages ?? [])
        {
            if (Matches($"{page.Title} {page.Description} {page.Keywords}"))
            {
                found.Add(new Result(page.Title, page.Description, page.Href, page.Group, page.Icon == "" ? null : page.Icon ?? KlyIconNames.File, page));
            }
            foreach (var heading in page.Headings ?? [])
            {
                if (Matches($"{heading.Text} {page.Title}"))
                {
                    found.Add(new Result(heading.Text, page.Title, $"{page.Href}#{heading.Id}", page.Group, KlyIconNames.Hash, heading));
                }
            }
        }
        return found.Take(Math.Max(0, MaxResults)).ToList();
    }

    private void ShowText(string? text)
    {
        if (_search.Text != text) _search.Text = text;
        if (!string.IsNullOrEmpty(text)) IsOpen = true;
        ShowResults();
        TextChanged?.Invoke(this, new TextChangedEventArgs(null, text));
    }

    private void ShowOpen(bool open)
    {
        _panel.IsVisible = open;
        OpenChanged?.Invoke(this, EventArgs.Empty);
    }

    // Groups keep the order of their first result; the results without a group come first.
    private void ShowResults()
    {
        _results.Children.Clear();
        var entries = Search(Text);
        if (entries.Count == 0)
        {
            _results.Children.Add(_empty);
            return;
        }
        var order = new List<string?>();
        foreach (var entry in entries)
        {
            if (!order.Contains(entry.Group)) order.Add(entry.Group);
        }
        foreach (var group in order.OrderBy(g => g is null ? 0 : 1))
        {
            if (group is not null) _results.Children.Add(GroupLabel(group));
            foreach (var entry in entries.Where(e => e.Group == group)) _results.Children.Add(Row(entry));
        }
    }

    private static Label GroupLabel(string text)
    {
        var label = PagesUi.Text(11, KlyTheme.TextMuted);
        PagesUi.Show(label, text);
        label.TextTransform = TextTransform.Uppercase;
        label.CharacterSpacing = 1.5;
        label.Padding = new Thickness(16, 8, 16, 4);
        SemanticProperties.SetHeadingLevel(label, SemanticHeadingLevel.Level3);
        return label;
    }

    private Border Row(Result entry)
    {
        var label = PagesUi.Text(14, KlyTheme.TextHighlighted);
        label.Text = entry.Label;
        label.IsVisible = true;
        label.LineBreakMode = LineBreakMode.TailTruncation;
        var description = PagesUi.Text(12, KlyTheme.TextMuted);
        PagesUi.Show(description, entry.Description);
        description.LineBreakMode = LineBreakMode.TailTruncation;

        var grid = new Grid
        {
            ColumnSpacing = 12,
            Padding = new Thickness(16, 8),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
        };
        if (entry.Icon is not null)
        {
            var icon = PagesUi.Icon(entry.Icon, KlySize.Md, KlyTheme.TextMuted);
            icon.VerticalOptions = LayoutOptions.Center;
            grid.Add(icon, 0, 0);
        }
        grid.Add(new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { label, description } }, 1, 0);

        var row = new Border { StrokeThickness = 0, MinimumHeightRequest = 40, Content = grid, BackgroundColor = Colors.Transparent };
        SemanticProperties.SetDescription(row, entry.Description is null ? entry.Label : $"{entry.Label}, {entry.Description}");
        PagesUi.Tappable(row, () => Select(entry));
        return row;
    }
}
