namespace Klyroo.UI.Maui;

/// <summary>The width breakpoints of the web theme (<c>sm</c>, <c>md</c>, <c>lg</c>, <c>xl</c>), in device-independent units, and what the layout controls derive from them.</summary>
internal static class LayoutBreakpoints
{
    public const double Sm = 640;
    public const double Md = 768;
    public const double Lg = 1024;
    public const double Xl = 1280;

    /// <summary>From <c>lg</c> up a sidebar is a column and the header shows its links; below it they give way to a panel and a menu.</summary>
    public static bool IsWide(double width) => width >= Lg;

    /// <summary>The <c>px-4 sm:px-6 lg:px-8</c> of the bars and the container.</summary>
    public static double HorizontalPadding(double width) => width >= Lg ? 32 : width >= Sm ? 24 : 16;
}
