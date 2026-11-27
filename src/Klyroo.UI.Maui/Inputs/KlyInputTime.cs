namespace Klyroo.UI.Maui;

/// <summary>A time field: the native <see cref="TimePicker"/> (the dialog of the platform) in a Klyroo.UI frame, bound to a <c>TimeOnly?</c>, in 12 or 24 hour format.</summary>
public class KlyInputTime : KlyFieldBase
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(TimeOnly?), typeof(KlyInputTime), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyInputTime)b).ShowValue((TimeOnly?)o, (TimeOnly?)v));
    /// <summary>Identifies <see cref="Hour12"/>.</summary>
    public static readonly BindableProperty Hour12Property = BindableProperty.Create(nameof(Hour12), typeof(bool?), typeof(KlyInputTime), null, propertyChanged: (b, _, v) => ((KlyInputTime)b)._picker.Format = FormatFor((bool?)v));

    private readonly TimePicker _picker;

    /// <summary>Creates a time field.</summary>
    public KlyInputTime() : this(new TimePicker { Time = null })
    {
    }

    private KlyInputTime(TimePicker picker) : base(picker)
    {
        _picker = picker;
        _picker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == TimePicker.TimeProperty.PropertyName) OnPicked(_picker.Time);
        };
        ApplyStyle();
    }

    /// <summary>Raised when the time changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<TimeOnly?>>? ValueChanged;

    /// <summary>The time, or <c>null</c> while nothing is chosen. Two-way bindable.</summary>
    public TimeOnly? Value { get => (TimeOnly?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Shows an AM/PM marker when <c>true</c>, a 24 hour clock when <c>false</c>. The format of the platform applies when it is <c>null</c>.</summary>
    public bool? Hour12 { get => (bool?)GetValue(Hour12Property); set => SetValue(Hour12Property, value); }

    internal TimePicker Picker => _picker;

    /// <inheritdoc />
    protected override void StyleText(KlyThemeColor text, KlyThemeColor placeholder, double fontSize)
    {
        _picker.SetThemeColor(TimePicker.TextColorProperty, text);
        _picker.FontSize = fontSize;
    }

    private static string FormatFor(bool? hour12) => hour12 switch
    {
        true => "h:mm tt",
        false => "HH:mm",
        _ => "t",
    };

    private void OnPicked(TimeSpan? picked)
    {
        TimeOnly? time = picked is { } span && span >= TimeSpan.Zero && span < TimeSpan.FromDays(1) ? TimeOnly.FromTimeSpan(span) : null;
        if (time != Value) Value = time;
    }

    private void ShowValue(TimeOnly? old, TimeOnly? value)
    {
        var span = value?.ToTimeSpan();
        if (_picker.Time != span) _picker.Time = span;
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<TimeOnly?>(old, value));
    }
}
