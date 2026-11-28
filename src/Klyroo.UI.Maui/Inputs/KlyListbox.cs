using System.Collections;

namespace Klyroo.UI.Maui;

/// <summary>
/// An always visible list of options with single or multiple selection. The selection is a list of values, bound with <see cref="Value"/>.
/// Choosing an option of a single selection list replaces the value; with <see cref="Multiple"/> it toggles the option.
/// </summary>
public class KlyListbox : Border, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IEnumerable), typeof(KlyListbox), null, propertyChanged: (b, _, _) => ((KlyListbox)b).Refresh());
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(IReadOnlyList<object>), typeof(KlyListbox), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyListbox)b).ShowValue((IReadOnlyList<object>?)o, (IReadOnlyList<object>?)v));
    /// <summary>Identifies <see cref="Multiple"/>.</summary>
    public static readonly BindableProperty MultipleProperty = BindableProperty.Create(nameof(Multiple), typeof(bool), typeof(KlyListbox), false);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyListbox), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyListbox)b).Refresh());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyListbox), KlySize.Md, propertyChanged: (b, _, _) => ((KlyListbox)b).Refresh());
    /// <summary>Identifies <see cref="Invalid"/>.</summary>
    public static readonly BindableProperty InvalidProperty = BindableProperty.Create(nameof(Invalid), typeof(bool), typeof(KlyListbox), false, propertyChanged: (b, _, _) => ((KlyListbox)b).ApplyStyle());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyListbox), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyListbox)b, (string?)v));

    private readonly InputsOptionList _list = new();
    private readonly InputsFieldTaken _taken = new();
    private List<InputsOption> _options = [];

    /// <summary>Creates a list.</summary>
    public KlyListbox()
    {
        Padding = new Thickness(4);
        HorizontalOptions = LayoutOptions.Fill;
        this.SetRadius(KlyTheme.RadiusOf(1));
        Content = new ScrollView { MaximumHeightRequest = 240, Content = _list };
        _list.Chosen += (_, option) => Choose(option);
        ApplyStyle();
    }

    /// <summary>Raised when the selection changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<IReadOnlyList<object>>>? ValueChanged;

    /// <summary>The options: <see cref="KlySelectItem"/>s, or any objects, each one shown as its <c>ToString()</c> and standing for itself.</summary>
    public IEnumerable? Items { get => (IEnumerable?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The values of the selected options. With a single selection the list holds at most one. Two-way bindable.</summary>
    public IReadOnlyList<object>? Value { get => (IReadOnlyList<object>?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Several options can be selected. Otherwise the list holds at most one value.</summary>
    public bool Multiple { get => (bool)GetValue(MultipleProperty); set => SetValue(MultipleProperty, value); }

    /// <summary>Semantic color of the check mark of a selected option.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the options, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Draws the error border, in the error color, around the list.</summary>
    public bool Invalid { get => (bool)GetValue(InvalidProperty); set => SetValue(InvalidProperty, value); }

    /// <summary>What a screen reader says for the list, such as its label.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal InputsOptionList List => _list;

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web listbox: aria-disabled:opacity-60.
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.6;
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeInvalid(this, InvalidProperty, state.Invalid);
        _taken.TakeName(this, AccessibleNameProperty, state.Name);
        _taken.TakeSize(this, SizeProperty, state.Size);
        SemanticProperties.SetHint(this, state.Hint);
    }

    private static double FontSizeOf(KlySize size) => size switch
    {
        KlySize.Xs or KlySize.Sm => 12,
        KlySize.Md or KlySize.Lg => 14,
        KlySize.Xl => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    private bool IsSelected(InputsOption option) => Value?.Any(option.HasValue) == true;

    private void Choose(InputsOption option)
    {
        if (!IsEnabled) return;
        var current = Value ?? [];
        IReadOnlyList<object> next;
        if (Multiple)
        {
            next = IsSelected(option) ? [.. current.Where(v => !option.HasValue(v))] : [.. current, option.Value!];
        }
        else
        {
            if (IsSelected(option) && current.Count == 1) return;
            next = [option.Value!];
        }
        Value = next;
    }

    private void ShowValue(IReadOnlyList<object>? old, IReadOnlyList<object>? value)
    {
        Refresh();
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<IReadOnlyList<object>>(old, value));
    }

    private void Refresh()
    {
        _options = InputsOptions.From(Items);
        _list.Show(_options, IsSelected, Color, FontSizeOf(Size));
    }

    private void ApplyStyle()
    {
        this.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        if (Invalid)
        {
            this.SetStroke(KlyTheme.Resolve(KlyColor.Error).Fill);
            StrokeThickness = 2;
        }
        else
        {
            this.SetStroke(KlyTheme.BorderAccented);
            StrokeThickness = 1;
        }
    }
}
