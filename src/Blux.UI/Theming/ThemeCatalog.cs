namespace Blux.UI.Theming;

internal static class ThemeCatalog
{
    /// <summary>Sorted, distinct class tokens of every theme: the input of the Tailwind build.</summary>
    public static IReadOnlyList<string> AllClassTokens(BluxOptions options) =>
        options.AllThemes
            .SelectMany(t => t.AllClassTokens())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
}
