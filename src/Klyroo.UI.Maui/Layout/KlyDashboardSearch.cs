using TextLabel = Microsoft.Maui.Controls.Label;

namespace Klyroo.UI.Maui;

/// <summary>
/// A command search that sits inline in the dashboard instead of in a dialog: a native <see cref="SearchBar"/> with a results list under it, shown while <see cref="Open"/>.
/// Every word typed must match the label, the description, the keywords or the group of a command. Place it in a panel or a toolbar; a <see cref="KlyDashboardSearchButton"/> of the same dashboard opens it.
/// </summary>
public class KlyDashboardSearch : Border
{
    /// <summary>Identifies <see cref="Groups"/>.</summary>
    public static readonly BindableProperty GroupsProperty = BindableProperty.Create(nameof(Groups), typeof(IReadOnlyList<KlyDashboardSearchGroup>), typeof(KlyDashboardSearch), null, propertyChanged: (b, _, _) => ((KlyDashboardSearch)b).Rebuild());
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyDashboardSearch), "", BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlyDashboardSearch)b).OnTextChanged((string?)v));
    /// <summary>Identifies <see cref="Placeholder"/>.</summary>
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(KlyDashboardSearch), "Type a command or search...", propertyChanged: (b, _, v) => ((KlyDashboardSearch)b)._search.Placeholder = (string?)v);
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyDashboardSearch), "Search", propertyChanged: (b, _, v) => SemanticProperties.SetDescription(((KlyDashboardSearch)b)._search, (string?)v));
    /// <summary>Identifies <see cref="Open"/>.</summary>
    public static readonly BindableProperty OpenProperty = BindableProperty.Create(nameof(Open), typeof(bool), typeof(KlyDashboardSearch), false, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlyDashboardSearch)b).OnOpenChanged((bool)v));
    /// <summary>Identifies <see cref="EmptyText"/>.</summary>
    public static readonly BindableProperty EmptyTextProperty = BindableProperty.Create(nameof(EmptyText), typeof(string), typeof(KlyDashboardSearch), "No results", propertyChanged: (b, _, v) => ((KlyDashboardSearch)b)._empty.Text = (string?)v);
    /// <summary>Identifies <see cref="CloseLabel"/>.</summary>
    public static readonly BindableProperty CloseLabelProperty = BindableProperty.Create(nameof(CloseLabel), typeof(string), typeof(KlyDashboardSearch), "Close", propertyChanged: (b, _, v) => ((KlyDashboardSearch)b)._close.Label = (string)v);

    private readonly SearchBar _search = new() { FontSize = 14, BackgroundColor = Colors.Transparent, HorizontalOptions = LayoutOptions.Fill };
    private readonly LayoutIconButton _close = new(KlyIconNames.Close, "Close") { IsVisible = false, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 4, 0) };
    private readonly VerticalStackLayout _results = new() { Padding = new Thickness(4) };
    private readonly ScrollView _list = new() { IsVisible = false, MaximumHeightRequest = 320 };
    private readonly TextLabel _empty = new() { Text = "No results", FontSize = 14, HorizontalTextAlignment = TextAlignment.Center, Padding = new Thickness(16, 24) };

    /// <summary>Creates a search.</summary>
    public KlyDashboardSearch()
    {
        this.Apply(new Surface(KlyTheme.Background, KlyTheme.Border, 1, KlyTheme.Text));
        this.SetRadius(KlyTheme.RadiusOf(1));
        _search.SetThemeColor(InputView.TextColorProperty, KlyTheme.Text);
        _search.SetThemeColor(InputView.PlaceholderColorProperty, KlyTheme.TextDimmed);
        _search.SetThemeColor(SearchBar.CancelButtonColorProperty, KlyTheme.TextMuted);
        _search.SetThemeColor(SearchBar.SearchIconColorProperty, KlyTheme.TextMuted);
        _empty.SetThemeColor(TextLabel.TextColorProperty, KlyTheme.TextMuted);
        _search.Placeholder = Placeholder;
        SemanticProperties.SetDescription(_search, Label);
        _search.TextChanged += (_, e) => Text = e.NewTextValue ?? "";
        _search.Focused += (_, _) => Open = true;
        _search.SearchButtonPressed += (_, _) =>
        {
            // The return key picks the first command that can be activated.
            if (Matches().FirstOrDefault(m => !m.Item.Disabled) is { Item: { } first }) Select(first);
        };
        _close.Clicked += (_, _) => Open = false;

        var bar = new Grid { ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)] };
        bar.Add(_search, 0);
        bar.Add(_close, 1);
        _list.Content = _results;
        Content = new VerticalStackLayout { Children = { bar, _list } };
        Rebuild();
    }

    /// <summary>Raised when the user activates a command that is not disabled. The search closes and clears itself first. A link command carries its <see cref="KlyDashboardSearchItem.Href"/>.</summary>
    public event EventHandler<KlyDashboardSearchItem>? ItemSelected;

    /// <summary>Raised with the new state when the results open or close.</summary>
    public event EventHandler<ToggledEventArgs>? OpenChanged;

    /// <summary>The commands, in groups.</summary>
    public IReadOnlyList<KlyDashboardSearchGroup>? Groups { get => (IReadOnlyList<KlyDashboardSearchGroup>?)GetValue(GroupsProperty); set => SetValue(GroupsProperty, value); }

    /// <summary>The text typed in the search field. Two-way bindable.</summary>
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Hint of the search field. Defaults to "Type a command or search...".</summary>
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>Accessible name of the search field. Defaults to "Search".</summary>
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Shows the results under the field: they open when the field gets the focus, and close when a command is chosen or with the close button. Two-way bindable.</summary>
    public bool Open { get => (bool)GetValue(OpenProperty); set => SetValue(OpenProperty, value); }

    /// <summary>Text shown when no command matches. Defaults to "No results".</summary>
    public string EmptyText { get => (string)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    /// <summary>Accessible name of the button that closes the results. Defaults to "Close".</summary>
    public string CloseLabel { get => (string)GetValue(CloseLabelProperty); set => SetValue(CloseLabelProperty, value); }

    /// <summary>Opens the results and puts the cursor in the field.</summary>
    internal void FocusInput()
    {
        Open = true;
        _search.Focus();
    }

    /// <summary>The commands that match the text, each with the heading of its group.</summary>
    internal IReadOnlyList<(string? Group, KlyDashboardSearchItem Item)> Matches()
    {
        var words = (Text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var matches = new List<(string?, KlyDashboardSearchItem)>();
        foreach (var group in Groups ?? [])
        {
            foreach (var item in group.Items)
            {
                var haystack = string.Join(' ', new[] { item.Label, item.Description, item.Keywords, group.Label }.Where(s => !string.IsNullOrEmpty(s)));
                if (words.All(w => haystack.Contains(w, StringComparison.OrdinalIgnoreCase))) matches.Add((group.Label, item));
            }
        }
        return matches;
    }

    /// <summary>Activates a command, as a tap does.</summary>
    internal void Select(KlyDashboardSearchItem item)
    {
        if (item.Disabled) return;
        Open = false;
        Text = "";
        ItemSelected?.Invoke(this, item);
    }

    private void OnTextChanged(string? text)
    {
        if (_search.Text != text) _search.Text = text;
        Rebuild();
    }

    private void OnOpenChanged(bool open)
    {
        Rebuild();
        OpenChanged?.Invoke(this, new ToggledEventArgs(open));
    }

    private void Rebuild()
    {
        _list.IsVisible = Open;
        _close.IsVisible = Open;
        _results.Children.Clear();
        if (!Open) return;

        var matches = Matches();
        if (matches.Count == 0)
        {
            _results.Add(_empty);
            return;
        }

        string? current = null;
        var first = true;
        foreach (var (group, item) in matches)
        {
            if (first || group != current)
            {
                first = false;
                current = group;
                if (!string.IsNullOrEmpty(group)) _results.Add(BuildHeading(group));
            }
            _results.Add(BuildRow(item));
        }
    }

    private static TextLabel BuildHeading(string text)
    {
        var heading = new TextLabel { Text = text, FontSize = 12, FontAttributes = FontAttributes.Bold, Padding = new Thickness(8, 8, 8, 4) };
        heading.SetThemeColor(TextLabel.TextColorProperty, KlyTheme.TextMuted);
        SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level3);
        return heading;
    }

    private View BuildRow(KlyDashboardSearchItem item)
    {
        var row = new Grid { ColumnSpacing = 8, Padding = new Thickness(8, 6), MinimumHeightRequest = 40, ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto)] };
        if (!string.IsNullOrEmpty(item.Icon)) row.Add(new KlyIcon { Name = item.Icon, IconColor = KlyTheme.TextMuted, VerticalOptions = LayoutOptions.Center }, 0);

        var texts = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center };
        var label = new TextLabel { Text = item.Label, FontSize = 14 };
        label.SetThemeColor(TextLabel.TextColorProperty, KlyTheme.Text);
        texts.Add(label);
        if (!string.IsNullOrEmpty(item.Description))
        {
            var description = new TextLabel { Text = item.Description, FontSize = 12 };
            description.SetThemeColor(TextLabel.TextColorProperty, KlyTheme.TextMuted);
            texts.Add(description);
        }
        row.Add(texts, 1);

        if (item.Count is { } count) row.Add(Muted(count.ToString("N0", System.Globalization.CultureInfo.CurrentCulture)), 2);
        if (!string.IsNullOrEmpty(item.Shortcut)) row.Add(Muted(item.Shortcut), 3);

        row.IsEnabled = !item.Disabled;
        row.Opacity = item.Disabled ? 0.6 : 1;
        if (!item.Disabled) row.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Select(item)) });
        return row;
    }

    private static TextLabel Muted(string text)
    {
        var label = new TextLabel { Text = text, FontSize = 12, VerticalOptions = LayoutOptions.Center };
        label.SetThemeColor(TextLabel.TextColorProperty, KlyTheme.TextMuted);
        return label;
    }
}
