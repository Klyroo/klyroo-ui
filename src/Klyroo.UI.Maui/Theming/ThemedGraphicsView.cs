namespace Klyroo.UI.Maui;

/// <summary>
/// A <see cref="GraphicsView"/> that redraws when the app theme changes. A control that draws itself (an icon, a chart, a progress ring)
/// resolves its colors in <c>Draw</c> with <see cref="KlyThemeColor.Resolve"/>; this base keeps them current. The subscription is only held while the view has a handler.
/// </summary>
public abstract class ThemedGraphicsView : GraphicsView, IDrawable
{
    /// <summary>Creates the view with itself as the drawable.</summary>
    protected ThemedGraphicsView()
    {
        Drawable = this;
        HandlerChanged += (_, _) =>
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeChanged -= OnThemeChanged;
                if (Handler is not null) app.RequestedThemeChanged += OnThemeChanged;
            }
        };
    }

    /// <summary>Draws the view. Resolve themed colors here with <see cref="KlyThemeColor.Resolve"/>.</summary>
    /// <param name="canvas">The canvas to draw on.</param>
    /// <param name="dirtyRect">The area to draw, from 0,0 to the size of the view.</param>
    public abstract void Draw(ICanvas canvas, RectF dirtyRect);

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => Invalidate();
}
