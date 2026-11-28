namespace Klyroo.UI.Maui;

/// <summary>
/// Shows the toasts added through <see cref="IToastService"/> (or <see cref="Show"/>): stacked at the bottom end of the page, newest last, at most five at a time,
/// each closing by itself after <see cref="Duration"/> (a pointer or a finger on it pauses the countdown), with its close button, or with a swipe sideways.
/// Place one in the page, inside a <see cref="KlyOverlayHost"/>; it takes no room in the layout, and the page stays usable under the toasts.
/// </summary>
public class KlyToaster : ContentView
{
    private const int MaxToasts = 5;
    private const double PanelWidth = 384;

    /// <summary>Identifies <see cref="Duration"/>.</summary>
    public static readonly BindableProperty DurationProperty = BindableProperty.Create(nameof(Duration), typeof(TimeSpan), typeof(KlyToaster), TimeSpan.FromSeconds(5));
    /// <summary>Identifies <see cref="Service"/>.</summary>
    public static readonly BindableProperty ServiceProperty = BindableProperty.Create(nameof(Service), typeof(KlyToastService), typeof(KlyToaster), null, propertyChanged: (b, o, _) => ((KlyToaster)b).Reattach((KlyToastService?)o));
    /// <summary>Identifies <see cref="CloseLabel"/>.</summary>
    public static readonly BindableProperty CloseLabelProperty = BindableProperty.Create(nameof(CloseLabel), typeof(string), typeof(KlyToaster), "Close");

    private readonly List<OverlaysToastView> _toasts = [];
    private KlyToastService? _attached;
    private VerticalStackLayout? _viewport;
    private KlyOverlayLayer? _layer;

    /// <summary>Creates a toaster.</summary>
    public KlyToaster()
    {
        InputTransparent = true;
        WidthRequest = 0;
        HeightRequest = 0;
        ParentChanged += (_, _) => Reattach(null);
    }

    /// <summary>How long a toast stays on screen when it does not set its own duration. Defaults to 5 seconds.</summary>
    public TimeSpan Duration { get => (TimeSpan)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }

    /// <summary>The service that feeds this toaster. Null uses <see cref="KlyToastService.Default"/>.</summary>
    public KlyToastService? Service { get => (KlyToastService?)GetValue(ServiceProperty); set => SetValue(ServiceProperty, value); }

    /// <summary>Accessible name of the close button of a toast, in the language of the app.</summary>
    public string CloseLabel { get => (string)GetValue(CloseLabelProperty); set => SetValue(CloseLabelProperty, value); }

    /// <summary>The toasts on screen, oldest first.</summary>
    public IReadOnlyList<KlyToast> Toasts => _toasts.Select(t => t.Toast).ToArray();

    /// <summary>The views of the toasts on screen, for the tests.</summary>
    internal IReadOnlyList<OverlaysToastView> Views => _toasts;

    /// <summary>The wait used by the countdown of a toast; the tests replace it to avoid a clock.</summary>
    internal Func<TimeSpan, CancellationToken, Task>? Delay { get; set; }

    /// <summary>Shows a toast. Called by <see cref="KlyToastService"/>; works from any thread.</summary>
    /// <param name="toast">The notification.</param>
    /// <exception cref="InvalidOperationException">The toaster is not inside a <see cref="KlyOverlayHost"/>. Thrown on the UI thread when the call comes from another one.</exception>
    public void Show(KlyToast toast)
    {
        ArgumentNullException.ThrowIfNull(toast);
        OverlaysSupport.Post(this, () => ShowCore(toast));
    }

    private void ShowCore(KlyToast toast)
    {
        var host = KlyOverlayHost.Find(this);
        var viewport = EnsureLayer(host);

        var view = new OverlaysToastView(toast, toast.Duration ?? Duration, CloseLabel, Delay);
        view.Closed += (_, _) => Remove(view);
        _toasts.Add(view);
        viewport.Add(view);
        while (_toasts.Count > MaxToasts) Remove(_toasts[0]);
        view.Start();

        OverlaysSupport.Announce(string.Join(". ", new[] { toast.Title, toast.Description }.Where(s => !string.IsNullOrEmpty(s))));
    }

    private VerticalStackLayout EnsureLayer(KlyOverlayHost host)
    {
        if (_layer is { IsShown: true } layer && ReferenceEquals(layer.Host, host) && _viewport is not null) return _viewport;

        var viewport = new VerticalStackLayout
        {
            Spacing = 8,
            Margin = new Thickness(16),
            WidthRequest = Math.Min(PanelWidth, host.Width > 0 ? host.Width - 32 : PanelWidth),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.End,
        };
        SemanticProperties.SetDescription(viewport, "Notifications");
        // The page below stays usable: the layer has no backdrop and takes no back button.
        _layer = host.Show(viewport, new KlyOverlayLayerOptions { Modal = false, Dismissible = false, Transition = KlyOverlayTransition.None });
        _layer.HostSizeChanged += (_, size) => viewport.WidthRequest = Math.Min(PanelWidth, Math.Max(0, size.Width - 32));
        _viewport = viewport;
        return viewport;
    }

    private void Remove(OverlaysToastView view)
    {
        if (!_toasts.Remove(view)) return;
        view.Stop();
        _viewport?.Remove(view);
        if (_toasts.Count == 0 && _layer is { } layer)
        {
            layer.Host.Hide(layer);
            _layer = null;
            _viewport = null;
        }
    }

    private void Reattach(KlyToastService? previous)
    {
        (previous ?? _attached)?.Detach(this);
        _attached = null;
        if (Parent is null) return;
        _attached = Service ?? KlyToastService.Default;
        _attached.Attach(this);
    }
}
