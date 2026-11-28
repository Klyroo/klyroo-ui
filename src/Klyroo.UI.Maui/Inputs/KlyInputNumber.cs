using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// A number field with increment and decrement buttons and limits: the native <see cref="Entry"/> (numeric keyboard) in a Klyroo.UI frame.
/// Typed text is read when the field loses focus or the user presses return, then limited to <see cref="Min"/> and <see cref="Max"/>.
/// </summary>
public class KlyInputNumber : KlyFieldBase
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(decimal?), typeof(KlyInputNumber), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyInputNumber)b).ShowValue((decimal?)o, (decimal?)v));
    /// <summary>Identifies <see cref="Min"/>.</summary>
    public static readonly BindableProperty MinProperty = BindableProperty.Create(nameof(Min), typeof(decimal?), typeof(KlyInputNumber), null, propertyChanged: (b, _, _) => ((KlyInputNumber)b).UpdateButtons());
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(decimal?), typeof(KlyInputNumber), null, propertyChanged: (b, _, _) => ((KlyInputNumber)b).UpdateButtons());
    /// <summary>Identifies <see cref="Step"/>.</summary>
    public static readonly BindableProperty StepProperty = BindableProperty.Create(nameof(Step), typeof(decimal), typeof(KlyInputNumber), 1m, validateValue: (_, v) => (decimal)v > 0);
    /// <summary>Identifies <see cref="Placeholder"/>.</summary>
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(KlyInputNumber), null, propertyChanged: (b, _, v) => ((KlyInputNumber)b)._entry.Placeholder = (string?)v);
    /// <summary>Identifies <see cref="Culture"/>.</summary>
    public static readonly BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(KlyInputNumber), null, propertyChanged: (b, _, _) => ((KlyInputNumber)b).Show());
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyInputNumber), StackOrientation.Horizontal, propertyChanged: (b, _, _) => ((KlyInputNumber)b).Rebuild());

    private readonly Entry _entry;
    private readonly Grid _layout = new() { ColumnSpacing = 0, RowSpacing = 0 };
    private readonly InputsIconButton _decrement = new(KlyIconNames.Minus, "Decrease", KlySize.Md);
    private readonly InputsIconButton _increment = new(KlyIconNames.Plus, "Increase", KlySize.Md);

    /// <summary>Creates a number field.</summary>
    public KlyInputNumber() : this(new Entry { Keyboard = Keyboard.Numeric })
    {
    }

    private KlyInputNumber(Entry entry) : base(entry)
    {
        _entry = entry;
        _entry.Completed += (_, _) => CommitText();
        _entry.Unfocused += (_, _) => CommitText();
        _decrement.Clicked += (_, _) => Nudge(-1);
        _increment.Clicked += (_, _) => Nudge(1);
        Content = _layout;
        Rebuild();
        ApplyStyle();
        Show();
    }

    /// <summary>Raised when the value changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<decimal?>>? ValueChanged;

    /// <summary>The number, or <c>null</c> while the field is empty. Two-way bindable.</summary>
    public decimal? Value { get => (decimal?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Smallest value the user can enter or step to. There is no lower limit when it is <c>null</c>.</summary>
    public decimal? Min { get => (decimal?)GetValue(MinProperty); set => SetValue(MinProperty, value); }

    /// <summary>Largest value the user can enter or step to. There is no upper limit when it is <c>null</c>.</summary>
    public decimal? Max { get => (decimal?)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>Amount added or removed by the buttons. A step of zero or less is ignored.</summary>
    public decimal Step { get => (decimal)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>Text shown while the field is empty.</summary>
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>Culture used to show and read the number, such as <c>fr-FR</c>. Defaults to the current culture.</summary>
    public CultureInfo? Culture { get => (CultureInfo?)GetValue(CultureProperty); set => SetValue(CultureProperty, value); }

    /// <summary><c>Horizontal</c> puts the buttons on both sides of the field, <c>Vertical</c> stacks them at its end.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    internal Entry Entry => _entry;

    internal InputsIconButton DecrementButton => _decrement;

    internal InputsIconButton IncrementButton => _increment;

    /// <summary>Reads the text of the field as a value: <c>null</c> for an empty text. <c>false</c> when the text is not a number.</summary>
    internal static bool TryParse(string? text, CultureInfo culture, out decimal? value)
    {
        var clean = (text ?? string.Empty).Trim().Replace('−', '-').Replace(" ", string.Empty).Replace(" ", string.Empty).Replace(" ", string.Empty);
        if (clean.Length == 0)
        {
            value = null;
            return true;
        }
        if (decimal.TryParse(clean, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, culture, out var number))
        {
            value = number;
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>Reads the text of the field, as when it loses focus: the value becomes the number typed, limited to <see cref="Min"/> and <see cref="Max"/>. A text that is not a number is replaced by the current value.</summary>
    internal void CommitText()
    {
        if (TryParse(_entry.Text, Culture ?? CultureInfo.CurrentCulture, out var parsed)) Commit(parsed);
        else Show();
    }

    /// <inheritdoc />
    protected override void StyleText(KlyThemeColor text, KlyThemeColor placeholder, double fontSize)
    {
        // The frame of the base class pads the field; here the buttons must reach its edges, so the entry takes the padding.
        var pad = Padding;
        Padding = new Thickness(0);
        _entry.Margin = Orientation == StackOrientation.Horizontal ? new Thickness(0, pad.Top) : new Thickness(pad.Left, pad.Top);
        _entry.SetThemeColor(InputView.TextColorProperty, text);
        _entry.SetThemeColor(InputView.PlaceholderColorProperty, placeholder);
        _entry.FontSize = fontSize;
        _entry.HorizontalTextAlignment = Orientation == StackOrientation.Horizontal ? TextAlignment.Center : TextAlignment.Start;
        _decrement.IconColor = KlyTheme.TextMuted;
        _increment.IconColor = KlyTheme.TextMuted;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) UpdateButtons();
    }

    private decimal Clamp(decimal number)
    {
        if (Min is { } min && number < min) number = min;
        if (Max is { } max && number > max) number = max;
        return number;
    }

    private void Commit(decimal? number)
    {
        var next = number is null ? null : (decimal?)Clamp(number.Value);
        if (next == Value) Show();
        else Value = next;
    }

    private void Nudge(int direction)
    {
        var from = Value ?? Clamp(0m);
        Commit(from + (direction * Step));
    }

    private void ShowValue(decimal? old, decimal? value)
    {
        Show();
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<decimal?>(old, value));
    }

    private void Show()
    {
        var text = Value is { } number ? number.ToString("#,##0.##########", Culture ?? CultureInfo.CurrentCulture) : string.Empty;
        if (_entry.Text != text) _entry.Text = text;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        _decrement.IsEnabled = IsEnabled && !(Value is { } v && Min is { } min && v <= min);
        _increment.IsEnabled = IsEnabled && !(Value is { } w && Max is { } max && w >= max);
    }

    private void Rebuild()
    {
        _layout.Children.Clear();
        _layout.ColumnDefinitions.Clear();
        _layout.RowDefinitions.Clear();
        if (Orientation == StackOrientation.Horizontal)
        {
            _layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            _layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            _layout.Add(_decrement, 0);
            _layout.Add(_entry, 1);
            _layout.Add(_increment, 2);
            _decrement.MinimumHeightRequest = 40;
            _increment.MinimumHeightRequest = 40;
        }
        else
        {
            // The buttons share the height of the field, one above the other: each is half of it.
            _layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            _layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            _layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            _layout.Add(_entry, 0, 0);
            Grid.SetRowSpan(_entry, 2);
            _layout.Add(_increment, 1, 0);
            _layout.Add(_decrement, 1, 1);
            _decrement.MinimumHeightRequest = 20;
            _increment.MinimumHeightRequest = 20;
        }
        _entry.VerticalOptions = LayoutOptions.Center;
        ApplyStyle();
    }
}
