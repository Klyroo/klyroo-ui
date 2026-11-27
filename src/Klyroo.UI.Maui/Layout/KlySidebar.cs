namespace Klyroo.UI.Maui;

/// <summary>
/// An app sidebar with a header, a scrolling middle and a footer. From 1024 wide it is a column that can collapse to an icon rail and be resized; below it is a panel that slides in
/// from the start edge over a scrim, opened with <see cref="IsOpen"/> or the menu button of <see cref="MobileTrigger"/>.
/// The width it reacts to is the one of the page, or of the <see cref="KlyDashboardGroup"/> it sits in. In its panel form it covers the cell of its parent, so place it in a cell that spans the page content
/// (a <see cref="KlyDashboardGroup"/> does that for you).
/// </summary>
[ContentProperty(nameof(ChildContent))]
public class KlySidebar : Grid
{
    private const double RailWidth = 64;
    private const double PanelMaxWidth = 320;

    /// <summary>Identifies <see cref="ChildContent"/>.</summary>
    public static readonly BindableProperty ChildContentProperty = BindableProperty.Create(nameof(ChildContent), typeof(View), typeof(KlySidebar), null, propertyChanged: (b, _, v) => ((KlySidebar)b)._content.Content = (View?)v);
    /// <summary>Identifies <see cref="Header"/>.</summary>
    public static readonly BindableProperty HeaderProperty = BindableProperty.Create(nameof(Header), typeof(View), typeof(KlySidebar), null, propertyChanged: (b, _, v) => Fill(b, ((KlySidebar)b)._headerContent, (View?)v));
    /// <summary>Identifies <see cref="Footer"/>.</summary>
    public static readonly BindableProperty FooterProperty = BindableProperty.Create(nameof(Footer), typeof(View), typeof(KlySidebar), null, propertyChanged: (b, _, v) => Fill(b, ((KlySidebar)b)._footerContent, (View?)v));
    /// <summary>Identifies <see cref="SidebarId"/>.</summary>
    public static readonly BindableProperty SidebarIdProperty = BindableProperty.Create(nameof(SidebarId), typeof(string), typeof(KlySidebar), null);
    /// <summary>Identifies <see cref="Collapsible"/>.</summary>
    public static readonly BindableProperty CollapsibleProperty = BindableProperty.Create(nameof(Collapsible), typeof(bool), typeof(KlySidebar), true, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Collapsed"/>.</summary>
    public static readonly BindableProperty CollapsedProperty = BindableProperty.Create(nameof(Collapsed), typeof(bool), typeof(KlySidebar), false, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlySidebar)b).OnCollapsedChanged((bool)v));
    /// <summary>Identifies <see cref="Resizable"/>.</summary>
    public static readonly BindableProperty ResizableProperty = BindableProperty.Create(nameof(Resizable), typeof(bool), typeof(KlySidebar), false, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="SidebarWidth"/>.</summary>
    public static readonly BindableProperty SidebarWidthProperty = BindableProperty.Create(nameof(SidebarWidth), typeof(double), typeof(KlySidebar), 256d, BindingMode.TwoWay, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="MinWidth"/>.</summary>
    public static readonly BindableProperty MinWidthProperty = BindableProperty.Create(nameof(MinWidth), typeof(double), typeof(KlySidebar), 192d);
    /// <summary>Identifies <see cref="MaxWidth"/>.</summary>
    public static readonly BindableProperty MaxWidthProperty = BindableProperty.Create(nameof(MaxWidth), typeof(double), typeof(KlySidebar), 448d);
    /// <summary>Identifies <see cref="MobileTrigger"/>.</summary>
    public static readonly BindableProperty MobileTriggerProperty = BindableProperty.Create(nameof(MobileTrigger), typeof(bool), typeof(KlySidebar), true, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="MobileTitle"/>.</summary>
    public static readonly BindableProperty MobileTitleProperty = BindableProperty.Create(nameof(MobileTitle), typeof(string), typeof(KlySidebar), "Sidebar", propertyChanged: (b, _, v) => ((KlySidebar)b)._mobileTitle.Text = (string?)v);
    /// <summary>Identifies <see cref="IsOpen"/>.</summary>
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(KlySidebar), false, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlySidebar)b).OnOpenChanged((bool)v));
    /// <summary>Identifies <see cref="OpenLabel"/>.</summary>
    public static readonly BindableProperty OpenLabelProperty = BindableProperty.Create(nameof(OpenLabel), typeof(string), typeof(KlySidebar), "Open sidebar", propertyChanged: (b, _, v) => ((KlySidebar)b)._trigger.Label = (string)v);
    /// <summary>Identifies <see cref="CloseLabel"/>.</summary>
    public static readonly BindableProperty CloseLabelProperty = BindableProperty.Create(nameof(CloseLabel), typeof(string), typeof(KlySidebar), "Close", propertyChanged: (b, _, v) => ((KlySidebar)b)._close.Label = (string)v);
    /// <summary>Identifies <see cref="CollapseLabel"/>.</summary>
    public static readonly BindableProperty CollapseLabelProperty = BindableProperty.Create(nameof(CollapseLabel), typeof(string), typeof(KlySidebar), "Collapse sidebar", propertyChanged: (b, _, v) => ((KlySidebar)b)._collapse.Label = (string)v);
    /// <summary>Identifies <see cref="ResizeLabel"/>.</summary>
    public static readonly BindableProperty ResizeLabelProperty = BindableProperty.Create(nameof(ResizeLabel), typeof(string), typeof(KlySidebar), "Resize sidebar", propertyChanged: (b, _, v) => ((KlySidebar)b)._handle.Label = (string)v);

    private readonly Border _scrim = new() { StrokeThickness = 0, IsVisible = false };
    private readonly Grid _panelHost = new() { HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Fill };
    private readonly Grid _frame = new();
    private readonly Label _mobileTitle = new() { Text = "Sidebar", FontSize = 16, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center };
    private readonly LayoutIconButton _close = new(KlyIconNames.Close, "Close") { VerticalOptions = LayoutOptions.Center };
    private readonly Grid _mobileHeader = new() { HeightRequest = 64, Padding = new Thickness(16, 0), ColumnSpacing = 8 };
    private readonly Border _mobileLine = LayoutParts.Line();
    private readonly Grid _header = new() { HeightRequest = 64, ColumnSpacing = 8 };
    private readonly ContentView _headerContent = LayoutParts.Slot();
    private readonly LayoutIconButton _collapse = new(KlyIconNames.ChevronLeft, "Collapse sidebar") { VerticalOptions = LayoutOptions.Center };
    private readonly Border _headerLine = LayoutParts.Line();
    private readonly ScrollView _scroll = new() { Orientation = ScrollOrientation.Vertical };
    private readonly ContentView _content = new();
    private readonly ContentView _footerContent = LayoutParts.Slot();
    private readonly Border _footerLine = LayoutParts.Line();
    private readonly KlyDashboardResizeHandle _handle = new() { HorizontalOptions = LayoutOptions.End, IsVisible = false };
    private readonly LayoutIconButton _trigger = new(KlyIconNames.Menu, "Open sidebar") { HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(8), IsVisible = false };

    private double _availableWidth;
    private bool _panel = true;
    private bool _external;
    private Page? _page;

    /// <summary>Creates a sidebar.</summary>
    public KlySidebar()
    {
        _scrim.SetThemeColor(BackgroundColorProperty, LayoutParts.Scrim);
        _scrim.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => IsOpen = false) });
        AutomationProperties.SetIsInAccessibleTree(_scrim, false);

        _mobileTitle.SetThemeColor(Label.TextColorProperty, KlyTheme.TextHighlighted);
        _close.Clicked += (_, _) => IsOpen = false;
        _mobileHeader.ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)];
        _mobileHeader.Add(_mobileTitle, 0);
        _mobileHeader.Add(_close, 1);
        SemanticProperties.SetHeadingLevel(_mobileTitle, SemanticHeadingLevel.Level2);

        _header.ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)];
        _header.Padding = new Thickness(16, 0);
        _headerContent.VerticalOptions = LayoutOptions.Center;
        _header.Add(_headerContent, 0);
        _header.Add(_collapse, 1);
        _collapse.Clicked += (_, _) => Collapsed = !Collapsed;

        _scroll.Content = _content;
        _footerContent.VerticalOptions = LayoutOptions.Center;

        _frame.RowDefinitions =
        [
            new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto),
            new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto),
        ];
        _frame.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        _frame.Add(_mobileHeader, 0, 0);
        _frame.Add(_mobileLine, 0, 1);
        _frame.Add(_header, 0, 2);
        _frame.Add(_headerLine, 0, 3);
        _frame.Add(_scroll, 0, 4);
        _frame.Add(_footerLine, 0, 5);
        _frame.Add(_footerContent, 0, 6);
        // The border of the column, on its end side.
        _panelHost.Add(_frame);
        _panelHost.Add(LayoutParts.Line(vertical: true));
        _handle.TranslationX = 10;
        _handle.Label = ResizeLabel;
        _handle.Dragged += (_, distance) => ResizeBy(distance);
        _panelHost.Add(_handle);

        this.Add(_scrim);
        this.Add(_panelHost);
        this.Add(_trigger);
        _trigger.Clicked += (_, _) => IsOpen = true;

        Loaded += (_, _) => WatchPage();
        Unloaded += (_, _) => UnwatchPage();
        ApplyState();
        // Views added later inside the sidebar get its state too.
        DescendantAdded += (_, _) => NotifyParts();
    }

    /// <summary>Raised with the new state when the sidebar collapses to icons or expands.</summary>
    public event EventHandler<ToggledEventArgs>? CollapsedChanged;

    /// <summary>Raised with the new state when the panel (below 1024 wide) opens or closes.</summary>
    public event EventHandler<ToggledEventArgs>? OpenChanged;

    /// <summary>The scrolling middle: navigation, search.</summary>
    public View? ChildContent { get => (View?)GetValue(ChildContentProperty); set => SetValue(ChildContentProperty, value); }

    /// <summary>Top of the sidebar: a logo and a title. Hidden while the sidebar is collapsed, next to the collapse button.</summary>
    public View? Header { get => (View?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Bottom of the sidebar: the signed-in user, settings.</summary>
    public View? Footer { get => (View?)GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    /// <summary>A name that <see cref="KlyDashboardSidebarCollapse"/> and <see cref="KlyDashboardSidebarToggle"/> find the sidebar by (their <c>For</c>); the <c>Id</c> of the web component, renamed because <c>Element.Id</c> is a unique identifier. Without it they take the first sidebar of the dashboard.</summary>
    public string? SidebarId { get => (string?)GetValue(SidebarIdProperty); set => SetValue(SidebarIdProperty, value); }

    /// <summary>Shows the button that collapses the sidebar to icons, in its header (default).</summary>
    public bool Collapsible { get => (bool)GetValue(CollapsibleProperty); set => SetValue(CollapsibleProperty, value); }

    /// <summary>Whether the column is collapsed to an icon rail. Two-way bindable. The views inside see it through <see cref="CollapsedChanged"/>.</summary>
    public bool Collapsed { get => (bool)GetValue(CollapsedProperty); set => SetValue(CollapsedProperty, value); }

    /// <summary>Adds a handle on the edge to resize the column by dragging, between <see cref="MinWidth"/> and <see cref="MaxWidth"/>.</summary>
    public bool Resizable { get => (bool)GetValue(ResizableProperty); set => SetValue(ResizableProperty, value); }

    /// <summary>Width of the expanded column in units, 256 by default. Two-way bindable, so an app can keep what the user resized to. It is called <c>SidebarWidth</c> because <c>Width</c> is the rendered width of any view.</summary>
    public double SidebarWidth { get => (double)GetValue(SidebarWidthProperty); set => SetValue(SidebarWidthProperty, value); }

    /// <summary>Smallest width the user can resize the column to. Defaults to 192.</summary>
    public double MinWidth { get => (double)GetValue(MinWidthProperty); set => SetValue(MinWidthProperty, value); }

    /// <summary>Largest width the user can resize the column to. Defaults to 448.</summary>
    public double MaxWidth { get => (double)GetValue(MaxWidthProperty); set => SetValue(MaxWidthProperty, value); }

    /// <summary>Shows a menu button over the content that opens the panel, while the sidebar is a closed panel (default). Turn it off when another button opens it, such as <see cref="KlyDashboardSidebarToggle"/>.</summary>
    public bool MobileTrigger { get => (bool)GetValue(MobileTriggerProperty); set => SetValue(MobileTriggerProperty, value); }

    /// <summary>Title of the panel, in the language of the app. Defaults to "Sidebar".</summary>
    public string MobileTitle { get => (string)GetValue(MobileTitleProperty); set => SetValue(MobileTitleProperty, value); }

    /// <summary>Whether the panel is open. Two-way bindable. Only meaningful below 1024 wide; the sidebar closes it when it grows into a column.</summary>
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>Accessible name of the menu button that opens the panel. Defaults to "Open sidebar".</summary>
    public string OpenLabel { get => (string)GetValue(OpenLabelProperty); set => SetValue(OpenLabelProperty, value); }

    /// <summary>Accessible name of the button that closes the panel. Defaults to "Close".</summary>
    public string CloseLabel { get => (string)GetValue(CloseLabelProperty); set => SetValue(CloseLabelProperty, value); }

    /// <summary>Accessible name of the collapse button. Defaults to "Collapse sidebar".</summary>
    public string CollapseLabel { get => (string)GetValue(CollapseLabelProperty); set => SetValue(CollapseLabelProperty, value); }

    /// <summary>Accessible name of the resize handle. Defaults to "Resize sidebar".</summary>
    public string ResizeLabel { get => (string)GetValue(ResizeLabelProperty); set => SetValue(ResizeLabelProperty, value); }

    /// <summary>True while the sidebar is the slide-in panel (below 1024 wide), false while it is a column. It starts as a panel, as the mobile first styles of the web.</summary>
    public bool IsPanel => _panel;

    /// <summary>The width the panel takes: 320, or 85% of the available width when that is less.</summary>
    internal double PanelWidth => _availableWidth > 0 ? Math.Min(PanelMaxWidth, 0.85 * _availableWidth) : PanelMaxWidth;

    /// <summary>Tells the sidebar how wide the space is that it lives in: the page, or the dashboard. A dashboard group calls it, and the sidebar stops watching the page.</summary>
    internal void UpdateAvailableWidth(double width, bool external = false)
    {
        if (external) _external = true;
        else if (_external) return;
        _availableWidth = width;
        var panel = !LayoutBreakpoints.IsWide(width);
        if (panel == _panel)
        {
            ApplyState();
            return;
        }
        _panel = panel;
        if (!panel && IsOpen) IsOpen = false;
        ApplyState();
        NotifyParts();
    }

    /// <summary>Moves the edge of the column by <paramref name="distance"/> units, within the limits.</summary>
    internal void ResizeBy(double distance)
    {
        if (_panel || Collapsed) return;
        SidebarWidth = Math.Clamp(SidebarWidth + distance, MinWidth, MaxWidth);
    }

    private static void Fill(BindableObject owner, ContentView slot, View? view)
    {
        LayoutParts.Fill(slot, view);
        ((KlySidebar)owner).ApplyState();
    }

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlySidebar)bindable).ApplyState();

    private void OnCollapsedChanged(bool collapsed)
    {
        ApplyState();
        NotifyParts();
        CollapsedChanged?.Invoke(this, new ToggledEventArgs(collapsed));
    }

    private void OnOpenChanged(bool open)
    {
        ApplyState();
        OpenChanged?.Invoke(this, new ToggledEventArgs(open));
    }

    private void ApplyState()
    {
        var panel = _panel;
        var rail = !panel && Collapsed;

        HorizontalOptions = panel ? LayoutOptions.Fill : LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Fill;
        // The panel draws over its neighbors; so does a resize handle that sticks out of the column.
        ZIndex = panel ? 10 : Resizable ? 1 : 0;
        // A closed panel covers its cell but must not catch the touches: only the menu button does.
        CascadeInputTransparent = false;
        InputTransparent = panel && !IsOpen;

        _scrim.IsVisible = panel && IsOpen;
        _panelHost.IsVisible = !panel || IsOpen;
        _panelHost.WidthRequest = panel ? PanelWidth : rail ? RailWidth : SidebarWidth;
        _trigger.IsVisible = panel && !IsOpen && MobileTrigger;

        _mobileHeader.IsVisible = panel;
        _mobileLine.IsVisible = panel;
        _collapse.IsVisible = !panel && Collapsible;
        _collapse.IconName = Collapsed ? KlyIconNames.ChevronRight : KlyIconNames.ChevronLeft;
        _headerContent.IsVisible = Header is not null && !rail;
        var hasHeader = _headerContent.IsVisible || _collapse.IsVisible;
        _header.IsVisible = hasHeader;
        _headerLine.IsVisible = hasHeader;
        _header.Padding = rail ? new Thickness(0) : new Thickness(16, 0);
        LayoutParts.Column(_collapse, rail ? 0 : 1);
        LayoutParts.ColumnSpan(_collapse, rail ? 2 : 1);
        _collapse.HorizontalOptions = rail ? LayoutOptions.Center : LayoutOptions.End;

        _scroll.Padding = rail ? new Thickness(8, 16) : new Thickness(16);
        _footerContent.IsVisible = Footer is not null;
        _footerLine.IsVisible = Footer is not null;
        _footerContent.Padding = rail ? new Thickness(8, 16) : new Thickness(16);
        _footerContent.HorizontalOptions = rail ? LayoutOptions.Center : LayoutOptions.Fill;

        _handle.IsVisible = !panel && !rail && Resizable;
    }

    private void NotifyParts()
    {
        foreach (var part in LayoutTree.Descendants<ILayoutSidebarPart>(this).ToList()) part.SidebarChanged(this);
    }

    private void WatchPage()
    {
        if (_external) return;
        UnwatchPage();
        _page = LayoutTree.Ancestor<Page>(this);
        if (_page is null) return;
        _page.SizeChanged += OnPageSizeChanged;
        if (_page.Width > 0) UpdateAvailableWidth(_page.Width);
    }

    private void UnwatchPage()
    {
        if (_page is null) return;
        _page.SizeChanged -= OnPageSizeChanged;
        _page = null;
    }

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        if (_page is { Width: > 0 }) UpdateAvailableWidth(_page.Width);
    }
}
