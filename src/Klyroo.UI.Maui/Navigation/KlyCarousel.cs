using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// Slides that snap into place: swipe the slides, or use the previous and next buttons and the dots. It is a row of slides that follows the finger (a pan) and settles
/// on a slide with <c>TranslateTo</c>, so any view can be a slide. Autoplay stops while the pointer or a finger is on it and has a pause button.
/// </summary>
public class KlyCarousel : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyCarouselItem>), typeof(KlyCarousel), Array.Empty<KlyCarouselItem>(), propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Index"/>.</summary>
    public static readonly BindableProperty IndexProperty = BindableProperty.Create(nameof(Index), typeof(int), typeof(KlyCarousel), 0, BindingMode.TwoWay, propertyChanged: (b, o, n) => ((KlyCarousel)b).OnIndexChanged((int)n));
    /// <summary>Identifies <see cref="Loop"/>.</summary>
    public static readonly BindableProperty LoopProperty = BindableProperty.Create(nameof(Loop), typeof(bool), typeof(KlyCarousel), false, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AutoPlay"/>.</summary>
    public static readonly BindableProperty AutoPlayProperty = BindableProperty.Create(nameof(AutoPlay), typeof(int), typeof(KlyCarousel), 0, propertyChanged: (b, _, _) => { ((KlyCarousel)b).Rebuild(); });
    /// <summary>Identifies <see cref="Dots"/>.</summary>
    public static readonly BindableProperty DotsProperty = BindableProperty.Create(nameof(Dots), typeof(bool), typeof(KlyCarousel), true, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Arrows"/>.</summary>
    public static readonly BindableProperty ArrowsProperty = BindableProperty.Create(nameof(Arrows), typeof(bool), typeof(KlyCarousel), true, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyCarousel), KlyColor.Primary, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyCarousel), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyCarousel)b, (string?)v));
    /// <summary>Identifies <see cref="PreviousSlideLabel"/>.</summary>
    public static readonly BindableProperty PreviousSlideLabelProperty = BindableProperty.Create(nameof(PreviousSlideLabel), typeof(string), typeof(KlyCarousel), "Previous slide", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="NextSlideLabel"/>.</summary>
    public static readonly BindableProperty NextSlideLabelProperty = BindableProperty.Create(nameof(NextSlideLabel), typeof(string), typeof(KlyCarousel), "Next slide", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="PauseSlidesLabel"/>.</summary>
    public static readonly BindableProperty PauseSlidesLabelProperty = BindableProperty.Create(nameof(PauseSlidesLabel), typeof(string), typeof(KlyCarousel), "Pause automatic slide show", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="PlaySlidesLabel"/>.</summary>
    public static readonly BindableProperty PlaySlidesLabelProperty = BindableProperty.Create(nameof(PlaySlidesLabel), typeof(string), typeof(KlyCarousel), "Start automatic slide show", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="GoToSlideLabel"/>.</summary>
    public static readonly BindableProperty GoToSlideLabelProperty = BindableProperty.Create(nameof(GoToSlideLabel), typeof(string), typeof(KlyCarousel), "Go to slide {0}", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="SlideOfLabel"/>.</summary>
    public static readonly BindableProperty SlideOfLabelProperty = BindableProperty.Create(nameof(SlideOfLabel), typeof(string), typeof(KlyCarousel), "{0} of {1}", propertyChanged: Rebuild);

    private readonly Grid _viewport = new() { IsClippedToBounds = true };
    private readonly HorizontalStackLayout _track = new() { Spacing = 0, HorizontalOptions = LayoutOptions.Start };
    private readonly HorizontalStackLayout _controls = new() { Spacing = 8, HorizontalOptions = LayoutOptions.Center };
    private readonly List<ContentView> _slides = [];
    private readonly List<NavigationButton> _dots = [];
    private NavigationButton? _previous;
    private NavigationButton? _next;
    private NavigationButton? _toggle;
    private IDispatcherTimer? _timer;
    private double _width;
    private double _panStart;
    private double _panTotal;
    private bool _panning;
    private bool _hovered;
    private bool _userPaused;

    /// <summary>Creates a carousel.</summary>
    public KlyCarousel()
    {
        Spacing = 12;
        _viewport.Children.Add(_track);
        Children.Add(_viewport);
        Children.Add(_controls);
        _viewport.SizeChanged += (_, _) => ApplyWidth(_viewport.Width);

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += (_, e) => OnPan(e);
        _viewport.GestureRecognizers.Add(pan);
        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => _hovered = true;
        pointer.PointerExited += (_, _) => _hovered = false;
        _viewport.GestureRecognizers.Add(pointer);

        HandlerChanged += (_, _) => UpdateTimer();
        Rebuild();
    }

    /// <summary>Raised with the index of the visible slide when it changes, by the user or by code.</summary>
    public event EventHandler<int>? IndexChanged;

    /// <summary>The slides, as <see cref="KlyCarouselItem"/>.</summary>
    public IReadOnlyList<KlyCarouselItem> Items { get => (IReadOnlyList<KlyCarouselItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>Zero-based index of the current slide. Two-way bindable.</summary>
    public int Index { get => (int)GetValue(IndexProperty); set => SetValue(IndexProperty, value); }

    /// <summary>Wrap from the last slide to the first and back with the buttons, a swipe and autoplay.</summary>
    public bool Loop { get => (bool)GetValue(LoopProperty); set => SetValue(LoopProperty, value); }

    /// <summary>Milliseconds between automatic slide changes. Zero (the default) turns autoplay off. It stops while a pointer or a finger is on the carousel and has a pause button.</summary>
    public int AutoPlay { get => (int)GetValue(AutoPlayProperty); set => SetValue(AutoPlayProperty, value); }

    /// <summary>Show a button for each slide.</summary>
    public bool Dots { get => (bool)GetValue(DotsProperty); set => SetValue(DotsProperty, value); }

    /// <summary>Show the previous and next buttons.</summary>
    public bool Arrows { get => (bool)GetValue(ArrowsProperty); set => SetValue(ArrowsProperty, value); }

    /// <summary>Semantic color of the current dot.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>What a screen reader says for the carousel.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>What a screen reader says for the previous button.</summary>
    public string PreviousSlideLabel { get => (string)GetValue(PreviousSlideLabelProperty); set => SetValue(PreviousSlideLabelProperty, value); }

    /// <summary>What a screen reader says for the next button.</summary>
    public string NextSlideLabel { get => (string)GetValue(NextSlideLabelProperty); set => SetValue(NextSlideLabelProperty, value); }

    /// <summary>What a screen reader says for the button that pauses autoplay.</summary>
    public string PauseSlidesLabel { get => (string)GetValue(PauseSlidesLabelProperty); set => SetValue(PauseSlidesLabelProperty, value); }

    /// <summary>What a screen reader says for the button that starts autoplay.</summary>
    public string PlaySlidesLabel { get => (string)GetValue(PlaySlidesLabelProperty); set => SetValue(PlaySlidesLabelProperty, value); }

    /// <summary>What a screen reader says for a dot; <c>{0}</c> is the number of the slide.</summary>
    public string GoToSlideLabel { get => (string)GetValue(GoToSlideLabelProperty); set => SetValue(GoToSlideLabelProperty, value); }

    /// <summary>What a screen reader says for a slide that has no label; <c>{0}</c> is its number and <c>{1}</c> the count.</summary>
    public string SlideOfLabel { get => (string)GetValue(SlideOfLabelProperty); set => SetValue(SlideOfLabelProperty, value); }

    /// <summary>The current slide kept inside the list.</summary>
    internal int Current => Math.Clamp(Index, 0, Math.Max(Items.Count - 1, 0));

    internal IReadOnlyList<ContentView> Slides => _slides;

    internal IReadOnlyList<NavigationButton> DotButtons => _dots;

    internal NavigationButton? PreviousButton => _previous;

    internal NavigationButton? NextButton => _next;

    internal NavigationButton? ToggleButton => _toggle;

    internal HorizontalStackLayout Track => _track;

    internal bool AutoPlayRunning => !_userPaused && AutoPlay > 0;

    /// <summary>The index a step from <paramref name="current"/> leads to, or <paramref name="current"/> when the carousel cannot go that way.</summary>
    internal static int Step(int current, int delta, int count, bool loop)
    {
        if (count <= 0) return 0;
        var target = current + delta;
        if (loop) return (target % count + count) % count;
        return Math.Clamp(target, 0, count - 1);
    }

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyCarousel)bindable).Rebuild();

    private void OnIndexChanged(int index)
    {
        Refresh();
        Show(animated: true);
        IndexChanged?.Invoke(this, index);
    }

    private void Rebuild()
    {
        var items = Items;
        _track.Children.Clear();
        _slides.Clear();
        for (var i = 0; i < items.Count; i++)
        {
            var label = items[i].Label ?? string.Format(CultureInfo.CurrentCulture, SlideOfLabel, i + 1, items.Count);
            var slide = new ContentView { Content = items[i].Content, WidthRequest = _width > 0 ? _width : -1 };
            SemanticProperties.SetDescription(slide, label);
            _slides.Add(slide);
            _track.Children.Add(slide);
        }
        BuildControls();
        Refresh();
        Show(animated: false);
        UpdateTimer();
    }

    private NavigationButton RoundButton(string icon, string description)
    {
        var button = new NavigationButton();
        button.Face.Padding = new Thickness(8);
        button.Face.SetRadius(20);
        button.Face.Apply(new Surface(KlyTheme.Background, KlyTheme.BorderAccented, 1, KlyTheme.Text));
        button.Face.Content = NavigationLayout.Icon(icon, KlySize.Sm, KlyTheme.Text);
        SemanticProperties.SetDescription(button, description);
        return button;
    }

    private void BuildControls()
    {
        _controls.Children.Clear();
        _dots.Clear();
        _previous = _next = _toggle = null;
        var items = Items;

        if (AutoPlay > 0)
        {
            _toggle = RoundButton(KlyIconNames.Pause, PauseSlidesLabel);
            _toggle.Clicked += (_, _) =>
            {
                _userPaused = !_userPaused;
                UpdateToggle();
                UpdateTimer();
            };
            _controls.Children.Add(_toggle);
            UpdateToggle();
        }
        if (Arrows)
        {
            _previous = RoundButton(KlyIconNames.ChevronLeft, PreviousSlideLabel);
            _previous.Clicked += (_, _) => Go(-1);
            _controls.Children.Add(_previous);
        }
        if (Dots)
        {
            var dots = new HorizontalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
            for (var i = 0; i < items.Count; i++)
            {
                var index = i;
                var dot = new NavigationButton();
                dot.Face.WidthRequest = 12;
                dot.Face.HeightRequest = 12;
                dot.Face.SetRadius(6);
                dot.Clicked += (_, _) => Index = index;
                SemanticProperties.SetDescription(dot, string.Format(CultureInfo.CurrentCulture, GoToSlideLabel, i + 1));
                _dots.Add(dot);
                dots.Children.Add(dot);
            }
            _controls.Children.Add(dots);
        }
        if (Arrows)
        {
            _next = RoundButton(KlyIconNames.ChevronRight, NextSlideLabel);
            _next.Clicked += (_, _) => Go(1);
            _controls.Children.Add(_next);
        }
        _controls.IsVisible = _controls.Children.Count > 0;
    }

    private void UpdateToggle()
    {
        if (_toggle is null) return;
        var icon = (KlyIcon)_toggle.Face.Content!;
        icon.Name = _userPaused ? KlyIconNames.Play : KlyIconNames.Pause;
        SemanticProperties.SetDescription(_toggle, _userPaused ? PlaySlidesLabel : PauseSlidesLabel);
    }

    /// <summary>Styles the dots and the end buttons for the current slide.</summary>
    private void Refresh()
    {
        var set = KlyTheme.Resolve(Color);
        var current = Current;
        for (var i = 0; i < _dots.Count; i++)
        {
            var isCurrent = i == current;
            _dots[i].Face.Apply(isCurrent
                ? new Surface(set.Fill, set.Text, 1, set.Text)
                : new Surface(KlyTheme.BackgroundAccented, KlyTheme.BorderAccented, 1, KlyTheme.Text));
        }
        var count = Items.Count;
        if (_previous is not null) _previous.IsEnabled = Loop ? count > 1 : current > 0;
        if (_next is not null) _next.IsEnabled = Loop ? count > 1 : current < count - 1;
    }

    private void Go(int delta)
    {
        var target = Step(Current, delta, Items.Count, Loop);
        if (target != Current) Index = target;
    }

    /// <summary>Sizes the slides to the width of the viewport and puts the track on the current slide.</summary>
    internal void ApplyWidth(double width)
    {
        if (width <= 0) return;
        _width = width;
        foreach (var slide in _slides) slide.WidthRequest = width;
        Show(animated: false);
    }

    private void Show(bool animated)
    {
        if (_width <= 0 || _panning) return;
        var target = -Current * _width;
        _track.AbortAnimation("TranslateTo");
        if (!animated || Handler is null) _track.TranslationX = target;
        else _ = _track.TranslateToAsync(target, 0, 250, Easing.CubicOut);
    }

    private void OnPan(PanUpdatedEventArgs e)
    {
        if (_width <= 0 || Items.Count == 0) return;
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panning = true;
                _track.AbortAnimation("TranslateTo");
                _panStart = _track.TranslationX;
                _panTotal = 0;
                break;
            case GestureStatus.Running when _panning:
                Drag(e.TotalX);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                Release(e.StatusType == GestureStatus.Completed ? _panTotal : 0);
                break;
        }
    }

    /// <summary>Moves the track with the finger. Without <see cref="Loop"/> the first and last slides resist.</summary>
    internal void Drag(double distance)
    {
        _panning = true;
        _panTotal = distance;
        var min = -(Items.Count - 1) * _width;
        var position = _panStart + distance;
        if (!Loop && (position > 0 || position < min)) position = _panStart + distance / 3;
        _track.TranslationX = position;
    }

    /// <summary>Ends a pan: a pull of a quarter of the width or more moves to the next slide, anything less settles back.</summary>
    internal void Release(double moved)
    {
        _panning = false;
        var threshold = _width / 4;
        var delta = moved <= -threshold ? 1 : moved >= threshold ? -1 : 0;
        var target = Step(Current, delta, Items.Count, Loop);
        if (target != Current) Index = target;
        else Show(animated: true);
    }

    /// <summary>Starts a pan at the current position, for the tests.</summary>
    internal void BeginPan()
    {
        _panning = true;
        _panStart = _track.TranslationX;
    }

    private void UpdateTimer()
    {
        _timer?.Stop();
        _timer = null;
        if (Handler is null || AutoPlay <= 0 || _userPaused) return;
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(AutoPlay);
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    /// <summary>One automatic step: does nothing while a pointer or a finger is on the carousel, and stops on the last slide without <see cref="Loop"/>.</summary>
    internal void Tick()
    {
        if (_panning || _hovered || _userPaused) return;
        Go(1);
    }
}
