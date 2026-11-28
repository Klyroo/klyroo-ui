namespace Klyroo.UI.Maui;

/// <summary>
/// A splitter to drag between two panels of a <see cref="KlyDashboardGroup"/>, or on the edge of a <see cref="KlySidebar"/>. Put it between two <see cref="KlyDashboardPanel"/>s,
/// or let <see cref="KlyDashboardPanel.Resizable"/> place one. It is hidden below 1024 wide, where the panels do not resize.
/// </summary>
public class KlyDashboardResizeHandle : Border
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(double), typeof(KlyDashboardResizeHandle), 50d, propertyChanged: (b, _, _) => ((KlyDashboardResizeHandle)b).ApplyAccessibility());
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyDashboardResizeHandle), "Resize panels", propertyChanged: (b, _, _) => ((KlyDashboardResizeHandle)b).ApplyAccessibility());

    private readonly Border _line = new() { StrokeThickness = 0, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Fill, InputTransparent = true };
    private double _last;
    private bool _dragging;

    /// <summary>Creates a handle.</summary>
    public KlyDashboardResizeHandle()
    {
        // A wide strip to grab with a finger around a thin line.
        StrokeThickness = 0;
        BackgroundColor = Colors.Transparent;
        WidthRequest = 20;
        VerticalOptions = LayoutOptions.Fill;
        HorizontalOptions = LayoutOptions.Center;
        ZIndex = 5;
        Content = _line;
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += (_, e) => HandlePan(e.StatusType, e.TotalX);
        GestureRecognizers.Add(pan);
        ApplyStyle();
        ApplyAccessibility();
    }

    /// <summary>The size in percent of the panel before the handle, read out by a screen reader. A panel keeps it up to date.</summary>
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Accessible name, in the language of the app.</summary>
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>True while a finger or the mouse drags the handle.</summary>
    public bool IsDragging => _dragging;

    /// <summary>Raised with the distance in units the handle moved since the last event, positive toward the end side.</summary>
    internal event EventHandler<double>? Dragged;

    /// <summary>Moves the handle by <paramref name="distance"/> units, as a drag would.</summary>
    internal void Drag(double distance) => Dragged?.Invoke(this, distance);

    internal void SetDragging(bool dragging)
    {
        _dragging = dragging;
        ApplyStyle();
    }

    /// <summary>What a pan gesture does: starts a drag, reports the distance since the last update, ends the drag.</summary>
    internal void HandlePan(GestureStatus status, double totalX)
    {
        switch (status)
        {
            case GestureStatus.Started:
                _last = 0;
                SetDragging(true);
                break;
            case GestureStatus.Running:
                var distance = totalX - _last;
                _last = totalX;
                if (distance != 0) Drag(distance);
                break;
            default:
                SetDragging(false);
                break;
        }
    }

    private void ApplyStyle()
    {
        _line.WidthRequest = _dragging ? 4 : 2;
        _line.SetThemeColor(BackgroundColorProperty, _dragging ? KlyTheme.BorderAccented : KlyTheme.Border);
        _line.SetRadius(2);
    }

    private void ApplyAccessibility()
    {
        SemanticProperties.SetDescription(this, Label);
        SemanticProperties.SetHint(this, $"{Math.Round(Value)}%");
    }
}
