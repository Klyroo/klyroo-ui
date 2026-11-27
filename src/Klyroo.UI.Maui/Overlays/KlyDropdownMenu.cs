namespace Klyroo.UI.Maui;

/// <summary>
/// A menu that opens under its trigger. <see cref="ContentView.Content"/> is the trigger (its first button opens the menu), <see cref="Items"/> are the entries.
/// An item with <see cref="KlyMenuItem.Children"/> opens a submenu next to it. A tap outside and the back button close the menu, a selection closes it and raises <see cref="ItemSelected"/>.
/// Needs a <see cref="KlyOverlayHost"/> above it.
/// </summary>
public class KlyDropdownMenu : KlyOverlayBase
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyMenuItem>), typeof(KlyDropdownMenu), Array.Empty<KlyMenuItem>());
    /// <summary>Identifies <see cref="Placement"/>.</summary>
    public static readonly BindableProperty PlacementProperty = BindableProperty.Create(nameof(Placement), typeof(KlyPlacement), typeof(KlyDropdownMenu), KlyPlacement.BottomStart);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyDropdownMenu), "Menu");

    private readonly List<KlyOverlayLayer> _submenus = [];
    private OverlaysMenu? _menu;

    /// <summary>Creates a dropdown menu.</summary>
    public KlyDropdownMenu()
    {
    }

    /// <summary>Raised with the item the user selected, after the menu has closed. Items with a submenu do not raise it.</summary>
    public event EventHandler<KlyMenuItem>? ItemSelected;

    /// <summary>The entries. A heading followed by items and a separator makes a labelled group. Read when the menu opens.</summary>
    public IReadOnlyList<KlyMenuItem> Items { get => (IReadOnlyList<KlyMenuItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>Where the menu sits against the trigger. Defaults to <see cref="KlyPlacement.BottomStart"/>.</summary>
    public KlyPlacement Placement { get => (KlyPlacement)GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }

    /// <summary>What a screen reader says for the menu. Defaults to "Menu".</summary>
    public string AccessibleName { get => (string)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>The menu while it is shown, for the tests.</summary>
    internal OverlaysMenu? Menu => _menu;

    /// <summary>The layers of the open submenus, outermost first, for the tests.</summary>
    internal IReadOnlyList<KlyOverlayLayer> Submenus => _submenus;

    /// <inheritdoc />
    protected override KlyOverlayLayer Present(KlyOverlayHost host)
    {
        var menu = new OverlaysMenu(Items, submenus: true, AccessibleName);
        menu.ItemActivated += (_, row) => OnActivated(row, depth: 0);
        _menu = menu;

        var layer = host.Show(menu, new KlyOverlayLayerOptions
        {
            Modal = true,
            Dim = false,
            Anchor = Content ?? this,
            Placement = Placement,
            Offset = 4,
        });
        layer.HostSizeChanged += (_, size) => menu.MaximumHeightRequest = Math.Max(0, size.Height - 16);
        if (host.Height > 0) menu.MaximumHeightRequest = Math.Max(0, host.Height - 16);
        return layer;
    }

    /// <inheritdoc />
    protected override void OnLayerRemoved()
    {
        CloseSubmenus(0);
        _menu = null;
    }

    private void OnActivated(OverlaysMenuRow row, int depth)
    {
        CloseSubmenus(depth);
        if (row.HasSubmenu && Layer?.Host is { } host)
        {
            var submenu = new OverlaysMenu(row.Item.Children!, submenus: true, row.Item.Label);
            submenu.ItemActivated += (_, nested) => OnActivated(nested, depth + 1);
            // The submenu lets touches through: a tap outside reaches the backdrop of the menu below, which closes the whole chain.
            var layer = host.Show(submenu, new KlyOverlayLayerOptions { Modal = false, Anchor = row, Placement = KlyPlacement.RightStart, Offset = 0 });
            layer.Dismissed += (_, _) => _submenus.Remove(layer);
            _submenus.Add(layer);
            return;
        }

        var item = row.Item;
        IsOpen = false;
        ItemSelected?.Invoke(this, item);
        OverlaysSupport.OpenLink(item.Href);
    }

    private void CloseSubmenus(int depth)
    {
        for (var i = _submenus.Count - 1; i >= depth; i--)
        {
            var layer = _submenus[i];
            _submenus.RemoveAt(i);
            layer.Host.Hide(layer);
        }
    }
}
