namespace Klyroo.UI.Maui;

/// <summary>One star of a <see cref="KlyInputRating"/>: the Lucide star, outlined, and filled from its start up to a fraction of its width.</summary>
internal sealed class InputsStar : ThemedGraphicsView
{
    private readonly PathF? _path;
    private double _filled;
    private KlyColor _color = KlyColor.Primary;
    private double _dimension = 24;

    public InputsStar()
    {
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        _path = KlyIcons.TryGet(KlyIconNames.Star, out var paths) ? PathBuilder.Build(paths[0]) : null;
        Dimension = 24;
    }

    /// <summary>How much of the star is filled: 0, 0.5 or 1.</summary>
    public double Filled
    {
        get => _filled;
        set
        {
            _filled = value;
            Invalidate();
        }
    }

    public KlyColor Color
    {
        get => _color;
        set
        {
            _color = value;
            Invalidate();
        }
    }

    /// <summary>Width and height of the star.</summary>
    public double Dimension
    {
        get => _dimension;
        set
        {
            _dimension = value;
            WidthRequest = value;
            HeightRequest = value;
            Invalidate();
        }
    }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_path is null) return;
        var scale = (float)(_dimension / BuiltinIcons.Size);
        canvas.Scale(scale, scale);
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.StrokeSize = 1.5f;
        canvas.StrokeColor = KlyTheme.TextDimmed.Resolve();
        canvas.DrawPath(_path);

        if (_filled <= 0) return;
        var set = KlyTheme.Resolve(_color);
        canvas.SaveState();
        canvas.ClipRectangle(0, 0, (float)(BuiltinIcons.Size * Math.Min(_filled, 1)), (float)BuiltinIcons.Size);
        canvas.FillColor = set.Fill.Resolve();
        canvas.FillPath(_path);
        canvas.StrokeColor = set.Text.Resolve();
        canvas.DrawPath(_path);
        canvas.RestoreState();
    }
}
