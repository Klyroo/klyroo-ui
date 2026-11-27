using System.Runtime.CompilerServices;

namespace Klyroo.UI.Maui;

/// <summary>
/// A wrapper that gives a control its label, description, hint, help and error text. It tells the control inside it (a <see cref="KlyFieldBase"/> or any other input of Klyroo.UI.Maui)
/// whether the field has an error, its label as accessible name, its text as accessible hint and its size. Inside a <see cref="KlyForm"/>, <see cref="Field"/> names the property of the model whose validation message shows as the error.
/// </summary>
[ContentProperty(nameof(Content))]
public class KlyFormField : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyFormField), null, propertyChanged: (b, _, _) => ((KlyFormField)b).Refresh());
    /// <summary>Identifies <see cref="Hint"/>.</summary>
    public static readonly BindableProperty HintProperty = BindableProperty.Create(nameof(Hint), typeof(string), typeof(KlyFormField), null, propertyChanged: (b, _, _) => ((KlyFormField)b).Refresh());
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyFormField), null, propertyChanged: (b, _, _) => ((KlyFormField)b).Refresh());
    /// <summary>Identifies <see cref="Help"/>.</summary>
    public static readonly BindableProperty HelpProperty = BindableProperty.Create(nameof(Help), typeof(string), typeof(KlyFormField), null, propertyChanged: (b, _, _) => ((KlyFormField)b).Refresh());
    /// <summary>Identifies <see cref="Error"/>.</summary>
    public static readonly BindableProperty ErrorProperty = BindableProperty.Create(nameof(Error), typeof(string), typeof(KlyFormField), null, propertyChanged: (b, _, _) => ((KlyFormField)b).Refresh());
    /// <summary>Identifies <see cref="Field"/>.</summary>
    public static readonly BindableProperty FieldProperty = BindableProperty.Create(nameof(Field), typeof(string), typeof(KlyFormField), null, propertyChanged: (b, _, _) => ((KlyFormField)b).Refresh());
    /// <summary>Identifies <see cref="Required"/>.</summary>
    public static readonly BindableProperty RequiredProperty = BindableProperty.Create(nameof(Required), typeof(bool), typeof(KlyFormField), false, propertyChanged: (b, _, _) => ((KlyFormField)b).Refresh());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyFormField), KlySize.Md, propertyChanged: (b, _, _) => ((KlyFormField)b).Refresh());
    /// <summary>Identifies <see cref="Content"/>.</summary>
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(KlyFormField), null, propertyChanged: (b, _, v) => ((KlyFormField)b).ShowContent((View?)v));

    private readonly Label _label = new() { FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center };
    private readonly Label _hint = new() { VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.End };
    private readonly Grid _labelWrapper = new() { ColumnSpacing = 4, IsVisible = false };
    private readonly Label _description = new() { IsVisible = false };
    private readonly ContentView _container = new() { Margin = new Thickness(0, 4, 0, 0) };
    private readonly Label _message = new() { Margin = new Thickness(0, 8, 0, 0), IsVisible = false };
    private readonly ConditionalWeakTable<BindableObject, InputsFieldTaken> _taken = new();

    /// <summary>Creates a form field.</summary>
    public KlyFormField()
    {
        Spacing = 0;
        _labelWrapper.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        _labelWrapper.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _labelWrapper.Add(_label, 0);
        _labelWrapper.Add(_hint, 1);
        SemanticProperties.SetHeadingLevel(_label, SemanticHeadingLevel.None);

        // Tapping the label puts the focus in the control, as the label of a web field does.
        var focus = new TapGestureRecognizer();
        focus.Tapped += (_, _) => FocusControl();
        _label.GestureRecognizers.Add(focus);

        Children.Add(_labelWrapper);
        Children.Add(_description);
        Children.Add(_container);
        Children.Add(_message);
        Refresh();
    }

    /// <summary>Label of the control.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Short text next to the label, such as <c>Optional</c>.</summary>
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Text shown under the label, and read by a screen reader with the control.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Help text shown under the control while there is no error.</summary>
    public string? Help { get => (string?)GetValue(HelpProperty); set => SetValue(HelpProperty, value); }

    /// <summary>Error text. Without it, the first validation message of <see cref="Field"/> in the enclosing <see cref="KlyForm"/> is shown.</summary>
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>The name of the property of the model of the enclosing <see cref="KlyForm"/> that the control edits, to show its validation message.</summary>
    public string? Field { get => (string?)GetValue(FieldProperty); set => SetValue(FieldProperty, value); }

    /// <summary>Marks the label as required with an asterisk.</summary>
    public bool Required { get => (bool)GetValue(RequiredProperty); set => SetValue(RequiredProperty, value); }

    /// <summary>Size of the texts of the field, and of the control inside it when the control has no size of its own.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>The control, such as a <see cref="KlyInput"/>.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>The error text shown: <see cref="Error"/>, or the first validation message of <see cref="Field"/>.</summary>
    public string? ErrorText => !string.IsNullOrEmpty(Error)
        ? Error
        : Field is { Length: > 0 } name && InputsVisualTree.Ancestor<KlyForm>(this) is { } form ? form.GetMessages(name).FirstOrDefault() : null;

    internal Label LabelView => _label;

    internal Label HintView => _hint;

    internal Label DescriptionView => _description;

    internal Label MessageView => _message;

    internal ContentView Container => _container;

    /// <summary>Shows the texts and tells the control inside about the field: used when a property changes and by the form when its validation changes.</summary>
    internal void Refresh()
    {
        var fontSize = Size switch
        {
            KlySize.Xs or KlySize.Sm => 12d,
            KlySize.Md or KlySize.Lg => 14d,
            KlySize.Xl => 16d,
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };
        var error = KlyTheme.Resolve(KlyColor.Error);
        var errorText = ErrorText;

        // The required marker is an asterisk after the label, in the error text color.
        var text = new FormattedString();
        text.Spans.Add(new Span { Text = Label });
        if (Required)
        {
            var star = new Span { Text = " *" };
            star.SetThemeColor(Span.TextColorProperty, error.Text);
            text.Spans.Add(star);
        }
        _label.FormattedText = string.IsNullOrEmpty(Label) ? null : text;
        _label.FontSize = fontSize;
        _label.SetThemeColor(LabelTextColor, KlyTheme.Text);
        _hint.Text = Hint;
        _hint.FontSize = fontSize;
        _hint.SetThemeColor(LabelTextColor, KlyTheme.TextMuted);
        _labelWrapper.IsVisible = !string.IsNullOrEmpty(Label) || !string.IsNullOrEmpty(Hint);

        _description.Text = Description;
        _description.IsVisible = !string.IsNullOrEmpty(Description);
        _description.FontSize = fontSize;
        _description.SetThemeColor(LabelTextColor, KlyTheme.TextMuted);

        // An error replaces the help text.
        _message.Text = errorText ?? Help;
        _message.IsVisible = !string.IsNullOrEmpty(_message.Text);
        _message.FontSize = fontSize;
        _message.SetThemeColor(LabelTextColor, errorText is null ? KlyTheme.TextMuted : error.Text);

        var hint = string.Join(", ", new[] { Description, Hint, errorText ?? Help }.Where(t => !string.IsNullOrEmpty(t)));
        Apply(new InputsFieldState(errorText is not null, string.IsNullOrEmpty(Label) ? null : Label, hint.Length == 0 ? null : hint, IsSet(SizeProperty) ? Size : null));
    }

    /// <summary>The controls that take the state of the field: the content itself, and the controls inside it, without looking inside a control that handles the state of its own parts.</summary>
    internal IEnumerable<Element> Controls()
    {
        if (Content is null) yield break;
        yield return Content;
        if (Content is KlyFieldBase || Content is IInputsFieldTarget) yield break;
        foreach (var element in InputsVisualTree.Descendants(Content, e => e is not (KlyFieldBase or IInputsFieldTarget))) yield return element;
    }

    private void Apply(InputsFieldState state)
    {
        foreach (var element in Controls())
        {
            switch (element)
            {
                case KlyFieldBase field:
                    var taken = _taken.GetOrCreateValue(field);
                    taken.TakeInvalid(field, KlyFieldBase.InvalidProperty, state.Invalid);
                    taken.TakeName(field, KlyFieldBase.AccessibleNameProperty, state.Name);
                    taken.TakeSize(field, KlyFieldBase.SizeProperty, state.Size);
                    if (field.Content is { } inner) SemanticProperties.SetHint(inner, state.Hint);
                    break;
                case IInputsFieldTarget target:
                    target.ApplyField(state);
                    break;
            }
        }
    }

    private void ShowContent(View? content)
    {
        _container.Content = content;
        Refresh();
    }

    private void FocusControl()
    {
        foreach (var element in Controls())
        {
            var target = element is KlyFieldBase { Content: { } inner } ? inner : element as View;
            if (target is not null && target.IsEnabled && target.Focus()) return;
        }
    }

    /// <inheritdoc />
    protected override void OnParentSet()
    {
        base.OnParentSet();
        Refresh();
    }

    // Inside this class, Label is the property that holds the text: the property of a label that holds its color is reached by its full name.
    private static BindableProperty LabelTextColor => Microsoft.Maui.Controls.Label.TextColorProperty;
}
