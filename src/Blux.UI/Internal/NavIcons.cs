using Microsoft.AspNetCore.Components;

namespace Blux.UI.Internal;

/// <summary>Icons of the navigation family, 24x24 strokes in currentColor.</summary>
internal static class NavIcons
{
    public static RenderFragment ChevronRight(string? @class = null) => Icon(@class, "m9 18 6-6-6-6");
    public static RenderFragment ChevronLeft(string? @class = null) => Icon(@class, "m15 18-6-6 6-6");
    public static RenderFragment ChevronsLeft(string? @class = null) => Icon(@class, "m11 17-5-5 5-5M18 17l-5-5 5-5");
    public static RenderFragment ChevronsRight(string? @class = null) => Icon(@class, "m6 17 5-5-5-5M13 17l5-5-5-5");

    private static RenderFragment Icon(string? @class, string path) => builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "viewBox", "0 0 24 24");
        builder.AddAttribute(2, "fill", "none");
        builder.AddAttribute(3, "stroke", "currentColor");
        builder.AddAttribute(4, "stroke-width", "2");
        builder.AddAttribute(5, "stroke-linecap", "round");
        builder.AddAttribute(6, "stroke-linejoin", "round");
        builder.AddAttribute(7, "aria-hidden", "true");
        builder.AddAttribute(8, "class", @class);
        builder.OpenElement(9, "path");
        builder.AddAttribute(10, "d", path);
        builder.CloseElement();
        builder.CloseElement();
    };
}
