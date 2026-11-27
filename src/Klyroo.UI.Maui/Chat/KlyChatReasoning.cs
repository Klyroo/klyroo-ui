using System.Diagnostics;
using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// The model's thinking, as a collapsible block. It opens when <see cref="Streaming"/> starts and closes a moment after it ends, unless the user opened or closed it in between.
/// While streaming the label shimmers; afterwards it tells how long the thinking took. Put the reasoning text, as a view, in <see cref="Content"/>.
/// </summary>
[ContentProperty(nameof(Content))]
public class KlyChatReasoning : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyChatReasoning), null, propertyChanged: (b, _, _) => ((KlyChatReasoning)b).Refresh());
    /// <summary>Identifies <see cref="Content"/>.</summary>
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(KlyChatReasoning), null, propertyChanged: (b, _, v) => ((KlyChatReasoning)b)._scroll.Content = (View?)v);
    /// <summary>Identifies <see cref="Streaming"/>.</summary>
    public static readonly BindableProperty StreamingProperty = BindableProperty.Create(nameof(Streaming), typeof(bool), typeof(KlyChatReasoning), false, propertyChanged: (b, o, v) => ((KlyChatReasoning)b).ShowStreaming((bool)o, (bool)v));
    /// <summary>Identifies <see cref="Duration"/>.</summary>
    public static readonly BindableProperty DurationProperty = BindableProperty.Create(nameof(Duration), typeof(int?), typeof(KlyChatReasoning), null, propertyChanged: (b, _, _) => ((KlyChatReasoning)b).Refresh());
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(View), typeof(KlyChatReasoning), null, propertyChanged: (b, _, _) => ((KlyChatReasoning)b).Refresh());
    /// <summary>Identifies <see cref="Chevron"/>.</summary>
    public static readonly BindableProperty ChevronProperty = BindableProperty.Create(nameof(Chevron), typeof(ChatChevron), typeof(KlyChatReasoning), ChatChevron.Trailing, propertyChanged: (b, _, _) => ((KlyChatReasoning)b).Refresh());
    /// <summary>Identifies <see cref="AutoOpen"/>.</summary>
    public static readonly BindableProperty AutoOpenProperty = BindableProperty.Create(nameof(AutoOpen), typeof(bool), typeof(KlyChatReasoning), true);
    /// <summary>Identifies <see cref="AutoCloseDelay"/>.</summary>
    public static readonly BindableProperty AutoCloseDelayProperty = BindableProperty.Create(nameof(AutoCloseDelay), typeof(int), typeof(KlyChatReasoning), 500);
    /// <summary>Identifies <see cref="Open"/>.</summary>
    public static readonly BindableProperty OpenProperty = BindableProperty.Create(nameof(Open), typeof(bool), typeof(KlyChatReasoning), false, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyChatReasoning)b).ShowOpen((bool)v));
    /// <summary>Identifies <see cref="ThinkingText"/>.</summary>
    public static readonly BindableProperty ThinkingTextProperty = BindableProperty.Create(nameof(ThinkingText), typeof(string), typeof(KlyChatReasoning), "Thinking...", propertyChanged: (b, _, _) => ((KlyChatReasoning)b).Refresh());
    /// <summary>Identifies <see cref="ThoughtText"/>.</summary>
    public static readonly BindableProperty ThoughtTextProperty = BindableProperty.Create(nameof(ThoughtText), typeof(string), typeof(KlyChatReasoning), "Thought", propertyChanged: (b, _, _) => ((KlyChatReasoning)b).Refresh());
    /// <summary>Identifies <see cref="ThoughtForFormat"/>.</summary>
    public static readonly BindableProperty ThoughtForFormatProperty = BindableProperty.Create(nameof(ThoughtForFormat), typeof(string), typeof(KlyChatReasoning), "Thought for {0} seconds", propertyChanged: (b, _, _) => ((KlyChatReasoning)b).Refresh());

    private readonly Grid _row = new() { HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Center };
    private readonly KlyIcon _chevron = new() { Name = KlyIconNames.ChevronDown, Size = KlySize.Sm, VerticalOptions = LayoutOptions.Center };
    private readonly ContentView _iconHost = new() { VerticalOptions = LayoutOptions.Center, IsVisible = false };
    private readonly Label _label = new() { FontSize = 14, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center };
    private readonly KlyChatShimmer _shimmer = new() { LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center, IsVisible = false };
    private readonly ChatDataTapArea _trigger;
    private readonly Grid _body = new() { IsVisible = false, Margin = new Thickness(0, 4) };
    private readonly BoxView _bar = new() { WidthRequest = 2, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Fill };
    private readonly ScrollView _scroll = new() { Orientation = ScrollOrientation.Vertical, MaximumHeightRequest = 200, Margin = new Thickness(12, 4, 0, 4) };
    private long _started;
    private int? _measured;
    private bool _userToggled;
    private bool _autoOpened;

    /// <summary>Creates a reasoning block.</summary>
    public KlyChatReasoning()
    {
        _shimmer.FontSize = 14;
        _row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _row.Children.Add(_chevron);
        _row.Children.Add(_iconHost);
        _row.Children.Add(_label);
        _row.Children.Add(_shimmer);
        _trigger = new ChatDataTapArea(_row) { HorizontalOptions = LayoutOptions.Start };
        _trigger.Tapped += (_, _) =>
        {
            _userToggled = true;
            Open = !Open;
        };

        _body.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _body.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        _body.Children.Add(_bar);
        _body.Children.Add(_scroll);
        ChatDataGrid.Column(_scroll, 1);
        _bar.SetThemeColor(BoxView.ColorProperty, KlyTheme.Border);

        Children.Add(_trigger);
        Children.Add(_body);
        Refresh();
    }

    /// <summary>Raised with the new state when the block opens or closes, by the user or by the streaming state.</summary>
    public event EventHandler<ToggledEventArgs>? OpenChanged;

    /// <summary>The label while it streams, and afterwards when no duration is known. Defaults to <see cref="ThinkingText"/> while streaming and <see cref="ThoughtText"/> after.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>The reasoning, as a view. Style its text yourself: nothing is inherited from the block.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>The block is still receiving its content: it shimmers and, with <see cref="AutoOpen"/>, opens.</summary>
    public bool Streaming { get => (bool)GetValue(StreamingProperty); set => SetValue(StreamingProperty, value); }

    /// <summary>Seconds the thinking took, for the finished label. Without it the time between the start and the end of streaming is measured here.</summary>
    public int? Duration { get => (int?)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }

    /// <summary>A view, such as a <see cref="KlyIcon"/>, before the label.</summary>
    public View? Icon { get => (View?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Side of the chevron that shows the state: <c>Trailing</c> or <c>Leading</c>.</summary>
    public ChatChevron Chevron { get => (ChatChevron)GetValue(ChevronProperty); set => SetValue(ChevronProperty, value); }

    /// <summary>Opens when streaming starts. The user's own toggle in the meantime wins over the automatic close.</summary>
    public bool AutoOpen { get => (bool)GetValue(AutoOpenProperty); set => SetValue(AutoOpenProperty, value); }

    /// <summary>Milliseconds after streaming ends before it closes by itself. 0 keeps it open.</summary>
    public int AutoCloseDelay { get => (int)GetValue(AutoCloseDelayProperty); set => SetValue(AutoCloseDelayProperty, value); }

    /// <summary>Whether the reasoning shows. Two-way bindable.</summary>
    public bool Open { get => (bool)GetValue(OpenProperty); set => SetValue(OpenProperty, value); }

    /// <summary>Label while it streams, when <see cref="Text"/> is not set, in the language of the app.</summary>
    public string ThinkingText { get => (string)GetValue(ThinkingTextProperty); set => SetValue(ThinkingTextProperty, value); }

    /// <summary>Label once it has finished, when <see cref="Text"/> is not set and no duration is known, in the language of the app.</summary>
    public string ThoughtText { get => (string)GetValue(ThoughtTextProperty); set => SetValue(ThoughtTextProperty, value); }

    /// <summary>Label once it has finished with a known duration: <c>{0}</c> is the number of seconds, in the language of the app.</summary>
    public string ThoughtForFormat { get => (string)GetValue(ThoughtForFormatProperty); set => SetValue(ThoughtForFormatProperty, value); }

    /// <summary>The label as it stands.</summary>
    internal string LabelText => Streaming ? _shimmer.Text : _label.Text;
    internal ChatDataTapArea Trigger => _trigger;
    internal KlyIcon ChevronIcon => _chevron;
    internal Grid Body => _body;
    internal Label PlainLabel => _label;
    internal KlyChatShimmer Shimmer => _shimmer;
    internal Grid Row => _row;

    /// <summary>The clock that measures the thinking. Replaced in tests.</summary>
    internal Func<long> Timestamp { get; set; } = Stopwatch.GetTimestamp;

    /// <summary>Runs an action after a delay. Replaced in tests; the dispatcher of the control otherwise.</summary>
    internal Action<TimeSpan, Action>? Delay { get; set; }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) _trigger.IsEnabled = IsEnabled;
    }

    private void ShowOpen(bool open)
    {
        _body.IsVisible = open;
        _chevron.Rotation = open ? 180 : 0;
        OpenChanged?.Invoke(this, new ToggledEventArgs(open));
    }

    private void ShowStreaming(bool was, bool streaming)
    {
        if (streaming && !was)
        {
            _started = Timestamp();
            _measured = null;
            if (AutoOpen)
            {
                _userToggled = false;
                _autoOpened = !Open;
                Open = true;
            }
        }
        else if (!streaming && was)
        {
            _measured = Math.Max(1, (int)Math.Round(Stopwatch.GetElapsedTime(_started, Timestamp()).TotalSeconds));
            if (AutoOpen && _autoOpened && AutoCloseDelay > 0) Schedule(TimeSpan.FromMilliseconds(AutoCloseDelay), CloseAfterStreaming);
        }
        Refresh();
    }

    private void CloseAfterStreaming()
    {
        if (!_userToggled && !Streaming) Open = false;
        _autoOpened = false;
    }

    private void Schedule(TimeSpan delay, Action action)
    {
        if (Delay is { } custom) custom(delay, action);
        else Dispatcher.DispatchDelayed(delay, action);
    }

    private void Refresh()
    {
        var seconds = Duration ?? _measured;
        var label = Streaming ? Text ?? ThinkingText
            : seconds is { } s ? string.Format(CultureInfo.CurrentCulture, ThoughtForFormat, s)
            : Text ?? ThoughtText;
        _label.Text = label;
        _label.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
        _shimmer.Text = label;
        _label.IsVisible = !Streaming;
        _shimmer.IsVisible = Streaming;
        _trigger.Description = label;
        _chevron.IconColor = KlyTheme.TextMuted;

        _iconHost.Content = Icon;
        _iconHost.IsVisible = Icon is not null;

        // The chevron before or after the label; the gaps are margins so a part that is not there leaves none.
        var leading = Chevron == ChatChevron.Leading;
        ChatDataGrid.Column(_chevron, leading ? 0 : 3);
        ChatDataGrid.Column(_iconHost, 1);
        ChatDataGrid.Column(_label, 2);
        ChatDataGrid.Column(_shimmer, 2);
        _chevron.Margin = leading ? new Thickness(0, 0, 6, 0) : new Thickness(6, 0, 0, 0);
        _iconHost.Margin = new Thickness(0, 0, 6, 0);
    }
}
