using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI;

/// <summary>Icons used inside components, 24×24 strokes in currentColor. Size them with classes.</summary>
public static class BxIcons
{
    public static RenderFragment Close(string? @class = null) => Icon(@class, "M18 6 6 18M6 6l12 12");
    public static RenderFragment Check(string? @class = null) => Icon(@class, "M20 6 9 17l-5-5");
    public static RenderFragment Minus(string? @class = null) => Icon(@class, "M5 12h14");
    public static RenderFragment ChevronDown(string? @class = null) => Icon(@class, "m6 9 6 6 6-6");
    public static RenderFragment ChevronUpDown(string? @class = null) => Icon(@class, "m7 15 5 5 5-5M7 9l5-5 5 5");
    public static RenderFragment ArrowUp(string? @class = null) => Icon(@class, "m5 12 7-7 7 7M12 19V5");
    public static RenderFragment ArrowDown(string? @class = null) => Icon(@class, "M12 5v14m7-7-7 7-7-7");
    public static RenderFragment Info(string? @class = null) => Icon(@class, "M12 16v-4m0-4h.01M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0Z");
    public static RenderFragment CircleCheck(string? @class = null) => Icon(@class, "m9 12 2 2 4-4m7 2a10 10 0 1 1-20 0 10 10 0 0 1 20 0Z");
    public static RenderFragment TriangleAlert(string? @class = null) => Icon(@class, "M12 9v4m0 4h.01M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z");
    public static RenderFragment CircleX(string? @class = null) => Icon(@class, "m15 9-6 6m0-6 6 6m7-3a10 10 0 1 1-20 0 10 10 0 0 1 20 0Z");

    /// <summary>A built-in Lucide icon by name (see <see cref="BxIcon"/>); renders nothing for an unknown name.</summary>
    public static RenderFragment Named(string name, string? @class = null) =>
        IconRegistry.Builtin.TryGetValue(name, out var path) ? Icon(@class, path) : _ => { };

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
