namespace Klyroo.UI.Maui;

/// <summary>
/// Handle that moves a block of the editor by dragging. The web handle follows the block under the pointer; a native <see cref="Editor"/> cannot say where a line is
/// drawn, so this one sits beside the text and moves the block that holds the cursor: every <see cref="Step"/> units dragged up or down raise <see cref="Dragged"/> once,
/// and a <see cref="KlyEditor"/> that holds the handle moves the block one place. A block is a paragraph, a list or a fenced code block: lines up to the next blank line.
/// The handle is not a way in for a screen reader: the <see cref="EditorAction.MoveUp"/> and <see cref="EditorAction.MoveDown"/> toolbar buttons do the same.
/// </summary>
public class KlyEditorDragHandle : ContentView
{
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyEditorDragHandle), null, propertyChanged: (b, _, _) => ((KlyEditorDragHandle)b).Describe());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyEditorDragHandle), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyEditorDragHandle)b).ApplyStyle());
    /// <summary>Identifies <see cref="Step"/>.</summary>
    public static readonly BindableProperty StepProperty = BindableProperty.Create(nameof(Step), typeof(double), typeof(KlyEditorDragHandle), 32d, validateValue: (_, v) => (double)v > 0);

    private const string DefaultLabel = "Drag to move block";

    private readonly Border _face = new() { StrokeThickness = 0, WidthRequest = 24, HeightRequest = 24, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    private int _steps;
    private bool _dragging;

    /// <summary>Creates a handle.</summary>
    public KlyEditorDragHandle()
    {
        WidthRequest = HeightRequest = EditorToolbarButton.TouchTarget;
        _face.SetRadius(KlyTheme.RadiusOf(1) - 2);
        _face.Content = new KlyIcon { Name = KlyIconNames.Drag, Size = KlySize.Xs, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, InputTransparent = true, IconColor = KlyTheme.TextMuted };
        Content = _face;

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPan;
        GestureRecognizers.Add(pan);
        Describe();
        ApplyStyle();
    }

    /// <summary>Raised once per <see cref="Step"/> of drag, with the direction of the step.</summary>
    public event EventHandler<EditorDragEventArgs>? Dragged;

    /// <summary>What a screen reader says for the handle. Defaults to <c>Drag to move block</c>.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Semantic color of the handle while it is dragged.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Distance of drag, in device-independent units, that makes one step. Defaults to 32.</summary>
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.6;
    }

    /// <summary>Takes the distance dragged since the gesture began and raises <see cref="Dragged"/> for each step crossed, in either direction.</summary>
    internal void Pan(double totalY)
    {
        if (!IsEnabled) return;
        var steps = (int)Math.Truncate(totalY / Step);
        while (_steps < steps)
        {
            _steps++;
            Dragged?.Invoke(this, new EditorDragEventArgs(1));
        }
        while (_steps > steps)
        {
            _steps--;
            Dragged?.Invoke(this, new EditorDragEventArgs(-1));
        }
    }

    internal void EndPan()
    {
        _steps = 0;
        SetDragging(false);
    }

    internal bool Dragging => _dragging;

    private void OnPan(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _steps = 0;
                break;
            case GestureStatus.Running:
                SetDragging(true);
                Pan(e.TotalY);
                break;
            case GestureStatus.Completed or GestureStatus.Canceled:
                EndPan();
                break;
        }
    }

    private void SetDragging(bool dragging)
    {
        if (_dragging == dragging) return;
        _dragging = dragging;
        ApplyStyle();
    }

    private void Describe()
    {
        SemanticProperties.SetDescription(this, Label ?? DefaultLabel);
        SemanticProperties.SetHint(this, "Drag up or down to move the block that holds the cursor");
    }

    private void ApplyStyle()
    {
        // While it is dragged the handle takes the tint and the ring of a subtle surface, as a pressed toolbar button does.
        _face.Apply(_dragging ? Surface.For(ButtonVariant.Subtle, KlyTheme.Resolve(Color)) : new Surface(KlyThemeColor.Transparent, KlyThemeColor.Transparent, 0, KlyTheme.TextMuted));
    }
}
