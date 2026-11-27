namespace Klyroo.UI.Maui;

/// <summary>
/// Wraps the content of a page and owns the layers drawn above it: dialogs, panels, popovers, menus, tooltips and toasts.
/// Every overlay control of the library (<see cref="KlyModal"/>, <see cref="KlyPopover"/>, <see cref="KlyToaster"/>...) walks up the visual tree to the nearest host
/// and puts its layer there, so place one around the content of each page (or of the shell page) and the controls anywhere inside it:
/// <code>&lt;kly:KlyOverlayHost&gt;&lt;VerticalStackLayout&gt;...&lt;kly:KlyModal IsOpen="{Binding Open}" /&gt;&lt;/VerticalStackLayout&gt;&lt;/kly:KlyOverlayHost&gt;</code>
/// A modal layer has a backdrop that catches the touches outside the content, optionally dimmed and dismissing on tap, and hides the page from screen readers.
/// On Android the back button dismisses the top layer; call <see cref="DismissTop"/> from your own back handling on other platforms.
/// You can also use the host directly with <see cref="Show"/>, <see cref="ShowAsync{TResult}"/> and <see cref="ConfirmAsync"/>.
/// </summary>
[ContentProperty(nameof(Content))]
public class KlyOverlayHost : Grid
{
    /// <summary>Identifies <see cref="Content"/>.</summary>
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(KlyOverlayHost), null, propertyChanged: (b, o, n) => ((KlyOverlayHost)b).ReplaceContent((View?)o, (View?)n));

    private readonly List<KlyOverlayLayer> _layers = [];
    private OverlaysBackButton? _back;
    private bool _pageHidden;
    private bool? _pageExcluded;

    /// <summary>Creates a host.</summary>
    public KlyOverlayHost()
    {
        SizeChanged += (_, _) => OnHostResized();
        Loaded += (_, _) => RegisterBackButton();
        Unloaded += (_, _) => UnregisterBackButton();
    }

    /// <summary>Raised when a layer is added to or removed from <see cref="Layers"/>.</summary>
    public event EventHandler? LayersChanged;

    /// <summary>The page content the layers are drawn above.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>The layers that are shown, from the bottom to the top.</summary>
    public IReadOnlyList<KlyOverlayLayer> Layers => _layers;

    /// <summary>The host a view is in: the nearest <see cref="KlyOverlayHost"/> among its parents.</summary>
    /// <param name="from">The view to start from.</param>
    /// <exception cref="InvalidOperationException">There is none: wrap the page content in <see cref="KlyOverlayHost"/>.</exception>
    public static KlyOverlayHost Find(Element from) => TryFind(from, out var host) ? host : throw MissingHost(from);

    internal static InvalidOperationException MissingHost(Element from) => new(
        $"{from.GetType().Name} needs a KlyOverlayHost above it: wrap the page content in KlyOverlayHost, so the overlay controls have a layer to draw in.");

    /// <summary>Looks for the nearest <see cref="KlyOverlayHost"/> among the parents of a view.</summary>
    /// <param name="from">The view to start from.</param>
    /// <param name="host">The host found.</param>
    /// <returns>True when the view is inside a host.</returns>
    public static bool TryFind(Element? from, out KlyOverlayHost host)
    {
        for (var element = from; element is not null; element = element.Parent)
        {
            if (element is KlyOverlayHost found)
            {
                host = found;
                return true;
            }
        }
        host = null!;
        return false;
    }

    /// <summary>Shows a view above the page.</summary>
    /// <param name="content">The view. A view that is in another parent is taken out of it first.</param>
    /// <param name="options">Backdrop, dismissal, transition and anchor of the layer.</param>
    /// <returns>The layer, to hide it or to follow its dismissal.</returns>
    public KlyOverlayLayer Show(View content, KlyOverlayLayerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        options ??= new KlyOverlayLayerOptions();
        OverlaysSupport.Detach(content);

        var layer = new KlyOverlayLayer(this, content, options);
        var container = new Grid { HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill };
        layer.Container = container;
        if (options.Modal)
        {
            var backdrop = new BoxView { Color = Colors.Transparent };
            if (options.Dim) backdrop.SetThemeColor(BoxView.ColorProperty, OverlaysTheme.Scrim);
            AutomationProperties.SetIsInAccessibleTree(backdrop, false);
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                if (layer.Dismissible) Dismiss(layer);
            };
            backdrop.GestureRecognizers.Add(tap);
            layer.Backdrop = backdrop;
            container.Add(backdrop);
        }
        else
        {
            // The page below stays usable: only the content of the layer takes touches.
            container.InputTransparent = true;
            container.CascadeInputTransparent = false;
        }

        if (options.Anchor is not null)
        {
            content.HorizontalOptions = LayoutOptions.Start;
            content.VerticalOptions = LayoutOptions.Start;
            content.Margin = new Thickness(0);
            content.Opacity = 0;
            layer.ContentSizeChanged = (_, _) => Reposition(layer);
            content.SizeChanged += layer.ContentSizeChanged;
        }
        container.Add(content);
        Children.Add(container);

        layer.IsShown = true;
        _layers.Add(layer);
        UpdateState();
        if (Width > 0) layer.RaiseHostSizeChanged(new Size(Width, Height));
        Reposition(layer);
        RunTransition(layer, entering: true);
        return layer;
    }

    /// <summary>Removes a layer without telling it was dismissed: the way a control closes its own layer.</summary>
    /// <param name="layer">The layer to remove.</param>
    /// <returns>False when the layer was not shown.</returns>
    public bool Hide(KlyOverlayLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (!_layers.Remove(layer)) return false;
        layer.IsShown = false;
        if (layer.ContentSizeChanged is not null) layer.Content.SizeChanged -= layer.ContentSizeChanged;
        UpdateState();
        RunTransition(layer, entering: false);
        return true;
    }

    /// <summary>Removes a layer because the user dismissed it, and raises <see cref="KlyOverlayLayer.Dismissed"/>. Works on a layer that is not <see cref="KlyOverlayLayer.Dismissible"/>.</summary>
    /// <param name="layer">The layer to dismiss.</param>
    /// <returns>False when the layer was not shown.</returns>
    public bool Dismiss(KlyOverlayLayer layer)
    {
        if (!Hide(layer)) return false;
        layer.RaiseDismissed();
        return true;
    }

    /// <summary>
    /// What a back button does: dismisses the top layer that can be dismissed. A modal layer that is not dismissible takes the press and keeps the dialog open;
    /// layers that are neither (tooltips, toasts) are skipped. On Android the host calls it by itself.
    /// </summary>
    /// <returns>True when the press was taken, so the app must not navigate back.</returns>
    public bool DismissTop()
    {
        for (var i = _layers.Count - 1; i >= 0; i--)
        {
            var layer = _layers[i];
            if (layer.Dismissible)
            {
                Dismiss(layer);
                return true;
            }
            if (layer.Options.Modal) return true;
        }
        return false;
    }

    /// <summary>The bounds of a view in the coordinates of the host, once the view has been laid out. It follows scrolling.</summary>
    /// <param name="view">A view inside the host.</param>
    /// <exception cref="InvalidOperationException">The view is not inside this host.</exception>
    public Rect BoundsOf(View view)
    {
        double x = 0, y = 0;
        Element? current = view;
        while (current is not null && !ReferenceEquals(current, this))
        {
            if (current is VisualElement element)
            {
                x += element.X + element.TranslationX;
                y += element.Y + element.TranslationY;
            }
            if (current.Parent is ScrollView scroll)
            {
                x -= scroll.ScrollX;
                y -= scroll.ScrollY;
            }
            current = current.Parent;
        }
        if (current is null) throw new InvalidOperationException("The view is not inside this KlyOverlayHost.");
        return new Rect(x, y, Math.Max(view.Width, 0), Math.Max(view.Height, 0));
    }

    /// <summary>
    /// Shows a dialog built by <paramref name="content"/> and completes when it ends: with the value the view passes to <see cref="KlyOverlayContext{TResult}.Close"/>,
    /// or canceled after a tap outside, the back button, the close button, <see cref="KlyOverlayContext{TResult}.Cancel"/> or <paramref name="cancellationToken"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the value the dialog completes with. A dialog with nothing to return uses <c>bool</c>.</typeparam>
    /// <param name="content">Builds the body of the dialog from its context.</param>
    /// <param name="options">Title, size and behavior of the dialog.</param>
    /// <param name="cancellationToken">Cancels the dialog (it closes and the result is canceled).</param>
    public Task<KlyOverlayResult<TResult>> ShowAsync<TResult>(Func<KlyOverlayContext<TResult>, View> content, KlyOverlayOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        return ShowCore<TResult>(options ?? new KlyOverlayOptions(), context => (content(context), null), cancellationToken);
    }

    /// <summary>Asks a yes or no question in a dialog and returns true when the user confirms, false when they cancel or dismiss it.</summary>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="message">Text under the title.</param>
    /// <param name="confirmLabel">Label of the confirm button. Defaults to "Confirm".</param>
    /// <param name="cancelLabel">Label of the cancel button. Defaults to "Cancel".</param>
    /// <param name="color">Color of the confirm button; use <see cref="KlyColor.Error"/> for a destructive action. Defaults to primary.</param>
    /// <param name="cancellationToken">Cancels the dialog (it closes and the answer is false).</param>
    public async Task<bool> ConfirmAsync(string title, string? message = null, string? confirmLabel = null, string? cancelLabel = null, KlyColor? color = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(title);
        var options = new KlyOverlayOptions { Title = title, Description = message };
        var result = await ShowCore<bool>(options, context =>
        {
            var cancel = new KlyButton { Text = cancelLabel ?? "Cancel", Variant = ButtonVariant.Outline, Color = KlyColor.Neutral };
            var confirm = new KlyButton { Text = confirmLabel ?? "Confirm", Color = color ?? KlyColor.Primary };
            cancel.Clicked += (_, _) => context.Cancel();
            confirm.Clicked += (_, _) => context.Close(true);
            return (null, new HorizontalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.End, Children = { cancel, confirm } });
        }, cancellationToken);
        return !result.Canceled && result.Value;
    }

    private Task<KlyOverlayResult<TResult>> ShowCore<TResult>(KlyOverlayOptions options, Func<KlyOverlayContext<TResult>, (View? Body, View? Footer)> build, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return Task.FromResult(new KlyOverlayResult<TResult>(true, default));

        var completion = new TaskCompletionSource<KlyOverlayResult<TResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
        KlyOverlayLayer? layer = null;
        void End(bool canceled, TResult? value)
        {
            if (!completion.TrySetResult(new KlyOverlayResult<TResult>(canceled, value))) return;
            if (layer is not null) Hide(layer);
        }

        var context = new KlyOverlayContext<TResult>(value => End(false, value), () => End(true, default), () => completion.Task.IsCompleted);
        var (body, footer) = build(context);
        var panel = new OverlaysPanel
        {
            PanelTitle = options.Title,
            PanelDescription = options.Description,
            AccessibleName = options.AccessibleName,
            ShowClose = options.Close,
            BodyView = body,
            FooterView = footer,
        };
        panel.Configure(options.Kind == KlyOverlayKind.Modal ? OverlaysPanelMode.Modal : ModeOf(options.Side), options.Size, inset: false);
        panel.Refresh();
        panel.CloseRequested += (_, _) => End(true, default);

        layer = panel.Present(this, options.Dismissible);
        layer.Dismissed += (_, _) => completion.TrySetResult(new KlyOverlayResult<TResult>(true, default));
        if (cancellationToken.CanBeCanceled)
        {
            var registration = cancellationToken.Register(() => OverlaysSupport.Post(this, () => End(true, default)));
            completion.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
        }
        return completion.Task;
    }

    internal static OverlaysPanelMode ModeOf(SlideoverSide side) => side switch
    {
        SlideoverSide.Right => OverlaysPanelMode.Right,
        SlideoverSide.Left => OverlaysPanelMode.Left,
        SlideoverSide.Top => OverlaysPanelMode.Top,
        SlideoverSide.Bottom => OverlaysPanelMode.Bottom,
        _ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
    };

    internal static OverlaysPanelMode ModeOf(DrawerDirection direction) => direction switch
    {
        DrawerDirection.Right => OverlaysPanelMode.Right,
        DrawerDirection.Left => OverlaysPanelMode.Left,
        DrawerDirection.Top => OverlaysPanelMode.Top,
        DrawerDirection.Bottom => OverlaysPanelMode.Bottom,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    private void ReplaceContent(View? oldContent, View? newContent)
    {
        if (oldContent is not null) Children.Remove(oldContent);
        if (newContent is not null)
        {
            OverlaysSupport.Detach(newContent);
            Children.Insert(0, newContent);
        }
    }

    // The back button, the layers that hide the page from screen readers: both follow the layers that are shown.
    private void UpdateState()
    {
        var modal = _layers.Any(l => l.Options.Modal);
        if (modal != _pageHidden && Content is { } page)
        {
            // A dialog is the only thing a screen reader sees while it is open, as with a native dialog.
            if (modal)
            {
                _pageExcluded = AutomationProperties.GetExcludedWithChildren(page);
                AutomationProperties.SetExcludedWithChildren(page, true);
            }
            else
            {
                AutomationProperties.SetExcludedWithChildren(page, _pageExcluded);
            }
            _pageHidden = modal;
        }
        _back?.SetActive(_layers.Any(l => l.Dismissible || l.Options.Modal));
        LayersChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RegisterBackButton()
    {
        _back ??= OverlaysBackButton.Register(DismissTop);
        _back?.SetActive(_layers.Any(l => l.Dismissible || l.Options.Modal));
    }

    private void UnregisterBackButton()
    {
        _back?.Dispose();
        _back = null;
    }

    private void OnHostResized()
    {
        if (Width <= 0) return;
        var size = new Size(Width, Height);
        foreach (var layer in _layers.ToArray())
        {
            layer.RaiseHostSizeChanged(size);
            Reposition(layer);
        }
    }

    private void Reposition(KlyOverlayLayer layer)
    {
        var options = layer.Options;
        var content = layer.Content;
        if (options.Anchor is not { } anchor || Width <= 0 || content.Width <= 0 || content.Height <= 0) return;

        var bounds = BoundsOf(anchor);
        if (options.AnchorPoint is { } point) bounds = new Rect(bounds.X + point.X, bounds.Y + point.Y, 0, 0);
        var position = OverlaysPlacement.Position(bounds, new Size(content.Width, content.Height), new Size(Width, Height),
            options.Placement, options.Offset, OverlaysSupport.IsRtl(this));
        content.TranslationX = position.X;
        content.TranslationY = position.Y;
        content.Opacity = 1;
    }

    private void RunTransition(KlyOverlayLayer layer, bool entering)
    {
        // No animation without a platform view (a unit test), and none for anchored layers, which are placed against their trigger.
        if (Handler is null || layer.Options.Transition == KlyOverlayTransition.None || layer.Options.Anchor is not null)
        {
            if (!entering) RemoveContainer(layer);
            return;
        }
        _ = TransitionAsync(layer, entering);
    }

    private async Task TransitionAsync(KlyOverlayLayer layer, bool entering)
    {
        var content = layer.Content;
        var backdrop = layer.Backdrop;
        var transition = layer.Options.Transition;
        var (fromX, fromY) = transition switch
        {
            KlyOverlayTransition.SlideFromLeft => (-Width, 0d),
            KlyOverlayTransition.SlideFromRight => (Width, 0d),
            KlyOverlayTransition.SlideFromTop => (0d, -Height),
            KlyOverlayTransition.SlideFromBottom => (0d, Height),
            _ => (0d, 0d),
        };
        var slides = fromX != 0 || fromY != 0;

        if (entering)
        {
            if (backdrop is not null) backdrop.Opacity = 0;
            if (slides)
            {
                content.TranslationX = fromX;
                content.TranslationY = fromY;
            }
            else
            {
                content.Opacity = 0;
            }
            await Task.WhenAll(
                backdrop?.FadeToAsync(1, 150) ?? Task.FromResult(true),
                slides ? content.TranslateToAsync(0, 0, 220, Easing.CubicOut) : content.FadeToAsync(1, 150));
            return;
        }

        await Task.WhenAll(
            backdrop?.FadeToAsync(0, 150) ?? Task.FromResult(true),
            slides ? content.TranslateToAsync(fromX, fromY, 180, Easing.CubicIn) : content.FadeToAsync(0, 120));
        RemoveContainer(layer);
    }

    private void RemoveContainer(KlyOverlayLayer layer)
    {
        Children.Remove(layer.Container);
        layer.Container.Remove(layer.Content);
    }
}
