namespace Klyroo.UI.Maui;

/// <summary>One toast: a colored strip, a title and a description, a close button. It closes by itself after its duration (paused while a pointer or a finger is on it), and by a swipe sideways.</summary>
internal sealed class OverlaysToastView : Border
{
    /// <summary>How far a swipe must travel to dismiss the toast, in points.</summary>
    public const double SwipeDistance = 80;

    private readonly OverlaysCountdown? _countdown;
    private double _pan;
    private bool _closed;

    public OverlaysToastView(KlyToast toast, TimeSpan duration, string closeLabel, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        Toast = toast;
        this.ApplySurface(KlyTheme.RadiusOf(1));
        Padding = 0;

        var strip = new BoxView { WidthRequest = 4 };
        strip.SetThemeColor(BoxView.ColorProperty, KlyTheme.Resolve(toast.Color).Fill);
        AutomationProperties.SetIsInAccessibleTree(strip, false);

        var texts = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        if (!string.IsNullOrEmpty(toast.Title))
        {
            var title = new Label { Text = toast.Title, FontSize = 14, FontAttributes = FontAttributes.Bold };
            title.SetThemeColor(Label.TextColorProperty, KlyTheme.Text);
            texts.Add(title);
        }
        if (!string.IsNullOrEmpty(toast.Description))
        {
            var description = new Label { Text = toast.Description, FontSize = 14 };
            description.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
            texts.Add(description);
        }

        var close = new OverlaysCloseButton(closeLabel) { Margin = new Thickness(-8) };
        close.Clicked += (_, _) => Close();

        var inner = new Grid { Padding = new Thickness(16), ColumnSpacing = 10 };
        inner.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        inner.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        inner.Add(texts, 0);
        inner.Add(close, 1);

        var grid = new Grid { ColumnSpacing = 0 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.Add(strip, 0);
        grid.Add(inner, 1);
        Content = grid;

        // Said by a screen reader as a whole, and announced when the toast appears.
        SemanticProperties.SetDescription(this, string.Join(". ", new[] { toast.Title, toast.Description }.Where(s => !string.IsNullOrEmpty(s))));

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += (_, e) => OnPan(e.StatusType, e.TotalX);
        GestureRecognizers.Add(pan);
        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => _countdown?.Pause();
        pointer.PointerExited += (_, _) => _countdown?.Resume();
        GestureRecognizers.Add(pointer);

        if (duration > TimeSpan.Zero) _countdown = new OverlaysCountdown(duration, () => OverlaysSupport.Post(this, Close), delay);
        Loaded += async (_, _) => await AppearAsync();
    }

    public KlyToast Toast { get; }

    /// <summary>True once the toast has been closed, by its timer, its button or a swipe.</summary>
    public bool IsClosed => _closed;

    /// <summary>True while the auto-dismiss countdown runs.</summary>
    public bool CountdownRunning => _countdown?.IsRunning ?? false;

    /// <summary>Raised once, when the toast has closed and may be removed from the toaster.</summary>
    public event EventHandler? Closed;

    /// <summary>Starts the countdown of a toast that has a duration.</summary>
    public void Start() => _countdown?.Start();

    /// <summary>Stops the countdown, for a toast that is removed without closing.</summary>
    public void Stop() => _countdown?.Stop();

    /// <summary>Closes the toast: it fades out, then <see cref="Closed"/> is raised. Does nothing once closed.</summary>
    public void Close() => _ = CloseAsync(0);

    /// <summary>Follows a finger: the toast moves with it and fades, and a swipe of <see cref="SwipeDistance"/> closes it; a shorter one springs back.</summary>
    public void OnPan(GestureStatus status, double totalX)
    {
        if (_closed) return;
        switch (status)
        {
            case GestureStatus.Started:
                _pan = 0;
                _countdown?.Pause();
                break;
            case GestureStatus.Running:
                // Some platforms report the totals of a finished pan as zero, so the last running value decides.
                _pan = totalX;
                TranslationX = totalX;
                Opacity = Math.Max(0.3, 1 - Math.Abs(totalX) / (Width > 0 ? Width : 320));
                break;
            default:
                if (Math.Abs(_pan) >= SwipeDistance)
                {
                    _ = CloseAsync(Math.Sign(_pan));
                }
                else
                {
                    TranslationX = 0;
                    Opacity = 1;
                    _countdown?.Resume();
                }
                _pan = 0;
                break;
        }
    }

    private async Task CloseAsync(int direction)
    {
        if (_closed) return;
        _closed = true;
        _countdown?.Stop();
        if (Handler is not null)
        {
            // Out of the way a swipe pointed to, or down and faded when the timer or the button closed it.
            var move = direction == 0
                ? this.TranslateToAsync(TranslationX, TranslationY + 8, 180, Easing.CubicIn)
                : this.TranslateToAsync(direction * (Width > 0 ? Width : 320), TranslationY, 180, Easing.CubicIn);
            await Task.WhenAll(move, this.FadeToAsync(0, 180));
        }
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private async Task AppearAsync()
    {
        if (_closed || Handler is null) return;
        TranslationY = 8;
        Opacity = 0;
        await Task.WhenAll(this.TranslateToAsync(0, 0, 200, Easing.CubicOut), this.FadeToAsync(1, 200));
    }
}
