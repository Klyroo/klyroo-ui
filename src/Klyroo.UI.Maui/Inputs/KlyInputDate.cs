namespace Klyroo.UI.Maui;

/// <summary>
/// A date field: the native <see cref="DatePicker"/> (the dialog of the platform) in a Klyroo.UI frame, with a calendar button that opens it.
/// Picking a day keeps the time of day of the current value; a value that had none starts at midnight.
/// </summary>
public class KlyInputDate : KlyFieldBase
{
    private static readonly DateTime DefaultMin = new(1900, 1, 1);
    private static readonly DateTime DefaultMax = new(2100, 12, 31);

    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(DateTime?), typeof(KlyInputDate), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyInputDate)b).ShowValue((DateTime?)o, (DateTime?)v));
    /// <summary>Identifies <see cref="Min"/>.</summary>
    public static readonly BindableProperty MinProperty = BindableProperty.Create(nameof(Min), typeof(DateTime?), typeof(KlyInputDate), null, propertyChanged: (b, _, v) => ((KlyInputDate)b)._picker.MinimumDate = ((DateTime?)v)?.Date ?? DefaultMin);
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(DateTime?), typeof(KlyInputDate), null, propertyChanged: (b, _, v) => ((KlyInputDate)b)._picker.MaximumDate = ((DateTime?)v)?.Date ?? DefaultMax);

    private readonly DatePicker _picker;
    private readonly InputsIconButton _open = new(KlyIconNames.Calendar, "Choose date", KlySize.Md);

    /// <summary>Creates a date field.</summary>
    public KlyInputDate() : this(new DatePicker { Date = null })
    {
    }

    private KlyInputDate(DatePicker picker) : base(picker)
    {
        _picker = picker;
        _picker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == DatePicker.DateProperty.PropertyName) OnPicked(_picker.Date?.Date);
        };
        _open.Clicked += (_, _) => _picker.IsOpen = true;

        Content = null;
        var layout = new Grid { ColumnSpacing = 6 };
        layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _picker.VerticalOptions = LayoutOptions.Center;
        layout.Add(_picker, 0);
        layout.Add(_open, 1);
        Content = layout;
        ApplyStyle();
    }

    /// <summary>Raised when the date changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<DateTime?>>? ValueChanged;

    /// <summary>The date, or <c>null</c> while nothing is chosen. Two-way bindable.</summary>
    public DateTime? Value { get => (DateTime?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Earliest date the user can pick. A default of 1 January 1900 applies when it is <c>null</c>.</summary>
    public DateTime? Min { get => (DateTime?)GetValue(MinProperty); set => SetValue(MinProperty, value); }

    /// <summary>Latest date the user can pick. A default of 31 December 2100 applies when it is <c>null</c>.</summary>
    public DateTime? Max { get => (DateTime?)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    internal DatePicker Picker => _picker;

    internal InputsIconButton OpenButton => _open;

    /// <inheritdoc />
    protected override void StyleText(KlyThemeColor text, KlyThemeColor placeholder, double fontSize)
    {
        _picker.SetThemeColor(DatePicker.TextColorProperty, text);
        _picker.FontSize = fontSize;
        _open.IconColor = KlyTheme.TextMuted;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) _open.IsEnabled = IsEnabled;
    }

    private void OnPicked(DateTime? picked)
    {
        if (picked == Value?.Date) return;
        Value = picked is { } day ? day + (Value?.TimeOfDay ?? TimeSpan.Zero) : null;
    }

    private void ShowValue(DateTime? old, DateTime? value)
    {
        if (_picker.Date?.Date != value?.Date) _picker.Date = value?.Date;
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<DateTime?>(old, value));
    }
}
