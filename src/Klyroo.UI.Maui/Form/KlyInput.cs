namespace Klyroo.UI.Maui;

/// <summary>A single-line text field: the native <see cref="Entry"/> in a Klyroo.UI frame.</summary>
public class KlyInput : KlyFieldBase
{
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyInput), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyInput)b).ShowText((string?)o, (string?)v));
    /// <summary>Identifies <see cref="Placeholder"/>.</summary>
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(KlyInput), null, propertyChanged: (b, _, v) => ((KlyInput)b)._entry.Placeholder = (string?)v);
    /// <summary>Identifies <see cref="Keyboard"/>.</summary>
    public static readonly BindableProperty KeyboardProperty = BindableProperty.Create(nameof(Keyboard), typeof(Keyboard), typeof(KlyInput), Keyboard.Default, propertyChanged: (b, _, v) => ((KlyInput)b)._entry.Keyboard = (Keyboard)v);
    /// <summary>Identifies <see cref="IsPassword"/>.</summary>
    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(nameof(IsPassword), typeof(bool), typeof(KlyInput), false, propertyChanged: (b, _, v) => ((KlyInput)b)._entry.IsPassword = (bool)v);
    /// <summary>Identifies <see cref="IsReadOnly"/>.</summary>
    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(KlyInput), false, propertyChanged: (b, _, v) => ((KlyInput)b)._entry.IsReadOnly = (bool)v);
    /// <summary>Identifies <see cref="MaxLength"/>.</summary>
    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(KlyInput), int.MaxValue, propertyChanged: (b, _, v) => ((KlyInput)b)._entry.MaxLength = (int)v);
    /// <summary>Identifies <see cref="ReturnType"/>.</summary>
    public static readonly BindableProperty ReturnTypeProperty = BindableProperty.Create(nameof(ReturnType), typeof(ReturnType), typeof(KlyInput), ReturnType.Default, propertyChanged: (b, _, v) => ((KlyInput)b)._entry.ReturnType = (ReturnType)v);

    private readonly Entry _entry;

    /// <summary>Creates an input.</summary>
    public KlyInput() : this(new Entry())
    {
    }

    private KlyInput(Entry entry) : base(entry)
    {
        _entry = entry;
        _entry.TextChanged += (_, e) => SetValue(TextProperty, e.NewTextValue);
        _entry.Completed += (_, _) => Completed?.Invoke(this, EventArgs.Empty);
        ApplyStyle();
    }

    /// <summary>Raised when the text changes, by the user or by code.</summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged;

    /// <summary>Raised when the user presses the return key.</summary>
    public event EventHandler? Completed;

    /// <summary>The text. Two-way bindable.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Text shown while the field is empty.</summary>
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>The soft keyboard to show: text, email, numeric, telephone, URL.</summary>
    public Keyboard Keyboard { get => (Keyboard)GetValue(KeyboardProperty); set => SetValue(KeyboardProperty, value); }

    /// <summary>Hides the characters, for a password.</summary>
    public bool IsPassword { get => (bool)GetValue(IsPasswordProperty); set => SetValue(IsPasswordProperty, value); }

    /// <summary>The user can select and copy the text but not change it.</summary>
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    /// <summary>Maximum number of characters.</summary>
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }

    /// <summary>What the return key of the soft keyboard says: done, next, search, send.</summary>
    public ReturnType ReturnType { get => (ReturnType)GetValue(ReturnTypeProperty); set => SetValue(ReturnTypeProperty, value); }

    /// <inheritdoc />
    protected override void StyleText(KlyThemeColor text, KlyThemeColor placeholder, double fontSize)
    {
        _entry.SetThemeColor(InputView.TextColorProperty, text);
        _entry.SetThemeColor(InputView.PlaceholderColorProperty, placeholder);
        _entry.FontSize = fontSize;
    }

    private void ShowText(string? old, string? text)
    {
        if (_entry.Text != text) _entry.Text = text;
        TextChanged?.Invoke(this, new TextChangedEventArgs(old, text));
    }
}
