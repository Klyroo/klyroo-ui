namespace Klyroo.UI.Maui;

/// <summary>
/// The navigation of a documentation site: a tree of pages with collapsible groups. A top-level entry with children is a section heading with its pages below it.
/// A native app has no address: set <see cref="CurrentHref"/> to the page being shown and the entry that points at it is marked current, as is every group above it.
/// </summary>
public class KlyContentNavigation : ContentView
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyContentNavigationItem>), typeof(KlyContentNavigation), null, propertyChanged: (b, _, _) => ((KlyContentNavigation)b).Rebuild());
    /// <summary>Identifies <see cref="CurrentHref"/>.</summary>
    public static readonly BindableProperty CurrentHrefProperty = BindableProperty.Create(nameof(CurrentHref), typeof(string), typeof(KlyContentNavigation), null, propertyChanged: (b, _, _) => ((KlyContentNavigation)b).Rebuild());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyContentNavigation), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyContentNavigation)b).Rebuild());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyContentNavigation), "Documentation", propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyContentNavigation)b, (string?)v));

    private readonly VerticalStackLayout _root = new() { Spacing = 16 };

    // What the user opened or closed by hand, by item (the records compare by value: two entries with the same texts must not share a state).
    private readonly Dictionary<KlyContentNavigationItem, bool> _toggled = new(ReferenceEqualityComparer.Instance);

    /// <summary>Creates a navigation.</summary>
    public KlyContentNavigation()
    {
        Content = _root;
        SemanticProperties.SetDescription(this, AccessibleName);
    }

    /// <summary>Raised when the user taps an entry that has an address. <see cref="PageLinkEventArgs.Item"/> is the <see cref="KlyContentNavigationItem"/>.</summary>
    public event EventHandler<PageLinkEventArgs>? LinkClicked;

    /// <summary>The entries. A top-level entry with children is a section heading with its pages below it.</summary>
    public IReadOnlyList<KlyContentNavigationItem>? Items { get => (IReadOnlyList<KlyContentNavigationItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The address of the page being shown. An entry is current when its <see cref="KlyContentNavigationItem.Href"/> is this address, or, unless it is <see cref="KlyContentNavigationItem.Exact"/>, a path above it.</summary>
    public string? CurrentHref { get => (string?)GetValue(CurrentHrefProperty); set => SetValue(CurrentHrefProperty, value); }

    /// <summary>The color of the mark on the current page.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>What a screen reader says for the navigation, in the language of the app.</summary>
    public string AccessibleName { get => (string)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal VerticalStackLayout Root => _root;

    internal bool IsActive(KlyContentNavigationItem item)
    {
        if (item.Active is { } forced) return forced;
        if (item.Children is { Count: > 0 }) return item.Children.Any(IsActive);
        return !string.IsNullOrEmpty(item.Href) && Matches(CurrentHref, item.Href, item.Exact);
    }

    internal bool IsOpen(KlyContentNavigationItem item) => _toggled.TryGetValue(item, out var open) ? open : item.Open ?? IsActive(item);

    internal void Toggle(KlyContentNavigationItem item)
    {
        if (item.Disabled) return;
        _toggled[item] = !IsOpen(item);
        Rebuild();
    }

    internal void Activate(KlyContentNavigationItem item)
    {
        if (!item.Disabled) PagesLinks.Follow(this, LinkClicked, item.Href, item);
    }

    // The same address ignoring case, a query, a fragment and a trailing slash, or a path above the current one. The root only matches itself.
    internal static bool Matches(string? current, string href, bool exact)
    {
        var a = Clean(current);
        var b = Clean(href);
        if (a is null || b is null) return false;
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
        return !exact && b.Length > 1 && a.StartsWith(b + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Clean(string? href)
    {
        if (string.IsNullOrEmpty(href)) return null;
        var end = href.IndexOfAny(['?', '#']);
        var path = (end < 0 ? href : href[..end]).TrimEnd('/');
        return path.Length == 0 ? "/" : path;
    }

    private void Rebuild()
    {
        _root.Children.Clear();
        foreach (var item in Items ?? []) _root.Children.Add(Entry(item, 0));
    }

    private View Entry(KlyContentNavigationItem item, int depth)
    {
        if (item.Children is not { Count: > 0 } children) return Row(item, depth, false);

        var list = new VerticalStackLayout { Spacing = 2 };
        foreach (var child in children) list.Children.Add(Entry(child, depth + 1));

        View body = list;
        if (depth == 0)
        {
            list.Margin = new Thickness(0, 4, 0, 0);
        }
        else
        {
            // A group below the first level is indented under a line on its start edge, the way the web group is.
            var guided = new Grid { ColumnSpacing = 8, Margin = new Thickness(12, 2, 0, 0), ColumnDefinitions = { new ColumnDefinition(1), new ColumnDefinition(GridLength.Star) } };
            guided.Add(PagesUi.Line(KlyTheme.Border, vertical: true), 0, 0);
            guided.Add(list, 1, 0);
            body = guided;
        }
        body.IsVisible = IsOpen(item);
        return new VerticalStackLayout { Children = { Row(item, depth, true), body } };
    }

    private Border Row(KlyContentNavigationItem item, int depth, bool isGroup)
    {
        var set = KlyTheme.Resolve(Color);
        var active = !isGroup && IsActive(item);
        var section = isGroup && depth == 0;

        var label = PagesUi.Text(section ? 11 : 14, active ? KlyTheme.TextHighlighted : KlyTheme.TextMuted, active ? FontAttributes.Bold : FontAttributes.None);
        label.Text = item.Title;
        label.IsVisible = true;
        label.LineBreakMode = LineBreakMode.TailTruncation;
        label.VerticalOptions = LayoutOptions.Center;
        if (section)
        {
            label.TextTransform = TextTransform.Uppercase;
            label.CharacterSpacing = 1.5;
        }

        var grid = new Grid
        {
            ColumnSpacing = 8,
            Padding = new Thickness(8, section ? 4 : 6),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto),
            },
        };
        if (item.Icon is not null) grid.Add(PagesUi.Icon(item.Icon, KlySize.Xs, active ? KlyTheme.TextHighlighted : KlyTheme.TextMuted), 0, 0);
        grid.Add(label, 1, 0);
        if (item.Badge is not null) grid.Add(new KlyBadge { Text = item.Badge, Color = item.BadgeColor ?? KlyColor.Neutral, Variant = KlyVariant.Subtle, Size = KlySize.Xs }, 2, 0);
        if (isGroup)
        {
            var chevron = PagesUi.Icon(KlyIconNames.ChevronDown, KlySize.Xs, KlyTheme.TextMuted);
            chevron.Rotation = IsOpen(item) ? 180 : 0;
            grid.Add(chevron, 3, 0);
        }

        // The mark of the current page is a line on the start edge, drawn over the padding of the row.
        var frame = new Grid { Children = { grid } };
        if (active)
        {
            var bar = new BoxView { WidthRequest = 2, HorizontalOptions = LayoutOptions.Start };
            bar.SetThemeColor(BoxView.ColorProperty, set.Text);
            frame.Children.Add(bar);
        }

        var row = new Border { StrokeThickness = 0, MinimumHeightRequest = 40, Content = frame, IsEnabled = !item.Disabled, Opacity = item.Disabled ? 0.6 : 1 };
        row.SetRadius(KlyTheme.RadiusOf(0.75));
        row.SetThemeColor(BackgroundColorProperty, active ? KlyTheme.BackgroundMuted : KlyThemeColor.Transparent);
        SemanticProperties.SetDescription(row, item.Title);
        PagesUi.Tappable(row, () =>
        {
            if (isGroup) Toggle(item);
            else Activate(item);
        });
        return row;
    }
}
