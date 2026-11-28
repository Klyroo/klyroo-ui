namespace Klyroo.UI.Maui;

/// <summary>A checkbox with a label and an optional description: the native <see cref="CheckBox"/>, drawn in the semantic color. Tapping the text toggles it.</summary>
public class KlyCheckbox : Grid
{
    /// <summary>Identifies <see cref="IsChecked"/>.</summary>
    public static readonly BindableProperty IsCheckedProperty = BindableProperty.Create(nameof(IsChecked), typeof(bool), typeof(KlyCheckbox), false, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyCheckbox)b).ShowChecked((bool)o, (bool)v));
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyCheckbox), null, propertyChanged: (b, _, v) => ((KlyCheckbox)b).ShowText(((KlyCheckbox)b)._label, (string?)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyCheckbox), null, propertyChanged: (b, _, v) => ((KlyCheckbox)b).ShowText(((KlyCheckbox)b)._description, (string?)v));
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyCheckbox), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyCheckbox)b).ApplyStyle());

    private readonly CheckBox _box = new() { VerticalOptions = LayoutOptions.Start };
    private readonly Label _label = new() { FontSize = 14, FontAttributes = FontAttributes.Bold, IsVisible = false };
    private readonly Label _description = new() { FontSize = 14, IsVisible = false };

    /// <summary>Creates a checkbox.</summary>
    public KlyCheckbox()
    {
        ColumnSpacing = 6;
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var texts = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { _label, _description } };
        var toggle = new TapGestureRecognizer();
        toggle.Tapped += (_, _) =>
        {
            if (IsEnabled) IsChecked = !IsChecked;
        };
        texts.GestureRecognizers.Add(toggle);

        this.Add(_box, 0);
        this.Add(texts, 1);
        _box.CheckedChanged += (_, e) => SetValue(IsCheckedProperty, e.Value);
        ApplyStyle();
    }

    /// <summary>Raised when the box is checked or unchecked, by the user or by code.</summary>
    public event EventHandler<CheckedChangedEventArgs>? CheckedChanged;

    /// <summary>Whether the box is checked. Two-way bindable.</summary>
    public bool IsChecked { get => (bool)GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }

    /// <summary>Label of the checkbox.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Text under the label.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Semantic color of the checked box.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web checkbox: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled))
        {
            _box.IsEnabled = IsEnabled;
            Opacity = IsEnabled ? 1 : 0.6;
        }
    }

    private void ShowChecked(bool old, bool value)
    {
        if (_box.IsChecked != value) _box.IsChecked = value;
        CheckedChanged?.Invoke(this, new CheckedChangedEventArgs(value));
    }

    private void ShowText(Label label, string? text)
    {
        label.Text = text;
        label.IsVisible = !string.IsNullOrEmpty(text);
    }

    private void ApplyStyle()
    {
        // The text color, not the fill: a light fill such as the lime primary has no contrast against the page, the border of the box must.
        _box.SetThemeColor(CheckBox.ColorProperty, KlyTheme.Resolve(Color).Text);
        _label.SetThemeColor(Label.TextColorProperty, KlyTheme.Text);
        _description.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
    }
}
