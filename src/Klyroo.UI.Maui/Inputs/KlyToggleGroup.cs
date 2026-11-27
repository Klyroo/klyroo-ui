namespace Klyroo.UI.Maui;

/// <summary>
/// A row or column of toggle buttons bound to a list of values. With <see cref="Multiple"/> any number of buttons can be pressed; otherwise pressing a button releases the others,
/// and pressing the pressed one releases it. Use <see cref="StackLayout.Orientation"/> for the direction: horizontal by default.
/// </summary>
public class KlyToggleGroup : StackLayout, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyToggleItem>), typeof(KlyToggleGroup), null, propertyChanged: (b, _, _) => ((KlyToggleGroup)b).Rebuild());
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(IReadOnlyList<string>), typeof(KlyToggleGroup), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyToggleGroup)b).ShowValue((IReadOnlyList<string>?)o, (IReadOnlyList<string>?)v));
    /// <summary>Identifies <see cref="Multiple"/>.</summary>
    public static readonly BindableProperty MultipleProperty = BindableProperty.Create(nameof(Multiple), typeof(bool), typeof(KlyToggleGroup), false);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(ToggleVariant), typeof(KlyToggleGroup), ToggleVariant.Outline, propertyChanged: (b, _, _) => ((KlyToggleGroup)b).Rebuild());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyToggleGroup), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyToggleGroup)b).Rebuild());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyToggleGroup), KlySize.Md, propertyChanged: (b, _, _) => ((KlyToggleGroup)b).Rebuild());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyToggleGroup), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyToggleGroup)b, (string?)v));

    private readonly List<(KlyToggleItem Item, KlyToggle Toggle)> _toggles = [];
    private readonly InputsFieldTaken _taken = new();

    /// <summary>Creates a toggle group.</summary>
    public KlyToggleGroup()
    {
        Orientation = StackOrientation.Horizontal;
        Spacing = 4;
        HorizontalOptions = LayoutOptions.Start;
    }

    /// <summary>Raised when the selection changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<IReadOnlyList<string>>>? ValueChanged;

    /// <summary>The buttons.</summary>
    public IReadOnlyList<KlyToggleItem>? Items { get => (IReadOnlyList<KlyToggleItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The values of the pressed buttons. Two-way bindable.</summary>
    public IReadOnlyList<string>? Value { get => (IReadOnlyList<string>?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Several buttons can be pressed at once; otherwise pressing a button releases the others.</summary>
    public bool Multiple { get => (bool)GetValue(MultipleProperty); set => SetValue(MultipleProperty, value); }

    /// <summary>Look of the buttons: <c>Outline</c>, <c>Soft</c> or <c>Ghost</c>.</summary>
    public ToggleVariant Variant { get => (ToggleVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Semantic color of the pressed state.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the buttons, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>What a screen reader says for the group, such as its label.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal IReadOnlyList<(KlyToggleItem Item, KlyToggle Toggle)> Toggles => _toggles;

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled))
        {
            foreach (var (item, toggle) in _toggles) toggle.IsEnabled = IsEnabled && !item.Disabled;
        }
        else if (propertyName == nameof(Orientation))
        {
            Rebuild();
        }
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeName(this, AccessibleNameProperty, state.Name);
        _taken.TakeSize(this, SizeProperty, state.Size);
        SemanticProperties.SetHint(this, state.Hint);
    }

    private bool IsPressed(KlyToggleItem item) => Value?.Contains(item.Value) == true;

    private void Press(KlyToggleItem item)
    {
        var current = Value ?? [];
        if (Multiple) Value = IsPressed(item) ? [.. current.Where(v => v != item.Value)] : [.. current, item.Value];
        else Value = IsPressed(item) ? [] : [item.Value];
    }

    private void ShowValue(IReadOnlyList<string>? old, IReadOnlyList<string>? value)
    {
        foreach (var (item, toggle) in _toggles) toggle.Value = IsPressed(item);
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<IReadOnlyList<string>>(old, value));
    }

    private void Rebuild()
    {
        Children.Clear();
        _toggles.Clear();
        foreach (var item in Items ?? [])
        {
            var toggle = new KlyToggle
            {
                Label = item.Label,
                Icon = item.Icon,
                Variant = Variant,
                Color = Color,
                Size = Size,
                Value = IsPressed(item),
                IsEnabled = IsEnabled && !item.Disabled,
                AutoToggle = false,
                // A vertical group stretches its buttons to its width.
                HorizontalOptions = Orientation == StackOrientation.Vertical ? LayoutOptions.Fill : LayoutOptions.Start,
            };
            var pressed = item;
            toggle.Tapped += (_, _) => Press(pressed);
            _toggles.Add((item, toggle));
            Children.Add(toggle);
        }
    }
}
