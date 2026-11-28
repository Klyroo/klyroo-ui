namespace Klyroo.UI.Maui;

/// <summary>A multi-line text field: the native <see cref="Editor"/> in a Klyroo.UI frame. It grows with its text.</summary>
public class KlyTextarea : KlyFieldBase
{
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyTextarea), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyTextarea)b).ShowText((string?)o, (string?)v));
    /// <summary>Identifies <see cref="Placeholder"/>.</summary>
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(KlyTextarea), null, propertyChanged: (b, _, v) => ((KlyTextarea)b)._editor.Placeholder = (string?)v);
    /// <summary>Identifies <see cref="Rows"/>.</summary>
    public static readonly BindableProperty RowsProperty = BindableProperty.Create(nameof(Rows), typeof(int), typeof(KlyTextarea), 3, propertyChanged: (b, _, _) => ((KlyTextarea)b).ApplyStyle());
    /// <summary>Identifies <see cref="IsReadOnly"/>.</summary>
    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(KlyTextarea), false, propertyChanged: (b, _, v) => ((KlyTextarea)b)._editor.IsReadOnly = (bool)v);
    /// <summary>Identifies <see cref="MaxLength"/>.</summary>
    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(KlyTextarea), int.MaxValue, propertyChanged: (b, _, v) => ((KlyTextarea)b)._editor.MaxLength = (int)v);

    private readonly Editor _editor;

    /// <summary>Creates a textarea.</summary>
    public KlyTextarea() : this(new Editor { AutoSize = EditorAutoSizeOption.TextChanges })
    {
    }

    private KlyTextarea(Editor editor) : base(editor)
    {
        _editor = editor;
        _editor.TextChanged += (_, e) => SetValue(TextProperty, e.NewTextValue);
        ApplyStyle();
    }

    /// <summary>Raised when the text changes, by the user or by code.</summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged;

    /// <summary>The text. Two-way bindable.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Text shown while the field is empty.</summary>
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>Number of lines the field shows when it is empty. It grows beyond that with its text.</summary>
    public int Rows { get => (int)GetValue(RowsProperty); set => SetValue(RowsProperty, value); }

    /// <summary>The user can select and copy the text but not change it.</summary>
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    /// <summary>Maximum number of characters.</summary>
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }

    /// <inheritdoc />
    protected override void StyleText(KlyThemeColor text, KlyThemeColor placeholder, double fontSize)
    {
        _editor.SetThemeColor(InputView.TextColorProperty, text);
        _editor.SetThemeColor(InputView.PlaceholderColorProperty, placeholder);
        _editor.FontSize = fontSize;
        // A line is about one and a half times the font size.
        _editor.MinimumHeightRequest = Math.Max(1, Rows) * fontSize * 1.5;
    }

    private void ShowText(string? old, string? text)
    {
        if (_editor.Text != text) _editor.Text = text;
        TextChanged?.Invoke(this, new TextChangedEventArgs(old, text));
    }
}
