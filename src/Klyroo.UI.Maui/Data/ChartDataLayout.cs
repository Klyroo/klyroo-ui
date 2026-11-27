namespace Klyroo.UI.Maui;

internal readonly record struct ChartDataTick(double Value, double Position, string Label);

internal readonly record struct ChartDataLabel(double Position, string Text);

internal readonly record struct ChartDataLine(double X1, double Y1, double X2, double Y2, bool Zero);

internal readonly record struct ChartDataPoint(int Row, double X, double Y);

internal readonly record struct ChartDataBar(int Row, double X, double Y, double Width, double Height);

internal readonly record struct ChartDataHit(int Row, double X, double Y, double Width, double Height);

/// <summary>A slice of a donut: where it starts and how far it sweeps, in radians clockwise from the top, with its value and share.</summary>
internal readonly record struct ChartDataSlice(int Row, double Start, double Sweep, double Value, double Fraction);

internal sealed class ChartDataSeriesShape
{
    /// <summary>Line paths, one per run of values without a gap.</summary>
    public List<PathF> Lines { get; } = [];

    /// <summary>Filled paths of an area chart, one per run.</summary>
    public List<PathF> Areas { get; } = [];
    public List<ChartDataPoint> Points { get; } = [];
    public List<ChartDataBar> Bars { get; } = [];
}

/// <summary>Everything a chart draws, computed from the data and the size: scales, ticks, shapes. No drawing, so it can be tested alone. The same maths as the SVG chart of Klyroo.UI.</summary>
internal sealed class ChartDataLayout
{
    private const double LabelChar = 6.2;
    private const double MaxBar = 56;
    private const int MaxTicks = 1000;

    public double Width { get; private init; }
    public double Height { get; private init; }
    public double Left { get; private set; }
    public double Top { get; private set; }
    public double Right { get; private set; }
    public double Bottom { get; private set; }

    /// <summary>Bars lie along the x axis instead of the y axis; the index axis is then vertical.</summary>
    public bool Horizontal { get; private init; }
    public List<ChartDataTick> ValueTicks { get; } = [];
    public List<ChartDataLabel> IndexLabels { get; } = [];
    public List<ChartDataLine> GridLines { get; } = [];
    public List<ChartDataSeriesShape> Series { get; } = [];
    public List<ChartDataHit> Hits { get; } = [];
    public List<ChartDataSlice> Slices { get; } = [];
    public double CenterX { get; private set; }
    public double CenterY { get; private set; }
    public double Radius { get; private set; }
    public double InnerRadius { get; private set; }

    /// <summary>The value range the axis covers, after rounding out to nice ticks.</summary>
    public double DomainMin { get; private set; }
    public double DomainMax { get; private set; }

    /// <summary>Position along the value axis (y for vertical charts, x for horizontal bars).</summary>
    public double ValuePosition(double value)
    {
        // The direct span, unless it overflows: halving would turn subnormal values into zeros (0 / 0 is NaN).
        var span = DomainMax - DomainMin;
        double t;
        if (double.IsFinite(span))
        {
            t = span > 0 ? (value - DomainMin) / span : 0.5;
        }
        else
        {
            t = (value / 2 - DomainMin / 2) / (DomainMax / 2 - DomainMin / 2);
        }
        // A range that cannot be told apart places every value in the middle, at a coordinate that is always finite.
        if (!double.IsFinite(t)) t = 0.5;
        return Horizontal ? Left + t * (Right - Left) : Bottom - t * (Bottom - Top);
    }

    public static ChartDataLayout Build(
        KlyChartType type,
        bool horizontal,
        bool stacked,
        KlyChartCurve curve,
        IReadOnlyList<string> index,
        IReadOnlyList<IReadOnlyList<double?>> values,
        double width,
        double height,
        bool xAxis,
        bool yAxis,
        bool hasXTitle,
        bool hasYTitle,
        double thickness,
        Func<double, string> format)
    {
        var layout = new ChartDataLayout
        {
            Width = width,
            Height = height,
            Horizontal = type == KlyChartType.Bar && horizontal,
        };
        if (type == KlyChartType.Donut)
        {
            layout.BuildDonut(values.Count == 0 ? [] : values[0], thickness);
        }
        else
        {
            layout.BuildCartesian(type, stacked && type != KlyChartType.Line, curve, index, values, xAxis, yAxis, hasXTitle, hasYTitle, format);
        }
        return layout;
    }

