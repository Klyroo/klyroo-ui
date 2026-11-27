namespace Klyroo.UI.Maui;

/// <summary>
/// The root of a dashboard: a row of a <see cref="KlyDashboardSidebar"/> and <see cref="KlyDashboardPanel"/>s that fills its parent, so the parent needs a size (the page, or a frame with a height).
/// From 1024 wide the sidebar is a column and the panels share what it leaves, as weights in percent; below, the sidebar is a slide-in panel over the panels and the resize handles are hidden.
/// Controls inside (<see cref="KlyDashboardSidebarToggle"/>, <see cref="KlyDashboardSidebarCollapse"/>, <see cref="KlyDashboardSearchButton"/>) find their dashboard, sidebar and search by themselves.
/// </summary>
public class KlyDashboardGroup : Grid
{
    /// <summary>Identifies <see cref="Sizes"/>.</summary>
    public static readonly BindableProperty SizesProperty = BindableProperty.Create(nameof(Sizes), typeof(IReadOnlyList<double>), typeof(KlyDashboardGroup), null, BindingMode.TwoWay, propertyChanged: (b, _, _) => ((KlyDashboardGroup)b).Rebuild());

    private const double HandleColumn = 0;
    private bool _narrow = true;
    private double _width;
    private double[] _resolved = [];

    /// <summary>Creates a dashboard.</summary>
    public KlyDashboardGroup()
    {
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        this.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        ChildAdded += OnChildAdded;
        ChildRemoved += OnChildRemoved;
        // Controls added deeper in the tree (a toggle in a navbar of a panel) learn the state of the dashboard too.
        DescendantAdded += (_, _) => NotifyParts();
        SizeChanged += (_, _) => UpdateWidth(Width);
    }

    /// <summary>Raised with the size of each panel, as weights in percent, when the user resizes them.</summary>
    public event EventHandler<IReadOnlyList<double>>? SizesChanged;

    /// <summary>
    /// Sizes of the panels in percent, one per panel in document order. Two-way bindable: bind it to keep what the user resized to.
    /// Without it the panels take their own <see cref="KlyDashboardPanel.Size"/>, and equal shares of what is left.
    /// </summary>
    public IReadOnlyList<double>? Sizes { get => (IReadOnlyList<double>?)GetValue(SizesProperty); set => SetValue(SizesProperty, value); }

    /// <summary>True below 1024 wide, where the sidebar is a slide-in panel. The dashboard starts narrow, as the mobile first styles of the web.</summary>
    public bool IsNarrow => _narrow;

    /// <summary>The size of each panel in percent now, in document order.</summary>
    internal IReadOnlyList<double> CurrentSizes => _resolved;

    /// <summary>The sidebar a control is meant for: the one with that <see cref="KlySidebar.SidebarId"/>, or the first one when none is given.</summary>
    internal KlySidebar? FindSidebar(string? id)
    {
        var sidebars = LayoutTree.Descendants<KlySidebar>(this);
        return id is null ? sidebars.FirstOrDefault() : sidebars.FirstOrDefault(s => s.SidebarId == id);
    }

    internal static KlyDashboardGroup? Of(Element? element) => LayoutTree.Ancestor<KlyDashboardGroup>(element);

    internal void UpdateWidth(double width)
    {
        if (width <= 0) return;
        _width = width;
        var narrow = !LayoutBreakpoints.IsWide(width);
        foreach (var sidebar in Children.OfType<KlySidebar>()) sidebar.UpdateAvailableWidth(width, external: true);
        if (narrow == _narrow) return;
        _narrow = narrow;
        Rebuild();
    }

    /// <summary>Moves the edge between the panel at <paramref name="index"/> and the next one by <paramref name="percent"/>, within the limits of both.</summary>
    internal void ResizeBy(int index, double percent)
    {
        var panels = Children.OfType<KlyDashboardPanel>().ToList();
        if (index < 0 || index >= panels.Count - 1) return;
        var sizes = Resolve(panels);
        var before = panels[index];
        var after = panels[index + 1];
        var low = Math.Max(before.MinSize - sizes[index], sizes[index + 1] - after.MaxSize);
        var high = Math.Min(before.MaxSize - sizes[index], sizes[index + 1] - after.MinSize);
        var delta = high < low ? 0 : Math.Clamp(percent, low, high);
        if (delta == 0) return;
        sizes[index] += delta;
        sizes[index + 1] -= delta;
        SetValue(SizesProperty, sizes);
        SizesChanged?.Invoke(this, sizes);
    }

