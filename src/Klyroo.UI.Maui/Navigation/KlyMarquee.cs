namespace Klyroo.UI.Maui;

/// <summary>
/// Content that scrolls by itself, in a loop. Give a single <see cref="Content"/> view and it crosses the control from one edge to the other and starts again;
/// give a <see cref="ContentTemplate"/> and the control builds the row once per copy (<see cref="Repeat"/>) for a seamless loop, with no gap at the join.
/// It pauses while the pointer hovers it (desktop), while <see cref="IsPaused"/> is set, and it is a timer on <c>TranslationX</c> or <c>TranslationY</c> while the control is on screen.
/// </summary>
[ContentProperty(nameof(Content))]
public class KlyMarquee : Grid
{
    /// <summary>Identifies <see cref="Content"/>.</summary>
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(KlyMarquee), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ContentTemplate"/>.</summary>
    public static readonly BindableProperty ContentTemplateProperty = BindableProperty.Create(nameof(ContentTemplate), typeof(DataTemplate), typeof(KlyMarquee), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyMarquee), StackOrientation.Horizontal, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Reverse"/>.</summary>
    public static readonly BindableProperty ReverseProperty = BindableProperty.Create(nameof(Reverse), typeof(bool), typeof(KlyMarquee), false);
    /// <summary>Identifies <see cref="PauseOnHover"/>.</summary>
    public static readonly BindableProperty PauseOnHoverProperty = BindableProperty.Create(nameof(PauseOnHover), typeof(bool), typeof(KlyMarquee), true);
    /// <summary>Identifies <see cref="Overlay"/>.</summary>
    public static readonly BindableProperty OverlayProperty = BindableProperty.Create(nameof(Overlay), typeof(bool), typeof(KlyMarquee), true, propertyChanged: (b, _, v) => ((KlyMarquee)b).ShowOverlay((bool)v));
    /// <summary>Identifies <see cref="Duration"/>.</summary>
    public static readonly BindableProperty DurationProperty = BindableProperty.Create(nameof(Duration), typeof(double), typeof(KlyMarquee), 30d);
    /// <summary>Identifies <see cref="Repeat"/>.</summary>
    public static readonly BindableProperty RepeatProperty = BindableProperty.Create(nameof(Repeat), typeof(int), typeof(KlyMarquee), 1, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Gap"/>.</summary>
    public static readonly BindableProperty GapProperty = BindableProperty.Create(nameof(Gap), typeof(double), typeof(KlyMarquee), 16d, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="IsPaused"/>.</summary>
    public static readonly BindableProperty IsPausedProperty = BindableProperty.Create(nameof(IsPaused), typeof(bool), typeof(KlyMarquee), false);

    /// <summary>The width or height of a fade at an edge.</summary>
    internal const double FadeLength = 48;

    private readonly View[] _fades = new View[2];
    private StackBase _track = new HorizontalStackLayout();
    private readonly List<View> _groups = [];
    private IDispatcherTimer? _timer;
    private DateTime _last;
    private TimeSpan _elapsed;
    private bool _hovered;

    /// <summary>Creates a marquee.</summary>
    public KlyMarquee()
    {
        IsClippedToBounds = true;
        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => _hovered = true;
        pointer.PointerExited += (_, _) => _hovered = false;
        GestureRecognizers.Add(pointer);
        HandlerChanged += (_, _) => UpdateTimer();
        for (var i = 0; i < _fades.Length; i++) _fades[i] = new BoxView { InputTransparent = true, Color = Colors.Transparent };
        Rebuild();
    }

    /// <summary>A view that crosses the control and starts again from the other edge. Used when no <see cref="ContentTemplate"/> is set.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>Builds the whole row once per copy, so the row can repeat with no gap. Replaces <see cref="Content"/>.</summary>
    public DataTemplate? ContentTemplate { get => (DataTemplate?)GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    /// <summary>Direction of the scrolling: <c>Horizontal</c> or <c>Vertical</c>.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Scrolls the other way.</summary>
    public bool Reverse { get => (bool)GetValue(ReverseProperty); set => SetValue(ReverseProperty, value); }

    /// <summary>Pauses while the pointer is over the control. A touch screen has no pointer: use <see cref="IsPaused"/>.</summary>
    public bool PauseOnHover { get => (bool)GetValue(PauseOnHoverProperty); set => SetValue(PauseOnHoverProperty, value); }

    /// <summary>Fades the edges into the page background.</summary>
    public bool Overlay { get => (bool)GetValue(OverlayProperty); set => SetValue(OverlayProperty, value); }

    /// <summary>Seconds for one full loop.</summary>
    public double Duration { get => (double)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }

    /// <summary>Number of extra copies of the row built from <see cref="ContentTemplate"/>. One is enough unless the row is narrower than the control.</summary>
    public int Repeat { get => (int)GetValue(RepeatProperty); set => SetValue(RepeatProperty, value); }

    /// <summary>Space between the copies of the row, in device-independent units.</summary>
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }

    /// <summary>Stops the motion while it is true. Set it for people who ask the system for less motion.</summary>
    public bool IsPaused { get => (bool)GetValue(IsPausedProperty); set => SetValue(IsPausedProperty, value); }

    internal bool Seamless => ContentTemplate is not null;

    internal IReadOnlyList<View> Groups => _groups;

    internal StackBase Track => _track;

    internal IReadOnlyList<View> Fades => _fades;

    internal bool IsHovered { get => _hovered; set => _hovered = value; }

    /// <summary>Lengths to use instead of the measured ones: for the tests, which have no layout.</summary>
    internal (double Container, double Group)? Measured { get; set; }

    /// <summary>
    /// Where the track sits, in device-independent units, at <paramref name="progress"/> of a loop (0 to 1).
    /// A seamless loop moves by one row and its gap; a single view moves from past one edge to past the other.
    /// </summary>
    internal static double Position(double progress, double container, double group, double gap, bool seamless, bool reverse)
    {
        var t = Math.Clamp(reverse ? 1 - progress : progress, 0, 1);
        if (seamless) return -t * (group + gap);
        return container + t * (-group - container);
    }

    /// <summary>Moves the animation on by a time span and places the track. Does nothing while paused or hovered.</summary>
    internal void Advance(TimeSpan elapsed)
    {
        if (IsPaused || (PauseOnHover && _hovered)) return;
        _elapsed += elapsed;
        Place();
    }

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyMarquee)bindable).Rebuild();

