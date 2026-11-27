using GraphicsFont = Microsoft.Maui.Graphics.Font;

namespace Klyroo.UI.Maui;

/// <summary>
/// The drawing of a <see cref="KlyChart{TItem, TIndex}"/>: grid, axes, lines, areas, bars and slices, then the tooltip of the row under the finger.
/// Series differ by dash, marker and fill pattern as well as by color, as on the web. Colors are resolved while drawing, so a theme change redraws it.
/// </summary>
internal sealed class ChartDataCanvas : ThemedGraphicsView
{
    private static readonly float[]?[] Dashes = [null, [7, 4], [2, 4], [9, 4, 2, 4], [1, 3]];

    private ChartDataModel _model = new();
    private ChartDataLayout? _layout;
    private SizeF _laidOutFor;

    public ChartDataCanvas()
    {
        StartInteraction += (_, e) => Touch(e.Touches, false);
        DragInteraction += (_, e) => Touch(e.Touches, false);
        EndInteraction += (_, e) => Touch(e.Touches, true);
    }

    /// <summary>Raised when a bar, a point or a slice is tapped, with the row and the series (-1 when none could be named).</summary>
    public event Action<int, int>? Picked;

    /// <summary>The row the finger is on, or the last one it touched. -1 when none.</summary>
    public int ActiveRow { get; internal set; } = -1;

    /// <summary>Shows the tooltip of the active row.</summary>
    public bool TooltipEnabled { get; set; } = true;

    /// <summary>The numbers and the options to draw.</summary>
    public ChartDataModel Model
    {
        get => _model;
        set
        {
            _model = value;
            _layout = null;
            ActiveRow = -1;
            Invalidate();
        }
    }

    /// <summary>The layout for a size, built again when the size changes.</summary>
    public ChartDataLayout LayoutFor(double width, double height)
    {
        var size = new SizeF((float)width, (float)height);
        if (_layout is null || size != _laidOutFor)
        {
            var m = _model;
            _layout = ChartDataLayout.Build(
                m.Type, m.Horizontal, m.Stacked, m.Curve, m.Index, m.Values.Select(v => (IReadOnlyList<double?>)v).ToList(),
                width, height, m.XAxis, m.YAxis, m.XLabel is not null, m.YLabel is not null, m.Thickness, m.Format);
            _laidOutFor = size;
        }
        return _layout;
    }

    /// <summary>Tells the canvas a finger is at a point: it activates the row there, and on lift it reports the pick. The size is the one of the view unless given.</summary>
    internal void TouchAt(PointF point, bool lifted, Size? size = null)
    {
        var width = size?.Width ?? Width;
        var height = size?.Height ?? Height;
        if (_model.Type == KlyChartType.Funnel || width <= 0 || height <= 0) return;
        var layout = LayoutFor(width, height);
        var row = layout.RowAt(point.X, point.Y);
        if (row != ActiveRow)
        {
            ActiveRow = row;
            Invalidate();
        }
        if (lifted && row >= 0) Picked?.Invoke(row, _model.Donut ? 0 : layout.SeriesAt(row, point.X, point.Y));
    }

