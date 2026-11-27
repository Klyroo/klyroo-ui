namespace Klyroo.UI.Maui;

/// <summary>
/// A search field over a list of commands in groups. Typing filters the list (every word must match the label, description, keywords or group label),
/// the first match is highlighted and Enter, or a tap, activates a command and raises <see cref="ItemSelected"/>.
/// It is an inline control: put it in the body of a <see cref="KlyModal"/> or a <see cref="KlyDrawer"/> for the usual palette.
/// </summary>
public class KlyCommandPalette : Grid
{
    /// <summary>Identifies <see cref="Groups"/>.</summary>
    public static readonly BindableProperty GroupsProperty = BindableProperty.Create(nameof(Groups), typeof(IReadOnlyList<KlyCommandGroup>), typeof(KlyCommandPalette), Array.Empty<KlyCommandGroup>(), propertyChanged: (b, _, _) => ((KlyCommandPalette)b).Rebuild());
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyCommandPalette), "", BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlyCommandPalette)b).OnTextChanged((string?)v ?? ""));
    /// <summary>Identifies <see cref="Placeholder"/>.</summary>
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(KlyCommandPalette), "Search commands", propertyChanged: (b, _, v) => ((KlyCommandPalette)b)._entry.Placeholder = (string?)v);
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyCommandPalette), "Search commands", propertyChanged: (b, _, v) => ((KlyCommandPalette)b).ApplyLabel((string?)v));
    /// <summary>Identifies <see cref="CloseOnSelect"/>.</summary>
    public static readonly BindableProperty CloseOnSelectProperty = BindableProperty.Create(nameof(CloseOnSelect), typeof(bool), typeof(KlyCommandPalette), false);
    /// <summary>Identifies <see cref="EmptyText"/>.</summary>
    public static readonly BindableProperty EmptyTextProperty = BindableProperty.Create(nameof(EmptyText), typeof(string), typeof(KlyCommandPalette), "No results found", propertyChanged: (b, _, v) => ((KlyCommandPalette)b)._empty.Text = (string?)v);
    /// <summary>Identifies <see cref="ResultsText"/>.</summary>
    public static readonly BindableProperty ResultsTextProperty = BindableProperty.Create(nameof(ResultsText), typeof(string), typeof(KlyCommandPalette), "{0} results available");

    private readonly Entry _entry = new() { FontSize = 14, MinimumHeightRequest = 48, BackgroundColor = Colors.Transparent, VerticalOptions = LayoutOptions.Center, ReturnType = ReturnType.Done };
    private readonly BoxView _inputLine = new() { HeightRequest = 1 };
    private readonly VerticalStackLayout _list = new() { Spacing = 0, Padding = new Thickness(8) };
    private readonly Label _empty = new() { Text = "No results found", FontSize = 14, HorizontalTextAlignment = TextAlignment.Center, Padding = new Thickness(24, 40), IsVisible = false };
    private readonly List<GroupView> _groups = [];
    private readonly List<OverlaysCommandRow> _rows = [];
    private OverlaysCommandRow? _highlighted;

    private sealed record GroupView(VerticalStackLayout View, View? Separator, List<OverlaysCommandRow> Rows);

    /// <summary>Creates an empty palette: set <see cref="Groups"/>.</summary>
    public KlyCommandPalette()
    {
        RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        RowDefinitions.Add(new RowDefinition(GridLength.Star));

        var input = new Grid { Padding = new Thickness(16, 0), ColumnSpacing = 12 };
        input.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        input.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        input.Add(new KlyIcon { Name = KlyIconNames.Search, Size = KlySize.Sm, IconColor = KlyTheme.TextMuted, VerticalOptions = LayoutOptions.Center }, 0);
        input.Add(_entry, 1);
        this.Add(input, 0, 0);
        this.Add(_inputLine, 0, 1);

        _entry.Placeholder = Placeholder;
        _entry.SetThemeColor(Entry.TextColorProperty, KlyTheme.TextHighlighted);
        _entry.SetThemeColor(Entry.PlaceholderColorProperty, KlyTheme.TextDimmed);
        _empty.SetThemeColor(Microsoft.Maui.Controls.Label.TextColorProperty, KlyTheme.TextMuted);
        SetLine(focused: false);
        _entry.HandlerChanged += (_, _) => PlatformTweaks.RemoveNativeFieldChrome(_entry);
        _entry.Focused += (_, _) => SetLine(focused: true);
        _entry.Unfocused += (_, _) => SetLine(focused: false);
        _entry.TextChanged += (_, e) => SetValue(TextProperty, e.NewTextValue ?? "");
        _entry.Completed += (_, _) =>
        {
            if (_highlighted is { } row) Select(row);
        };

        var listBox = new VerticalStackLayout { Spacing = 0, Children = { _list, _empty } };
        this.Add(new ScrollView { Content = listBox, MaximumHeightRequest = 384 }, 0, 2);
        ApplyLabel(Label);
    }

    /// <summary>Raised with the command the user activated: a tap on a row, or Enter on the highlighted one. A disabled command never raises it.</summary>
    public event EventHandler<KlyCommandItem>? ItemSelected;

    /// <summary>The commands, in groups. A group without a label has no heading.</summary>
    public IReadOnlyList<KlyCommandGroup> Groups { get => (IReadOnlyList<KlyCommandGroup>)GetValue(GroupsProperty); set => SetValue(GroupsProperty, value); }

    /// <summary>What is typed in the search field. Two-way bindable; setting it filters the list.</summary>
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Hint of the search field.</summary>
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>What a screen reader says for the search field.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Activating a command closes the <see cref="KlyModal"/>, <see cref="KlyDrawer"/> or <see cref="KlySlideover"/> the palette is in.</summary>
    public bool CloseOnSelect { get => (bool)GetValue(CloseOnSelectProperty); set => SetValue(CloseOnSelectProperty, value); }

    /// <summary>Text shown when no command matches.</summary>
    public string? EmptyText { get => (string?)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    /// <summary>What a screen reader hears as the search narrows the list: <c>{0}</c> is the number of commands left.</summary>
    public string? ResultsText { get => (string?)GetValue(ResultsTextProperty); set => SetValue(ResultsTextProperty, value); }

    /// <summary>The number of commands that match the search.</summary>
    internal int VisibleCount => _rows.Count(r => r.IsVisible);

    /// <summary>The command Enter activates, or null when none matches.</summary>
    internal KlyCommandItem? HighlightedItem => _highlighted?.Item;

    /// <summary>The rows of all the commands, matching or not, in order.</summary>
    internal IReadOnlyList<OverlaysCommandRow> Rows => _rows;

    /// <summary>The text shown when nothing matches, and whether it is shown.</summary>
    internal Label EmptyLabel => _empty;

    /// <summary>The headings and the separators of the groups, to check which groups are shown.</summary>
    internal IReadOnlyList<(View Group, View? Separator)> GroupViews => _groups.Select(g => ((View)g.View, g.Separator)).ToArray();

    /// <summary>Whether a command matches a search: every word of the search is somewhere in its label, description, keywords or group label, whatever the case.</summary>
    internal static bool Matches(KlyCommandGroup group, KlyCommandItem item, string query)
    {
        var tokens = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return true;
        var text = string.Join(' ', new[] { item.Label, item.Description, item.Keywords, group.Label }.Where(s => !string.IsNullOrEmpty(s)));
        return tokens.All(token => text.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private void OnTextChanged(string value)
    {
        if (_entry.Text != value) _entry.Text = value;
        Refilter(reset: true);
    }

    private void ApplyLabel(string? label)
    {
        SemanticProperties.SetDescription(_entry, label);
        SemanticProperties.SetDescription(_list, label);
    }

    private void SetLine(bool focused) =>
        _inputLine.SetThemeColor(BoxView.ColorProperty, focused ? KlyTheme.Resolve(KlyColor.Primary).Text : KlyTheme.Border);

    private void Rebuild()
    {
        _list.Clear();
        _groups.Clear();
        _rows.Clear();
        _highlighted = null;
        var index = 0;
        foreach (var group in Groups)
        {
            var view = new VerticalStackLayout { Spacing = 0 };
            View? separator = null;
            if (index++ > 0)
            {
                // Groups after the first are set apart by a line, which the first visible group does not show.
                var line = new BoxView { HeightRequest = 1, Margin = new Thickness(0, 8) };
                line.SetThemeColor(BoxView.ColorProperty, KlyTheme.Border);
                AutomationProperties.SetIsInAccessibleTree(line, false);
                separator = line;
                view.Add(line);
            }
            if (!string.IsNullOrEmpty(group.Label))
            {
                var heading = new Label { Text = group.Label, FontSize = 11, FontAttributes = FontAttributes.Bold, TextTransform = TextTransform.Uppercase, CharacterSpacing = 1, Padding = new Thickness(10, 6) };
                heading.SetThemeColor(Microsoft.Maui.Controls.Label.TextColorProperty, KlyTheme.TextDimmed);
                SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level3);
                view.Add(heading);
            }
            var rows = new List<OverlaysCommandRow>();
            foreach (var item in group.Items)
            {
                var row = new OverlaysCommandRow(group, item);
                row.Activated += (_, _) => Select(row);
                row.Hovered += (_, _) => Highlight(row);
                rows.Add(row);
                _rows.Add(row);
                view.Add(row);
            }
            _groups.Add(new GroupView(view, separator, rows));
            _list.Add(view);
        }
        Refilter(reset: true);
    }

    // Cheap on every keystroke: a few hundred commands are a few hundred string searches, and only visibility changes.
    private void Refilter(bool reset)
    {
        var query = Text;
        var visible = 0;
        var firstVisibleGroup = true;
        foreach (var group in _groups)
        {
            var any = false;
            foreach (var row in group.Rows)
            {
                var match = Matches(row.Group, row.Item, query);
                row.IsVisible = match;
                any |= match;
                if (match) visible++;
            }
            group.View.IsVisible = any;
            if (group.Separator is not null) group.Separator.IsVisible = !firstVisibleGroup;
            if (any) firstVisibleGroup = false;
        }
        _empty.IsVisible = visible == 0;

        var keep = !reset && _highlighted is { IsVisible: true, Item.IsEnabled: true };
        Highlight(keep ? _highlighted : _rows.FirstOrDefault(r => r.IsVisible && r.Item.IsEnabled));
        if (reset && _rows.Count > 0)
        {
            OverlaysSupport.Announce(visible == 0 ? EmptyText ?? "" : string.Format(System.Globalization.CultureInfo.CurrentCulture, ResultsText ?? "{0}", visible));
        }
    }

    private void Highlight(OverlaysCommandRow? row)
    {
        if (_highlighted is not null && !ReferenceEquals(_highlighted, row)) _highlighted.Highlighted = false;
        _highlighted = row;
        if (row is not null) row.Highlighted = true;
    }

    private void Select(OverlaysCommandRow row)
    {
        var item = row.Item;
        if (!item.IsEnabled) return;
        Highlight(row);
        ItemSelected?.Invoke(this, item);
        OverlaysSupport.OpenLink(item.Href);
        if (CloseOnSelect) OverlaysPanel.Find(this)?.RequestClose();
    }
}
