namespace Klyroo.UI.Maui;

/// <summary>
/// Resizable panels separated by drag handles. Drag a handle to move the border between its two neighbors; <see cref="Sizes"/> reports the result once the gesture ends.
/// The control needs a size from its parent. Each handle is 40 units wide (or high) so a finger can grab it.
/// </summary>
public class KlySplitter : Grid
{
    /// <summary>Identifies <see cref="Panels"/>.</summary>
    public static readonly BindableProperty PanelsProperty = BindableProperty.Create(nameof(Panels), typeof(IReadOnlyList<KlySplitterPanel>), typeof(KlySplitter), Array.Empty<KlySplitterPanel>(), propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Sizes"/>.</summary>
    public static readonly BindableProperty SizesProperty = BindableProperty.Create(nameof(Sizes), typeof(IReadOnlyList<double>), typeof(KlySplitter), null, BindingMode.TwoWay, propertyChanged: (b, o, n) => ((KlySplitter)b).OnSizesChanged((IReadOnlyList<double>?)o, (IReadOnlyList<double>?)n));
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlySplitter), StackOrientation.Horizontal, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Step"/>.</summary>
    public static readonly BindableProperty StepProperty = BindableProperty.Create(nameof(Step), typeof(double), typeof(KlySplitter), 5d);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlySplitter), KlyColor.Primary, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ResizeLabel"/>.</summary>
    public static readonly BindableProperty ResizeLabelProperty = BindableProperty.Create(nameof(ResizeLabel), typeof(string), typeof(KlySplitter), "Resize panels", propertyChanged: Rebuild);

    /// <summary>The width of a vertical handle, the height of a horizontal one: a touch target.</summary>
    internal const double HandleThickness = 40;

    private readonly List<View> _handles = [];
    private readonly List<View> _panelViews = [];
    private readonly List<BoxView> _handleLines = [];
    private double[] _sizes = [];
    private double[] _dragStart = [];

    /// <summary>Creates a splitter.</summary>
    public KlySplitter()
    {
        IsClippedToBounds = true;
        Rebuild();
    }

    /// <summary>Raised with the size of each panel, as weights in percent, when it changes: once a drag ends, after <see cref="Resize"/>, or when set by code.</summary>
    public event EventHandler<KlyNavigationValueEventArgs<IReadOnlyList<double>?>>? SizesChanged;

    /// <summary>The panels, in order, as <see cref="KlySplitterPanel"/>. A handle sits between two neighbors.</summary>
    public IReadOnlyList<KlySplitterPanel> Panels { get => (IReadOnlyList<KlySplitterPanel>)GetValue(PanelsProperty); set => SetValue(PanelsProperty, value); }

    /// <summary>Sizes in percent, one per panel. When not set, the sizes of the panels (or an equal share of what they leave) apply. Two-way bindable.</summary>
    public IReadOnlyList<double>? Sizes { get => (IReadOnlyList<double>?)GetValue(SizesProperty); set => SetValue(SizesProperty, value); }

    /// <summary><c>Horizontal</c> places the panels side by side (the handles are vertical); <c>Vertical</c> stacks them.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Size change of <see cref="Nudge"/>, in percent. A drag has no step.</summary>
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>Semantic color of a handle while it is dragged.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>What a screen reader says for a handle. With more than two panels the number of the handle follows.</summary>
    public string ResizeLabel { get => (string)GetValue(ResizeLabelProperty); set => SetValue(ResizeLabelProperty, value); }

    /// <summary>The sizes that show now, in percent.</summary>
    internal IReadOnlyList<double> CurrentSizes => _sizes;

    internal IReadOnlyList<View> Handles => _handles;

    internal IReadOnlyList<View> PanelViews => _panelViews;

    /// <summary>The initial sizes: the size of each panel, and an equal share of what they leave for those without one.</summary>
    internal static double[] InitialSizes(IReadOnlyList<KlySplitterPanel> panels)
    {
        var specified = panels.Where(p => p.Size.HasValue).Sum(p => p.Size!.Value);
        var free = panels.Count(p => !p.Size.HasValue);
        var each = free == 0 ? 0 : Math.Max(0, 100 - specified) / free;
        return [.. panels.Select(p => p.Size ?? each)];
    }

    /// <summary>The range of the size of panel <paramref name="index"/> when the handle after it moves: its own limits and those of its neighbor.</summary>
    internal static (double Min, double Max) Range(IReadOnlyList<KlySplitterPanel> panels, IReadOnlyList<double> sizes, int index)
    {
        var pair = sizes[index] + sizes[index + 1];
        var a = panels[index];
        var b = panels[index + 1];
        return (Math.Max(a.MinSize, pair - b.MaxSize), Math.Min(a.MaxSize, pair - b.MinSize));
    }

    /// <summary>Moves the border after panel <paramref name="handleIndex"/> by <paramref name="delta"/> percent (negative moves it back), within the limits of the two panels, and reports the new sizes.</summary>
    /// <param name="handleIndex">The handle, from 0 for the one after the first panel.</param>
    /// <param name="delta">The change of size of the panel before the handle, in percent.</param>
    public void Resize(int handleIndex, double delta)
    {
        var panels = Panels;
        if (handleIndex < 0 || handleIndex >= panels.Count - 1) throw new ArgumentOutOfRangeException(nameof(handleIndex));
        var sizes = (double[])_sizes.Clone();
        Move(sizes, sizes, handleIndex, delta);
        Sizes = sizes;
    }

    /// <summary>Moves the border after panel <paramref name="handleIndex"/> by one <see cref="Step"/>: a negative <paramref name="direction"/> shrinks the panel before it, a positive one grows it. For a keyboard, a switch or an accessibility action.</summary>
    /// <param name="handleIndex">The handle, from 0 for the one after the first panel.</param>
    /// <param name="direction">Negative or positive.</param>
    public void Nudge(int handleIndex, int direction) => Resize(handleIndex, Math.Sign(direction) * Step);

    // Writes into target the sizes of the pair around a handle, starting from the sizes of start.
    private void Move(double[] start, double[] target, int handle, double delta)
    {
        var (min, max) = Range(Panels, start, handle);
        var pair = start[handle] + start[handle + 1];
        var a = Math.Clamp(start[handle] + delta, min, Math.Max(min, max));
        target[handle] = a;
        target[handle + 1] = pair - a;
    }

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlySplitter)bindable).Rebuild();

    private void OnSizesChanged(IReadOnlyList<double>? oldValue, IReadOnlyList<double>? newValue)
    {
        Rebuild();
        SizesChanged?.Invoke(this, new KlyNavigationValueEventArgs<IReadOnlyList<double>?>(newValue, oldValue));
    }

    private void Rebuild()
    {
        var panels = Panels;
        var horizontal = Orientation == StackOrientation.Horizontal;
        _sizes = Sizes is { } given && given.Count == panels.Count ? [.. given] : InitialSizes(panels);

        Children.Clear();
        RowDefinitions.Clear();
        ColumnDefinitions.Clear();
        _handles.Clear();
        _panelViews.Clear();
        _handleLines.Clear();
        RowSpacing = 0;
        ColumnSpacing = 0;

        var cell = 0;
        for (var i = 0; i < panels.Count; i++)
        {
            var weight = new GridLength(Math.Max(_sizes[i], 0.0001), GridUnitType.Star);
            if (horizontal) ColumnDefinitions.Add(new ColumnDefinition(weight));
            else RowDefinitions.Add(new RowDefinition(weight));
            var panel = new ContentView { Content = panels[i].Content, IsClippedToBounds = true };
            _panelViews.Add(panel);
            Place(panel, cell++, horizontal);

            if (i < panels.Count - 1)
            {
                if (horizontal) ColumnDefinitions.Add(new ColumnDefinition(new GridLength(HandleThickness)));
                else RowDefinitions.Add(new RowDefinition(new GridLength(HandleThickness)));
                var handle = Handle(i, horizontal);
                _handles.Add(handle);
                Place(handle, cell++, horizontal);
            }
        }
    }

    private void Place(View view, int cell, bool horizontal)
    {
        Children.Add(view);
        if (horizontal) SetColumn((BindableObject)view, cell);
        else SetRow((BindableObject)view, cell);
    }

    private View Handle(int index, bool horizontal)
    {
        var handle = new Grid { BackgroundColor = Colors.Transparent };
        var line = NavigationLayout.Line(KlyTheme.BorderAccented, width: horizontal ? 1 : null, height: horizontal ? null : 1);
        line.HorizontalOptions = horizontal ? LayoutOptions.Center : LayoutOptions.Fill;
        line.VerticalOptions = horizontal ? LayoutOptions.Fill : LayoutOptions.Center;
        handle.Children.Add(line);
        _handleLines.Add(line);

        // A grip on the line says it can be dragged; it lies across the line of a horizontal handle.
        var grip = NavigationLayout.Icon(KlyIconNames.Drag, KlySize.Sm, KlyTheme.TextMuted);
        grip.Rotation = horizontal ? 0 : 90;
        handle.Children.Add(grip);

        var label = Panels.Count == 2 ? ResizeLabel : $"{ResizeLabel} {index + 1}";
        SemanticProperties.SetDescription(handle, label);
        SemanticProperties.SetHint(handle, string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}%", Math.Round(_sizes[index])));

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += (_, e) => OnPan(index, e, horizontal);
        handle.GestureRecognizers.Add(pan);
        return handle;
    }

    private void OnPan(int index, PanUpdatedEventArgs e, bool horizontal)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _dragStart = (double[])_sizes.Clone();
                _handleLines[index].SetThemeColor(BoxView.ColorProperty, KlyTheme.Resolve(Color).Fill);
                break;
            case GestureStatus.Running when _dragStart.Length == _sizes.Length:
                Drag(index, horizontal ? e.TotalX : e.TotalY, horizontal ? Width : Height);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _handleLines[index].SetThemeColor(BoxView.ColorProperty, KlyTheme.BorderAccented);
                if (_dragStart.Length == _sizes.Length) Sizes = (double[])_sizes.Clone();
                _dragStart = [];
                break;
        }
    }

    /// <summary>Moves the border of a handle by a distance in device-independent units, during a drag. The sizes are reported when the drag ends.</summary>
    internal void Drag(int handle, double distance, double length)
    {
        var available = length - HandleThickness * (Panels.Count - 1);
        if (available <= 0 || _dragStart.Length != _sizes.Length) return;
        var delta = distance / available * _dragStart.Sum();
        Array.Copy(_dragStart, _sizes, _sizes.Length);
        Move(_dragStart, _sizes, handle, delta);

        var horizontal = Orientation == StackOrientation.Horizontal;
        foreach (var i in new[] { handle, handle + 1 })
        {
            var weight = new GridLength(Math.Max(_sizes[i], 0.0001), GridUnitType.Star);
            if (horizontal) ColumnDefinitions[i * 2].Width = weight;
            else RowDefinitions[i * 2].Height = weight;
        }
    }

    /// <summary>Starts a drag, for the tests.</summary>
    internal void BeginDrag() => _dragStart = (double[])_sizes.Clone();

    /// <summary>Ends a drag and reports the sizes, for the tests.</summary>
    internal void EndDrag()
    {
        if (_dragStart.Length == _sizes.Length) Sizes = (double[])_sizes.Clone();
        _dragStart = [];
    }
}
