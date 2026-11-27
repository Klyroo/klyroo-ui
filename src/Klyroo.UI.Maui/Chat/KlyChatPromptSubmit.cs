namespace Klyroo.UI.Maui;

/// <summary>
/// The button of a chat prompt, by status: send while idle, stop while the answer is on its way, retry after an error.
/// Inside a <see cref="KlyChatPrompt"/> it follows the prompt; alone it follows <see cref="Status"/>. Without a <see cref="Stopped"/> handler the stop button is disabled
/// (nothing could happen); without a <see cref="Reloaded"/> handler an error shows the send button, since a retry that does nothing is worse than a button that sends again.
/// </summary>
public class KlyChatPromptSubmit : Grid
{
    /// <summary>Identifies <see cref="Status"/>.</summary>
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(nameof(Status), typeof(ChatStatus?), typeof(KlyChatPromptSubmit), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyChatPromptSubmit), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor?), typeof(KlyChatPromptSubmit), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyChatPromptSubmit), KlySize.Md, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="SendLabel"/>.</summary>
    public static readonly BindableProperty SendLabelProperty = BindableProperty.Create(nameof(SendLabel), typeof(string), typeof(KlyChatPromptSubmit), "Send", propertyChanged: Restyle);
    /// <summary>Identifies <see cref="StopLabel"/>.</summary>
    public static readonly BindableProperty StopLabelProperty = BindableProperty.Create(nameof(StopLabel), typeof(string), typeof(KlyChatPromptSubmit), "Stop", propertyChanged: Restyle);
    /// <summary>Identifies <see cref="RetryLabel"/>.</summary>
    public static readonly BindableProperty RetryLabelProperty = BindableProperty.Create(nameof(RetryLabel), typeof(string), typeof(KlyChatPromptSubmit), "Retry", propertyChanged: Restyle);

    private readonly ChatDataIconButton _button = new();
    private EventHandler? _stopped;
    private EventHandler? _reloaded;
    private ChatStatus _contextStatus = ChatStatus.Idle;
    private bool _contextDisabled;

    /// <summary>Creates a button.</summary>
    public KlyChatPromptSubmit()
    {
        Children.Add(_button);
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.End;
        _button.Clicked += OnClicked;
        ApplyStyle();
    }

    /// <summary>Raised when the button is pressed while idle: the message is to be sent.</summary>
    public event EventHandler? Clicked;

    /// <summary>Raised when the button is pressed while the answer is submitted or streaming.</summary>
    public event EventHandler? Stopped
    {
        add { _stopped += value; ApplyStyle(); }
        remove { _stopped -= value; ApplyStyle(); }
    }

    /// <summary>Raised when the button is pressed after an error.</summary>
    public event EventHandler? Reloaded
    {
        add { _reloaded += value; ApplyStyle(); }
        remove { _reloaded -= value; ApplyStyle(); }
    }

    /// <summary>The status to show. Defaults to the one of the enclosing <see cref="KlyChatPrompt"/>, else idle.</summary>
    public ChatStatus? Status { get => (ChatStatus?)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }

    /// <summary>Accessible name. Defaults to <see cref="SendLabel"/>, <see cref="StopLabel"/> or <see cref="RetryLabel"/> for the status.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>The color of the send button. The stop and retry buttons keep their own. Defaults to primary.</summary>
    public KlyColor? Color { get => (KlyColor?)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the button, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Accessible name of the send button, in the language of the app.</summary>
    public string SendLabel { get => (string)GetValue(SendLabelProperty); set => SetValue(SendLabelProperty, value); }

    /// <summary>Accessible name of the stop button, in the language of the app.</summary>
    public string StopLabel { get => (string)GetValue(StopLabelProperty); set => SetValue(StopLabelProperty, value); }

    /// <summary>Accessible name of the retry button, in the language of the app.</summary>
    public string RetryLabel { get => (string)GetValue(RetryLabelProperty); set => SetValue(RetryLabelProperty, value); }

    /// <summary>The status of the prompt around the button.</summary>
    internal ChatStatus ContextStatus { get => _contextStatus; set { _contextStatus = value; ApplyStyle(); } }

    /// <summary>The prompt around the button is disabled.</summary>
    internal bool ContextDisabled { get => _contextDisabled; set { _contextDisabled = value; ApplyStyle(); } }

    /// <summary>Set by the prompt, which forwards stopping: whether anyone listens to it. When null the button looks at its own <see cref="Stopped"/>.</summary>
    internal bool? StopHandled { get; set; }

    /// <summary>Set by the prompt, which forwards retrying: whether anyone listens to it. When null the button looks at its own <see cref="Reloaded"/>.</summary>
    internal bool? ReloadHandled { get; set; }

    internal ChatDataIconButton Button => _button;

    /// <summary>The status the button shows: the one given or the one of the prompt, with an error that nobody can retry shown as idle.</summary>
    internal ChatStatus EffectiveStatus
    {
        get
        {
            var status = Status ?? _contextStatus;
            return status == ChatStatus.Error && !HasReload ? ChatStatus.Idle : status;
        }
    }

    /// <summary>Re-reads the handlers the prompt forwards to.</summary>
    internal void Refresh() => ApplyStyle();

    private bool HasStop => StopHandled ?? _stopped is not null;
    private bool HasReload => ReloadHandled ?? _reloaded is not null;

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlyChatPromptSubmit)bindable).ApplyStyle();

    private void OnClicked(object? sender, EventArgs e)
    {
        switch (EffectiveStatus)
        {
            case ChatStatus.Idle:
                Clicked?.Invoke(this, EventArgs.Empty);
                break;
            case ChatStatus.Error:
                _reloaded?.Invoke(this, EventArgs.Empty);
                break;
            default:
                _stopped?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) ApplyStyle();
    }

    private void ApplyStyle()
    {
        var status = EffectiveStatus;
        var busy = status is ChatStatus.Submitted or ChatStatus.Streaming;
        switch (status)
        {
            case ChatStatus.Idle:
                _button.Color = Color ?? KlyColor.Primary;
                _button.Variant = ButtonVariant.Solid;
                _button.IconName = KlyIconNames.ArrowUp;
                _button.IconSize = KlySize.Md;
                _button.Description = Label ?? SendLabel;
                break;
            case ChatStatus.Error:
                _button.Color = KlyColor.Error;
                _button.Variant = ButtonVariant.Soft;
                _button.IconName = KlyIconNames.Reload;
                _button.IconSize = KlySize.Md;
                _button.Description = Label ?? RetryLabel;
                break;
            default:
                _button.Color = KlyColor.Neutral;
                _button.Variant = ButtonVariant.Subtle;
                _button.IconName = KlyIconNames.Stop;
                _button.IconSize = KlySize.Xs;
                _button.Description = Label ?? StopLabel;
                break;
        }

        // The size follows the web button: the square of the height of its size.
        _button.Dimension = Size switch
        {
            KlySize.Xs => 28,
            KlySize.Sm => 32,
            KlySize.Md => 40,
            KlySize.Lg => 44,
            KlySize.Xl => 48,
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };

        // Stop without a handler would be an enabled button that does nothing.
        _button.IsEnabled = IsEnabled && !_contextDisabled && !(busy && !HasStop);
    }
}
