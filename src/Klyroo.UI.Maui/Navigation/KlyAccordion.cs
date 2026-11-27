namespace Klyroo.UI.Maui;

/// <summary>A stack of headers that each open and close a panel. One item is open at a time, or several with <see cref="Multiple"/>.</summary>
public class KlyAccordion : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyAccordionItem>), typeof(KlyAccordion), Array.Empty<KlyAccordionItem>(), propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(IReadOnlyList<string>), typeof(KlyAccordion), null, BindingMode.TwoWay, propertyChanged: (b, o, n) => ((KlyAccordion)b).OnValueChanged((IReadOnlyList<string>?)o, (IReadOnlyList<string>?)n));
    /// <summary>Identifies <see cref="Multiple"/>.</summary>
    public static readonly BindableProperty MultipleProperty = BindableProperty.Create(nameof(Multiple), typeof(bool), typeof(KlyAccordion), false, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Collapsible"/>.</summary>
    public static readonly BindableProperty CollapsibleProperty = BindableProperty.Create(nameof(Collapsible), typeof(bool), typeof(KlyAccordion), true);
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int), typeof(KlyAccordion), 3, propertyChanged: Rebuild);

    private sealed record Part(NavigationButton Header, KlyIcon Chevron, View Body);

    private readonly List<Part> _parts = [];
    private IReadOnlyList<string> _open = [];

    /// <summary>Creates an accordion.</summary>
    public KlyAccordion()
    {
        Spacing = 0;
        Rebuild();
    }

    /// <summary>Raised with the values of the open items when they change, by the user or by code.</summary>
    public event EventHandler<KlyNavigationValueEventArgs<IReadOnlyList<string>?>>? ValueChanged;

    /// <summary>The items of the accordion, as <see cref="KlyAccordionItem"/>.</summary>
    public IReadOnlyList<KlyAccordionItem> Items { get => (IReadOnlyList<KlyAccordionItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>Values of the open items. When not set, the items with <see cref="KlyAccordionItem.DefaultOpen"/> start open. Two-way bindable.</summary>
    public IReadOnlyList<string>? Value { get => (IReadOnlyList<string>?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Several items can be open at once.</summary>
    public bool Multiple { get => (bool)GetValue(MultipleProperty); set => SetValue(MultipleProperty, value); }

    /// <summary>With a single open item, it can be closed too.</summary>
    public bool Collapsible { get => (bool)GetValue(CollapsibleProperty); set => SetValue(CollapsibleProperty, value); }

    /// <summary>Heading level (1 to 6) of the item headers, for the screen reader outline.</summary>
    public int HeadingLevel { get => (int)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    internal IReadOnlyList<NavigationButton> Headers => _parts.Select(p => p.Header).ToList();

    internal IReadOnlyList<View> Bodies => _parts.Select(p => p.Body).ToList();

    internal IReadOnlyList<KlyIcon> Chevrons => _parts.Select(p => p.Chevron).ToList();

    private static string ValueOf(KlyAccordionItem item, int index) => item.Value ?? index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyAccordion)bindable).Rebuild();

    // The items that start open when the open set is not controlled: the first one only without Multiple.
    private IReadOnlyList<string> DefaultValues()
    {
        var open = new List<string>();
        for (var i = 0; i < Items.Count; i++)
        {
            if (!Items[i].DefaultOpen) continue;
            open.Add(ValueOf(Items[i], i));
            if (!Multiple) break;
        }
        return open;
    }

    private void OnValueChanged(IReadOnlyList<string>? oldValue, IReadOnlyList<string>? newValue)
    {
        Refresh();
        ValueChanged?.Invoke(this, new KlyNavigationValueEventArgs<IReadOnlyList<string>?>(newValue, oldValue));
    }

    private void Rebuild()
    {
        Children.Clear();
        _parts.Clear();
        var items = Items;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var index = i;

            var chevron = NavigationLayout.Icon(KlyIconNames.ChevronDown, KlySize.Md, KlyTheme.TextMuted);
            var label = NavigationLayout.TextLabel(14, KlyTheme.Text, lineBreak: LineBreakMode.WordWrap);
            label.Text = item.Label;
            var row = new Grid { ColumnSpacing = 6, ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            if (item.Icon is { } icon) row.Add(NavigationLayout.Icon(icon, KlySize.Md, KlyTheme.Text), 0);
            row.Add(label, 1);
            row.Add(chevron, 2);

            var header = new NavigationButton { IsEnabled = !item.Disabled };
            header.Face.HorizontalOptions = LayoutOptions.Fill;
            header.Face.Padding = new Thickness(0, 14);
            header.Face.Content = row;
            header.Clicked += (_, _) => Toggle(index);
            SemanticProperties.SetDescription(header, item.Label);
            SemanticProperties.SetHeadingLevel(header, NavigationLayout.HeadingLevelOf(HeadingLevel));

            var body = new ContentView { Content = item.Content ?? BodyText(item.Text), Padding = new Thickness(0, 0, 0, 14), IsVisible = false };

            var section = new VerticalStackLayout { Spacing = 0, Children = { header, body } };
            Children.Add(section);
            if (i < items.Count - 1) Children.Add(NavigationLayout.Line(KlyTheme.Border, height: 1));
            _parts.Add(new Part(header, chevron, body));
        }
        Refresh();
    }

    private static Label BodyText(string? text)
    {
        var label = NavigationLayout.TextLabel(14, KlyTheme.TextMuted, lineBreak: LineBreakMode.WordWrap);
        label.Text = text;
        return label;
    }

    private void Refresh()
    {
        _open = Value ?? DefaultValues();
        var items = Items;
        for (var i = 0; i < _parts.Count && i < items.Count; i++)
        {
            var open = _open.Contains(ValueOf(items[i], i));
            _parts[i].Body.IsVisible = open;
            NavigationLayout.Rotate(_parts[i].Chevron, open ? 180 : 0);
        }
    }

    private void Toggle(int index)
    {
        var items = Items;
        var item = items[index];
        if (item.Disabled) return;
        var value = ValueOf(item, index);
        var open = _open.Contains(value);

        // A single open item that cannot collapse stays open; opening an item closes the others unless Multiple.
        if (open && !Multiple && !Collapsible) return;
        HashSet<string> next = open
            ? [.. _open.Where(v => v != value)]
            : Multiple ? [.. _open, value] : [value];
        Value = Enumerable.Range(0, items.Count).Select(i => ValueOf(items[i], i)).Where(next.Contains).ToArray();
    }
}
