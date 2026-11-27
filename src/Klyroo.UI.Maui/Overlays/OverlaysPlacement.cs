namespace Klyroo.UI.Maui;

/// <summary>Where a floating panel goes against its anchor, in the coordinates of the host: the side asked for when it fits, the opposite one when only that fits, and always inside the host.</summary>
internal static class OverlaysPlacement
{
    private enum Side { Top, Right, Bottom, Left }

    /// <summary>The top-left corner of the panel.</summary>
    /// <param name="anchor">Bounds of the trigger in the host (a zero-size rectangle for a point such as a pointer).</param>
    /// <param name="panel">Size of the panel.</param>
    /// <param name="host">Size of the host.</param>
    /// <param name="placement">Side and alignment asked for.</param>
    /// <param name="offset">Gap between the anchor and the panel.</param>
    /// <param name="rtl">Right-to-left flow: start and end swap on the top and bottom sides.</param>
    /// <param name="margin">Smallest distance kept to the edges of the host.</param>
    public static Point Position(Rect anchor, Size panel, Size host, KlyPlacement placement, double offset, bool rtl = false, double margin = 8)
    {
        var (side, align) = Split(placement);
        var chosen = side;
        if (Room(side, anchor, host) < Need(side, panel, offset))
        {
            var opposite = Opposite(side);
            if (Room(opposite, anchor, host) >= Need(opposite, panel, offset) || Room(opposite, anchor, host) > Room(side, anchor, host)) chosen = opposite;
        }

        double x, y;
        switch (chosen)
        {
            case Side.Top:
            case Side.Bottom:
                y = chosen == Side.Top ? anchor.Top - offset - panel.Height : anchor.Bottom + offset;
                x = align == 0
                    ? anchor.Center.X - panel.Width / 2
                    : (align < 0) != rtl ? anchor.Left : anchor.Right - panel.Width;
                break;
            default:
                x = chosen == Side.Left ? anchor.Left - offset - panel.Width : anchor.Right + offset;
                y = align == 0
                    ? anchor.Center.Y - panel.Height / 2
                    : align < 0 ? anchor.Top : anchor.Bottom - panel.Height;
                break;
        }

        return new Point(Clamp(x, margin, host.Width - panel.Width - margin), Clamp(y, margin, host.Height - panel.Height - margin));
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(value, Math.Max(min, max)));

    private static double Room(Side side, Rect anchor, Size host) => side switch
    {
        Side.Top => anchor.Top,
        Side.Bottom => host.Height - anchor.Bottom,
        Side.Left => anchor.Left,
        _ => host.Width - anchor.Right,
    };

    private static double Need(Side side, Size panel, double offset) => (side is Side.Top or Side.Bottom ? panel.Height : panel.Width) + offset;

    private static Side Opposite(Side side) => side switch { Side.Top => Side.Bottom, Side.Bottom => Side.Top, Side.Left => Side.Right, _ => Side.Left };

    private static (Side Side, int Align) Split(KlyPlacement placement) => placement switch
    {
        KlyPlacement.Top => (Side.Top, 0),
        KlyPlacement.TopStart => (Side.Top, -1),
        KlyPlacement.TopEnd => (Side.Top, 1),
        KlyPlacement.Right => (Side.Right, 0),
        KlyPlacement.RightStart => (Side.Right, -1),
        KlyPlacement.RightEnd => (Side.Right, 1),
        KlyPlacement.Bottom => (Side.Bottom, 0),
        KlyPlacement.BottomStart => (Side.Bottom, -1),
        KlyPlacement.BottomEnd => (Side.Bottom, 1),
        KlyPlacement.Left => (Side.Left, 0),
        KlyPlacement.LeftStart => (Side.Left, -1),
        KlyPlacement.LeftEnd => (Side.Left, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, null),
    };
}
