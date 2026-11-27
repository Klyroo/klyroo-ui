namespace Klyroo.UI.Maui;

/// <summary>The hue bar of a <see cref="KlyColorPicker"/>: the colors of the wheel from red to red, with a thumb at the current hue.</summary>
internal sealed class InputsHueBar : ThemedGraphicsView
{
    private const float BarHeight = 12;
    private const float ThumbRadius = 8;
    private double _hue;

    public InputsHueBar()
    {
        HeightRequest = 32;
        HorizontalOptions = LayoutOptions.Fill;
        StartInteraction += (_, e) => Move(e.Touches[0]);
        DragInteraction += (_, e) => Move(e.Touches[0]);
    }

    public event EventHandler<double>? Moved;

    public void Show(double hue)
    {
        _hue = hue;
        Invalidate();
    }

    /// <summary>The hue under a touch of a bar of a given width, from 0 to 359 degrees.</summary>
    public static double At(double x, double width) => width <= 0 ? 0 : Math.Round(Math.Clamp(x / width, 0, 1) * 359);

    public void Move(PointF touch) => Move(touch, Width);

    /// <summary>A touch in a bar of a given width.</summary>
    public void Move(PointF touch, double width)
    {
        if (IsEnabled) Moved?.Invoke(this, At(touch.X, width));
    }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var top = (dirtyRect.Height - BarHeight) / 2;
        var bar = new RectF(0, top, dirtyRect.Width, BarHeight);
        var stops = new[] { 0d, 60, 120, 180, 240, 300, 360 }.Select(h => new PaintGradientStop((float)(h / 360), InputsColor.ToColor(h, 1, 1))).ToArray();
        var paint = new LinearGradientPaint(stops, new Point(0, 0), new Point(1, 0));
        canvas.SetFillPaint(paint, bar);
        canvas.FillRoundedRectangle(bar, BarHeight / 2);
        canvas.StrokeColor = KlyTheme.BorderAccented.Resolve();
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(bar.Inflate(-0.5f, -0.5f), BarHeight / 2);

        var centre = new PointF((float)(_hue / 359 * dirtyRect.Width), dirtyRect.Height / 2);
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 1;
        canvas.DrawCircle(centre, ThumbRadius + 0.5f);
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 2;
        canvas.DrawCircle(centre, ThumbRadius - 0.5f);
    }
}
