using System.Collections;

namespace Klyroo.UI.Maui;

/// <summary>
/// A select with a searchable list, single or multiple selection. The list opens inline under the field, with a filter field at its top;
/// the selection is a list of values, bound with <see cref="Value"/>. The options are filled by the app: to search a remote source, listen to <see cref="SearchChanged"/> and replace <see cref="Items"/>.
/// </summary>
public class KlySelectMenu : VerticalStackLayout, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IEnumerable), typeof(KlySelectMenu), null, propertyChanged: (b, _, _) => ((KlySelectMenu)b).Refresh());
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(IReadOnlyList<object>), typeof(KlySelectMenu), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlySelectMenu)b).ShowValue((IReadOnlyList<object>?)o, (IReadOnlyList<object>?)v));
    /// <summary>Identifies <see cref="Multiple"/>.</summary>
    public static readonly BindableProperty MultipleProperty = BindableProperty.Create(nameof(Multiple), typeof(bool), typeof(KlySelectMenu), false);
    /// <summary>Identifies <see cref="Create"/>.</summary>
    public static readonly BindableProperty CreateProperty = BindableProperty.Create(nameof(Create), typeof(bool), typeof(KlySelectMenu), false, propertyChanged: (b, _, _) => ((KlySelectMenu)b).Refresh());
    /// <summary>Identifies <see cref="Placeholder"/>.</summary>
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(KlySelectMenu), null, propertyChanged: (b, _, _) => ((KlySelectMenu)b).Refresh());
    /// <summary>Identifies <see cref="SearchLabel"/>.</summary>
    public static readonly BindableProperty SearchLabelProperty = BindableProperty.Create(nameof(SearchLabel), typeof(string), typeof(KlySelectMenu), "Search", propertyChanged: (b, _, v) => ((KlySelectMenu)b).ShowSearchLabel((string?)v));
    /// <summary>Identifies <see cref="IsOpen"/>.</summary>
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(KlySelectMenu), false, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlySelectMenu)b).ShowOpen((bool)v));
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlySelectMenu), KlyColor.Primary, propertyChanged: (b, _, v) => { var menu = (KlySelectMenu)b; menu._field.Color = (KlyColor)v; menu.Refresh(); });
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(FieldVariant), typeof(KlySelectMenu), FieldVariant.Outline, propertyChanged: (b, _, v) => ((KlySelectMenu)b)._field.Variant = (FieldVariant)v);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlySelectMenu), KlySize.Md, propertyChanged: (b, _, v) => ((KlySelectMenu)b)._field.Size = (KlySize)v);
    /// <summary>Identifies <see cref="Invalid"/>.</summary>
    public static readonly BindableProperty InvalidProperty = BindableProperty.Create(nameof(Invalid), typeof(bool), typeof(KlySelectMenu), false, propertyChanged: (b, _, v) => ((KlySelectMenu)b)._field.Invalid = (bool)v);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlySelectMenu), null, propertyChanged: (b, _, _) => ((KlySelectMenu)b).UpdateSemantics());

    private readonly InputsPickerField _field = new(KlyIconNames.ChevronUpDown);
    private readonly InputsMenuPanel _panel = new(true, "Search", "No results");
    private readonly InputsFieldTaken _taken = new();
    private List<InputsOption> _options = [];
    private string _search = string.Empty;

    /// <summary>Creates a select menu.</summary>
    public KlySelectMenu()
    {
        Spacing = 4;
        Children.Add(_field);
        Children.Add(_panel);
        _field.Tapped += (_, _) => IsOpen = !IsOpen;
        _panel.List.Chosen += (_, option) => Choose(option);
        _panel.Search!.TextChanged += (_, e) =>
        {
            _search = e.NewTextValue ?? string.Empty;
            Refresh();
            SearchChanged?.Invoke(this, e);
        };
        Refresh();
    }

    /// <summary>Raised when the selection changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<IReadOnlyList<object>>>? ValueChanged;

    /// <summary>Raised when the text of the filter field changes. A menu over a remote source loads the matching options here and replaces <see cref="Items"/>.</summary>
    public event EventHandler<TextChangedEventArgs>? SearchChanged;

    /// <summary>The options: <see cref="KlySelectItem"/>s, or any objects, each one shown as its <c>ToString()</c> and standing for itself.</summary>
    public IEnumerable? Items { get => (IEnumerable?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The values of the selected options. With a single selection the list holds at most one. Two-way bindable.</summary>
    public IReadOnlyList<object>? Value { get => (IReadOnlyList<object>?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Several values can be selected, and the list stays open while the user chooses. Otherwise the list holds at most one value and closes when one is chosen.</summary>
    public bool Multiple { get => (bool)GetValue(MultipleProperty); set => SetValue(MultipleProperty, value); }

    /// <summary>Offers the typed text as a new text value when no option has that label. A created value is kept in <see cref="Value"/> but is not added to <see cref="Items"/>.</summary>
    public bool Create { get => (bool)GetValue(CreateProperty); set => SetValue(CreateProperty, value); }

    /// <summary>Text shown while nothing is selected.</summary>
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>Label and placeholder of the filter field. Defaults to <c>Search</c>.</summary>
    public string? SearchLabel { get => (string?)GetValue(SearchLabelProperty); set => SetValue(SearchLabelProperty, value); }

    /// <summary>Whether the list is open. Two-way bindable; tapping the field toggles it.</summary>
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>Semantic color of the focus ring and of the check mark of a selected option.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Look of the field: <c>Outline</c>, <c>Soft</c>, <c>Subtle</c>, <c>Ghost</c> or <c>None</c>.</summary>
    public FieldVariant Variant { get => (FieldVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Size of the field, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Draws the error ring, in the error color, around the field.</summary>
    public bool Invalid { get => (bool)GetValue(InvalidProperty); set => SetValue(InvalidProperty, value); }

    /// <summary>What a screen reader says for the field, such as its label, before the selected text.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal InputsPickerField Field => _field;

    internal InputsMenuPanel Panel => _panel;

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web select menu: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.6;
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeInvalid(this, InvalidProperty, state.Invalid);
        _taken.TakeName(this, AccessibleNameProperty, state.Name);
        _taken.TakeSize(this, SizeProperty, state.Size);
        SemanticProperties.SetHint(_field.Label, state.Hint);
    }

    private string LabelOf(object value) => _options.FirstOrDefault(o => o.HasValue(value))?.Label ?? value.ToString() ?? string.Empty;

    private bool IsSelected(InputsOption option) => Value?.Any(option.HasValue) == true;

    private void Choose(InputsOption option)
    {
        if (!IsEnabled) return;
        var current = Value ?? [];
        if (Multiple)
        {
            Value = IsSelected(option) ? [.. current.Where(v => !option.HasValue(v))] : [.. current, option.Value!];
            return;
        }
        if (!(IsSelected(option) && current.Count == 1)) Value = [option.Value!];
        IsOpen = false;
    }

    private void ShowValue(IReadOnlyList<object>? old, IReadOnlyList<object>? value)
    {
        Refresh();
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<IReadOnlyList<object>>(old, value));
    }

    private void ShowOpen(bool open)
    {
        _panel.IsVisible = open;
        if (!open && _panel.Search is { Text.Length: > 0 } search) search.Text = string.Empty;
    }

    private void ShowSearchLabel(string? label)
    {
        var text = label ?? string.Empty;
        _panel.Search!.Placeholder = text;
        SemanticProperties.SetDescription(_panel.Search, text);
    }

    private void Refresh()
    {
        _options = InputsOptions.From(Items);
        var shown = InputsOptions.Filter(_options, _search);

        // The option that creates the typed text, unless an option has that label or the text is already a value.
        var typed = _search.Trim();
        if (Create && typed.Length > 0
            && !_options.Any(o => string.Equals(o.Label, typed, StringComparison.CurrentCultureIgnoreCase))
            && Value?.Any(v => v is string text && text == typed) != true)
        {
            shown.Add(new InputsOption(typed, $"Create \"{typed}\"", false, null, KlyIconNames.Plus, null));
        }
        _panel.Show(shown, IsSelected, Color);

        var selected = Value ?? [];
        if (selected.Count == 0) _field.ShowText(Placeholder, true);
        else _field.ShowText(string.Join(", ", selected.Select(LabelOf)), false);
        _field.ShowIcon(selected.Count == 1 ? _options.FirstOrDefault(o => o.HasValue(selected[0]))?.Icon : null);
        UpdateSemantics();
    }

    private void UpdateSemantics()
    {
        var text = _field.Label.Text;
        var name = AccessibleName;
        SemanticProperties.SetDescription(_field.Label, string.IsNullOrEmpty(name) ? text : string.IsNullOrEmpty(text) ? name : name + ", " + text);
    }
}
