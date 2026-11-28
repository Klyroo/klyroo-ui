namespace Klyroo.UI.Maui;

/// <summary>
/// Site navigation: links in a row or a column, and sub-items behind a button. Horizontally the sub-items of the open item show in a panel
/// under the row (inline, not an overlay); vertically they form collapsible groups. The menu does not navigate: handle <see cref="ItemInvoked"/>, then set <see cref="Current"/>.
/// </summary>
public class KlyNavigationMenu : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyNavigationItem>), typeof(KlyNavigationMenu), Array.Empty<KlyNavigationItem>(), propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyNavigationMenu), StackOrientation.Horizontal, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyNavigationMenu), KlyColor.Primary, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Current"/>.</summary>
    public static readonly BindableProperty CurrentProperty = BindableProperty.Create(nameof(Current), typeof(string), typeof(KlyNavigationMenu), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyNavigationMenu), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyNavigationMenu)b, (string?)v));

    // Open groups (vertical) or the one open item (horizontal), by reference: the items are records with value equality.
    private readonly HashSet<KlyNavigationItem> _open = new(ReferenceEqualityComparer.Instance);
    private readonly List<NavigationButton> _links = [];
    private readonly List<NavigationButton> _triggers = [];
    private readonly List<View> _groups = [];
    private readonly HashSet<KlyNavigationItem> _seen = new(ReferenceEqualityComparer.Instance);
    private Border? _panel;

    /// <summary>Creates a navigation menu.</summary>
    public KlyNavigationMenu()
    {
        Spacing = 4;
        Rebuild();
    }

    /// <summary>Raised when the user taps an enabled item that has an <see cref="KlyNavigationItem.Href"/>.</summary>
    public event EventHandler<KlyNavigationItem>? ItemInvoked;

    /// <summary>The links, with optional sub-items, as <see cref="KlyNavigationItem"/>.</summary>
    public IReadOnlyList<KlyNavigationItem> Items { get => (IReadOnlyList<KlyNavigationItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary><c>Horizontal</c> shows sub-items in a panel under the row; <c>Vertical</c> shows them as collapsible groups.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Semantic color of the active item.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>The address or route of the page that shows now. The items whose <see cref="KlyNavigationItem.Href"/> matches it are active.</summary>
    public string? Current { get => (string?)GetValue(CurrentProperty); set => SetValue(CurrentProperty, value); }

    /// <summary>What a screen reader says for the menu. Needed when the page has several.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal IReadOnlyList<NavigationButton> Links => _links;

    internal IReadOnlyList<NavigationButton> GroupTriggers => _triggers;

    internal Border? Panel => _panel;

    /// <summary>The collapsible groups of a vertical menu, in order, visible while open.</summary>
    internal IReadOnlyList<View> Groups => _groups;

    /// <summary>Whether <paramref name="current"/> is the page <paramref name="href"/> stands for: equal, or below it unless <paramref name="exact"/>. Query and fragment are ignored.</summary>
    internal static bool Matches(string? current, string href, bool exact)
    {
        static string Normalize(string value)
        {
            var end = value.IndexOfAny(['?', '#']);
            if (end >= 0) value = value[..end];
            return value.Length > 1 ? value.TrimEnd('/') : value;
        }
        if (string.IsNullOrEmpty(current)) return false;
        var page = Normalize(current);
        var target = Normalize(href);
        if (string.Equals(page, target, StringComparison.OrdinalIgnoreCase)) return true;
        // The root address is below nothing: only the exact page matches it.
        return !exact && target.Length > 1 && page.StartsWith(target + "/", StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsActive(KlyNavigationItem item)
    {
        if (item.Active is { } forced) return forced;
        if (item.Children is { Count: > 0 }) return item.Children.Any(IsActive);
        if (string.IsNullOrEmpty(item.Href)) return false;
        return Matches(Current, item.Href, item.Exact);
    }

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyNavigationMenu)bindable).Rebuild();

    private void Rebuild()
    {
        Children.Clear();
        _links.Clear();
        _triggers.Clear();
        _groups.Clear();
        _panel = null;
        var items = Items;
        // A group starts open when it holds the active item, the first time the menu shows it.
        foreach (var item in items) Seed(item);

        if (Orientation == StackOrientation.Vertical)
        {
            foreach (var item in items) Children.Add(Entry(item, vertical: true));
            return;
        }

        var row = new HorizontalStackLayout { Spacing = 4 };
        KlyNavigationItem? open = null;
        foreach (var item in items)
        {
            row.Children.Add(Entry(item, vertical: false));
            if (item.Children is { Count: > 0 } && _open.Contains(item)) open = item;
        }
        Children.Add(row);
        if (open is not null) Children.Add(_panel = BuildPanel(open));
    }

    private void Seed(KlyNavigationItem item)
    {
        if (item.Children is not { Count: > 0 } children) return;
        if (_seen.Add(item) && IsActive(item) && Orientation == StackOrientation.Vertical) _open.Add(item);
        foreach (var child in children) Seed(child);
    }

    private View Entry(KlyNavigationItem item, bool vertical)
    {
        if (item.Children is not { Count: > 0 } children) return Link(item, vertical);

        var open = _open.Contains(item);
        var trigger = Trigger(item, open, vertical);
        if (!vertical) return trigger;

        var group = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(9, 0, 0, 0) };
        foreach (var child in children) group.Children.Add(Entry(child, vertical: true));
        // The guide line of the group (border-s) runs along its left edge.
        var host = new Grid { Margin = new Thickness(12, 4, 0, 0), IsVisible = open };
        _groups.Add(host);
        host.Children.Add(group);
        var line = NavigationLayout.Line(KlyTheme.Border, width: 1);
        line.HorizontalOptions = LayoutOptions.Start;
        host.Children.Add(line);
        return new VerticalStackLayout { Spacing = 0, Children = { trigger, host } };
    }

    private Border BuildPanel(KlyNavigationItem item)
    {
        var list = new VerticalStackLayout { Spacing = 4 };
        foreach (var child in item.Children!) list.Children.Add(Link(child, vertical: true));
        var panel = new Border { Padding = new Thickness(6), MinimumWidthRequest = 192, HorizontalOptions = LayoutOptions.Start, Content = list };
        panel.SetRadius(KlyTheme.RadiusOf(1));
        panel.Apply(new Surface(KlyTheme.Background, KlyTheme.Border, 1, KlyTheme.Text));
        return panel;
    }

    private NavigationButton NewButton(KlyNavigationItem item, bool vertical)
    {
        var button = new NavigationButton { IsEnabled = !item.Disabled };
        button.Face.Padding = new Thickness(10, 6);
        button.Face.SetRadius(KlyTheme.RadiusOf(1));
        button.Face.HorizontalOptions = vertical ? LayoutOptions.Fill : LayoutOptions.Center;
        if (vertical) button.HorizontalOptions = LayoutOptions.Fill;
        SemanticProperties.SetDescription(button, item.Label);
        return button;
    }

    private HorizontalStackLayout Inner(KlyNavigationItem item, KlyThemeColor color)
    {
        var row = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        if (item.Icon is { } icon) row.Children.Add(NavigationLayout.Icon(icon, KlySize.Md, color));
        var label = NavigationLayout.TextLabel(14, color);
        label.Text = item.Label;
        row.Children.Add(label);
        return row;
    }

    private NavigationButton Link(KlyNavigationItem item, bool vertical)
    {
        var button = NewButton(item, vertical);
        var set = KlyTheme.Resolve(Color);
        var active = IsActive(item);
        var text = active ? set.Text : KlyTheme.TextMuted;
        // aria-current="page": a tint of the color, its text color and an inset ring.
        button.Face.Apply(active
            ? new Surface(set.Fill.WithAlpha(0.10f), set.Text.WithAlpha(0.75f), 1, text)
            : new Surface(KlyThemeColor.Transparent, KlyThemeColor.Transparent, 0, text));
        button.Face.Content = Inner(item, text);
        if (item.Href is not null) button.Clicked += (_, _) => ItemInvoked?.Invoke(this, item);
        _links.Add(button);
        return button;
    }

    private NavigationButton Trigger(KlyNavigationItem item, bool open, bool vertical)
    {
        var button = NewButton(item, vertical);
        var inner = Inner(item, KlyTheme.TextMuted);
        var chevron = NavigationLayout.Icon(KlyIconNames.ChevronDown, KlySize.Sm, KlyTheme.TextMuted);
        chevron.Rotation = open ? 180 : 0;
        inner.Children.Add(chevron);
        button.Face.Apply(new Surface(KlyThemeColor.Transparent, KlyThemeColor.Transparent, 0, KlyTheme.TextMuted));
        button.Face.Content = inner;
        button.Clicked += (_, _) => Toggle(item);
        _triggers.Add(button);
        return button;
    }

    private void Toggle(KlyNavigationItem item)
    {
        if (_open.Remove(item))
        {
            Rebuild();
            return;
        }
        // A horizontal menu shows one panel at a time.
        if (Orientation == StackOrientation.Horizontal) _open.Clear();
        _open.Add(item);
        Rebuild();
    }
}
