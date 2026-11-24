namespace Lux.UI.Images.Local;

internal enum LxPlanKind
{
    /// <summary>Resize to exactly <see cref="LxGeometryPlan.Width"/> by <see cref="LxGeometryPlan.Height"/>.</summary>
    Scale,
    /// <summary>Resize to the inner size, then pad to the full size.</summary>
    Letterbox,
    /// <summary>Resize to cover the box and crop the center (libvips shrink-on-load applies).</summary>
    Crop,
    /// <summary>Crop the source region that has the box ratio first, then resize it: for ratios whose covering size would be enormous.</summary>
    CropFromSource,
}

internal readonly record struct LxGeometryPlan(LxPlanKind Kind, int Width, int Height, int InnerWidth = 0, int InnerHeight = 0, int CropX = 0, int CropY = 0, int CropWidth = 0, int CropHeight = 0);

/// <summary>
/// The output size of every fit, decided from the (oriented) source size before libvips does any work, so the result can never exceed the
/// configured maximum whatever the ratio of the source. Follows sharp resize semantics: with both sides given, <c>cover</c>,
/// <c>outside</c>, <c>contain</c> and <c>fill</c> produce the box (or cover it) even from a smaller source, <c>inside</c> never enlarges;
/// with one side the ratio is kept and the image is never enlarged.
/// </summary>
internal static class LxImageGeometry
{
    public static LxGeometryPlan Plan(int sourceWidth, int sourceHeight, int? width, int? height, LxImageFit fit, int maxWidth, int maxHeight)
    {
        double sw = Math.Max(1, sourceWidth), sh = Math.Max(1, sourceHeight);
        var limit = Math.Min(maxWidth / sw, maxHeight / sh);

        if (width is not { } w || height is not { } h)
        {
            var scale = 1d;
            if (width is { } onlyWidth) scale = Math.Min(scale, onlyWidth / sw);
            else if (height is { } onlyHeight) scale = Math.Min(scale, onlyHeight / sh);
            scale = Math.Min(scale, limit);
            return new LxGeometryPlan(LxPlanKind.Scale, Clamp(sw * scale, maxWidth), Clamp(sh * scale, maxHeight));
        }

        w = Math.Min(w, maxWidth);
        h = Math.Min(h, maxHeight);
        double ratioW = w / sw, ratioH = h / sh;
        switch (fit)
        {
            case LxImageFit.Fill:
                return new LxGeometryPlan(LxPlanKind.Scale, w, h);

            case LxImageFit.Inside:
            {
                var scale = Math.Min(Math.Min(ratioW, ratioH), 1d);
                return new LxGeometryPlan(LxPlanKind.Scale, Clamp(sw * scale, maxWidth), Clamp(sh * scale, maxHeight));
            }

            case LxImageFit.Outside:
            {
                // The smallest size that covers the box; for ratios that cannot be covered within the maximum, the largest that fits.
                var scale = Math.Min(Math.Max(ratioW, ratioH), limit);
                return new LxGeometryPlan(LxPlanKind.Scale, Clamp(Math.Ceiling(sw * scale), maxWidth), Clamp(Math.Ceiling(sh * scale), maxHeight));
            }

            case LxImageFit.Contain:
            {
                var scale = Math.Min(ratioW, ratioH);
                int innerWidth = Math.Min(w, Clamp(sw * scale, maxWidth)), innerHeight = Math.Min(h, Clamp(sh * scale, maxHeight));
                return innerWidth == w && innerHeight == h
                    ? new LxGeometryPlan(LxPlanKind.Scale, w, h)
                    : new LxGeometryPlan(LxPlanKind.Letterbox, w, h, innerWidth, innerHeight);
            }

            default:
            {
                var scale = Math.Max(ratioW, ratioH);
                if (Math.Ceiling(sw * scale) <= maxWidth && Math.Ceiling(sh * scale) <= maxHeight) return new LxGeometryPlan(LxPlanKind.Crop, w, h);

                int cropWidth, cropHeight;
                if (sw * h >= sh * w)
                {
                    cropHeight = (int)sh;
                    cropWidth = Math.Clamp((int)Math.Round(sh * w / h), 1, (int)sw);
                }
                else
                {
                    cropWidth = (int)sw;
                    cropHeight = Math.Clamp((int)Math.Round(sw * h / w), 1, (int)sh);
                }
                return new LxGeometryPlan(LxPlanKind.CropFromSource, w, h, CropX: ((int)sw - cropWidth) / 2, CropY: ((int)sh - cropHeight) / 2, CropWidth: cropWidth, CropHeight: cropHeight);
            }
        }
    }

    private static int Clamp(double value, int max) => (int)Math.Clamp(Math.Round(value), 1, max);
}
