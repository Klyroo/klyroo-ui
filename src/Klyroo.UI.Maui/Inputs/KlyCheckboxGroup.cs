namespace Klyroo.UI.Maui;

/// <summary>A group of <see cref="KlyCheckbox"/>es bound to a list of values, with an optional legend. The values keep the order of <see cref="Items"/>, whatever order the boxes were checked in.</summary>
public class KlyCheckboxGroup : Border, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyCheckboxItem>), typeof(KlyCheckboxGroup), null, propertyChanged: (b, _, _) => ((KlyCheckboxGroup)b).Rebuild());
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(IReadOnlyList<object>), typeof(KlyCheckboxGroup), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyCheckboxGroup)b).ShowValue((IReadOnlyList<object>?)o, (IReadOnlyList<object>?)v));
    /// <summary>Identifies <see cref="Legend"/>.</summary>
    public static readonly BindableProperty LegendProperty = BindableProperty.Create(nameof(Legend), typeof(string), typeof(KlyCheckboxGroup), null, propertyChanged: (b, _, _) => ((KlyCheckboxGroup)b).ApplyStyle());
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyCheckboxGroup), StackOrientation.Vertical, propertyChanged: (b, _, _) => ((KlyCheckboxGroup)b).ApplyStyle());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyCheckboxGroup), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyCheckboxGroup)b).Rebuild());
    /// <summary>Identifies <see cref="Invalid"/>.</summary>
    public static readonly BindableProperty InvalidProperty = BindableProperty.Create(nameof(Invalid), typeof(bool), typeof(KlyCheckboxGroup), false, propertyChanged: (b, _, _) => ((KlyCheckboxGroup)b).ApplyStyle());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyCheckboxGroup), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyCheckboxGroup)b, (string?)v));

    private readonly InputsChoiceLayout _layout;
    private readonly InputsFieldTaken _taken = new();
    private readonly List<(KlyCheckboxItem Item, KlyCheckbox Box)> _boxes = [];
    private bool _syncing;

    /// <summary>Creates a checkbox group.</summary>
    public KlyCheckboxGroup()
    {
        _layout = new InputsChoiceLayout(this);
        ApplyStyle();
    }

    /// <summary>Raised when the selection changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<IReadOnlyList<object>>>? ValueChanged;

    /// <summary>The checkboxes.</summary>
    public IReadOnlyList<KlyCheckboxItem>? Items { get => (IReadOnlyList<KlyCheckboxItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The values of the checked boxes, in the order of <see cref="Items"/>. Two-way bindable.</summary>
    public IReadOnlyList<object>? Value { get => (IReadOnlyList<object>?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Caption of the group, drawn above the checkboxes.</summary>
    public string? Legend { get => (string?)GetValue(LegendProperty); set => SetValue(LegendProperty, value); }

    /// <summary>Whether the checkboxes are stacked (<c>Vertical</c>, the default) or side by side and wrapped (<c>Horizontal</c>).</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Semantic color of the checked boxes.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Draws the error frame, in the error color, around the group.</summary>
    public bool Invalid { get => (bool)GetValue(InvalidProperty); set => SetValue(InvalidProperty, value); }

    /// <summary>What a screen reader says for the group, such as its label.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal IReadOnlyList<(KlyCheckboxItem Item, KlyCheckbox Box)> Boxes => _boxes;

    internal InputsChoiceLayout ChoiceLayout => _layout;

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled))
        {
            foreach (var (item, box) in _boxes) box.IsEnabled = IsEnabled && !item.Disabled;
        }
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeInvalid(this, InvalidProperty, state.Invalid);
        _taken.TakeName(this, AccessibleNameProperty, state.Name);
        SemanticProperties.SetHint(this, state.Hint);
    }

    private bool IsChecked(KlyCheckboxItem item) => Value?.Any(v => Equals(v, item.Value)) == true;

    private void Toggle(KlyCheckboxItem changed, bool isChecked)
    {
        var selected = _boxes
            .Where(b => ReferenceEquals(b.Item, changed) ? isChecked : IsChecked(b.Item))
            .Select(b => b.Item.Value!)
            .ToList();
        Value = selected;
    }

    private void ShowValue(IReadOnlyList<object>? old, IReadOnlyList<object>? value)
    {
        _syncing = true;
        foreach (var (item, box) in _boxes)
        {
            var isChecked = IsChecked(item);
            if (box.IsChecked != isChecked) box.IsChecked = isChecked;
        }
        _syncing = false;
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<IReadOnlyList<object>>(old, value));
    }

    private void ApplyStyle()
    {
        // KlyCheckbox is drawn at its own size, so the legend takes the default size.
        _layout.ShowLegend(Legend, 14);
        _layout.ShowOrientation(Orientation);
        _layout.ShowInvalid(Invalid);
    }

    private void Rebuild()
    {
        _layout.Items.Children.Clear();
        _boxes.Clear();
        foreach (var item in Items ?? [])
        {
            var box = new KlyCheckbox
            {
                Text = item.Label,
                Description = item.Description,
                Color = Color,
                IsChecked = IsChecked(item),
                IsEnabled = IsEnabled && !item.Disabled,
                Margin = InputsChoiceLayout.ChoiceMargin(Orientation == StackOrientation.Vertical),
            };
            var changed = item;
            box.CheckedChanged += (_, e) =>
            {
                if (!_syncing) Toggle(changed, e.Value);
            };
            _boxes.Add((item, box));
            _layout.Items.Children.Add(box);
        }
    }
}
