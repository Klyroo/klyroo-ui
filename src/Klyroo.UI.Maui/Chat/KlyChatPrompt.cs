namespace Klyroo.UI.Maui;

/// <summary>
/// A message form: a multi-line native <see cref="Editor"/> that grows with its text up to <see cref="MaxRows"/>, a send button (stop while the answer is on its way, retry after an error),
/// room for attachments above and a footer below. Pressing the send button, or Enter on a hardware keyboard (Android), raises <see cref="Submitted"/> with the text and clears the field.
/// </summary>
public class KlyChatPrompt : Border
{
    /// <summary>The height of a line of the text area: the 20 units of the web <c>text-sm</c> line.</summary>
    private const double Line = 20;

    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyChatPrompt), null, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlyChatPrompt)b).ShowText((string?)v));
    /// <summary>Identifies <see cref="Placeholder"/>.</summary>
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(KlyChatPrompt), null, propertyChanged: (b, _, _) => ((KlyChatPrompt)b).ApplyStyle());
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyChatPrompt), null, propertyChanged: (b, _, _) => ((KlyChatPrompt)b).ApplyStyle());
    /// <summary>Identifies <see cref="Status"/>.</summary>
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(nameof(Status), typeof(ChatStatus), typeof(KlyChatPrompt), ChatStatus.Idle, propertyChanged: (b, _, _) => ((KlyChatPrompt)b).ApplyStyle());
    /// <summary>Identifies <see cref="SubmitOnEnter"/>.</summary>
    public static readonly BindableProperty SubmitOnEnterProperty = BindableProperty.Create(nameof(SubmitOnEnter), typeof(bool), typeof(KlyChatPrompt), true);
    /// <summary>Identifies <see cref="Rows"/>.</summary>
    public static readonly BindableProperty RowsProperty = BindableProperty.Create(nameof(Rows), typeof(int), typeof(KlyChatPrompt), 1, propertyChanged: (b, _, _) => ((KlyChatPrompt)b).ApplyStyle());
    /// <summary>Identifies <see cref="MaxRows"/>.</summary>
    public static readonly BindableProperty MaxRowsProperty = BindableProperty.Create(nameof(MaxRows), typeof(int), typeof(KlyChatPrompt), 6, propertyChanged: (b, _, _) => ((KlyChatPrompt)b).ApplyStyle());
    /// <summary>Identifies <see cref="Autofocus"/>.</summary>
    public static readonly BindableProperty AutofocusProperty = BindableProperty.Create(nameof(Autofocus), typeof(bool), typeof(KlyChatPrompt), false);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(FieldVariant), typeof(KlyChatPrompt), FieldVariant.Outline, propertyChanged: (b, _, _) => ((KlyChatPrompt)b).ApplyStyle());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyChatPrompt), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyChatPrompt)b).ApplyStyle());
    /// <summary>Identifies <see cref="Attachments"/>.</summary>
    public static readonly BindableProperty AttachmentsProperty = BindableProperty.Create(nameof(Attachments), typeof(View), typeof(KlyChatPrompt), null, propertyChanged: (b, _, _) => ((KlyChatPrompt)b).ApplyStyle());
    /// <summary>Identifies <see cref="Leading"/>.</summary>
    public static readonly BindableProperty LeadingProperty = BindableProperty.Create(nameof(Leading), typeof(View), typeof(KlyChatPrompt), null, propertyChanged: (b, _, _) => ((KlyChatPrompt)b).ApplyStyle());
    /// <summary>Identifies <see cref="Trailing"/>.</summary>
    public static readonly BindableProperty TrailingProperty = BindableProperty.Create(nameof(Trailing), typeof(View), typeof(KlyChatPrompt), null, propertyChanged: (b, _, _) => ((KlyChatPrompt)b).ApplyStyle());
    /// <summary>Identifies <see cref="Footer"/>.</summary>
    public static readonly BindableProperty FooterProperty = BindableProperty.Create(nameof(Footer), typeof(View), typeof(KlyChatPrompt), null, propertyChanged: (b, _, _) => ((KlyChatPrompt)b).ApplyStyle());

    private readonly Editor _editor = new() { AutoSize = EditorAutoSizeOption.TextChanges, BackgroundColor = Colors.Transparent, Margin = new Thickness(6), VerticalOptions = LayoutOptions.Center };
    private readonly ContentView _attachments = new() { IsVisible = false };
    private readonly ContentView _leading = new() { IsVisible = false, VerticalOptions = LayoutOptions.End };
    private readonly ContentView _trailing = new() { VerticalOptions = LayoutOptions.End };
    private readonly ContentView _footer = new() { IsVisible = false };
    private readonly KlyChatPromptSubmit _submit = new();
    private readonly VerticalStackLayout _form = new() { Spacing = 6 };
    private EventHandler? _stopped;
    private EventHandler? _reloaded;
    private bool _focused;

    /// <summary>Creates a prompt.</summary>
    public KlyChatPrompt()
    {
        _editor.TextChanged += (_, e) => SetValue(TextProperty, e.NewTextValue);
        _editor.Focused += (_, _) => SetFocused(true);
        _editor.Unfocused += (_, _) => SetFocused(false);
        _editor.HandlerChanged += (_, _) => PlatformTweaks.RemoveNativeFieldChrome(_editor);
        _ = new ChatDataEnterKey(_editor, HandleEnter);
        Loaded += (_, _) =>
        {
            if (Autofocus) _editor.Focus();
        };

        var row = new Grid { ColumnSpacing = 6, VerticalOptions = LayoutOptions.Fill };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.Children.Add(_leading);
        row.Children.Add(_editor);
        row.Children.Add(_trailing);
        ChatDataGrid.Column(_leading, 0);
        ChatDataGrid.Column(_editor, 1);
        ChatDataGrid.Column(_trailing, 2);
        _trailing.Content = _submit;

        _form.Children.Add(_attachments);
        _form.Children.Add(row);
        _form.Children.Add(_footer);
        Content = _form;
        Padding = new Thickness(8);
        this.SetRadius(12);
        HorizontalOptions = LayoutOptions.Fill;

        _submit.Clicked += (_, _) => Submit();
        _submit.Stopped += (_, e) => _stopped?.Invoke(this, e);
        _submit.Reloaded += (_, e) => _reloaded?.Invoke(this, e);
        ApplyStyle();
    }

    /// <summary>Raised with the text of the message when the user sends it. The field is cleared afterwards. Nothing is raised while the answer is on its way, the prompt is disabled, or there is neither text nor attachments.</summary>
    public event EventHandler<KlyChatPromptSubmittedEventArgs>? Submitted;

    /// <summary>Raised when the stop button is pressed. Without a handler the stop button is disabled.</summary>
    public event EventHandler? Stopped
    {
        add { _stopped += value; ApplyStyle(); }
        remove { _stopped -= value; ApplyStyle(); }
    }

    /// <summary>Raised when the retry button is pressed. Without a handler an error shows the send button.</summary>
    public event EventHandler? Reloaded
    {
        add { _reloaded += value; ApplyStyle(); }
        remove { _reloaded -= value; ApplyStyle(); }
    }

    /// <summary>The text of the field: a draft to start with, or what the user typed. Two-way bindable.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Hint shown in the text area while it is empty. Defaults to <c>Type a message...</c>.</summary>
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>Accessible name of the field. Defaults to <c>Message</c>: set it in the language of the app.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Where the conversation is. While <c>Submitted</c> or <c>Streaming</c> a message cannot be sent (the send button becomes a stop button); the field stays editable so the next message can be drafted.</summary>
    public ChatStatus Status { get => (ChatStatus)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }

    /// <summary>Enter sends and Shift+Enter adds a line. Set it to false and Ctrl or Cmd plus Enter sends instead. Only a hardware keyboard on Android is intercepted: the other keyboards add a line, and the send button sends.</summary>
    public bool SubmitOnEnter { get => (bool)GetValue(SubmitOnEnterProperty); set => SetValue(SubmitOnEnterProperty, value); }

    /// <summary>Rows of an empty field.</summary>
    public int Rows { get => (int)GetValue(RowsProperty); set => SetValue(RowsProperty, value); }

    /// <summary>The field grows with its text up to this many rows, then scrolls.</summary>
    public int MaxRows { get => (int)GetValue(MaxRowsProperty); set => SetValue(MaxRowsProperty, value); }

    /// <summary>Focuses the field when the control loads.</summary>
    public bool Autofocus { get => (bool)GetValue(AutofocusProperty); set => SetValue(AutofocusProperty, value); }

    /// <summary>Look of the prompt: <c>Outline</c>, <c>Soft</c>, <c>Subtle</c>, <c>Ghost</c> or <c>None</c>.</summary>
    public FieldVariant Variant { get => (FieldVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Semantic color of the focus ring.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Above the field: <see cref="KlyChatAttachment"/> items. A message may be sent with no text while this is set.</summary>
    public View? Attachments { get => (View?)GetValue(AttachmentsProperty); set => SetValue(AttachmentsProperty, value); }

    /// <summary>Before the field, in its row.</summary>
    public View? Leading { get => (View?)GetValue(LeadingProperty); set => SetValue(LeadingProperty, value); }

    /// <summary>Replaces the send button, after the field. An empty view hides it.</summary>
    public View? Trailing { get => (View?)GetValue(TrailingProperty); set => SetValue(TrailingProperty, value); }

    /// <summary>Below the field: buttons to attach files, a model picker.</summary>
    public View? Footer { get => (View?)GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    internal Editor Editor => _editor;
    internal KlyChatPromptSubmit SubmitButton => _submit;
    internal ContentView TrailingHost => _trailing;
    internal ContentView LeadingHost => _leading;
    internal ContentView AttachmentsHost => _attachments;
    internal ContentView FooterHost => _footer;

    /// <summary>Sends the message, unless the answer is on its way, the prompt is disabled, or there is neither text nor attachments.</summary>
    internal bool Submit()
    {
        var busy = Status is ChatStatus.Submitted or ChatStatus.Streaming;
        var empty = string.IsNullOrWhiteSpace(Text) && Attachments is null;
        if (busy || !IsEnabled || empty) return false;
        var text = Text ?? string.Empty;
        Submitted?.Invoke(this, new KlyChatPromptSubmittedEventArgs(text));
        Text = string.Empty;
        return true;
    }

    /// <summary>What the Enter key does: send, or leave the key to the editor for a new line. Returns true when the key was used.</summary>
    internal bool HandleEnter(bool shift, bool command)
    {
        if (SubmitOnEnter ? shift : !command) return false;
        Submit();
        return true;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web field: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled))
        {
            _editor.IsEnabled = IsEnabled;
            Opacity = IsEnabled ? 1 : 0.6;
            ApplyStyle();
        }
    }

    private void ShowText(string? text)
    {
        if (_editor.Text != text) _editor.Text = text;
    }

    private void SetFocused(bool focused)
    {
        _focused = focused;
        ApplyStyle();
    }

    private void ApplyStyle()
    {
        var set = KlyTheme.Resolve(Color);
        var (background, ring) = Variant switch
        {
            FieldVariant.Outline => (KlyTheme.Background, KlyTheme.BorderAccented),
            FieldVariant.Soft => (KlyTheme.BackgroundElevated.WithAlpha(0.5f), KlyThemeColor.Transparent),
            FieldVariant.Subtle => (KlyTheme.BackgroundElevated, KlyTheme.BorderAccented),
            FieldVariant.Ghost or FieldVariant.None => (KlyThemeColor.Transparent, KlyThemeColor.Transparent),
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null),
        };
        // A focused field gets a ring of the semantic color, and a tinted background where it has no border: all but the variant that asks for no ring.
        if (_focused && Variant != FieldVariant.None)
        {
            if (Variant is FieldVariant.Soft or FieldVariant.Ghost) background = KlyTheme.BackgroundElevated;
            this.SetStroke(set.Text);
            StrokeThickness = 2;
        }
        else
        {
            this.SetStroke(ring);
            StrokeThickness = ring == KlyThemeColor.Transparent ? 0 : 1;
        }
        this.SetThemeColor(BackgroundColorProperty, background);

        _editor.SetThemeColor(InputView.TextColorProperty, KlyTheme.Text);
        _editor.SetThemeColor(InputView.PlaceholderColorProperty, KlyTheme.TextDimmed);
        _editor.FontSize = 14;
        _editor.Placeholder = Placeholder ?? "Type a message...";
        SemanticProperties.SetDescription(_editor, Label ?? "Message");
        _editor.MinimumHeightRequest = Math.Max(1, Rows) * Line;
        _editor.MaximumHeightRequest = Math.Max(Math.Max(1, Rows), MaxRows) * Line;

        _attachments.Content = Attachments;
        _attachments.IsVisible = Attachments is not null;
        _leading.Content = Leading;
        _leading.IsVisible = Leading is not null;
        _trailing.Content = Trailing ?? _submit;
        _footer.Content = Footer;
        _footer.IsVisible = Footer is not null;

        _submit.ContextStatus = Status;
        _submit.ContextDisabled = !IsEnabled;
        _submit.StopHandled = _stopped is not null;
        _submit.ReloadHandled = _reloaded is not null;
        _submit.Refresh();
    }
}