    private void Rebuild()
    {
        var vertical = Orientation == StackOrientation.Vertical;
        Children.Clear();
        _groups.Clear();
        _track = vertical ? new VerticalStackLayout { Spacing = Gap } : new HorizontalStackLayout { Spacing = Gap };
        _track.HorizontalOptions = LayoutOptions.Start;
        _track.VerticalOptions = LayoutOptions.Start;

        if (ContentTemplate is { } template)
        {
            for (var copy = 0; copy < 1 + Math.Max(0, Repeat); copy++)
            {
                if (template.CreateContent() is not View group) throw new InvalidOperationException("The ContentTemplate of a KlyMarquee must build a View.");
                // The copies are for the eye only: a screen reader reads the first.
                if (copy > 0) AutomationProperties.SetIsInAccessibleTree(group, false);
                _groups.Add(group);
                _track.Children.Add(group);
            }
        }
        else if (Content is { } content)
        {
            _groups.Add(content);
            _track.Children.Add(content);
        }

        Children.Add(_track);
        foreach (var fade in _fades) Children.Add(fade);
        ShowOverlay(Overlay);
        _elapsed = TimeSpan.Zero;
        Place();
        UpdateTimer();
    }

    private void ShowOverlay(bool overlay)
    {
        var vertical = Orientation == StackOrientation.Vertical;
        for (var i = 0; i < _fades.Length; i++)
        {
            var fade = _fades[i];
            fade.IsVisible = overlay;
            var atStart = i == 0;
            fade.HorizontalOptions = vertical ? LayoutOptions.Fill : atStart ? LayoutOptions.Start : LayoutOptions.End;
            fade.VerticalOptions = vertical ? (atStart ? LayoutOptions.Start : LayoutOptions.End) : LayoutOptions.Fill;
            fade.WidthRequest = vertical ? -1 : FadeLength;
            fade.HeightRequest = vertical ? FadeLength : -1;
            // From the page background at the edge to transparent inside; each theme has its own background.
            var from = atStart ? new Point(0, 0) : vertical ? new Point(0, 1) : new Point(1, 0);
            var to = atStart ? (vertical ? new Point(0, 1) : new Point(1, 0)) : new Point(0, 0);
            fade.SetAppTheme<Brush>(BackgroundProperty, Gradient(KlyTheme.Background.Light, from, to), Gradient(KlyTheme.Background.Dark, from, to));
        }
    }

    private static LinearGradientBrush Gradient(Color background, Point from, Point to) =>
        new([new GradientStop(background, 0f), new GradientStop(background.WithAlpha(0f), 1f)], from, to);

    private void Place()
    {
        var vertical = Orientation == StackOrientation.Vertical;
        if (_groups.Count == 0 || Duration <= 0)
        {
            _track.TranslationX = 0;
            _track.TranslationY = 0;
            return;
        }
        var container = Measured?.Container ?? (vertical ? Height : Width);
        var group = Measured?.Group ?? (vertical ? _groups[0].Height : _groups[0].Width);
        if (container <= 0 || group <= 0) return;
        var progress = _elapsed.TotalSeconds % Duration / Duration;
        var position = Position(progress, container, group, Gap, Seamless, Reverse);
        if (vertical) _track.TranslationY = position;
        else _track.TranslationX = position;
    }

    private void UpdateTimer()
    {
        if (Handler is null)
        {
            _timer?.Stop();
            _timer = null;
            return;
        }
        if (_timer is not null) return;
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _last = DateTime.UtcNow;
        _timer.Tick += (_, _) =>
        {
            var now = DateTime.UtcNow;
            var delta = now - _last;
            _last = now;
            Advance(delta);
        };
        _timer.Start();
    }
}