    private void Touch(PointF[] touches, bool lifted)
    {
        if (touches.Length > 0) TouchAt(touches[0], lifted);
    }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var m = _model;
        if (m.Type == KlyChartType.Funnel || m.Series.Count == 0 || dirtyRect.Width <= 0 || dirtyRect.Height <= 0) return;
        var layout = LayoutFor(dirtyRect.Width, dirtyRect.Height);
        if (m.Donut) DrawDonut(canvas, layout, m);
        else DrawCartesian(canvas, layout, m);
        if (TooltipEnabled && ActiveRow >= 0 && ActiveRow < m.Index.Count) DrawTooltip(canvas, layout, m, dirtyRect);
    }

    private void DrawCartesian(ICanvas canvas, ChartDataLayout layout, ChartDataModel m)
    {
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = null;
        if (m.Grid)
        {
            foreach (var line in layout.GridLines)
            {
                canvas.StrokeColor = (line.Zero ? KlyTheme.BorderAccented : KlyTheme.Border).Resolve();
                canvas.DrawLine((float)line.X1, (float)line.Y1, (float)line.X2, (float)line.Y2);
            }
        }
        if (m.XAxis)
        {
            canvas.StrokeColor = KlyTheme.BorderAccented.Resolve();
            if (layout.Horizontal) canvas.DrawLine((float)layout.Left, (float)layout.Top, (float)layout.Left, (float)layout.Bottom);
            else canvas.DrawLine((float)layout.Left, (float)layout.Bottom, (float)layout.Right, (float)layout.Bottom);
            foreach (var label in layout.IndexLabels)
            {
                if (layout.Horizontal) Text(canvas, label.Text, 0, label.Position - 8, layout.Left - 8, 16, HorizontalAlignment.Right, VerticalAlignment.Center, KlyTheme.TextMuted, 11);
                else Text(canvas, label.Text, label.Position - 60, layout.Bottom + 4, 120, 16, HorizontalAlignment.Center, VerticalAlignment.Top, KlyTheme.TextMuted, 11);
            }
        }
        if (m.YAxis)
        {
            foreach (var tick in layout.ValueTicks)
            {
                if (layout.Horizontal) Text(canvas, tick.Label, tick.Position - 60, layout.Bottom + 4, 120, 16, HorizontalAlignment.Center, VerticalAlignment.Top, KlyTheme.TextMuted, 11);
                else Text(canvas, tick.Label, 0, tick.Position - 8, layout.Left - 8, 16, HorizontalAlignment.Right, VerticalAlignment.Center, KlyTheme.TextMuted, 11);
            }
        }
        var xTitleAlong = layout.Horizontal ? m.YLabel : m.XLabel;
        var yTitleAlong = layout.Horizontal ? m.XLabel : m.YLabel;
        if (xTitleAlong is not null) Text(canvas, xTitleAlong, layout.Left, layout.Height - 18, layout.Right - layout.Left, 16, HorizontalAlignment.Center, VerticalAlignment.Center, KlyTheme.TextMuted, 12, true);
        if (yTitleAlong is not null)
        {
            canvas.SaveState();
            canvas.Rotate(-90, 12, (float)((layout.Top + layout.Bottom) / 2));
            Text(canvas, yTitleAlong, 12 - 100, (layout.Top + layout.Bottom) / 2 - 8, 200, 16, HorizontalAlignment.Center, VerticalAlignment.Center, KlyTheme.TextMuted, 12, true);
            canvas.RestoreState();
        }

        for (var s = 0; s < m.Series.Count && s < layout.Series.Count; s++)
        {
            var shape = layout.Series[s];
            var color = m.Series[s].Colors.Text.Resolve();
            if (m.Type == KlyChartType.Area)
            {
                foreach (var area in shape.Areas) FillPattern(canvas, area, area.Bounds, s, color, false);
            }
            if (m.Type == KlyChartType.Bar)
            {
                foreach (var bar in shape.Bars)
                {
                    var path = new PathF();
                    path.AppendRoundedRectangle((float)bar.X, (float)bar.Y, (float)bar.Width, (float)bar.Height, 2);
                    FillPattern(canvas, path, new RectF((float)bar.X, (float)bar.Y, (float)bar.Width, (float)bar.Height), s, color, true);
                    canvas.StrokeColor = color;
                    canvas.StrokeSize = 1;
                    canvas.DrawPath(path);
                }
            }
            else
            {
                canvas.StrokeColor = color;
                canvas.StrokeSize = 2;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.StrokeLineJoin = LineJoin.Round;
                canvas.StrokeDashPattern = Dashes[s % Dashes.Length];
                foreach (var line in shape.Lines) canvas.DrawPath(line);
                canvas.StrokeDashPattern = null;
            }
        }

        // The active row: a light band over it and a marker on each of its values. A line chart with Points marks every value.
        if (ActiveRow >= 0)
        {
            var hit = layout.Hits.FirstOrDefault(h => h.Row == ActiveRow);
            if (hit != default)
            {
                canvas.FillColor = KlyTheme.Text.Resolve().WithAlpha(0.05f);
                canvas.FillRectangle((float)hit.X, (float)hit.Y, (float)hit.Width, (float)hit.Height);
            }
        }
        if (m.Type != KlyChartType.Bar)
        {
            for (var s = 0; s < m.Series.Count && s < layout.Series.Count; s++)
            {
                foreach (var point in layout.Series[s].Points)
                {
                    if (point.Row == ActiveRow || (m.Points && m.Type == KlyChartType.Line)) DrawMarker(canvas, s, point.X, point.Y, m.Series[s].Colors.Text.Resolve());
                }
            }
        }
    }

    private void DrawDonut(ICanvas canvas, ChartDataLayout layout, ChartDataModel m)
    {
        foreach (var slice in layout.Slices)
        {
            if (slice.Row >= m.Series.Count) continue;
            var color = m.Series[slice.Row].Colors.Text.Resolve();
            var path = layout.SlicePath(slice);
            FillPattern(canvas, path, path.Bounds, slice.Row, slice.Row == ActiveRow ? color.WithAlpha(0.7f) : color, true);
            canvas.StrokeColor = KlyTheme.Background.Resolve();
            canvas.StrokeSize = 2;
            canvas.StrokeDashPattern = null;
            canvas.DrawPath(path);
        }
    }

    // A fill that differs without color: plain, diagonal hatch, dots, the other diagonal. Areas stay light so the lines read over them.
    private static void FillPattern(ICanvas canvas, PathF path, RectF bounds, int series, Color color, bool strongFirst)
    {
        var strong = series % 4 == 0 && strongFirst;
        canvas.FillColor = color.WithAlpha(strong ? color.Alpha : color.Alpha * 0.2f);
        canvas.FillPath(path);
        var kind = series % 4;
        if (kind == 0) return;
        canvas.SaveState();
        canvas.ClipPath(path);
        canvas.StrokeColor = color;
        canvas.StrokeSize = 1.5f;
        canvas.StrokeDashPattern = null;
        canvas.FillColor = color;
        const float gap = 8;
        if (kind == 1)
        {
            for (var x = bounds.Left - bounds.Height; x < bounds.Right; x += gap) canvas.DrawLine(x, bounds.Bottom, x + bounds.Height, bounds.Top);
        }
        else if (kind == 3)
        {
            for (var x = bounds.Left - bounds.Height; x < bounds.Right; x += gap) canvas.DrawLine(x, bounds.Top, x + bounds.Height, bounds.Bottom);
        }
        else
        {
            for (var x = bounds.Left + gap / 2; x < bounds.Right; x += gap)
            {
                for (var y = bounds.Top + gap / 2; y < bounds.Bottom; y += gap) canvas.FillCircle(x, y, 1.3f);
            }
        }
        canvas.RestoreState();
    }

    // The marker shape cycles circle, square, triangle, diamond, so a series is told apart without color.
    private static void DrawMarker(ICanvas canvas, int series, double px, double py, Color color)
    {
        const float r = 4;
        float x = (float)px, y = (float)py;
        var path = new PathF();
        switch (series % 4)
        {
            case 0:
                path.AppendCircle(x, y, r);
                break;
            case 1:
                path.AppendRectangle(x - r, y - r, 2 * r, 2 * r);
                break;
            case 2:
                path.MoveTo(x, y - r - 1);
                path.LineTo(x + r + 1, y + r);
                path.LineTo(x - r - 1, y + r);
                path.Close();
                break;
            default:
                path.MoveTo(x, y - r - 1);
                path.LineTo(x + r + 1, y);
                path.LineTo(x, y + r + 1);
                path.LineTo(x - r - 1, y);
                path.Close();
                break;
        }
        canvas.FillColor = KlyTheme.Background.Resolve();
        canvas.FillPath(path);
        canvas.StrokeColor = color;
        canvas.StrokeSize = 2;
        canvas.StrokeDashPattern = null;
        canvas.DrawPath(path);
    }

    private void DrawTooltip(ICanvas canvas, ChartDataLayout layout, ChartDataModel m, RectF area)
    {
        var (title, lines) = m.Tip(ActiveRow);
        if (lines.Count == 0) return;

        // The box is sized from the text: about 6.5 units a character at 12.
        var titleWidth = title.Length * 6.8;
        var rowWidth = lines.Max(l => (l.Label.Length + l.Value.Length) * 6.5) + 22 + 24;
        var width = (float)Math.Max(128, Math.Max(titleWidth, rowWidth) + 20);
        var height = 12f + 18f + lines.Count * 18f;

        // Beside the row, on the side that has room.
        double anchorX, anchorY;
        if (m.Donut)
        {
            var slice = layout.Slices.FirstOrDefault(s => s.Row == ActiveRow);
            var angle = slice.Start + slice.Sweep / 2;
            var radius = (layout.Radius + layout.InnerRadius) / 2;
            anchorX = layout.CenterX + radius * Math.Sin(angle);
            anchorY = layout.CenterY - radius * Math.Cos(angle);
        }
        else
        {
            var hit = layout.Hits.FirstOrDefault(h => h.Row == ActiveRow);
            anchorX = hit.X + hit.Width / 2;
            anchorY = layout.Top;
        }
        var x = (float)anchorX + 12;
        if (x + width > area.Width - 4) x = (float)anchorX - 12 - width;
        x = Math.Clamp(x, 4, Math.Max(4, area.Width - width - 4));
        var y = Math.Clamp((float)anchorY, 4, Math.Max(4, area.Height - height - 4));

        canvas.FillColor = KlyTheme.BackgroundElevated.Resolve();
        canvas.FillRoundedRectangle(x, y, width, height, (float)KlyTheme.RadiusOf(1));
        canvas.StrokeColor = KlyTheme.Border.Resolve();
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = null;
        canvas.DrawRoundedRectangle(x, y, width, height, (float)KlyTheme.RadiusOf(1));
        Text(canvas, title, x + 10, y + 6, width - 20, 16, HorizontalAlignment.Left, VerticalAlignment.Center, KlyTheme.TextHighlighted, 12, true);
        for (var i = 0; i < lines.Count; i++)
        {
            var top = y + 6 + 18 + i * 18;
            canvas.FillColor = lines[i].Colors.Text.Resolve();
            canvas.FillRoundedRectangle(x + 10, top + 4, 10, 10, 2);
            Text(canvas, lines[i].Label, x + 26, top, width - 36, 18, HorizontalAlignment.Left, VerticalAlignment.Center, KlyTheme.TextMuted, 12);
            Text(canvas, lines[i].Value, x + 26, top, width - 36, 18, HorizontalAlignment.Right, VerticalAlignment.Center, KlyTheme.TextHighlighted, 12, true);
        }
    }

    private static void Text(ICanvas canvas, string text, double x, double y, double width, double height, HorizontalAlignment horizontal, VerticalAlignment vertical, KlyThemeColor color, float size, bool bold = false)
    {
        canvas.FontColor = color.Resolve();
        canvas.FontSize = size;
        canvas.Font = bold ? GraphicsFont.DefaultBold : GraphicsFont.Default;
        canvas.DrawString(text, (float)x, (float)y, (float)Math.Max(width, 1), (float)height, horizontal, vertical);
    }
}
