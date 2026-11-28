namespace Klyroo.UI.Maui;

/// <summary>A group of the native <see cref="RadioButton"/>s bound to one value, with an optional legend and a description under each label.</summary>
public class KlyRadioGroup : Border, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyRadioItem>), typeof(KlyRadioGroup), null, propertyChanged: (b, _, _) => ((KlyRadioGroup)b).Rebuild());
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(object), typeof(KlyRadioGroup), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyRadioGroup)b).ShowValue(o, v));
    /// <summary>Identifies <see cref="Legend"/>.</summary>
    public static readonly BindableProperty LegendProperty = BindableProperty.Create(nameof(Legend), typeof(string), typeof(KlyRadioGroup), null, propertyChanged: (b, _, _) => ((KlyRadioGroup)b).ApplyStyle());
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyRadioGroup), StackOrientation.Vertical, propertyChanged: (b, _, _) => ((KlyRadioGroup)b).ApplyStyle());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyRadioGroup), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyRadioGroup)b).Rebuild());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyRadioGroup), KlySize.Md, propertyChanged: (b, _, _) => ((KlyRadioGroup)b).Rebuild());
    /// <summary>Identifies <see cref="Invalid"/>.</summary>
    public static readonly BindableProperty InvalidProperty = BindableProperty.Create(nameof(Invalid), typeof(bool), typeof(KlyRadioGroup), false, propertyChanged: (b, _, _) => ((KlyRadioGroup)b).ApplyStyle());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyRadioGroup), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyRadioGroup)b, (string?)v));

    private readonly InputsChoiceLayout _layout;
    private readonly InputsFieldTaken _taken = new();
    private readonly string _groupName = "kly-radio-" + Guid.NewGuid().ToString("N");
    private readonly List<(KlyRadioItem Item, RadioButton Radio)> _radios = [];
    private bool _syncing;

    /// <summary>Creates a radio group.</summary>
    public KlyRadioGroup()
    {
        _layout = new InputsChoiceLayout(this);
        ApplyStyle();
    }

    /// <summary>Raised when the chosen value changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<object>>? ValueChanged;

    /// <summary>The radio buttons.</summary>
    public IReadOnlyList<KlyRadioItem>? Items { get => (IReadOnlyList<KlyRadioItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The value of the chosen radio button, or <c>null</c> while none is chosen. Two-way bindable.</summary>
    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Caption of the group, drawn above the radio buttons.</summary>
    public string? Legend { get => (string?)GetValue(LegendProperty); set => SetValue(LegendProperty, value); }

    /// <summary>Whether the radio buttons are stacked (<c>Vertical</c>, the default) or side by side and wrapped (<c>Horizontal</c>).</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Semantic color of the chosen radio button.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the labels, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Draws the error frame, in the error color, around the group.</summary>
    public bool Invalid { get => (bool)GetValue(InvalidProperty); set => SetValue(InvalidProperty, value); }

    /// <summary>What a screen reader says for the group, such as its label.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal IReadOnlyList<(KlyRadioItem Item, RadioButton Radio)> Radios => _radios;

    internal InputsChoiceLayout ChoiceLayout => _layout;

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled))
        {
            foreach (var (item, radio) in _radios) radio.IsEnabled = IsEnabled && !item.Disabled;
        }
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeInvalid(this, InvalidProperty, state.Invalid);
        _taken.TakeName(this, AccessibleNameProperty, state.Name);
        _taken.TakeSize(this, SizeProperty, state.Size);
        SemanticProperties.SetHint(this, state.Hint);
    }

    internal static double FontSizeOf(KlySize size) => size switch
    {
        KlySize.Xs or KlySize.Sm => 12,
        KlySize.Md or KlySize.Lg => 14,
        KlySize.Xl => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    private void ShowValue(object? old, object? value)
    {
        _syncing = true;
        foreach (var (item, radio) in _radios)
        {
            var chosen = Equals(item.Value, value);
            if (radio.IsChecked != chosen) radio.IsChecked = chosen;
        }
        _syncing = false;
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<object>(old, value));
    }

    private void ApplyStyle()
    {
        _layout.ShowLegend(Legend, FontSizeOf(Size));
        _layout.ShowOrientation(Orientation);
        _layout.ShowInvalid(Invalid);
    }

    private void Rebuild()
    {
        _layout.Items.Children.Clear();
        _radios.Clear();
        var set = KlyTheme.Resolve(Color);
        var fontSize = FontSizeOf(Size);
        foreach (var item in Items ?? [])
        {
            var label = new Label { Text = item.Label, FontSize = fontSize, FontAttributes = FontAttributes.Bold };
            label.SetThemeColor(Label.TextColorProperty, KlyTheme.Text);
            var texts = new VerticalStackLayout { Spacing = 2, Children = { label } };
            if (!string.IsNullOrEmpty(item.Description))
            {
                var description = new Label { Text = item.Description, FontSize = fontSize };
                description.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
                texts.Children.Add(description);
            }

            var radio = new RadioButton
            {
                GroupName = _groupName,
                Content = texts,
                IsChecked = Equals(item.Value, Value),
                IsEnabled = IsEnabled && !item.Disabled,
                Margin = InputsChoiceLayout.ChoiceMargin(Orientation == StackOrientation.Vertical),
            };
            // The text color, not the fill: a light fill such as the lime primary has no contrast against the page.
            radio.SetThemeColor(RadioButton.TextColorProperty, set.Text);
            radio.SetThemeColor(RadioButton.BorderColorProperty, KlyTheme.BorderAccented);
            SemanticProperties.SetDescription(radio, string.IsNullOrEmpty(item.Description) ? item.Label : item.Label + ", " + item.Description);
            var chosen = item;
            radio.CheckedChanged += (_, e) =>
            {
                if (e.Value && !_syncing) Value = chosen.Value;
            };
            _radios.Add((item, radio));
            _layout.Items.Children.Add(radio);
        }
        ApplyStyle();
    }
}
