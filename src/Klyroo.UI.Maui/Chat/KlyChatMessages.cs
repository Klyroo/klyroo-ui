using System.ComponentModel;
using Microsoft.Maui.Accessibility;
using Microsoft.Maui.Controls.Shapes;
using MauiLabel = Microsoft.Maui.Controls.Label;

namespace Klyroo.UI.Maui;

/// <summary>
/// A scrolling list of chat messages. It keeps the latest message in view while the user is at the bottom, stops following as soon as they scroll up
/// (a "jump to latest" button then appears), shows typing dots while an answer is on its way, and has a screen reader announce each assistant message once it is complete.
/// </summary>
[ContentProperty(nameof(Messages))]
public class KlyChatMessages : Grid
{
    private const string DotsName = "KlyChatMessagesDots";
    private const double PinThreshold = 24;

    /// <summary>Identifies <see cref="Status"/>.</summary>
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(nameof(Status), typeof(ChatStatus), typeof(KlyChatMessages), ChatStatus.Idle, propertyChanged: (b, o, v) => ((KlyChatMessages)b).ShowStatus((ChatStatus)o, (ChatStatus)v));
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyChatMessages), "Conversation", propertyChanged: (b, _, v) => SemanticProperties.SetDescription(((KlyChatMessages)b)._scroll, (string?)v));
    /// <summary>Identifies <see cref="AutoScroll"/>.</summary>
    public static readonly BindableProperty AutoScrollProperty = BindableProperty.Create(nameof(AutoScroll), typeof(bool), typeof(KlyChatMessages), true, propertyChanged: (b, _, _) => ((KlyChatMessages)b).ShowJump());
    /// <summary>Identifies <see cref="Indicator"/>.</summary>
    public static readonly BindableProperty IndicatorProperty = BindableProperty.Create(nameof(Indicator), typeof(View), typeof(KlyChatMessages), null, propertyChanged: (b, _, _) => ((KlyChatMessages)b).ShowStatus(ChatStatus.Idle, ((KlyChatMessages)b).Status));
    /// <summary>Identifies <see cref="Error"/>.</summary>
    public static readonly BindableProperty ErrorProperty = BindableProperty.Create(nameof(Error), typeof(View), typeof(KlyChatMessages), null, propertyChanged: (b, _, _) => ((KlyChatMessages)b).ShowStatus(ChatStatus.Idle, ((KlyChatMessages)b).Status));
    /// <summary>Identifies <see cref="ErrorText"/>.</summary>
    public static readonly BindableProperty ErrorTextProperty = BindableProperty.Create(nameof(ErrorText), typeof(string), typeof(KlyChatMessages), "Something went wrong. Try again.", propertyChanged: (b, _, v) => ((KlyChatMessages)b)._errorLabel.Text = (string?)v);
    /// <summary>Identifies <see cref="JumpToLatestLabel"/>.</summary>
    public static readonly BindableProperty JumpToLatestLabelProperty = BindableProperty.Create(nameof(JumpToLatestLabel), typeof(string), typeof(KlyChatMessages), "Jump to latest", propertyChanged: (b, _, v) => ((KlyChatMessages)b).ShowJumpLabel((string)v));
    /// <summary>Identifies <see cref="RespondingLabel"/>.</summary>
    public static readonly BindableProperty RespondingLabelProperty = BindableProperty.Create(nameof(RespondingLabel), typeof(string), typeof(KlyChatMessages), "Assistant is responding");

    private readonly ScrollView _scroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Default };
    private readonly VerticalStackLayout _content = new() { Spacing = 16, Padding = new Thickness(16) };
    private readonly VerticalStackLayout _messages = new() { Spacing = 16 };
    private readonly ContentView _indicatorHost = new() { IsVisible = false, HorizontalOptions = LayoutOptions.Start, Padding = new Thickness(4, 8) };
    private readonly HorizontalStackLayout _dots = new() { Spacing = 4 };
    private readonly Border[] _dotViews = [NewDot(), NewDot(), NewDot()];
    private readonly Border _errorBox = new() { StrokeThickness = 0, Padding = new Thickness(12, 8), IsVisible = false };
    private readonly MauiLabel _errorLabel = new() { FontSize = 14, Text = "Something went wrong. Try again." };
    private readonly Border _jump = new() { VerticalOptions = LayoutOptions.End, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 0, 12), IsVisible = false, StrokeThickness = 1 };
    private readonly MauiLabel _jumpLabel = new() { FontSize = 12, FontAttributes = FontAttributes.Bold, Text = "Jump to latest", VerticalTextAlignment = TextAlignment.Center };
    private readonly ChatDataTapArea _jumpTap;
    private bool _pinned = true;
    private bool _ready;

    /// <summary>Creates an empty list.</summary>
    public KlyChatMessages()
    {
        foreach (var dot in _dotViews) _dots.Children.Add(dot);
        _indicatorHost.Content = _dots;
        _errorBox.Content = _errorLabel;
        _errorBox.SetRadius(KlyTheme.RadiusOf(1));
        _errorBox.SetThemeColor(BackgroundColorProperty, KlyTheme.Resolve(KlyColor.Error).Fill.WithAlpha(0.1f));
        _errorLabel.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.Resolve(KlyColor.Error).Text);

        _content.Children.Add(_messages);
        _content.Children.Add(_indicatorHost);
        _content.Children.Add(_errorBox);
        _scroll.Content = _content;
        SemanticProperties.SetDescription(_scroll, Label);

        // The "jump to latest" pill: a native button over the text and the arrow, so it is a real button for the platform.
        var row = new HorizontalStackLayout { Spacing = 6, Padding = new Thickness(12, 0), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        row.Children.Add(new KlyIcon { Name = KlyIconNames.ArrowDown, Size = KlySize.Xs });
        row.Children.Add(_jumpLabel);
        _jumpLabel.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.Text);
        _jumpTap = new ChatDataTapArea(row) { Description = "Jump to latest" };
        _jumpTap.Tapped += (_, _) => JumpToLatest();
        _jump.Content = _jumpTap;
        _jump.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20) };
        _jump.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundElevated);
        _jump.SetStroke(KlyTheme.BorderAccented);

        Children.Add(_scroll);
        Children.Add(_jump);
        _scroll.Scrolled += (_, e) => UpdatePinned(e.ScrollY, _scroll.Height, _content.Height);
        _content.SizeChanged += (_, _) => FollowLatest();
        _messages.ChildAdded += (_, e) => OnMessageAdded(e.Element);
        _messages.ChildRemoved += (_, e) =>
        {
            if (e.Element is KlyChatMessage message) message.PropertyChanged -= OnMessageChanged;
        };
        ShowStatus(ChatStatus.Idle, Status);
    }

    /// <summary>The messages: <see cref="KlyChatMessage"/> items, in order.</summary>
    public IList<IView> Messages => _messages.Children;

    /// <summary>
    /// Where the conversation is. <c>Submitted</c> and <c>Streaming</c> show the typing indicator, <c>Error</c> shows <see cref="Error"/>.
    /// Update it as the request progresses.
    /// </summary>
    public ChatStatus Status { get => (ChatStatus)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }

    /// <summary>Accessible name of the list, in the language of the app.</summary>
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Follows the latest message while the user is at the bottom, and shows the "jump to latest" button once they scroll up. Without it the list never scrolls by itself.</summary>
    public bool AutoScroll { get => (bool)GetValue(AutoScrollProperty); set => SetValue(AutoScrollProperty, value); }

    /// <summary>Replaces the typing dots.</summary>
    public View? Indicator { get => (View?)GetValue(IndicatorProperty); set => SetValue(IndicatorProperty, value); }

    /// <summary>Replaces the message shown when <see cref="Status"/> is <c>Error</c>.</summary>
    public View? Error { get => (View?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Text of the message shown when <see cref="Status"/> is <c>Error</c> and there is no <see cref="Error"/> view.</summary>
    public string ErrorText { get => (string)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }

    /// <summary>Text of the "jump to latest" button.</summary>
    public string JumpToLatestLabel { get => (string)GetValue(JumpToLatestLabelProperty); set => SetValue(JumpToLatestLabelProperty, value); }

    /// <summary>What a screen reader announces when an answer starts to be written.</summary>
    public string RespondingLabel { get => (string)GetValue(RespondingLabelProperty); set => SetValue(RespondingLabelProperty, value); }

    /// <summary>True while the list is following the latest message: the user did not scroll away from the bottom.</summary>
    internal bool IsPinned => _pinned;

    /// <summary>True once the list is on screen: the messages that come after are announced, the ones that were there are not. Set by tests.</summary>
    internal bool Ready { get => _ready; set => _ready = value; }

    internal ScrollView Scroll => _scroll;
    internal Border JumpButton => _jump;
    internal ChatDataTapArea JumpTap => _jumpTap;
    internal ContentView IndicatorHost => _indicatorHost;
    internal HorizontalStackLayout Dots => _dots;
    internal Border ErrorBox => _errorBox;
    internal VerticalStackLayout MessageStack => _messages;

    /// <summary>What reaches the screen reader. Replaced in tests; the platform announces otherwise.</summary>
    internal Action<string> Announce { get; set; } = text =>
    {
        try
        {
            SemanticScreenReader.Default.Announce(text);
        }
        catch (FeatureNotSupportedException)
        {
            // No screen reader service on this platform: nothing to announce to.
        }
    };

    /// <summary>Tells whether the user is at the bottom: the viewport ends within a few units of the end of the content.</summary>
    internal static bool IsAtBottom(double scrollY, double viewport, double content) => scrollY + viewport >= content - PinThreshold;

    /// <summary>Follows the scroll position: scrolling away from the bottom stops the following and shows the jump button.</summary>
    internal void UpdatePinned(double scrollY, double viewport, double content)
    {
        _pinned = IsAtBottom(scrollY, viewport, content);
        ShowJump();
    }

    /// <summary>Scrolls to the latest message and follows it again.</summary>
    internal void JumpToLatest()
    {
        _pinned = true;
        ShowJump();
        ScrollToEnd(true);
    }

    /// <inheritdoc />
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        _ready = Handler is not null;
        SyncMotion();
    }

    private static Border NewDot() => new()
    {
        WidthRequest = 6,
        HeightRequest = 6,
        StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3) },
    };

    private void ShowJumpLabel(string text)
    {
        _jumpLabel.Text = text;
        _jumpTap.Description = text;
    }

    private void ShowJump() => _jump.IsVisible = AutoScroll && !_pinned;

    private void FollowLatest()
    {
        if (AutoScroll && _pinned) ScrollToEnd(false);
    }

    private void ScrollToEnd(bool animated)
    {
        if (_scroll.Handler is null || _content.Height <= 0) return;
        _ = _scroll.ScrollToAsync(0, _content.Height, animated);
    }

    private void OnMessageAdded(Element element)
    {
        if (element is not KlyChatMessage message) return;
        message.PropertyChanged += OnMessageChanged;
        // A message that arrives complete is announced, but the ones that were there when the page opened are not read out.
        if (_ready) AnnounceIfComplete(message);
    }

    private void OnMessageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(KlyChatMessage.Streaming) && sender is KlyChatMessage { Streaming: false } message) AnnounceIfComplete(message);
    }

    private void AnnounceIfComplete(KlyChatMessage message)
    {
        if (message.Role == ChatRole.Assistant && !message.Streaming && !string.IsNullOrWhiteSpace(message.Text)) Announce(message.Text);
    }

    private void ShowStatus(ChatStatus previous, ChatStatus status)
    {
        var busy = status is ChatStatus.Submitted or ChatStatus.Streaming;
        _indicatorHost.Content = Indicator ?? _dots;
        _indicatorHost.IsVisible = busy;
        foreach (var dot in _dotViews) dot.SetThemeColor(BackgroundColorProperty, KlyTheme.TextMuted);
        _errorBox.Content = Error ?? _errorLabel;
        _errorBox.IsVisible = status == ChatStatus.Error;
        _errorBox.Padding = Error is null ? new Thickness(12, 8) : new Thickness(0);
        if (_ready && previous != status)
        {
            if (status == ChatStatus.Submitted) Announce(RespondingLabel);
            else if (status == ChatStatus.Error && Error is null) Announce(ErrorText);
        }
        SyncMotion();
    }

    // The typing dots pulse one after the other while the indicator is shown.
    private void SyncMotion()
    {
        ChatDataMotion.Stop(_dots, DotsName);
        foreach (var dot in _dotViews) dot.Opacity = 1;
        if (Handler is not null && _indicatorHost.IsVisible && Indicator is null)
        {
            ChatDataMotion.Loop(_dots, DotsName, cycle =>
            {
                for (var i = 0; i < _dotViews.Length; i++) _dotViews[i].Opacity = ChatDataMotion.Pulse(cycle - i * 0.075);
            }, 2000);
        }
    }
}
