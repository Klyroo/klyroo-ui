namespace Klyroo.UI.Maui;

/// <summary>
/// A text field that turns each entered text into a removable tag and binds to the list of tags: the native <see cref="Entry"/> in a Klyroo.UI frame, with the tags before it.
/// Return or a comma adds the typed text; a text already in the list, or past <see cref="Max"/>, is ignored.
/// </summary>
public class KlyInputTags : KlyFieldBase
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(IReadOnlyList<string>), typeof(KlyInputTags), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyInputTags)b).ShowValue((IReadOnlyList<string>?)o, (IReadOnlyList<string>?)v));
    /// <summary>Identifies <see cref="Placeholder"/>.</summary>
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(KlyInputTags), null, propertyChanged: (b, _, v) => ((KlyInputTags)b)._entry.Placeholder = (string?)v);
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(int), typeof(KlyInputTags), 0, validateValue: (_, v) => (int)v >= 0);

    private readonly Entry _entry;
    private readonly FlexLayout _flow = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Center };
    private readonly List<(string Tag, InputsIconButton Remove)> _chips = [];
    private KlyColor? _chipColor;

    /// <summary>Creates a tag input.</summary>
    public KlyInputTags() : this(new Entry { MinimumWidthRequest = 80, VerticalOptions = LayoutOptions.Center })
    {
    }

    private KlyInputTags(Entry entry) : base(entry)
    {
        _entry = entry;
        _entry.TextChanged += (_, e) => OnTyped(e.NewTextValue);
        _entry.Completed += (_, _) => AddTyped();
        FlexLayout.SetGrow(_entry, 1);
        FlexLayout.SetBasis(_entry, new Microsoft.Maui.Layouts.FlexBasis(80, false));
        Content = null;
        _flow.Children.Add(_entry);
        Content = _flow;
        ApplyStyle();
    }

    /// <summary>Raised when the list of tags changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<IReadOnlyList<string>>>? ValueChanged;

    /// <summary>The tags, in order. Two-way bindable.</summary>
    public IReadOnlyList<string>? Value { get => (IReadOnlyList<string>?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Text shown while the field is empty.</summary>
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>Most tags allowed. Past it, typed text is ignored. Zero means no limit.</summary>
    public int Max { get => (int)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    internal Entry Entry => _entry;

    internal IReadOnlyList<(string Tag, InputsIconButton Remove)> Chips => _chips;

    /// <summary>Adds the typed text as a tag, as when the user presses return.</summary>
    internal void AddTyped()
    {
        var text = _entry.Text;
        _entry.Text = string.Empty;
        Add([text]);
    }

    /// <inheritdoc />
    protected override void StyleText(KlyThemeColor text, KlyThemeColor placeholder, double fontSize)
    {
        _entry.SetThemeColor(InputView.TextColorProperty, text);
        _entry.SetThemeColor(InputView.PlaceholderColorProperty, placeholder);
        _entry.FontSize = fontSize;
        if (_chipColor != Color) Rebuild();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled))
        {
            foreach (var (_, remove) in _chips) remove.IsEnabled = IsEnabled;
        }
    }

    private void OnTyped(string? text)
    {
        if (text is null || !text.Contains(',')) return;
        // A comma ends a tag: what is before the last comma is added, what follows stays in the field.
        var parts = text.Split(',');
        _entry.Text = parts[^1];
        Add(parts[..^1]);
    }

    private void Add(IEnumerable<string?> texts)
    {
        var tags = new List<string>(Value ?? []);
        foreach (var raw in texts)
        {
            var text = raw?.Trim();
            if (!string.IsNullOrEmpty(text) && !tags.Contains(text) && (Max == 0 || tags.Count < Max)) tags.Add(text);
        }
        if (!tags.SequenceEqual(Value ?? [])) Value = tags;
    }

    private void Remove(string tag)
    {
        Value = [.. (Value ?? []).Where(t => t != tag)];
        _entry.Focus();
    }

    private void ShowValue(IReadOnlyList<string>? old, IReadOnlyList<string>? value)
    {
        Rebuild();
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<IReadOnlyList<string>>(old, value));
    }

    private void Rebuild()
    {
        foreach (var view in _flow.Children.OfType<Border>().ToList()) _flow.Children.Remove(view);
        _chips.Clear();
        _chipColor = Color;
        var set = KlyTheme.Resolve(Color);
        var index = 0;
        foreach (var tag in Value ?? [])
        {
            var label = new Label { Text = tag, FontSize = 12, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center };
            label.SetThemeColor(Label.TextColorProperty, set.Text);
            var current = tag;
            var remove = new InputsIconButton(KlyIconNames.Close, $"Remove {tag}", KlySize.Xs, 28, 28) { IconColor = set.Text, IsEnabled = IsEnabled };
            remove.Clicked += (_, _) => Remove(current);
            var chip = new Border
            {
                Padding = new Thickness(8, 0, 0, 0),
                Margin = new Thickness(0, 2, 6, 2),
                Content = new HorizontalStackLayout { Spacing = 2, Children = { label, remove } },
            };
            chip.SetRadius(KlyTheme.RadiusOf(1) - 2);
            chip.SetThemeColor(BackgroundColorProperty, set.Fill.WithAlpha(0.10f));
            chip.SetStroke(set.Text.WithAlpha(0.75f));
            chip.StrokeThickness = 1;
            _flow.Children.Insert(index++, chip);
            _chips.Add((tag, remove));
        }
    }
}
