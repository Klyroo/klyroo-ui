namespace Klyroo.UI.Maui;

/// <summary>
/// A menu that opens at the pointer on a right click (desktop) or a long press with a finger (Android, iOS). <see cref="ContentView.Content"/> is the area that reacts,
/// <see cref="Items"/> are the entries; a selection closes the menu and raises <see cref="ItemSelected"/>. Needs a <see cref="KlyOverlayHost"/> above it.
/// </summary>
public class KlyContextMenu : KlyOverlayBase
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyMenuItem>), typeof(KlyContextMenu), Array.Empty<KlyMenuItem>());
    /// <summary>Identifies <see cref="Disabled"/>.</summary>
    public static readonly BindableProperty DisabledProperty = BindableProperty.Create(nameof(Disabled), typeof(bool), typeof(KlyContextMenu), false, propertyChanged: (b, _, v) =>
    {
        if ((bool)v && ((KlyContextMenu)b).IsOpen) ((KlyContextMenu)b).IsOpen = false;
    });
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyContextMenu), "Context menu");

    private OverlaysMenu? _menu;
    private Point? _point;

    /// <summary>Creates a context menu.</summary>
    public KlyContextMenu()
    {
    }

    /// <summary>Raised with the item the user selected, after the menu has closed.</summary>
    public event EventHandler<KlyMenuItem>? ItemSelected;

    /// <summary>The entries of the menu. An item with children is shown as a plain item: only <see cref="KlyDropdownMenu"/> has submenus. Read when the menu opens.</summary>
    public IReadOnlyList<KlyMenuItem> Items { get => (IReadOnlyList<KlyMenuItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The area gives no menu, and a menu that is open closes. The area itself stays enabled, unlike <c>IsEnabled</c>, which would also disable what it wraps.</summary>
    public bool Disabled { get => (bool)GetValue(DisabledProperty); set => SetValue(DisabledProperty, value); }

    /// <summary>What a screen reader says for the menu. Defaults to "Context menu".</summary>
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>The menu while it is shown, for the tests.</summary>
    internal OverlaysMenu? Menu => _menu;

    /// <summary>Opens the menu at a point of the area (its own coordinates), as a right click or a long press does. A disabled menu ignores it.</summary>
    /// <param name="point">The place of the pointer; null opens the menu at the center of the area.</param>
    internal void OpenAt(Point? point)
    {
        if (Disabled || !IsEnabled) return;
        _point = point;
        if (IsOpen) IsOpen = false;
        IsOpen = true;
    }

    /// <inheritdoc />
    protected override Action HookTrigger(View trigger)
    {
        var secondary = new TapGestureRecognizer { Buttons = ButtonsMask.Secondary };
        secondary.Tapped += (_, e) => OpenAt(e.GetPosition(trigger));
        trigger.GestureRecognizers.Add(secondary);
        var longPress = OverlaysTrigger.LongPress(trigger, point => OpenAt(point));
        return () =>
        {
            trigger.GestureRecognizers.Remove(secondary);
            longPress();
        };
    }

    /// <inheritdoc />
    protected override KlyOverlayLayer Present(KlyOverlayHost host)
    {
        var menu = new OverlaysMenu(Items, submenus: false, Label);
        menu.ItemActivated += (_, row) =>
        {
            var item = row.Item;
            IsOpen = false;
            ItemSelected?.Invoke(this, item);
            OverlaysSupport.OpenLink(item.Href);
        };
        _menu = menu;

        var anchor = Content ?? this;
        // With no pointer (the property was set from code) the menu opens at the center of the area.
        var point = _point ?? new Point(Math.Max(anchor.Width, 0) / 2, Math.Max(anchor.Height, 0) / 2);
        _point = null;
        var layer = host.Show(menu, new KlyOverlayLayerOptions
        {
            Modal = true,
            Dim = false,
            Anchor = anchor,
            AnchorPoint = point,
            Placement = KlyPlacement.BottomStart,
            Offset = 0,
        });
        layer.HostSizeChanged += (_, size) => menu.MaximumHeightRequest = Math.Max(0, size.Height - 16);
        if (host.Height > 0) menu.MaximumHeightRequest = Math.Max(0, host.Height - 16);
        return layer;
    }

    /// <inheritdoc />
    protected override void OnLayerRemoved() => _menu = null;
}
