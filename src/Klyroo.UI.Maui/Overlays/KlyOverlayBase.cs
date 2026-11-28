namespace Klyroo.UI.Maui;

/// <summary>
/// Base of the overlay controls: a control that sits in the page like any view, holds the trigger as its <see cref="ContentView.Content"/>,
/// and shows its panel as a layer of the nearest <see cref="KlyOverlayHost"/> while <see cref="IsOpen"/> is true.
/// The first button of the trigger (or the trigger itself, when it holds no button) opens and closes the overlay with a tap.
/// </summary>
public abstract class KlyOverlayBase : ContentView
{
    /// <summary>Identifies <see cref="IsOpen"/>.</summary>
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(KlyOverlayBase), false, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlyOverlayBase)b).OnIsOpenChanged((bool)v));

    private KlyOverlayLayer? _layer;
    private bool _shown;
    private bool _syncing;
    private Action? _unwire;

    private protected KlyOverlayBase()
    {
        ParentChanged += (_, _) => OnAttachmentChanged(loaded: false);
        Loaded += (_, _) => OnAttachmentChanged(loaded: true);
        Unloaded += (_, _) =>
        {
            if (_shown) IsOpen = false;
        };
    }

    /// <summary>Raised once when the overlay has been shown, by <see cref="IsOpen"/>, <see cref="Show"/> or a tap on the trigger.</summary>
    public event EventHandler? Opened;

    /// <summary>Raised once when the overlay has been hidden: by <see cref="IsOpen"/>, <see cref="Hide"/>, or the user dismissing it (a tap outside, the back button).</summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Whether the overlay is shown. Two-way bindable: the control sets it to false when the user dismisses the overlay.
    /// Set before the control is in the tree, it takes effect once the control is in a <see cref="KlyOverlayHost"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Set to true while the control is in a tree that has no <see cref="KlyOverlayHost"/>: wrap the page content in <see cref="KlyOverlayHost"/>.</exception>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set
        {
            // Checked before the value changes, so a missing host leaves the control closed.
            if (value && !_shown && Parent is not null && !KlyOverlayHost.TryFind(this, out _)) throw KlyOverlayHost.MissingHost(this);
            SetValue(IsOpenProperty, value);
        }
    }

    /// <summary>The layer of the overlay while it is shown.</summary>
    protected KlyOverlayLayer? Layer => _layer;

    /// <summary>Shows the overlay now, and throws when there is no host to show it in.</summary>
    /// <exception cref="InvalidOperationException">The control is not inside a <see cref="KlyOverlayHost"/>.</exception>
    public void Show()
    {
        if (_shown) return;
        SetIsOpenSilently(true);
        TryShow(throwIfMissing: true);
    }

    /// <summary>Hides the overlay.</summary>
    public void Hide() => IsOpen = false;

    /// <summary>Opens the overlay when it is closed and closes it when it is open. A disabled control ignores it.</summary>
    protected void Toggle()
    {
        if (IsEnabled) IsOpen = !IsOpen;
    }

    /// <summary>Puts the panel of the overlay in the host and returns its layer.</summary>
    /// <param name="host">The host to show the layer in.</param>
    protected abstract KlyOverlayLayer Present(KlyOverlayHost host);

    /// <summary>Called after the layer has been removed, whichever way. Drop what <see cref="Present"/> built.</summary>
    protected virtual void OnLayerRemoved()
    {
    }

    /// <summary>Wires the trigger to the overlay and returns how to undo it. By default the first button of the trigger toggles the overlay.</summary>
    /// <param name="trigger">The trigger, the <see cref="ContentView.Content"/> of the control.</param>
    protected virtual Action HookTrigger(View trigger) => OverlaysTrigger.Activate(trigger, Toggle);

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName != nameof(Content)) return;
        _unwire?.Invoke();
        _unwire = Content is { } trigger ? HookTrigger(trigger) : null;
    }

    private void OnIsOpenChanged(bool open)
    {
        if (_syncing) return;
        if (open)
        {
            // Not in a tree yet (object initializer, XAML): the overlay opens once the control is in a host.
            if (!_shown) TryShow(throwIfMissing: Parent is not null);
        }
        else
        {
            CloseLayer();
        }
    }

    private void OnAttachmentChanged(bool loaded)
    {
        if (Parent is null && !loaded)
        {
            if (_shown) IsOpen = false;
            return;
        }
        if (IsOpen && !_shown) TryShow(throwIfMissing: loaded);
    }

    private bool TryShow(bool throwIfMissing)
    {
        if (!KlyOverlayHost.TryFind(this, out var host))
        {
            if (!throwIfMissing) return false;
            SetIsOpenSilently(false);
            throw KlyOverlayHost.MissingHost(this);
        }

        try
        {
            _layer = Present(host);
        }
        catch
        {
            SetIsOpenSilently(false);
            throw;
        }
        _layer.Dismissed += OnLayerDismissed;
        _shown = true;
        Opened?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void OnLayerDismissed(object? sender, EventArgs e)
    {
        _layer = null;
        _shown = false;
        SetIsOpenSilently(false);
        OnLayerRemoved();
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void CloseLayer()
    {
        if (!_shown) return;
        var layer = _layer;
        _layer = null;
        _shown = false;
        if (layer is not null)
        {
            layer.Dismissed -= OnLayerDismissed;
            layer.Host.Hide(layer);
        }
        OnLayerRemoved();
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void SetIsOpenSilently(bool value)
    {
        _syncing = true;
        try
        {
            IsOpen = value;
        }
        finally
        {
            _syncing = false;
        }
    }
}
