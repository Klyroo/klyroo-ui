namespace Klyroo.UI.Maui;

/// <summary>
/// A tree of nodes shown as a flat list of rows, indented by depth. Tapping a row selects it and, when it has children, expands or collapses it.
/// All the visible rows are views: for a very large tree, expand lazily or page the data.
/// </summary>
public class KlyTree : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyTreeItem>), typeof(KlyTree), Array.Empty<KlyTreeItem>(), propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(string), typeof(KlyTree), null, BindingMode.TwoWay, propertyChanged: (b, o, n) => ((KlyTree)b).OnValueChanged((string?)o, (string?)n));
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyTree), KlyColor.Primary, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyTree), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyTree)b, (string?)v));

    /// <summary>The indent of each level, in device-independent units.</summary>
    internal const double Indent = 18;

    // The nodes that are expanded, by reference: the items are records with value equality.
    private readonly HashSet<KlyTreeItem> _expanded = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<KlyTreeItem> _seen = new(ReferenceEqualityComparer.Instance);
    private readonly List<NavigationButton> _rows = [];

    /// <summary>Creates a tree.</summary>
    public KlyTree()
    {
        Spacing = 2;
        Rebuild();
    }

    /// <summary>Raised with the value of the selected node when it changes, by the user or by code.</summary>
    public event EventHandler<KlyNavigationValueEventArgs<string?>>? ValueChanged;

    /// <summary>The nodes of the tree, as <see cref="KlyTreeItem"/>.</summary>
    public IReadOnlyList<KlyTreeItem> Items { get => (IReadOnlyList<KlyTreeItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>Value of the selected node. Two-way bindable.</summary>
    public string? Value { get => (string?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Semantic color of the selected node.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>What a screen reader says for the tree.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal IReadOnlyList<NavigationButton> Rows => _rows;

    internal static string ValueOf(KlyTreeItem item) => item.Value ?? item.Label;

    /// <summary>The rows that show, in order, with their depth: a node shows when all its ancestors are expanded.</summary>
    internal IReadOnlyList<(KlyTreeItem Item, int Depth)> VisibleNodes()
    {
        var result = new List<(KlyTreeItem, int)>();
        void Walk(IReadOnlyList<KlyTreeItem> nodes, int depth)
        {
            foreach (var node in nodes)
            {
                result.Add((node, depth));
                if (node.Children is { Count: > 0 } children && _expanded.Contains(node)) Walk(children, depth + 1);
            }
        }
        Walk(Items, 0);
        return result;
    }

    internal bool IsExpanded(KlyTreeItem item) => _expanded.Contains(item);

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyTree)bindable).Rebuild();

    private void OnValueChanged(string? oldValue, string? newValue)
    {
        Rebuild();
        ValueChanged?.Invoke(this, new KlyNavigationValueEventArgs<string?>(newValue, oldValue));
    }

    // A node starts expanded the first time the tree sees it when it says so.
    private void Seed(IReadOnlyList<KlyTreeItem> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.Children is not { Count: > 0 } children) continue;
            if (_seen.Add(node) && node.Expanded) _expanded.Add(node);
            Seed(children);
        }
    }

    private void Rebuild()
    {
        Children.Clear();
        _rows.Clear();
        Seed(Items);
        var set = KlyTheme.Resolve(Color);
        foreach (var (item, depth) in VisibleNodes())
        {
            var hasChildren = item.Children is { Count: > 0 };
            var selected = Value is not null && Value == ValueOf(item);
            var text = selected ? set.Text : KlyTheme.Text;

            var row = new NavigationButton { IsEnabled = !item.Disabled, Margin = new Thickness(depth * Indent, 0, 0, 0) };
            row.Face.HorizontalOptions = LayoutOptions.Fill;
            row.Face.Padding = new Thickness(8, 4);
            row.Face.SetRadius(KlyTheme.RadiusOf(1));
            row.Face.Apply(selected
                ? new Surface(set.Fill.WithAlpha(0.10f), KlyThemeColor.Transparent, 0, text)
                : new Surface(KlyThemeColor.Transparent, KlyThemeColor.Transparent, 0, text));

            var content = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
            if (hasChildren)
            {
                var chevron = NavigationLayout.Icon(KlyIconNames.ChevronRight, KlySize.Sm, KlyTheme.TextMuted);
                chevron.Rotation = _expanded.Contains(item) ? 90 : 0;
                content.Children.Add(chevron);
            }
            else
            {
                // The room of a chevron, so the labels of a level line up.
                content.Children.Add(new BoxView { WidthRequest = 16, HeightRequest = 16, Color = Colors.Transparent });
            }
            if (item.Icon is { } icon) content.Children.Add(NavigationLayout.Icon(icon, KlySize.Sm, text));
            var label = NavigationLayout.TextLabel(14, text);
            label.Text = item.Label;
            content.Children.Add(label);
            row.Face.Content = content;

            SemanticProperties.SetDescription(row, item.Label);
            var node = item;
            row.Clicked += (_, _) => Activate(node);
            _rows.Add(row);
            Children.Add(row);
        }
    }

    // A tap selects the node, and expands or collapses it when it has children.
    private void Activate(KlyTreeItem item)
    {
        if (item.Disabled) return;
        var toggled = false;
        if (item.Children is { Count: > 0 })
        {
            if (!_expanded.Remove(item)) _expanded.Add(item);
            toggled = true;
        }
        // A new selection rebuilds the rows through the change of Value; otherwise only the expansion did change.
        var value = ValueOf(item);
        if (Value != value) Value = value;
        else if (toggled) Rebuild();
    }
}
