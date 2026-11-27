namespace Klyroo.UI.Maui;

/// <summary>One arc of the ring of a <see cref="KlyMeter"/>: a share of the circle from an offset, both in percent of the circle.</summary>
internal sealed record DisplayRingArc(double Percent, double Offset, KlyThemeColor Color, bool RoundCap = false);

/// <summary>The ring of a <see cref="KlyMeter"/>: a track and one arc per share, drawn from the top clockwise.</summary>
internal sealed class DisplayMeterRing : ThemedGraphicsView
{
    // The proportions of the web drawing: a 36 unit box, a circle of radius 15.9155 (a circumference of 100) and a stroke of 3.5.
    private const float BoxSize = 36;
    private const float Radius = 15.9155f;
    private const float StrokeWidth = 3.5f;

    private IReadOnlyList<DisplayRingArc> _arcs = [];

    public DisplayMeterRing()
    {
        WidthRequest = 80;
        HeightRequest = 80;
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        AutomationProperties.SetIsInAccessibleTree(this, false);
    }

    /// <summary>The arcs over the track.</summary>
    public IReadOnlyList<DisplayRingArc> Arcs
    {
        get => _arcs;
        set
        {
            _arcs = value;
            Invalidate();
        }
    }

    /// <summary>The path of an arc: points every 2 degrees from the top, clockwise, so no angle convention of a platform is involved.</summary>
    public static PathF ArcPath(float centerX, float centerY, float radius, double offsetPercent, double percent)
    {
        var start = offsetPercent / 100 * 360;
        var sweep = Math.Clamp(percent, 0, 100) / 100 * 360;
        var steps = Math.Max(1, (int)Math.Ceiling(sweep / 2));
        PathF? path = null;
        for (var i = 0; i <= steps; i++)
        {
            var angle = (start + sweep * i / steps) * Math.PI / 180;
            var point = new PointF(centerX + radius * (float)Math.Sin(angle), centerY - radius * (float)Math.Cos(angle));
            if (path is null) path = new PathF(point);
            else path.LineTo(point);
        }
        return path!;
    }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var scale = Math.Min(dirtyRect.Width, dirtyRect.Height) / BoxSize;
        var centerX = dirtyRect.Center.X;
        var centerY = dirtyRect.Center.Y;
        canvas.StrokeSize = StrokeWidth * scale;
        canvas.StrokeColor = KlyTheme.BackgroundMuted.Resolve();
        canvas.DrawCircle(centerX, centerY, Radius * scale);
        foreach (var arc in _arcs)
        {
            if (arc.Percent <= 0) continue;
            canvas.StrokeColor = arc.Color.Resolve();
            canvas.StrokeLineCap = arc.RoundCap ? LineCap.Round : LineCap.Butt;
            canvas.DrawPath(ArcPath(centerX, centerY, Radius * scale, arc.Offset, arc.Percent));
        }
    }
}
