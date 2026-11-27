namespace Klyroo.UI.Maui;

/// <summary>The saturation and brightness area of a <see cref="KlyColorPicker"/>: saturation grows to the right, brightness upward, in the current hue.</summary>
internal sealed class InputsColorArea : ThemedGraphicsView
{
    private double _hue;
    private double _saturation;
    private double _brightness;

    public InputsColorArea()
    {
        HeightRequest = 160;
        HorizontalOptions = LayoutOptions.Fill;
        StartInteraction += (_, e) => Move(e.Touches[0]);
        DragInteraction += (_, e) => Move(e.Touches[0]);
    }

    public event EventHandler<(double Saturation, double Brightness)>? Moved;

    public void Show(double hue, double saturation, double brightness)
    {
        _hue = hue;
        _saturation = saturation;
        _brightness = brightness;
        Invalidate();
    }

    /// <summary>The saturation and brightness under a touch of an area of a given size.</summary>
    public static (double Saturation, double Brightness) At(double x, double y, double width, double height) =>
        (width <= 0 ? 0 : Math.Clamp(x / width, 0, 1), height <= 0 ? 0 : 1 - Math.Clamp(y / height, 0, 1));

    public void Move(PointF touch) => Move(touch, Width, Height);

    /// <summary>A touch in an area of a given size.</summary>
    public void Move(PointF touch, double width, double height)
    {
        if (!IsEnabled) return;
        Moved?.Invoke(this, At(touch.X, touch.Y, width, height));
    }

    private static LinearGradientPaint Gradient(Color from, Color to, Point start, Point end) =>
        new([new PaintGradientStop(0, from), new PaintGradientStop(1, to)], start, end);

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var radius = (float)KlyTheme.RadiusOf(1);
        canvas.FillColor = InputsColor.ToColor(_hue, 1, 1);
        canvas.FillRoundedRectangle(dirtyRect, radius);
        canvas.SetFillPaint(Gradient(Colors.White, Colors.White.WithAlpha(0), new Point(0, 0), new Point(1, 0)), dirtyRect);
        canvas.FillRoundedRectangle(dirtyRect, radius);
        canvas.SetFillPaint(Gradient(Colors.Black.WithAlpha(0), Colors.Black, new Point(0, 0), new Point(0, 1)), dirtyRect);
        canvas.FillRoundedRectangle(dirtyRect, radius);
        canvas.StrokeColor = KlyTheme.BorderAccented.Resolve();
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(dirtyRect.Inflate(-0.5f, -0.5f), radius);

        // The thumb is white with a thin black outline: it reads on every color of the area.
        var centre = new PointF((float)(_saturation * dirtyRect.Width), (float)((1 - _brightness) * dirtyRect.Height));
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 1;
        canvas.DrawCircle(centre, 8.5f);
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 2;
        canvas.DrawCircle(centre, 7.5f);
    }
}
