namespace Klyroo.UI.Maui;

/// <summary>
/// The button that collapses the sidebar of its dashboard to an icon rail, and expands it again. It shows from 1024 wide, where the sidebar is a column, and hides itself below.
/// It finds the <see cref="KlyDashboardGroup"/> it sits in, and the sidebar by <see cref="For"/>, by itself.
/// </summary>
public class KlyDashboardSidebarCollapse : ContentView, ILayoutDashboardPart
{
    /// <summary>Identifies <see cref="For"/>.</summary>
    public static readonly BindableProperty ForProperty = BindableProperty.Create(nameof(For), typeof(string), typeof(KlyDashboardSidebarCollapse), null);
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyDashboardSidebarCollapse), "Collapse sidebar", propertyChanged: (b, _, v) => ((KlyDashboardSidebarCollapse)b)._button.Label = (string)v);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(ButtonVariant), typeof(KlyDashboardSidebarCollapse), ButtonVariant.Ghost, propertyChanged: (b, _, v) => ((KlyDashboardSidebarCollapse)b)._button.Variant = (ButtonVariant)v);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyDashboardSidebarCollapse), KlyColor.Neutral, propertyChanged: (b, _, v) => ((KlyDashboardSidebarCollapse)b)._button.Color = (KlyColor)v);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyDashboardSidebarCollapse), KlySize.Md, propertyChanged: (b, _, v) => ((KlyDashboardSidebarCollapse)b)._button.Size = (KlySize)v);

    private readonly LayoutIconButton _button = new(KlyIconNames.PanelClose, "Collapse sidebar");

    /// <summary>Creates a collapse button.</summary>
    public KlyDashboardSidebarCollapse()
    {
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        Content = _button;
        _button.Clicked += (_, _) => Activate();
    }

    /// <summary>Raised after the button collapsed or expanded the sidebar.</summary>
    public event EventHandler? Clicked;

    /// <summary>The <see cref="KlySidebar.SidebarId"/> of the sidebar to collapse. Defaults to the first sidebar of the dashboard.</summary>
    public string? For { get => (string?)GetValue(ForProperty); set => SetValue(ForProperty, value); }

    /// <summary>Accessible name, in the language of the app. Defaults to "Collapse sidebar".</summary>
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Look of the button: <c>Solid</c>, <c>Outline</c>, <c>Soft</c>, <c>Subtle</c>, <c>Ghost</c> (default) or <c>Link</c>.</summary>
    public ButtonVariant Variant { get => (ButtonVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Semantic color of the button.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the button, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) _button.IsEnabled = IsEnabled;
    }

    /// <summary>Collapses or expands the sidebar, as a tap does.</summary>
    internal void Activate()
    {
        if (KlyDashboardGroup.Of(this)?.FindSidebar(For) is { } sidebar) sidebar.Collapsed = !sidebar.Collapsed;
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    void ILayoutDashboardPart.DashboardChanged(KlyDashboardGroup group) => IsVisible = !group.IsNarrow;
}