    /// <summary>Rounds a range out to ticks on 1, 2 or 5 times a power of ten.</summary>
    public static (double Min, double Max, double Step, List<double> Ticks) NiceScale(double min, double max, int target = 5)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max))
        {
            min = 0;
            max = 1;
        }
        if (!(max > min))
        {
            if (min == 0)
            {
                max = 1;
            }
            else
            {
                var pad = Math.Abs(min) * 0.1;
                min = Math.Max(min - pad, -double.MaxValue);
                max = Math.Min(max + pad, double.MaxValue);
            }
        }
        // The difference of two finite doubles can overflow: divide first then.
        var raw = (max - min) / target;
        if (!double.IsFinite(raw))
        {
            raw = max / target - min / target;
        }
        if (raw > 0 && double.IsFinite(raw))
        {
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            var normal = raw / magnitude;
            var step = (normal <= 1.5 ? 1 : normal <= 3 ? 2 : normal <= 7 ? 5 : 10) * magnitude;
            var first = Math.Floor(min / step + 1e-9);
            var last = Math.Ceiling(max / step - 1e-9);
            var count = last - first;
            if (step > 0 && double.IsFinite(step) && double.IsFinite(count) && count < MaxTicks)
            {
                // An integer counter: a double one stops moving once it passes 2^53.
                var ticks = new List<double>((int)count + 1);
                for (var k = 0; k <= (int)count; k++)
                {
                    ticks.Add(Math.Round((first + k) * step, 10));
                }
                if (ticks.All(double.IsFinite) && ticks.Zip(ticks.Skip(1), (x, y) => y > x).All(ok => ok))
                {
                    return (ticks[0], ticks[^1], step, ticks);
                }
            }
        }
        // Too many ticks, or ticks that cannot be told apart or represented: the two ends of the range.
        var span = max - min;
        return (min, max, double.IsFinite(span) ? span : double.MaxValue, [min, max]);
    }

    /// <summary>The row under a point of the drawing, or -1.</summary>
    public int RowAt(double x, double y)
    {
        if (Slices.Count > 0 || Radius > 0)
        {
            var dx = x - CenterX;
            var dy = y - CenterY;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance > Radius || distance < InnerRadius) return -1;
            // Angle clockwise from the top, in 0 to 2 pi.
            var angle = Math.Atan2(dy, dx) + Math.PI / 2;
            if (angle < 0) angle += Math.PI * 2;
            var start = 0.0;
            foreach (var slice in Slices)
            {
                if (angle >= start && angle < start + slice.Sweep) return slice.Row;
                start += slice.Sweep;
            }
            return -1;
        }
        foreach (var hit in Hits)
        {
            if (x >= hit.X && x <= hit.X + hit.Width && y >= hit.Y && y <= hit.Y + hit.Height) return hit.Row;
        }
        return -1;
    }

    /// <summary>The series the point is on, among those that have a mark in <paramref name="row"/>: the bar that holds it, else the nearest point. -1 when no series has a mark there.</summary>
    public int SeriesAt(int row, double x, double y)
    {
        var best = -1;
        var bestDistance = double.MaxValue;
        for (var s = 0; s < Series.Count; s++)
        {
            foreach (var bar in Series[s].Bars)
            {
                if (bar.Row != row) continue;
                if (x >= bar.X && x <= bar.X + bar.Width && y >= bar.Y && y <= bar.Y + bar.Height) return s;
                var distance = Horizontal ? Math.Abs(y - (bar.Y + bar.Height / 2)) : Math.Abs(x - (bar.X + bar.Width / 2));
                if (distance < bestDistance)
                {
                    best = s;
                    bestDistance = distance;
                }
            }
            foreach (var point in Series[s].Points)
            {
                if (point.Row != row) continue;
                var distance = Math.Abs(y - point.Y);
                if (distance < bestDistance)
                {
                    best = s;
                    bestDistance = distance;
                }
            }
        }
        return best;
    }

    private void BuildCartesian(
        KlyChartType type, bool stacked, KlyChartCurve curve, IReadOnlyList<string> index, IReadOnlyList<IReadOnlyList<double?>> values,
        bool xAxis, bool yAxis, bool hasXTitle, bool hasYTitle, Func<double, string> format)
    {
        var n = index.Count;
        var k = values.Count;
        for (var s = 0; s < k; s++)
        {
            Series.Add(new ChartDataSeriesShape());
        }

        // Extents of every value along the value axis. Stacked segments sit on the running totals of their row;
        // "edge" is the end of the segment that carries the value (where a line or the top of an area goes).
        var lo = new double[k, n];
        var hi = new double[k, n];
        var edge = new double[k, n];
        var positive = new double[n];
        var negative = new double[n];
        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        for (var s = 0; s < k; s++)
        {
            for (var i = 0; i < n; i++)
            {
                var v = values[s][i] ?? (stacked ? 0 : double.NaN);
                if (double.IsNaN(v) || double.IsInfinity(v))
                {
                    lo[s, i] = hi[s, i] = edge[s, i] = double.NaN;
                    continue;
                }
                if (stacked)
                {
                    if (v >= 0)
                    {
                        lo[s, i] = positive[i];
                        hi[s, i] = positive[i] += v;
                        edge[s, i] = hi[s, i];
                    }
                    else
                    {
                        hi[s, i] = negative[i];
                        lo[s, i] = negative[i] += v;
                        edge[s, i] = lo[s, i];
                    }
                }
                else
                {
                    edge[s, i] = v;
                    lo[s, i] = type == KlyChartType.Line ? v : Math.Min(0, v);
                    hi[s, i] = type == KlyChartType.Line ? v : Math.Max(0, v);
                }
                min = Math.Min(min, lo[s, i]);
                max = Math.Max(max, hi[s, i]);
            }
        }
        if (double.IsInfinity(min))
        {
            min = 0;
            max = 1;
        }
        else if (type != KlyChartType.Line)
        {
            min = Math.Min(min, 0);
            max = Math.Max(max, 0);
        }
        var scale = NiceScale(min, max);
        DomainMin = scale.Min;
        DomainMax = scale.Max;

        var tickLabels = scale.Ticks.Select(format).ToList();
        var valueLabelWidth = tickLabels.Max(t => t.Length) * LabelChar + 10;
        var indexLabelWidth = n == 0 ? 0 : Math.Min(160, index.Max(t => t.Length) * LabelChar + 10);

        const double titleSpace = 18;
        if (Horizontal)
        {
            Left = (xAxis ? indexLabelWidth : 0) + 8 + (hasXTitle ? titleSpace : 0);
            Bottom = Height - ((yAxis ? 22 : 6) + (hasYTitle ? titleSpace : 0));
        }
        else
        {
            Left = (yAxis ? valueLabelWidth : 0) + 8 + (hasYTitle ? titleSpace : 0);
            Bottom = Height - ((xAxis ? 22 : 6) + (hasXTitle ? titleSpace : 0));
        }
        Top = 10;
        Right = Width - 14;

        foreach (var tick in scale.Ticks)
        {
            var position = ValuePosition(tick);
            ValueTicks.Add(new ChartDataTick(tick, position, format(tick)));
            var zero = tick == 0 && scale.Min < 0;
            GridLines.Add(Horizontal ? new ChartDataLine(position, Top, position, Bottom, zero) : new ChartDataLine(Left, position, Right, position, zero));
        }

        if (n == 0)
        {
            return;
        }

        // Position of each row along the index axis, and the band each row owns.
        var span = Horizontal ? Bottom - Top : Right - Left;
        var start = Horizontal ? Top : Left;
        var centers = new double[n];
        var bands = new (double From, double To)[n];
        double spacing;
        if (type == KlyChartType.Bar)
        {
            spacing = span / n;
            for (var i = 0; i < n; i++)
            {
                centers[i] = start + spacing * (i + 0.5);
                bands[i] = (start + spacing * i, start + spacing * (i + 1));
            }
        }
        else
        {
            var pad = Math.Min(24, span / (2 * n));
            spacing = n > 1 ? (span - 2 * pad) / (n - 1) : span;
            for (var i = 0; i < n; i++)
            {
                centers[i] = n > 1 ? start + pad + spacing * i : start + span / 2;
            }
            for (var i = 0; i < n; i++)
            {
                var from = i == 0 ? start : (centers[i - 1] + centers[i]) / 2;
                var to = i == n - 1 ? start + span : (centers[i] + centers[i + 1]) / 2;
                bands[i] = (from, to);
            }
        }

        // Index labels, thinned so they do not overlap.
        var needed = Horizontal ? 16 : Math.Min(index.Max(t => t.Length), 24) * LabelChar + 8;
        var every = Math.Max(1, (int)Math.Ceiling(needed / Math.Max(spacing, 1)));
        for (var i = 0; i < n; i += every)
        {
            IndexLabels.Add(new ChartDataLabel(centers[i], index[i]));
        }

        for (var i = 0; i < n; i++)
        {
            var (from, to) = bands[i];
            Hits.Add(Horizontal ? new ChartDataHit(i, Left, from, Right - Left, to - from) : new ChartDataHit(i, from, Top, to - from, Bottom - Top));
        }

        if (type == KlyChartType.Bar)
        {
            BuildBars(stacked, lo, hi, values, bands);
            return;
        }

        for (var s = 0; s < k; s++)
        {
            BuildLine(type == KlyChartType.Area, curve, Series[s], n, centers, s, lo, hi, edge, stacked);
        }
    }

    private void BuildBars(bool stacked, double[,] lo, double[,] hi, IReadOnlyList<IReadOnlyList<double?>> values, (double From, double To)[] bands)
    {
        var k = values.Count;
        for (var i = 0; i < bands.Length; i++)
        {
            var (from, to) = bands[i];
            var group = (to - from) * 0.7;
            var each = Math.Max(1, Math.Min(MaxBar, stacked ? group : group / Math.Max(k, 1) - 2));
            var total = stacked ? each : each * k + 2 * (k - 1);
            var origin = (from + to) / 2 - total / 2;
            for (var s = 0; s < k; s++)
            {
                if (double.IsNaN(lo[s, i]) || values[s][i] is null)
                {
                    continue;
                }
                var u0 = stacked ? origin : origin + s * (each + 2);
                var a = ValuePosition(lo[s, i]);
                var b = ValuePosition(hi[s, i]);
                // A bar of value 0 would vanish; keep a visible sliver so its value stays reachable.
                var size = Math.Max(Math.Abs(a - b), 1);
                Series[s].Bars.Add(Horizontal
                    ? new ChartDataBar(i, Math.Min(a, b), u0, size, each)
                    : new ChartDataBar(i, u0, Math.Min(a, b), each, size));
            }
        }
    }

    private void BuildLine(bool area, KlyChartCurve curve, ChartDataSeriesShape shape, int n, double[] centers, int s, double[,] lo, double[,] hi, double[,] edge, bool stacked)
    {
        var upper = new List<(double X, double Y)>();
        var lower = new List<(double X, double Y)>();
        var baseline = ValuePosition(Math.Clamp(0, DomainMin, DomainMax));

        void Flush()
        {
            if (upper.Count == 0)
            {
                return;
            }
            var line = new PathF();
            AppendCurve(line, upper, curve, true);
            shape.Lines.Add(line);
            if (area)
            {
                lower.Reverse();
                var fill = new PathF();
                AppendCurve(fill, upper, curve, true);
                AppendCurve(fill, lower, curve, false);
                fill.Close();
                shape.Areas.Add(fill);
            }
            upper.Clear();
            lower.Clear();
        }

        for (var i = 0; i < n; i++)
        {
            if (double.IsNaN(edge[s, i]))
            {
                Flush();
                continue;
            }
            var y = ValuePosition(edge[s, i]);
            upper.Add((centers[i], y));
            // Stacked: the other end of the segment, where the running total stood before this value.
            lower.Add((centers[i], stacked ? ValuePosition(edge[s, i] == hi[s, i] ? lo[s, i] : hi[s, i]) : baseline));
            shape.Points.Add(new ChartDataPoint(i, centers[i], y));
        }
        Flush();
    }

    /// <summary>Adds a straight, stepped or smooth monotone curve through the points to a path, starting with a move or, to continue a shape, a line to the first point.</summary>
    public static void AppendCurve(PathF path, IReadOnlyList<(double X, double Y)> points, KlyChartCurve curve, bool move)
    {
        if (points.Count == 0)
        {
            return;
        }
        if (move) path.MoveTo((float)points[0].X, (float)points[0].Y);
        else path.LineTo((float)points[0].X, (float)points[0].Y);
        if (points.Count == 1)
        {
            // A single value still needs a segment to take a stroke.
            path.LineTo((float)points[0].X + 0.01f, (float)points[0].Y);
            return;
        }
        if (curve == KlyChartCurve.Monotone && points.Count > 2)
        {
            var t = Tangents(points);
            for (var i = 0; i < points.Count - 1; i++)
            {
                var (x0, y0) = points[i];
                var (x1, y1) = points[i + 1];
                var dx = (x1 - x0) / 3;
                path.CurveTo((float)(x0 + dx), (float)(y0 + dx * t[i]), (float)(x1 - dx), (float)(y1 - dx * t[i + 1]), (float)x1, (float)y1);
            }
            return;
        }
        for (var i = 1; i < points.Count; i++)
        {
            var (x, y) = points[i];
            if (curve == KlyChartCurve.Step)
            {
                var mid = (points[i - 1].X + x) / 2;
                path.LineTo((float)mid, (float)points[i - 1].Y);
                path.LineTo((float)mid, (float)y);
            }
            path.LineTo((float)x, (float)y);
        }
    }

    // Monotone cubic tangents (Fritsch and Carlson), as in d3's monotoneX.
    private static double[] Tangents(IReadOnlyList<(double X, double Y)> p)
    {
        var n = p.Count;
        var t = new double[n];
        double Slope(int i) => p[i + 1].X == p[i].X ? 0 : (p[i + 1].Y - p[i].Y) / (p[i + 1].X - p[i].X);
        for (var i = 1; i < n - 1; i++)
        {
            double s0 = Slope(i - 1), s1 = Slope(i);
            double h0 = p[i].X - p[i - 1].X, h1 = p[i + 1].X - p[i].X;
            var q = h0 + h1 == 0 ? 0 : (s0 * h1 + s1 * h0) / (h0 + h1);
            t[i] = (Math.Sign(s0) + Math.Sign(s1)) * Math.Min(Math.Min(Math.Abs(s0), Math.Abs(s1)), 0.5 * Math.Abs(q));
        }
        t[0] = EndTangent(p[0], p[1], t[1]);
        t[n - 1] = EndTangent(p[n - 1], p[n - 2], t[n - 2]);
        return t;
    }

    private static double EndTangent((double X, double Y) a, (double X, double Y) b, double inner)
    {
        var h = b.X - a.X;
        return h == 0 ? inner : (3 * (b.Y - a.Y) / h - inner) / 2;
    }

    private void BuildDonut(IReadOnlyList<double?> values, double thickness)
    {
        CenterX = Width / 2;
        CenterY = Height / 2;
        Radius = Math.Max(10, Math.Min(Width, Height) / 2 - 6);
        InnerRadius = Math.Max(0, Radius - Math.Max(thickness, 1));
        var amounts = values.Select(v => v is > 0 && double.IsFinite(v.Value) ? v.Value : 0).ToArray();
        var total = amounts.Sum();
        var angle = 0.0;
        for (var i = 0; i < amounts.Length; i++)
        {
            if (total <= 0 || amounts[i] <= 0)
            {
                continue;
            }
            var fraction = amounts[i] / total;
            var sweep = fraction * Math.PI * 2;
            Slices.Add(new ChartDataSlice(i, angle, sweep, amounts[i], fraction));
            angle += sweep;
        }
    }

    /// <summary>The outline of a slice as a polygon: the outer arc, then the inner one back, a point every few degrees.</summary>
    public PathF SlicePath(ChartDataSlice slice)
    {
        var path = new PathF();
        var steps = Math.Max(2, (int)Math.Ceiling(slice.Sweep / (Math.PI / 90)));
        PointF At(double radius, double angle) => new(
            (float)(CenterX + radius * Math.Sin(angle)),
            (float)(CenterY - radius * Math.Cos(angle)));
        for (var i = 0; i <= steps; i++)
        {
            var p = At(Radius, slice.Start + slice.Sweep * i / steps);
            if (i == 0) path.MoveTo(p.X, p.Y);
            else path.LineTo(p.X, p.Y);
        }
        if (InnerRadius <= 0.5)
        {
            path.LineTo((float)CenterX, (float)CenterY);
        }
        else
        {
            for (var i = steps; i >= 0; i--)
            {
                var p = At(InnerRadius, slice.Start + slice.Sweep * i / steps);
                path.LineTo(p.X, p.Y);
            }
        }
        path.Close();
        return path;
    }
}
