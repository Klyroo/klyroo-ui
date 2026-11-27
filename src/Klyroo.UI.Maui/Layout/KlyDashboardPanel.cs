namespace Klyroo.UI.Maui;

/// <summary>
/// A column of a dashboard with a header, a scrolling body and a footer. The panels of a <see cref="KlyDashboardGroup"/> share the width the sidebar leaves, as weights in percent;
/// <see cref="Resizable"/> adds a handle on the end edge. Use <see cref="ChildContent"/> instead of the three areas to fill the whole panel with one view that scrolls by itself.
/// </summary>
[ContentProperty(nameof(ChildContent))]
public class KlyDashboardPanel : Grid, ILayoutDashboardPart
{
    /// <summary>Identifies <see cref="ChildContent"/>.</summary>
    public static readonly BindableProperty ChildContentProperty = BindableProperty.Create(nameof(ChildContent), typeof(View), typeof(KlyDashboardPanel), null, propertyChanged: (b, _, _) => ((KlyDashboardPanel)b).ApplyParts());
    /// <summary>Identifies <see cref="Header"/>.</summary>
    public static readonly BindableProperty HeaderProperty = BindableProperty.Create(nameof(Header), typeof(View), typeof(KlyDashboardPanel), null, propertyChanged: (b, _, _) => ((KlyDashboardPanel)b).ApplyParts());
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyDashboardPanel), null, propertyChanged: (b, _, _) => ((KlyDashboardPanel)b).ApplyParts());
    /// <summary>Identifies <see cref="Footer"/>.</summary>
    public static readonly BindableProperty FooterProperty = BindableProperty.Create(nameof(Footer), typeof(View), typeof(KlyDashboardPanel), null, propertyChanged: (b, _, _) => ((KlyDashboardPanel)b).ApplyParts());
    /// <summary>Identifies <see cref="Resizable"/>.</summary>
    public static readonly BindableProperty ResizableProperty = BindableProperty.Create(nameof(Resizable), typeof(bool), typeof(KlyDashboardPanel), false, propertyChanged: (b, _, _) => ((KlyDashboardPanel)b).ApplyParts());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(double?), typeof(KlyDashboardPanel), null);
    /// <summary>Identifies <see cref="MinSize"/>.</summary>
    public static readonly BindableProperty MinSizeProperty = BindableProperty.Create(nameof(MinSize), typeof(double), typeof(KlyDashboardPanel), 0d);
    /// <summary>Identifies <see cref="MaxSize"/>.</summary>
    public static readonly BindableProperty MaxSizeProperty = BindableProperty.Create(nameof(MaxSize), typeof(double), typeof(KlyDashboardPanel), 100d);
    /// <summary>Identifies <see cref="ResizeLabel"/>.</summary>
    public static readonly BindableProperty ResizeLabelProperty = BindableProperty.Create(nameof(ResizeLabel), typeof(string), typeof(KlyDashboardPanel), "Resize panels", propertyChanged: (b, _, v) => ((KlyDashboardPanel)b)._handle.Label = (string)v);

    private readonly Grid _areas = new() { RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto)] };
    private readonly ContentView _header = LayoutParts.Slot();
    private readonly ContentView _body = new();
    private readonly ScrollView _scroll = new() { Orientation = ScrollOrientation.Vertical, Padding = new Thickness(16) };
    private readonly ContentView _footer = LayoutParts.Slot();
    private readonly Border _footerLine = LayoutParts.Line();
    private readonly ContentView _whole = new() { IsVisible = false };
    private readonly Border _trailing = LayoutParts.Line(vertical: true);
    private readonly KlyDashboardResizeHandle _handle = new() { HorizontalOptions = LayoutOptions.End, IsVisible = false };
    private bool _narrow = true;

    /// <summary>Creates a panel.</summary>
    public KlyDashboardPanel()
    {
        _footer.VerticalOptions = LayoutOptions.End;
        _scroll.Content = _body;
        _areas.Add(_header, 0, 0);
        _areas.Add(_scroll, 0, 1);
        _areas.Add(_footerLine, 0, 2);
        _areas.Add(_footer, 0, 2);
        _footerLine.VerticalOptions = LayoutOptions.Start;
        _trailing.IsVisible = false;
        _handle.TranslationX = 10;
        _handle.Dragged += (_, distance) => ResizeRequested?.Invoke(this, distance);

        this.Add(_areas);
        this.Add(_whole);
        this.Add(_trailing);
        this.Add(_handle);

        SizeChanged += (_, _) => _scroll.Padding = new Thickness(LayoutBreakpoints.HorizontalPadding(Width) <= 16 ? 16 : 24);
        ApplyParts();
    }

    /// <summary>The content of the whole panel. Replaces <see cref="Header"/>, <see cref="Body"/> and <see cref="Footer"/>, and does not scroll by itself.</summary>
    public View? ChildContent { get => (View?)GetValue(ChildContentProperty); set => SetValue(ChildContentProperty, value); }

    /// <summary>The top of the panel: usually a <see cref="KlyDashboardNavbar"/> and a <see cref="KlyDashboardToolbar"/>.</summary>
    public View? Header { get => (View?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>The scrolling middle.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>The bottom of the panel, under its scrolling body.</summary>
    public View? Footer { get => (View?)GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    /// <summary>Adds a <see cref="KlyDashboardResizeHandle"/> on the end edge, to resize the panel against the next one. Not shown below 1024 wide.</summary>
    public bool Resizable { get => (bool)GetValue(ResizableProperty); set => SetValue(ResizableProperty, value); }

    /// <summary>Size in percent of the dashboard when nothing says otherwise. Panels without one share what the others leave.</summary>
    public double? Size { get => (double?)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Smallest share of the dashboard the panel can take, in percent.</summary>
    public double MinSize { get => (double)GetValue(MinSizeProperty); set => SetValue(MinSizeProperty, value); }

    /// <summary>Largest share of the dashboard the panel can take, in percent. Defaults to 100.</summary>
    public double MaxSize { get => (double)GetValue(MaxSizeProperty); set => SetValue(MaxSizeProperty, value); }

    /// <summary>Accessible name of the resize handle. Defaults to "Resize panels".</summary>
    public string ResizeLabel { get => (string)GetValue(ResizeLabelProperty); set => SetValue(ResizeLabelProperty, value); }

    /// <summary>Raised with the distance in units the user dragged the handle, positive toward the end side. The dashboard turns it into sizes.</summary>
    internal event EventHandler<double>? ResizeRequested;

    /// <summary>Draws the border on the end edge. The dashboard turns it on for every panel but the last.</summary>
    internal bool ShowTrailingBorder
    {
        get => _trailing.IsVisible;
        set => _trailing.IsVisible = value;
    }

    /// <summary>True while the handle is shown, which is below 1024 wide never.</summary>
    internal bool HandleVisible => _handle.IsVisible;

    /// <summary>The size in percent the dashboard gave the panel now, shown by the handle to a screen reader.</summary>
    internal void ShowSize(double percent) => _handle.Value = percent;

    /// <inheritdoc />
    void ILayoutDashboardPart.DashboardChanged(KlyDashboardGroup group)
    {
        _narrow = group.IsNarrow;
        ApplyParts();
    }

    private void ApplyParts()
    {
        var whole = ChildContent;
        _whole.Content = whole;
        _whole.IsVisible = whole is not null;
        _areas.IsVisible = whole is null;
        // A view has one parent: fill the slots from the properties each time.
        LayoutParts.Fill(_header, Header);
        _body.Content = Body;
        LayoutParts.Fill(_footer, Footer);
        _footerLine.IsVisible = Footer is not null;
        _handle.IsVisible = Resizable && !_narrow;
        ZIndex = Resizable ? 1 : 0;
    }
}
