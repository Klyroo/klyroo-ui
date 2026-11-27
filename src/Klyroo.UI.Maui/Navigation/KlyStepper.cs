namespace Klyroo.UI.Maui;

/// <summary>A list of steps; the current one is highlighted and the steps before it are completed. With <see cref="Interactive"/> the user can tap a step.</summary>
public class KlyStepper : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyStepperItem>), typeof(KlyStepper), Array.Empty<KlyStepperItem>(), propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Current"/>.</summary>
    public static readonly BindableProperty CurrentProperty = BindableProperty.Create(nameof(Current), typeof(int), typeof(KlyStepper), 0, BindingMode.TwoWay, propertyChanged: (b, o, n) => ((KlyStepper)b).OnCurrentChanged((int)n));
    /// <summary>Identifies <see cref="Interactive"/>.</summary>
    public static readonly BindableProperty InteractiveProperty = BindableProperty.Create(nameof(Interactive), typeof(bool), typeof(KlyStepper), false, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyStepper), StackOrientation.Horizontal, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyStepper), KlyColor.Primary, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyStepper), KlySize.Md, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyStepper), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription(((KlyStepper)b)._trackHost, (string?)v));
    /// <summary>Identifies <see cref="CompletedLabel"/>.</summary>
    public static readonly BindableProperty CompletedLabelProperty = BindableProperty.Create(nameof(CompletedLabel), typeof(string), typeof(KlyStepper), "completed", propertyChanged: Rebuild);

    private readonly ContentView _trackHost = new();
    private readonly ContentView _content = new();
    private readonly List<View> _indicators = [];

    /// <summary>Creates a stepper.</summary>
    public KlyStepper()
    {
        Spacing = 16;
        Children.Add(_trackHost);
        Children.Add(_content);
        Rebuild();
    }

    /// <summary>Raised with the index of the current step when it changes, by the user or by code.</summary>
    public event EventHandler<int>? CurrentChanged;

    /// <summary>The steps, as <see cref="KlyStepperItem"/>.</summary>
    public IReadOnlyList<KlyStepperItem> Items { get => (IReadOnlyList<KlyStepperItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>Index of the current step, starting at 0. Earlier steps are completed. Two-way bindable.</summary>
    public int Current { get => (int)GetValue(CurrentProperty); set => SetValue(CurrentProperty, value); }

    /// <summary>Lets the user tap a step to make it the current one. Without it the stepper only shows progress.</summary>
    public bool Interactive { get => (bool)GetValue(InteractiveProperty); set => SetValue(InteractiveProperty, value); }

    /// <summary>Direction of the layout: <c>Horizontal</c> puts the text under the markers, <c>Vertical</c> beside them.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Semantic color of the completed and the current steps.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the markers, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>What a screen reader says for the list of steps.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>The word a screen reader adds after the title of a finished step.</summary>
    public string CompletedLabel { get => (string)GetValue(CompletedLabelProperty); set => SetValue(CompletedLabelProperty, value); }

    /// <summary>The current step kept inside the list, or -1 for an empty stepper.</summary>
    internal int EffectiveCurrent => Items.Count == 0 ? -1 : Math.Clamp(Current, 0, Items.Count - 1);

    internal IReadOnlyList<View> Indicators => _indicators;

    internal ContentView ContentHost => _content;

    /// <summary>The state of the step at an index: <c>completed</c>, <c>active</c> or <c>inactive</c>.</summary>
    internal static string StateOf(int index, int current) => index < current ? "completed" : index == current ? "active" : "inactive";

    /// <summary>The diameter of the marker of a size: <c>size-6</c> to <c>size-10</c> on the web.</summary>
    internal static (double Diameter, double Font) MetricsOf(KlySize size) => size switch
    {
        KlySize.Xs => (24d, 12d),
        KlySize.Sm => (28d, 12d),
        KlySize.Md => (32d, 14d),
        KlySize.Lg => (36d, 14d),
        KlySize.Xl => (40d, 16d),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyStepper)bindable).Rebuild();

    private void OnCurrentChanged(int current)
    {
        Rebuild();
        CurrentChanged?.Invoke(this, current);
    }

    private void Rebuild()
    {
        var items = Items;
        var current = EffectiveCurrent;
        var set = KlyTheme.Resolve(Color);
        var (diameter, font) = MetricsOf(Size);
        _indicators.Clear();

        var entries = new List<NavigationTrackEntry>();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var state = StateOf(i, current);
            var indicator = Indicator(item, i, state, set, diameter, font);
            _indicators.Add(indicator);
            entries.Add(new NavigationTrackEntry(indicator, state == "completed" ? set.Text : KlyTheme.BorderAccented, Text(item, state)));
        }
        _trackHost.Content = entries.Count == 0 ? null : NavigationTrack.Build(entries, Orientation == StackOrientation.Vertical);
        SemanticProperties.SetDescription(_trackHost, AccessibleName);
        _content.Content = current >= 0 ? items[current].Content : null;
    }

    private View Indicator(KlyStepperItem item, int index, string state, KlyColorSet set, double diameter, double font)
    {
        var filled = state != "inactive";
        var text = filled ? set.Contrast : KlyTheme.TextMuted;

        var disc = new Border { WidthRequest = diameter, HeightRequest = diameter, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        disc.SetRadius(diameter / 2);
        disc.Apply(filled
            ? new Surface(set.Fill, set.Text, 2, text)
            : new Surface(KlyTheme.Background, KlyTheme.BorderAccented, 2, text));
        disc.Content = item.Icon is { } icon
            ? NavigationLayout.Icon(icon, diameter >= 36 ? KlySize.Md : KlySize.Sm, text)
            : state == "completed"
                ? NavigationLayout.Icon(KlyIconNames.Check, diameter >= 36 ? KlySize.Md : KlySize.Sm, text)
                : Number(index + 1, font, text);

        // The ring of the current step (ring-4, a quarter of the color) is the ring around the disc; every step has the room for it so the markers line up.
        var ring = new Border
        {
            WidthRequest = diameter + 8,
            HeightRequest = diameter + 8,
            StrokeThickness = 0,
            Content = disc,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };
        ring.SetRadius((diameter + 8) / 2);
        ring.SetThemeColor(BackgroundColorProperty, state == "active" ? set.Fill.WithAlpha(0.25f) : KlyThemeColor.Transparent);

        var description = $"{index + 1}. {item.Title}";
        if (Interactive && !item.Disabled)
        {
            var button = new NavigationButton();
            button.Face.StrokeThickness = 0;
            button.Face.Content = ring;
            button.Clicked += (_, _) => Current = index;
            SemanticProperties.SetDescription(button, description);
            return button;
        }
        SemanticProperties.SetDescription(ring, description);
        return ring;
    }

    private static Label Number(int number, double font, KlyThemeColor color)
    {
        var label = NavigationLayout.TextLabel(font, color, lineBreak: LineBreakMode.NoWrap);
        label.Text = number.ToString(System.Globalization.CultureInfo.CurrentCulture);
        label.HorizontalTextAlignment = TextAlignment.Center;
        AutomationProperties.SetIsInAccessibleTree(label, false);
        return label;
    }

    private View Text(KlyStepperItem item, string state)
    {
        var title = NavigationLayout.TextLabel(14, KlyTheme.Text, lineBreak: LineBreakMode.WordWrap);
        title.Text = item.Title;
        // The finished state is not drawn in words: a screen reader gets it after the title (the web sr-only status).
        if (state == "completed") SemanticProperties.SetDescription(title, $"{item.Title} ({CompletedLabel})");
        var stack = new VerticalStackLayout { Spacing = 0, Children = { title } };
        if (item.Description is not null)
        {
            var description = NavigationLayout.TextLabel(14, KlyTheme.TextMuted, lineBreak: LineBreakMode.WordWrap);
            description.Text = item.Description;
            stack.Children.Add(description);
        }
        return stack;
    }
}
