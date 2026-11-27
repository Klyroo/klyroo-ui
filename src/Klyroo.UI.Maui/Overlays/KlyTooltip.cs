namespace Klyroo.UI.Maui;

/// <summary>
/// A short label next to its trigger. It shows on a long press with a finger (Android, iOS), when a pointer hovers the trigger for <see cref="Delay"/> milliseconds (desktop),
/// and at once when the trigger gets the keyboard focus. <see cref="ContentView.Content"/> is the trigger; a tap on it does not toggle the tooltip.
/// It never takes touches: the page stays usable. Needs a <see cref="KlyOverlayHost"/> above it.
/// </summary>
public class KlyTooltip : KlyOverlayBase
{
    private const int TouchDuration = 2500;

    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyTooltip), null, propertyChanged: (b, _, _) => ((KlyTooltip)b).ShowContent());
    /// <summary>Identifies <see cref="PanelContent"/>.</summary>
    public static readonly BindableProperty PanelContentProperty = BindableProperty.Create(nameof(PanelContent), typeof(View), typeof(KlyTooltip), null, propertyChanged: (b, _, _) => ((KlyTooltip)b).ShowContent());
    /// <summary>Identifies <see cref="Placement"/>.</summary>
    public static readonly BindableProperty PlacementProperty = BindableProperty.Create(nameof(Placement), typeof(KlyPlacement), typeof(KlyTooltip), KlyPlacement.Top);
    /// <summary>Identifies <see cref="Delay"/>.</summary>
    public static readonly BindableProperty DelayProperty = BindableProperty.Create(nameof(Delay), typeof(int), typeof(KlyTooltip), 300);

    private readonly Label _label = new() { FontSize = 12 };
    private Border? _bubble;
    private CancellationTokenSource? _pending;
    private View? _hinted;

    /// <summary>Creates a tooltip.</summary>
    public KlyTooltip()
    {
        _label.SetThemeColor(Label.TextColorProperty, KlyTheme.Background);
    }

    /// <summary>Text of the tooltip. Use <see cref="PanelContent"/> for other content.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Content of the tooltip, instead of <see cref="Text"/>.</summary>
    public View? PanelContent { get => (View?)GetValue(PanelContentProperty); set => SetValue(PanelContentProperty, value); }

    /// <summary>Where the tooltip sits against the trigger. Defaults to <see cref="KlyPlacement.Top"/>.</summary>
    public KlyPlacement Placement { get => (KlyPlacement)GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }

    /// <summary>Hover delay before showing, in milliseconds. A long press and the keyboard focus show it at once.</summary>
    public int Delay { get => (int)GetValue(DelayProperty); set => SetValue(DelayProperty, value); }

    /// <summary>The bubble while the tooltip is shown, for the tests.</summary>
    internal Border? Bubble => _bubble;

    /// <summary>Starts the hover delay, as a pointer entering the trigger does.</summary>
    internal void PointerEntered() => ShowAfter(TimeSpan.FromMilliseconds(Math.Max(0, Delay)), hideAfter: null);

    /// <summary>Hides the tooltip, as a pointer leaving the trigger does.</summary>
    internal void PointerExited() => Dismiss();

    /// <summary>Shows the tooltip for a moment, as a long press does.</summary>
    internal void LongPressed() => ShowAfter(TimeSpan.Zero, TimeSpan.FromMilliseconds(TouchDuration));

    /// <inheritdoc />
    protected override Action HookTrigger(View trigger)
    {
        ShowContent();
        var hover = OverlaysTrigger.Hover(trigger, PointerEntered, PointerExited);
        var focus = OverlaysTrigger.Focus(trigger, () => ShowAfter(TimeSpan.Zero, hideAfter: null), Dismiss);
        var longPress = OverlaysTrigger.LongPress(trigger, _ => LongPressed());
        return () =>
        {
            hover();
            focus();
            longPress();
            CancelPending();
        };
    }

    /// <inheritdoc />
    protected override KlyOverlayLayer Present(KlyOverlayHost host)
    {
        var bubble = new Border { Padding = new Thickness(8, 4), MaximumWidthRequest = 320, StrokeThickness = 0 };
        bubble.SetRadius(KlyTheme.RadiusOf(1));
        bubble.SetThemeColor(BackgroundColorProperty, KlyTheme.Text);
        _bubble = bubble;
        ShowContent();
        return host.Show(bubble, new KlyOverlayLayerOptions
        {
            Modal = false,
            Dismissible = false,
            Anchor = Content ?? this,
            Placement = Placement,
            Offset = 6,
        });
    }

    /// <inheritdoc />
    protected override void OnLayerRemoved()
    {
        if (_bubble is not null) _bubble.Content = null;
        _bubble = null;
    }

    private void ShowContent()
    {
        // The hint of the trigger carries the text, so a screen reader reads it without a long press; a hint the app set itself is left alone.
        if (Content is { } trigger && (string.IsNullOrEmpty(SemanticProperties.GetHint(trigger)) || ReferenceEquals(_hinted, trigger)))
        {
            SemanticProperties.SetHint(trigger, Text);
            _hinted = trigger;
        }
        if (_bubble is not { } bubble) return;
        if (PanelContent is { } custom)
        {
            OverlaysSupport.Detach(custom);
            bubble.Content = custom;
        }
        else
        {
            _label.Text = Text;
            bubble.Content = _label;
        }
    }

    private void ShowAfter(TimeSpan delay, TimeSpan? hideAfter)
    {
        CancelPending();
        var source = _pending = new CancellationTokenSource();
        _ = RunAsync(delay, hideAfter, source.Token);
    }

    private async Task RunAsync(TimeSpan delay, TimeSpan? hideAfter, CancellationToken token)
    {
        try
        {
            if (delay > TimeSpan.Zero) await Task.Delay(delay, token);
            OverlaysSupport.Post(this, () =>
            {
                if (!token.IsCancellationRequested) IsOpen = true;
            });
            if (hideAfter is { } after)
            {
                await Task.Delay(after, token);
                OverlaysSupport.Post(this, () =>
                {
                    if (!token.IsCancellationRequested) IsOpen = false;
                });
            }
        }
        catch (OperationCanceledException)
        {
            // The pointer left, or the trigger lost the focus, before the delay ended.
        }
    }

    private void Dismiss()
    {
        CancelPending();
        IsOpen = false;
    }

    private void CancelPending()
    {
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = null;
    }
}
