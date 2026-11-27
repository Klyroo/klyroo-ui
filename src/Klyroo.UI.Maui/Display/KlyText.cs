namespace Klyroo.UI.Maui;

/// <summary>One or more paragraphs, or a quote, with an optional title, a clamp to a number of lines and a footer.</summary>
public class KlyText : KlyBlockBase
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyText), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(string), typeof(KlyText), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Paragraphs"/>.</summary>
    public static readonly BindableProperty ParagraphsProperty = BindableProperty.Create(nameof(Paragraphs), typeof(IReadOnlyList<string>), typeof(KlyText), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Author"/>.</summary>
    public static readonly BindableProperty AuthorProperty = BindableProperty.Create(nameof(Author), typeof(string), typeof(KlyText), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Meta"/>.</summary>
    public static readonly BindableProperty MetaProperty = BindableProperty.Create(nameof(Meta), typeof(string), typeof(KlyText), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="MaxLines"/>.</summary>
    public static readonly BindableProperty MaxLinesProperty = BindableProperty.Create(nameof(MaxLines), typeof(int?), typeof(KlyText), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(KlyTextVariant), typeof(KlyText), KlyTextVariant.Default, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="IsExpanded"/>.</summary>
    public static readonly BindableProperty IsExpandedProperty = BindableProperty.Create(nameof(IsExpanded), typeof(bool), typeof(KlyText), false, BindingMode.TwoWay, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ReadMoreLabel"/>.</summary>
    public static readonly BindableProperty ReadMoreLabelProperty = BindableProperty.Create(nameof(ReadMoreLabel), typeof(string), typeof(KlyText), "Read more", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ShowLessLabel"/>.</summary>
    public static readonly BindableProperty ShowLessLabelProperty = BindableProperty.Create(nameof(ShowLessLabel), typeof(string), typeof(KlyText), "Show less", propertyChanged: Rebuild);

    private readonly VerticalStackLayout _stack = new() { Spacing = 8 };
    private Label? _body;
    private Button? _toggle;

    /// <summary>Creates a text block.</summary>
    public KlyText() : base(KlySurface.Plain)
    {
        Body.Add(_stack);
        Rebuild();
    }

    /// <summary>The bold first line of the text.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The text of the block, used when <see cref="Paragraphs"/> is not set.</summary>
    public string? Value { get => (string?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The paragraphs of the block. When set, they replace <see cref="Value"/>.</summary>
    public IReadOnlyList<string>? Paragraphs { get => (IReadOnlyList<string>?)GetValue(ParagraphsProperty); set => SetValue(ParagraphsProperty, value); }

    /// <summary>The author shown in the footer, before <see cref="Meta"/>.</summary>
    public string? Author { get => (string?)GetValue(AuthorProperty); set => SetValue(AuthorProperty, value); }

    /// <summary>Extra text shown in the footer, after <see cref="Author"/>.</summary>
    public string? Meta { get => (string?)GetValue(MetaProperty); set => SetValue(MetaProperty, value); }

    /// <summary>When greater than zero, clamps <see cref="Value"/> to this number of lines and adds a Read more button. The button shows even when the text is shorter, and <see cref="Paragraphs"/> are not clamped.</summary>
    public int? MaxLines { get => (int?)GetValue(MaxLinesProperty); set => SetValue(MaxLinesProperty, value); }

    /// <summary>The layout: <see cref="KlyTextVariant.Default"/> or <see cref="KlyTextVariant.Quote"/>.</summary>
    public KlyTextVariant Variant { get => (KlyTextVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Whether a clamped text shows all its lines. Two-way bindable; the button toggles it.</summary>
    public bool IsExpanded { get => (bool)GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }

    /// <summary>Text of the button that expands a clamped text, in the language of the app.</summary>
    public string ReadMoreLabel { get => (string)GetValue(ReadMoreLabelProperty); set => SetValue(ReadMoreLabelProperty, value); }

    /// <summary>Text of the button that clamps an expanded text again, in the language of the app.</summary>
    public string ShowLessLabel { get => (string)GetValue(ShowLessLabelProperty); set => SetValue(ShowLessLabelProperty, value); }

    /// <summary>The label of the value, for tests.</summary>
    internal Label? BodyLabel => _body;

    internal Button? ToggleButton => _toggle;

    private bool Clamped => MaxLines is > 0 && !IsExpanded;

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyText)bindable).Rebuild();

    private void Rebuild()
    {
        _stack.Children.Clear();
        _body = null;
        _toggle = null;
        var quote = Variant == KlyTextVariant.Quote;
        var attributes = quote ? FontAttributes.Italic : FontAttributes.None;

        if (!string.IsNullOrEmpty(Title))
        {
            var title = DisplayParts.NewLabel(14, KlyTheme.TextHighlighted, FontAttributes.Bold | attributes);
            title.Text = Title;
            _stack.Children.Add(title);
        }

        if (Paragraphs is { Count: > 0 } paragraphs)
        {
            foreach (var paragraph in paragraphs)
            {
                var label = DisplayParts.NewLabel(14, KlyTheme.Text, attributes);
                label.Text = paragraph;
                _stack.Children.Add(label);
            }
        }
        else if (!string.IsNullOrEmpty(Value))
        {
            _body = DisplayParts.NewLabel(14, KlyTheme.Text, attributes, Clamped ? LineBreakMode.TailTruncation : LineBreakMode.WordWrap);
            _body.Text = Value;
            if (Clamped) _body.MaxLines = MaxLines!.Value;
            _stack.Children.Add(_body);
        }

        if (MaxLines is > 0)
        {
            _toggle = DisplayParts.TextButton(IsExpanded ? ShowLessLabel : ReadMoreLabel, DisplayParts.PrimaryText, 12, () => IsExpanded = !IsExpanded);
            _stack.Children.Add(_toggle);
        }

        var footer = string.Join(" · ", new[] { Author, Meta }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (footer.Length > 0)
        {
            var label = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            label.Text = footer;
            _stack.Children.Add(label);
        }

        // A quote has a start border and an indent; the first thing a screen reader needs is that it is one text.
        DisplayParts.Detach(_stack);
        Body.Children.Clear();
        if (quote)
        {
            var bar = new Border { WidthRequest = 2, StrokeThickness = 0, VerticalOptions = LayoutOptions.Fill };
            bar.SetThemeColor(BackgroundColorProperty, DisplayParts.PrimaryText);
            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            row.Add(bar, 0);
            row.Add(_stack, 1);
            Body.Children.Add(row);
        }
        else
        {
            Body.Children.Add(_stack);
        }
    }
}
