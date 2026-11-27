namespace Klyroo.UI.Maui;

/// <summary>
/// The table of contents of a page. Give <see cref="ScrollView"/> the scroll view of the page and mark each heading view with an
/// <c>AutomationId</c>: a tap on a link scrolls to the heading whose id is the one of the link, and while the page scrolls the link of the heading
/// being read is highlighted (the last heading above the top quarter of the view, or the first one before any is above it).
/// Below <see cref="Breakpoint"/> the list is folded behind its title, as a disclosure; above it, and without <see cref="Collapsible"/>, it is always shown.
/// </summary>
public class KlyContentToc : ContentView
{
    /// <summary>Identifies <see cref="Links"/>.</summary>
    public static readonly BindableProperty LinksProperty = BindableProperty.Create(nameof(Links), typeof(IReadOnlyList<KlyContentTocLink>), typeof(KlyContentToc), null, propertyChanged: (b, _, _) => ((KlyContentToc)b).Rebuild());
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyContentToc), "On this page", propertyChanged: (b, _, _) => ((KlyContentToc)b).ShowTitle());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyContentToc), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyContentToc)b).Rebuild());
    /// <summary>Identifies <see cref="Collapsible"/>.</summary>
    public static readonly BindableProperty CollapsibleProperty = BindableProperty.Create(nameof(Collapsible), typeof(bool), typeof(KlyContentToc), true, propertyChanged: (b, _, _) => ((KlyContentToc)b).Arrange());
    /// <summary>Identifies <see cref="Breakpoint"/>.</summary>
    public static readonly BindableProperty BreakpointProperty = BindableProperty.Create(nameof(Breakpoint), typeof(KlyContentTocBreakpoint), typeof(KlyContentToc), KlyContentTocBreakpoint.Lg, propertyChanged: (b, _, _) => ((KlyContentToc)b).Arrange());
    /// <summary>Identifies <see cref="IsOpen"/>.</summary>
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(KlyContentToc), false, BindingMode.TwoWay, propertyChanged: (b, _, _) => ((KlyContentToc)b).Arrange());
    /// <summary>Identifies <see cref="ScrollView"/>.</summary>
    public static readonly BindableProperty ScrollViewProperty = BindableProperty.Create(nameof(ScrollView), typeof(ScrollView), typeof(KlyContentToc), null, propertyChanged: (b, o, n) => ((KlyContentToc)b).Watch((ScrollView?)o, (ScrollView?)n));

    private readonly Label _trigger = PagesUi.Text(14, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _title = PagesUi.Text(14, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly KlyIcon _chevron = PagesUi.Icon(KlyIconNames.ChevronDown, KlySize.Xs, KlyTheme.TextHighlighted);
    private readonly Grid _triggerRow = new() { MinimumHeightRequest = 40, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
    private readonly VerticalStackLayout _list = new();
    private readonly Grid _content = new() { ColumnDefinitions = { new ColumnDefinition(1), new ColumnDefinition(GridLength.Star) } };
    private IReadOnlyList<(KlyContentTocLink Link, Grid Row, BoxView Marker, Label Label)> _rows = [];
    private double _width;

    /// <summary>Creates a table of contents.</summary>
    public KlyContentToc()
    {
        _trigger.IsVisible = true;
        _title.IsVisible = true;
        _title.Margin = new Thickness(0, 8);
        _trigger.VerticalOptions = LayoutOptions.Center;
        _chevron.VerticalOptions = LayoutOptions.Center;
        _triggerRow.Add(_trigger, 0, 0);
        _triggerRow.Add(_chevron, 1, 0);
        PagesUi.Tappable(_triggerRow, () => IsOpen = !IsOpen);
        _content.Add(PagesUi.Line(KlyTheme.Border, vertical: true), 0, 0);
        _content.Add(_list, 1, 0);
        Content = new VerticalStackLayout { Children = { _triggerRow, _title, _content } };
        ShowTitle();
        SizeChanged += (_, _) => Relayout(Width);
        Arrange();
    }

    /// <summary>Raised when the user taps a link, before the control scrolls. <see cref="PageLinkEventArgs.Href"/> is <c>#id</c> and <see cref="PageLinkEventArgs.Item"/> the <see cref="KlyContentTocLink"/>. Set <see cref="PageLinkEventArgs.Handled"/> to scroll yourself.</summary>
    public event EventHandler<PageLinkEventArgs>? LinkClicked;

    /// <summary>The headings of the page, in document order. Each <c>Id</c> must be the automation id of a view in <see cref="ScrollView"/>.</summary>
    public IReadOnlyList<KlyContentTocLink>? Links { get => (IReadOnlyList<KlyContentTocLink>?)GetValue(LinksProperty); set => SetValue(LinksProperty, value); }

    /// <summary>The title, and the accessible name of the list, in the language of the app. "On this page" by default.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The color of the link of the heading being read.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Folds the list behind its title below <see cref="Breakpoint"/>, as a disclosure. Above it, and without this, the list is always shown.</summary>
    public bool Collapsible { get => (bool)GetValue(CollapsibleProperty); set => SetValue(CollapsibleProperty, value); }

    /// <summary>Width from which the table of contents is a plain list instead of a folded block.</summary>
    public KlyContentTocBreakpoint Breakpoint { get => (KlyContentTocBreakpoint)GetValue(BreakpointProperty); set => SetValue(BreakpointProperty, value); }

    /// <summary>Whether the folded list is open. It only matters while the list is folded. Two-way bindable.</summary>
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>The scroll view that holds the headings: a tap scrolls it, and its scrolling drives the highlighted link.</summary>
    public ScrollView? ScrollView { get => (ScrollView?)GetValue(ScrollViewProperty); set => SetValue(ScrollViewProperty, value); }

    /// <summary>The id of the heading being read, or <c>null</c> when none was set yet.</summary>
    public string? ActiveId { get; private set; }

    internal double BreakpointWidth => Breakpoint switch
    {
        KlyContentTocBreakpoint.Lg => 1024,
        KlyContentTocBreakpoint.Xl => 1280,
        KlyContentTocBreakpoint.Xxl => 1536,
        _ => throw new ArgumentOutOfRangeException(nameof(Breakpoint), Breakpoint, null),
    };

    /// <summary>Whether the list is folded behind its title now: collapsible and narrower than the breakpoint.</summary>
    internal bool IsFolded => Collapsible && _width < BreakpointWidth;

    internal Label TriggerLabel => _trigger;

    internal Label TitleLabel => _title;

    internal Grid TriggerRow => _triggerRow;

    internal Grid ListHost => _content;

    internal VerticalStackLayout List => _list;

    internal IReadOnlyList<(KlyContentTocLink Link, Grid Row, BoxView Marker, Label Label)> Rows => _rows;

    internal void Relayout(double width)
    {
        _width = width;
        Arrange();
    }

    internal void Activate(KlyContentTocLink link)
    {
        var href = "#" + link.Id;
        if (PagesLinks.Raise(this, LinkClicked, href, link).Handled) return;
        SetActive(link.Id);
        if (ScrollView is { } scroll && PagesScroll.Resolve(scroll, href) is { } target) PagesScroll.ScrollTo(scroll, target);
    }

    /// <summary>Marks the heading being read for a scroll position, with the viewport height of the scroll view.</summary>
    internal void Spy(double scrollY, double viewportHeight, Func<KlyContentTocLink, double?> offsetOf)
    {
        var candidates = (Links ?? []).Select(link => (link, offset: offsetOf(link))).Where(c => c.offset is not null).ToList();
        var index = PagesScroll.ActiveIndex(candidates.Select(c => c.offset!.Value).ToList(), scrollY + viewportHeight * 0.25);
        if (index >= 0) SetActive(candidates[index].link.Id);
    }

    private void ShowTitle()
    {
        _trigger.Text = Title;
        _title.Text = Title;
        SemanticProperties.SetDescription(_list, Title);
    }

    private void Arrange()
    {
        var folded = IsFolded;
        _triggerRow.IsVisible = folded;
        _title.IsVisible = !folded;
        _content.IsVisible = !folded || IsOpen;
        _chevron.Rotation = IsOpen ? 180 : 0;
        SemanticProperties.SetDescription(_triggerRow, Title);
    }

    private void Watch(ScrollView? old, ScrollView? scroll)
    {
        if (old is not null) old.Scrolled -= OnScrolled;
        if (scroll is not null) scroll.Scrolled += OnScrolled;
    }

    private void OnScrolled(object? sender, ScrolledEventArgs e)
    {
        if (ScrollView is not { } scroll) return;
        Spy(e.ScrollY, scroll.Height, link => PagesScroll.Resolve(scroll, "#" + link.Id) is { } target ? PagesScroll.OffsetOf(target, scroll) : null);
    }

    private void SetActive(string? id)
    {
        if (ActiveId == id) return;
        ActiveId = id;
        ApplyStyle();
    }

    private void Rebuild()
    {
        _list.Children.Clear();
        var rows = new List<(KlyContentTocLink, Grid, BoxView, Label)>();
        foreach (var link in Links ?? [])
        {
            var label = PagesUi.Text(14, KlyTheme.TextMuted);
            label.Text = link.Text;
            label.IsVisible = true;
            label.VerticalOptions = LayoutOptions.Center;
            var marker = new BoxView { WidthRequest = 2 };
            var row = new Grid
            {
                MinimumHeightRequest = 40,
                ColumnDefinitions = { new ColumnDefinition(2), new ColumnDefinition(GridLength.Star) },
            };
            row.Add(marker, 0, 0);
            row.Add(label, 1, 0);
            // ps-3 for a level 2 heading, 12 more units for each level below.
            label.Margin = new Thickness(12 * (Math.Clamp(link.Level, 2, 5) - 1), 0, 0, 0);
            SemanticProperties.SetDescription(row, link.Text);
            PagesUi.Tappable(row, () => Activate(link));
            _list.Children.Add(row);
            rows.Add((link, row, marker, label));
        }
        _rows = rows;
        ApplyStyle();
    }

    // The current heading: its marker and its text take the semantic color, the others are muted.
    private void ApplyStyle()
    {
        var set = KlyTheme.Resolve(Color);
        foreach (var (link, _, marker, label) in _rows)
        {
            var active = link.Id == ActiveId;
            marker.SetThemeColor(BoxView.ColorProperty, active ? set.Text : KlyThemeColor.Transparent);
            label.SetThemeColor(Label.TextColorProperty, active ? set.Text : KlyTheme.TextMuted);
            label.FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
        }
    }
}
