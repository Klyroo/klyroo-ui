namespace Klyroo.UI.Maui;

/// <summary>Tabs that switch between content panels: a row (or a column) of tab buttons and the panel of the selected one.</summary>
public class KlyTabs : Grid
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyTabItem>), typeof(KlyTabs), Array.Empty<KlyTabItem>(), propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(string), typeof(KlyTabs), null, BindingMode.TwoWay, propertyChanged: (b, o, n) => ((KlyTabs)b).OnValueChanged((string?)o, (string?)n));
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(TabsVariant), typeof(KlyTabs), TabsVariant.Pill, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyTabs), StackOrientation.Horizontal, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyTabs), KlyColor.Primary, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyTabs), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription(((KlyTabs)b)._listHost, (string?)v));

    private readonly List<NavigationButton> _triggers = [];
    private readonly ContentView _panel = new();
    private readonly ContentView _listHost = new();

    /// <summary>Creates tabs.</summary>
    public KlyTabs()
    {
        Rebuild();
    }

    /// <summary>Raised with the value of the selected tab when it changes, by the user or by code.</summary>
    public event EventHandler<KlyNavigationValueEventArgs<string?>>? ValueChanged;

    /// <summary>The tabs, as <see cref="KlyTabItem"/>.</summary>
    public IReadOnlyList<KlyTabItem> Items { get => (IReadOnlyList<KlyTabItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>Value of the selected tab. When not set, the first enabled tab is selected. Two-way bindable.</summary>
    public string? Value { get => (string?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Look of the tabs: <c>Pill</c> or <c>Link</c>.</summary>
    public TabsVariant Variant { get => (TabsVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Direction of the tab buttons: <c>Horizontal</c> puts them above the panel, <c>Vertical</c> beside it.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Semantic color of the selected tab of the <c>Link</c> variant.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>What a screen reader says for the list of tabs.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal IReadOnlyList<NavigationButton> TabButtons => _triggers;

    internal ContentView Panel => _panel;

    internal ContentView ListHost => _listHost;

    /// <summary>The value of the selected tab: <see cref="Value"/>, else the first enabled tab.</summary>
    internal string? Current => Value ?? (NavigationLayout.FirstEnabled(Items, i => i.Disabled) is var first and >= 0 ? ValueOf(Items[first], first) : null);

    private static string ValueOf(KlyTabItem item, int index) => item.Value ?? index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyTabs)bindable).Rebuild();

    private void OnValueChanged(string? oldValue, string? newValue)
    {
        Refresh();
        ValueChanged?.Invoke(this, new KlyNavigationValueEventArgs<string?>(newValue, oldValue));
    }

    private void Rebuild()
    {
        var vertical = Orientation == StackOrientation.Vertical;
        var link = Variant == TabsVariant.Link;
        var items = Items;

        // The tab buttons share the width of a horizontal list (flex-1) and stack in a vertical one.
        var list = new Grid { ColumnSpacing = 4, RowSpacing = 4 };
        _triggers.Clear();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var index = i;
            var trigger = Trigger(item, link);
            trigger.Clicked += (_, _) => Select(index);
            _triggers.Add(trigger);
            if (vertical)
            {
                list.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                list.Add(trigger, 0, i);
            }
            else
            {
                list.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                list.Add(trigger, i, 0);
            }
        }

        if (link)
        {
            // A line under (or beside) the tab buttons, which the underline of the selected one overlaps.
            var host = new Grid();
            host.Children.Add(list);
            var line = vertical
                ? NavigationLayout.Line(KlyTheme.Border, width: 1)
                : NavigationLayout.Line(KlyTheme.Border, height: 1);
            line.HorizontalOptions = vertical ? LayoutOptions.End : LayoutOptions.Fill;
            line.VerticalOptions = vertical ? LayoutOptions.Fill : LayoutOptions.End;
            host.Children.Add(line);
            _listHost.Content = host;
        }
        else
        {
            var pill = new Border { Padding = new Thickness(4), Content = list };
            pill.SetRadius(KlyTheme.RadiusOf(1) + 2);
            pill.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundElevated);
            pill.StrokeThickness = 0;
            _listHost.Content = pill;
        }
        _listHost.HorizontalOptions = vertical ? LayoutOptions.Start : LayoutOptions.Fill;
        _listHost.VerticalOptions = vertical ? LayoutOptions.Start : LayoutOptions.Fill;
        SemanticProperties.SetDescription(_listHost, AccessibleName);

        Children.Clear();
        RowDefinitions.Clear();
        ColumnDefinitions.Clear();
        RowSpacing = 0;
        ColumnSpacing = 0;
        if (vertical)
        {
            ColumnSpacing = 8;
            ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            this.Add(_listHost, 0, 0);
            this.Add(_panel, 1, 0);
        }
        else
        {
            RowSpacing = 8;
            RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            RowDefinitions.Add(new RowDefinition(GridLength.Star));
            this.Add(_listHost, 0, 0);
            this.Add(_panel, 0, 1);
        }
        Refresh();
    }

    private NavigationButton Trigger(KlyTabItem item, bool link)
    {
        var button = new NavigationButton { IsEnabled = !item.Disabled };
        button.Face.HorizontalOptions = LayoutOptions.Fill;
        SemanticProperties.SetDescription(button, item.Label);

        var row = new HorizontalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        if (item.Icon is { } icon) row.Children.Add(NavigationLayout.Icon(icon, KlySize.Sm, KlyTheme.TextMuted));
        var label = NavigationLayout.TextLabel(14, KlyTheme.TextMuted);
        label.Text = item.Label;
        row.Children.Add(label);

        if (link)
        {
            // The underline is a 2 unit bar under the content.
            var content = new Grid { RowDefinitions = { new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) } };
            content.Add(row, 0, 0);
            content.Add(NavigationLayout.Line(KlyThemeColor.Transparent, height: 2), 0, 1);
            button.Face.Content = content;
            button.Face.Padding = new Thickness(12, 8, 12, 0);
        }
        else
        {
            button.Face.Content = row;
            button.Face.Padding = new Thickness(12, 6);
            button.Face.SetRadius(KlyTheme.RadiusOf(1));
        }
        return button;
    }

    private void Select(int index)
    {
        var item = Items[index];
        var value = ValueOf(item, index);
        if (item.Disabled || value == Current) return;
        Value = value;
    }

    /// <summary>Shows the selected tab and its panel.</summary>
    private void Refresh()
    {
        var items = Items;
        var current = Current;
        var set = KlyTheme.Resolve(Color);
        var link = Variant == TabsVariant.Link;
        View? content = null;
        for (var i = 0; i < _triggers.Count && i < items.Count; i++)
        {
            var selected = ValueOf(items[i], i) == current;
            var visual = _triggers[i].Face;
            var text = selected ? (link ? set.Text : KlyTheme.Text) : KlyTheme.TextMuted;
            if (link)
            {
                var underline = (BoxView)((Grid)visual.Content!).Children[1];
                underline.SetThemeColor(BoxView.ColorProperty, selected ? set.Text : KlyThemeColor.Transparent);
            }
            else
            {
                // aria-selected: the page background, the text color and an inset ring in the muted color.
                visual.Apply(selected
                    ? new Surface(KlyTheme.Background, KlyTheme.TextMuted, 1, text)
                    : new Surface(KlyThemeColor.Transparent, KlyThemeColor.Transparent, 0, text));
            }
            var row = (HorizontalStackLayout)(link ? ((Grid)visual.Content!).Children[0] : visual.Content!);
            foreach (var child in row.Children)
            {
                if (child is Label label) label.SetThemeColor(Label.TextColorProperty, text);
                else if (child is KlyIcon icon) icon.IconColor = text;
            }
            if (selected) content = items[i].Content;
        }
        _panel.Content = content;
    }
}