    /// <summary>The sizes in percent: the bound ones when they fit the panels, else the panel sizes with the rest shared between the others.</summary>
    internal double[] Resolve(IReadOnlyList<KlyDashboardPanel> panels)
    {
        if (Sizes is { } bound && bound.Count == panels.Count && bound.All(s => s >= 0)) return [.. bound];
        var known = panels.Where(p => p.Size.HasValue).Sum(p => p.Size!.Value);
        var unsized = panels.Count(p => !p.Size.HasValue);
        var rest = Math.Max(0, 100 - known);
        var share = unsized == 0 ? 0 : rest > 0 ? rest / unsized : 1;
        return [.. panels.Select(p => p.Size ?? share)];
    }

    private void OnChildAdded(object? sender, ElementEventArgs e)
    {
        switch (e.Element)
        {
            case KlyDashboardPanel panel:
                panel.ResizeRequested += OnPanelResizeRequested;
                panel.PropertyChanged += OnPanelPropertyChanged;
                break;
            case KlyDashboardResizeHandle handle:
                handle.Dragged += OnHandleDragged;
                break;
        }
        Rebuild();
    }

    private void OnChildRemoved(object? sender, ElementEventArgs e)
    {
        switch (e.Element)
        {
            case KlyDashboardPanel panel:
                panel.ResizeRequested -= OnPanelResizeRequested;
                panel.PropertyChanged -= OnPanelPropertyChanged;
                break;
            case KlyDashboardResizeHandle handle:
                handle.Dragged -= OnHandleDragged;
                break;
        }
        Rebuild();
    }

    private void OnPanelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(KlyDashboardPanel.Size)) Rebuild();
    }

    private void OnPanelResizeRequested(object? sender, double distance)
    {
        var panels = Children.OfType<KlyDashboardPanel>().ToList();
        ResizeBy(panels.IndexOf((KlyDashboardPanel)sender!), ToPercent(panels, distance));
    }

    private void OnHandleDragged(object? sender, double distance)
    {
        // A handle between two panels resizes the one before it against the one after it.
        var handle = (KlyDashboardResizeHandle)sender!;
        var panels = Children.OfType<KlyDashboardPanel>().ToList();
        var before = Children.TakeWhile(c => !ReferenceEquals(c, handle)).OfType<KlyDashboardPanel>().Count() - 1;
        ResizeBy(before, ToPercent(panels, distance));
    }

    /// <summary>A distance in units as a share of the width the panels have together.</summary>
    private double ToPercent(IReadOnlyList<KlyDashboardPanel> panels, double distance)
    {
        var width = panels.Sum(p => Math.Max(0, p.Width));
        if (width <= 0) width = _width > 0 ? _width : Width;
        return width <= 0 ? 0 : distance / width * 100;
    }

    private void Rebuild()
    {
        var panels = Children.OfType<KlyDashboardPanel>().ToList();
        _resolved = Resolve(panels);

        ColumnDefinitions.Clear();
        var column = 0;
        var panelIndex = 0;
        var overlays = new List<View>();
        foreach (var child in Children.OfType<View>())
        {
            switch (child)
            {
                case KlySidebar sidebar when _narrow:
                    // A slide-in panel covers the whole dashboard.
                    overlays.Add(sidebar);
                    break;
                case KlySidebar sidebar:
                    ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                    LayoutParts.Column(sidebar, column);
                    LayoutParts.ColumnSpan(sidebar, 1);
                    column++;
                    break;
                case KlyDashboardPanel panel:
                    ColumnDefinitions.Add(new ColumnDefinition(new GridLength(Math.Max(_resolved[panelIndex], 0.0001), GridUnitType.Star)));
                    LayoutParts.Column(panel, column);
                    LayoutParts.ColumnSpan(panel, 1);
                    panel.ShowTrailingBorder = panelIndex < panels.Count - 1;
                    panel.ShowSize(_resolved[panelIndex]);
                    panelIndex++;
                    column++;
                    break;
                case KlyDashboardResizeHandle handle:
                    // A handle takes no width: it overlaps the two panels around it.
                    ColumnDefinitions.Add(new ColumnDefinition(new GridLength(HandleColumn)));
                    LayoutParts.Column(handle, column);
                    LayoutParts.ColumnSpan(handle, 1);
                    handle.IsVisible = !_narrow;
                    column++;
                    break;
                default:
                    overlays.Add(child);
                    break;
            }
        }

        if (column == 0) ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        var span = Math.Max(1, ColumnDefinitions.Count);
        foreach (var overlay in overlays)
        {
            LayoutParts.Column(overlay, 0);
            LayoutParts.ColumnSpan(overlay, span);
        }
        NotifyParts();
    }

    private void NotifyParts()
    {
        foreach (var part in LayoutTree.Descendants<ILayoutDashboardPart>(this).ToList()) part.DashboardChanged(this);
    }
}
