namespace Klyroo.UI.Maui;

/// <summary>
/// A tool call of the assistant: its name, whether it runs or failed, and optionally what it was given and returned.
/// With <see cref="Content"/> the block is collapsible; without it, it is a plain line. <see cref="Streaming"/> makes the name shimmer;
/// <see cref="Actions"/> go below the name (an approval, say).
/// </summary>
[ContentProperty(nameof(Content))]
public class KlyChatTool : Grid
{
    private const string SpinnerName = "KlyChatToolSpinner";

    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyChatTool), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Suffix"/>.</summary>
    public static readonly BindableProperty SuffixProperty = BindableProperty.Create(nameof(Suffix), typeof(string), typeof(KlyChatTool), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Content"/>.</summary>
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(KlyChatTool), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(View), typeof(KlyChatTool), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Loading"/>.</summary>
    public static readonly BindableProperty LoadingProperty = BindableProperty.Create(nameof(Loading), typeof(bool), typeof(KlyChatTool), false, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Error"/>.</summary>
    public static readonly BindableProperty ErrorProperty = BindableProperty.Create(nameof(Error), typeof(bool), typeof(KlyChatTool), false, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Streaming"/>.</summary>
    public static readonly BindableProperty StreamingProperty = BindableProperty.Create(nameof(Streaming), typeof(bool), typeof(KlyChatTool), false, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(ChatToolVariant), typeof(KlyChatTool), ChatToolVariant.Inline, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Chevron"/>.</summary>
    public static readonly BindableProperty ChevronProperty = BindableProperty.Create(nameof(Chevron), typeof(ChatChevron), typeof(KlyChatTool), ChatChevron.Trailing, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Actions"/>.</summary>
    public static readonly BindableProperty ActionsProperty = BindableProperty.Create(nameof(Actions), typeof(View), typeof(KlyChatTool), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Open"/>.</summary>
    public static readonly BindableProperty OpenProperty = BindableProperty.Create(nameof(Open), typeof(bool), typeof(KlyChatTool), false, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlyChatTool)b).ShowOpen((bool)v));
    /// <summary>Identifies <see cref="RunningLabel"/>.</summary>
    public static readonly BindableProperty RunningLabelProperty = BindableProperty.Create(nameof(RunningLabel), typeof(string), typeof(KlyChatTool), "running", propertyChanged: Restyle);
    /// <summary>Identifies <see cref="FailedLabel"/>.</summary>
    public static readonly BindableProperty FailedLabelProperty = BindableProperty.Create(nameof(FailedLabel), typeof(string), typeof(KlyChatTool), "failed", propertyChanged: Restyle);

    private readonly Border _surface = new() { StrokeThickness = 0 };
    private readonly VerticalStackLayout _stack = new();
    private readonly Grid _row = new() { HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Center };
    private readonly KlyIcon _chevron = new() { Name = KlyIconNames.ChevronDown, Size = KlySize.Sm, VerticalOptions = LayoutOptions.Center };
    private readonly KlyIcon _status = new() { Size = KlySize.Sm, VerticalOptions = LayoutOptions.Center, IsVisible = false };
    private readonly ContentView _iconHost = new() { VerticalOptions = LayoutOptions.Center, IsVisible = false };
    private readonly Label _name = new() { FontSize = 14, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center };
    private readonly KlyChatShimmer _shimmer = new() { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center, IsVisible = false };
    private readonly Label _suffix = new() { FontSize = 14, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center, IsVisible = false };
    private readonly ChatDataTapArea _trigger;
    private readonly ContentView _actions = new() { IsVisible = false };
    private readonly Grid _body = new() { IsVisible = false };
    private readonly BoxView _leftBar = new() { WidthRequest = 2, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Fill };
    private readonly BoxView _topLine = new() { HeightRequest = 1 };
    private readonly ScrollView _scroll = new() { Orientation = ScrollOrientation.Both, MaximumHeightRequest = 200 };

    /// <summary>Creates a tool call.</summary>
    public KlyChatTool()
    {
        for (var i = 0; i < 6; i++) _row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _row.Children.Add(_chevron);
        _row.Children.Add(_status);
        _row.Children.Add(_iconHost);
        _row.Children.Add(_name);
        _row.Children.Add(_shimmer);
        _row.Children.Add(_suffix);
        _trigger = new ChatDataTapArea(_row) { HorizontalOptions = LayoutOptions.Start };
        _trigger.Tapped += (_, _) => Open = !Open;

        _body.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _body.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _body.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _body.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        _body.Children.Add(_topLine);
        _body.Children.Add(_leftBar);
        _body.Children.Add(_scroll);
        ChatDataGrid.ColumnSpan(_topLine, 2);
        ChatDataGrid.Row(_leftBar, 1);
        ChatDataGrid.Row(_scroll, 1);
        ChatDataGrid.Column(_scroll, 1);
        _topLine.SetThemeColor(BoxView.ColorProperty, KlyTheme.Border);
        _leftBar.SetThemeColor(BoxView.ColorProperty, KlyTheme.Border);

        _stack.Children.Add(_trigger);
        _stack.Children.Add(_actions);
        _stack.Children.Add(_body);
        _surface.Content = _stack;
        Children.Add(_surface);
        ApplyStyle();
    }

    /// <summary>Raised with the new state when the details open or close.</summary>
    public event EventHandler<ToggledEventArgs>? OpenChanged;

    /// <summary>The name of the tool call: <c>Searching the web</c>.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Quieter text after the name: the query, the file.</summary>
    public string? Suffix { get => (string?)GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }

    /// <summary>What the tool was given and returned, as a view. Setting it makes the block collapsible. Style its text yourself (small, muted, monospace): nothing is inherited from the block.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>A view, such as a <see cref="KlyIcon"/>, before the name, replaced by a spinner while <see cref="Loading"/> and by an alert on <see cref="Error"/>.</summary>
    public View? Icon { get => (View?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>The call is running: a spinner replaces the icon.</summary>
    public bool Loading { get => (bool)GetValue(LoadingProperty); set => SetValue(LoadingProperty, value); }

    /// <summary>The call failed: error colors, and the failure is read out after the name.</summary>
    public bool Error { get => (bool)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>The name shimmers, as the arguments or the output still stream in.</summary>
    public bool Streaming { get => (bool)GetValue(StreamingProperty); set => SetValue(StreamingProperty, value); }

    /// <summary>Look of the tool call: <c>Inline</c> or <c>Card</c>.</summary>
    public ChatToolVariant Variant { get => (ChatToolVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Side of the chevron that shows the state: <c>Trailing</c> or <c>Leading</c>.</summary>
    public ChatChevron Chevron { get => (ChatChevron)GetValue(ChevronProperty); set => SetValue(ChevronProperty, value); }

    /// <summary>Below the name, outside the collapsible part: approve and deny buttons.</summary>
    public View? Actions { get => (View?)GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

    /// <summary>Whether the details show. Two-way bindable.</summary>
    public bool Open { get => (bool)GetValue(OpenProperty); set => SetValue(OpenProperty, value); }

    /// <summary>What a screen reader adds after the name while the call runs, in the language of the app.</summary>
    public string RunningLabel { get => (string)GetValue(RunningLabelProperty); set => SetValue(RunningLabelProperty, value); }

    /// <summary>What a screen reader adds after the name when the call failed, in the language of the app.</summary>
    public string FailedLabel { get => (string)GetValue(FailedLabelProperty); set => SetValue(FailedLabelProperty, value); }

    internal Border Surface => _surface;
    internal ChatDataTapArea Trigger => _trigger;
    internal Grid Row => _row;
    internal Grid Body => _body;
    internal KlyIcon StatusIcon => _status;
    internal KlyIcon ChevronIcon => _chevron;
    internal Label NameLabel => _name;
    internal KlyChatShimmer NameShimmer => _shimmer;
    internal Label SuffixLabel => _suffix;
    internal ContentView ActionsHost => _actions;
    internal BoxView LeftBar => _leftBar;
    internal BoxView TopLine => _topLine;
    internal ScrollView BodyScroll => _scroll;
    internal bool Collapsible => Content is not null;

    /// <inheritdoc />
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        SyncMotion();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) _trigger.IsEnabled = IsEnabled;
    }

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlyChatTool)bindable).ApplyStyle();

    private void ShowOpen(bool open)
    {
        ApplyStyle();
        OpenChanged?.Invoke(this, new ToggledEventArgs(open));
    }

    private void ApplyStyle()
    {
        var card = Variant == ChatToolVariant.Card;
        var collapsible = Collapsible;
        var errorSet = KlyTheme.Resolve(KlyColor.Error);
        var muted = Error ? errorSet.Text : KlyTheme.TextMuted;

        // The block: a bordered card or a plain line.
        _surface.SetRadius(KlyTheme.RadiusOf(1));
        _surface.SetThemeColor(BackgroundColorProperty, card ? KlyTheme.BackgroundElevated.WithAlpha(0.5f) : KlyThemeColor.Transparent);
        _surface.SetStroke(KlyTheme.BorderAccented);
        _surface.StrokeThickness = card ? 1 : 0;
        HorizontalOptions = card ? LayoutOptions.Fill : LayoutOptions.Start;

        // The line: [chevron] [spinner or alert or icon] name suffix [chevron].
        var leading = Chevron == ChatChevron.Leading;
        _chevron.IsVisible = collapsible;
        _chevron.IconColor = KlyTheme.TextMuted;
        _chevron.Rotation = Open ? 180 : 0;
        _chevron.Margin = leading ? new Thickness(0, 0, 6, 0) : new Thickness(6, 0, 0, 0);
        ChatDataGrid.Column(_chevron, leading ? 0 : 5);
        _status.IsVisible = Loading || Error;
        _status.Name = Loading ? KlyIconNames.Loading : KlyIconNames.Caution;
        _status.IconColor = Error && !Loading ? errorSet.Text : KlyTheme.TextMuted;
        _status.Margin = new Thickness(0, 0, 6, 0);
        ChatDataGrid.Column(_status, 1);
        var custom = Icon is not null && !Loading && !Error;
        _iconHost.Content = custom ? Icon : null;
        _iconHost.IsVisible = custom;
        _iconHost.Margin = new Thickness(0, 0, 6, 0);
        ChatDataGrid.Column(_iconHost, 2);

        _name.Text = Text;
        _name.SetThemeColor(Label.TextColorProperty, Error ? errorSet.Text : KlyTheme.TextMuted);
        _name.IsVisible = !Streaming;
        _shimmer.Text = Text ?? string.Empty;
        _shimmer.IsVisible = Streaming;
        ChatDataGrid.Column(_name, 3);
        ChatDataGrid.Column(_shimmer, 3);
        _suffix.Text = Suffix;
        _suffix.IsVisible = !string.IsNullOrEmpty(Suffix);
        _suffix.Margin = new Thickness(6, 0, 0, 0);
        _suffix.SetThemeColor(Label.TextColorProperty, muted);
        ChatDataGrid.Column(_suffix, 4);

        var state = Loading ? RunningLabel : Error ? FailedLabel : null;
        var spoken = string.Join(' ', new[] { Text, Suffix }.Where(p => !string.IsNullOrEmpty(p)));
        _trigger.Description = state is null ? spoken : $"{spoken} ({state})";
        _trigger.Interactive = collapsible;
        _trigger.MinimumTarget = 40;
        _trigger.Margin = card ? new Thickness(12, 0) : new Thickness(0);
        _trigger.HorizontalOptions = LayoutOptions.Start;

        // Under the name: the actions, then the details.
        _actions.Content = Actions;
        _actions.IsVisible = Actions is not null;
        _actions.Margin = card ? new Thickness(12, 0, 12, 8) : new Thickness(0, 4);

        _scroll.Content = Content;
        _body.IsVisible = collapsible && Open;
        _leftBar.IsVisible = !card;
        _topLine.IsVisible = card;
        ChatDataGrid.Row(_topLine, 0);
        _scroll.Margin = card ? new Thickness(12) : new Thickness(12, 4, 0, 4);
        _body.Margin = card ? new Thickness(0) : new Thickness(0, 4);
        _scroll.Orientation = ScrollOrientation.Both;
        SyncMotion();
    }

    // The spinner of a running call turns while the block is on screen.
    private void SyncMotion()
    {
        ChatDataMotion.Stop(_status, SpinnerName);
        _status.Rotation = 0;
        if (Handler is not null && Loading) ChatDataMotion.Loop(_status, SpinnerName, value => _status.Rotation = value * 360, 1000);
    }
}
