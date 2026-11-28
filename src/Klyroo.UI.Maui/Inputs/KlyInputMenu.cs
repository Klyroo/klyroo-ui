using System.Collections;

namespace Klyroo.UI.Maui;

/// <summary>
/// A text field with a list of suggestions that filters as the user types, listed inline under the field. By default the typed text is the value (a string) and the
/// suggestions complete it; with <see cref="Strict"/>, or when an item has a value that is not text, only a suggestion is a valid value and the field shows its label.
/// The suggestions are filled by the app: to search a remote source, listen to <see cref="SearchChanged"/> and replace <see cref="Items"/>.
/// </summary>
public class KlyInputMenu : VerticalStackLayout, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IEnumerable), typeof(KlyInputMenu), null, propertyChanged: (b, _, _) => ((KlyInputMenu)b).Refresh());
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(object), typeof(KlyInputMenu), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyInputMenu)b).ShowValue(o, v));
    /// <summary>Identifies <see cref="Strict"/>.</summary>
    public static readonly BindableProperty StrictProperty = BindableProperty.Create(nameof(Strict), typeof(bool), typeof(KlyInputMenu), false, propertyChanged: (b, _, _) => ((KlyInputMenu)b).Refresh());
    /// <summary>Identifies <see cref="Placeholder"/>.</summary>
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(KlyInputMenu), null, propertyChanged: (b, _, v) => ((KlyInputMenu)b)._field.Entry.Placeholder = (string?)v);
    /// <summary>Identifies <see cref="IsOpen"/>.</summary>
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(KlyInputMenu), false, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlyInputMenu)b)._panel.IsVisible = (bool)v);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyInputMenu), KlyColor.Primary, propertyChanged: (b, _, v) => { var menu = (KlyInputMenu)b; menu._field.Color = (KlyColor)v; menu.Refresh(); });
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(FieldVariant), typeof(KlyInputMenu), FieldVariant.Outline, propertyChanged: (b, _, v) => ((KlyInputMenu)b)._field.Variant = (FieldVariant)v);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyInputMenu), KlySize.Md, propertyChanged: (b, _, v) => ((KlyInputMenu)b)._field.Size = (KlySize)v);
    /// <summary>Identifies <see cref="Invalid"/>.</summary>
    public static readonly BindableProperty InvalidProperty = BindableProperty.Create(nameof(Invalid), typeof(bool), typeof(KlyInputMenu), false, propertyChanged: (b, _, v) => ((KlyInputMenu)b)._field.Invalid = (bool)v);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyInputMenu), null, propertyChanged: (b, _, v) => ((KlyInputMenu)b)._field.AccessibleName = (string?)v);

    private readonly InputsMenuField _field = new();
    private readonly InputsMenuPanel _panel = new(false, string.Empty, "No results");
    private readonly InputsFieldTaken _taken = new();
    private List<InputsOption> _options = [];
    private string _filter = string.Empty;
    private bool _showing;

    /// <summary>Creates an input menu.</summary>
    public KlyInputMenu()
    {
        Spacing = 4;
        Children.Add(_field);
        Children.Add(_panel);
        var entry = _field.Entry;
        entry.TextChanged += (_, e) => OnTyped(e);
        entry.Completed += (_, _) => Commit();
        entry.Focused += (_, _) => OnFocused();
        entry.Unfocused += (_, _) => OnUnfocused();
        _field.Chevron.Clicked += (_, _) =>
        {
            _filter = IsStrict ? string.Empty : entry.Text ?? string.Empty;
            Refresh();
            IsOpen = !IsOpen;
        };
        _panel.List.Chosen += (_, option) => Choose(option);
        Refresh();
    }

    /// <summary>Raised when the value changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<object>>? ValueChanged;

    /// <summary>Raised when the user types in the field. A menu over a remote source loads the matching suggestions here and replaces <see cref="Items"/>.</summary>
    public event EventHandler<TextChangedEventArgs>? SearchChanged;

    /// <summary>The suggestions: <see cref="KlySelectItem"/>s, or any objects, each one shown as its <c>ToString()</c> and standing for itself.</summary>
    public IEnumerable? Items { get => (IEnumerable?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The typed text (a string), or with <see cref="Strict"/> the value of the chosen suggestion. Two-way bindable. A free text is read when the field loses focus or the user presses return.</summary>
    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Only a suggestion is a valid value: <see cref="Value"/> is the chosen item's value and the field shows its label. It is always the case when an item has a value that is not a string.</summary>
    public bool Strict { get => (bool)GetValue(StrictProperty); set => SetValue(StrictProperty, value); }

    /// <summary>Text shown while the field is empty.</summary>
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>Whether the suggestions are shown. Two-way bindable; the field opens them when it gets focus or the user types.</summary>
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>Semantic color of the focus ring and of the check mark of the chosen suggestion.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Look of the field: <c>Outline</c>, <c>Soft</c>, <c>Subtle</c>, <c>Ghost</c> or <c>None</c>.</summary>
    public FieldVariant Variant { get => (FieldVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Size of the field, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Draws the error ring, in the error color, around the field.</summary>
    public bool Invalid { get => (bool)GetValue(InvalidProperty); set => SetValue(InvalidProperty, value); }

    /// <summary>What a screen reader says for the field, such as its label.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal InputsMenuField Field => _field;

    internal InputsMenuPanel Panel => _panel;

    private bool IsStrict => Strict || _options.Any(o => o.Value is not string);

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web input menu: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.6;
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeInvalid(this, InvalidProperty, state.Invalid);
        _taken.TakeName(this, AccessibleNameProperty, state.Name);
        _taken.TakeSize(this, SizeProperty, state.Size);
        SemanticProperties.SetHint(_field.Entry, state.Hint);
    }

    /// <summary>Reads the text of the field as the value, as when it loses focus: a free text becomes the value, a strict field shows the label of its value again.</summary>
    internal void Commit()
    {
        if (IsStrict)
        {
            ShowText();
            return;
        }
        var text = _field.Entry.Text;
        var next = string.IsNullOrEmpty(text) ? null : text;
        if (!Equals(next, Value)) Value = next;
    }

    private string? LabelOf(object? value) => value is null ? null : _options.FirstOrDefault(o => o.HasValue(value))?.Label ?? value.ToString();

    private bool IsSelected(InputsOption option) => IsStrict && Value is not null && option.HasValue(Value);

    private void OnTyped(TextChangedEventArgs e)
    {
        if (_showing) return;
        _filter = e.NewTextValue ?? string.Empty;
        // Emptying a strict field clears the value: no suggestion is left to be it.
        if (IsStrict && _filter.Length == 0 && Value is not null) Value = null;
        Refresh();
        IsOpen = true;
        SearchChanged?.Invoke(this, e);
    }

    private void OnFocused()
    {
        _filter = IsStrict ? string.Empty : _field.Entry.Text ?? string.Empty;
        Refresh();
        IsOpen = true;
    }

    private void OnUnfocused()
    {
        Commit();
        // A tap on a suggestion takes the focus first: close after it had its chance.
        if (Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread() is { } dispatcher)
        {
            dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () =>
            {
                if (!_field.Entry.IsFocused) IsOpen = false;
            });
        }
        else
        {
            IsOpen = false;
        }
    }

    private void Choose(InputsOption option)
    {
        if (!IsEnabled) return;
        if (IsStrict) Value = option.Value;
        else Value = option.Label;
        ShowText();
        _filter = string.Empty;
        Refresh();
        IsOpen = false;
    }

    private void ShowValue(object? old, object? value)
    {
        ShowText();
        Refresh();
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<object>(old, value));
    }

    private void ShowText()
    {
        var text = IsStrict ? LabelOf(Value) ?? string.Empty : Value?.ToString() ?? string.Empty;
        if (_field.Entry.Text == text) return;
        _showing = true;
        _field.Entry.Text = text;
        _showing = false;
    }

    private void Refresh()
    {
        _options = InputsOptions.From(Items);
        _panel.Show(InputsOptions.Filter(_options, _filter), IsSelected, Color);
    }
}
