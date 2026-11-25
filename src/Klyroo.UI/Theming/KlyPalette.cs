using System.Text.RegularExpressions;
using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// A palette of 11 shades (50, 100, 200 … 900, 950) that a semantic color is mapped to, like the palettes of Tailwind CSS
/// Use the Tailwind v4 defaults (<see cref="Blue"/>, <see cref="Zinc"/> …), <see cref="Custom(string, IReadOnlyDictionary{int, string})"/>
/// for your own, or <see cref="Klyroo"/>, the hand-tuned colors Klyroo.UI ships with.
/// </summary>
/// <remarks>
/// Shade values are <c>#rgb</c>, <c>#rrggbb</c> or <c>oklch(L C H)</c>. The tokens of a semantic color (the fill, the text color
/// for light surfaces and the text color on the fill) are picked from the shades so that the contrast rules of the library
/// hold; see <see cref="KlyColorOptions"/>.
/// </remarks>
public sealed partial class KlyPalette
{
    /// <summary>The shade numbers of a palette, lightest first.</summary>
    public static IReadOnlyList<int> ShadeNumbers { get; } = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950];

    // Not a static field: the generated partial declares static palettes whose initializers run Create, in an unspecified order.
    private const string NamePattern = "^[a-z][a-z0-9-]*$";

    private readonly string[] _shades;

    private KlyPalette(string name, string[] shades)
    {
        Name = name;
        _shades = shades;
    }

    /// <summary>
    /// The colors Klyroo.UI ships with, tuned by hand for contrast (a lime primary, #b6ff00, and a teal secondary). Assigned to a
    /// semantic color it keeps that color's default; it has no shades.
    /// </summary>
    public static KlyPalette Klyroo { get; } = new("klyroo", []);

    /// <summary>The name of the palette, such as <c>blue</c>.</summary>
    public string Name { get; }

    /// <summary>True for <see cref="Klyroo"/>, which carries no shades.</summary>
    public bool IsKlyroo => _shades.Length == 0;

    /// <summary>The shades as given, lightest first (empty for <see cref="Klyroo"/>).</summary>
    public IReadOnlyList<string> Shades => _shades;

    /// <summary>One shade as given, for example <c>palette[500]</c>.</summary>
    public string this[int shade]
    {
        get
        {
            var index = ShadeNumbers.ToList().IndexOf(shade);
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(shade), shade, "A shade is 50, 100, 200, 300, 400, 500, 600, 700, 800, 900 or 950.");
            }
            return IsKlyroo ? throw new InvalidOperationException("KlyPalette.Klyroo has no shades.") : _shades[index];
        }
    }

    /// <summary>A palette from all 11 shades, keyed by shade number (50 to 950).</summary>
    public static KlyPalette Custom(string name, IReadOnlyDictionary<int, string> shades)
    {
        ArgumentNullException.ThrowIfNull(shades);
        var values = new string[ShadeNumbers.Count];
        for (var i = 0; i < values.Length; i++)
        {
            if (!shades.TryGetValue(ShadeNumbers[i], out var value))
            {
                throw new ArgumentException($"Palette '{name}' is missing shade {ShadeNumbers[i]}: define all of 50, 100, 200, 300, 400, 500, 600, 700, 800, 900 and 950.", nameof(shades));
            }
            values[i] = value;
        }
        return Create(name, values);
    }

    /// <summary>A palette from its 11 shades, lightest (50) first.</summary>
    public static KlyPalette Custom(string name, params string[] shades)
    {
        ArgumentNullException.ThrowIfNull(shades);
        if (shades.Length != ShadeNumbers.Count)
        {
            throw new ArgumentException($"A palette has {ShadeNumbers.Count} shades (50 to 950), got {shades.Length}.", nameof(shades));
        }
        return Create(name, [.. shades]);
    }

    private static KlyPalette Tailwind(string name, params string[] shades) => Create(name, shades);

    private static KlyPalette Create(string name, string[] shades)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!Regex.IsMatch(name, NamePattern, RegexOptions.CultureInvariant))
        {
            throw new ArgumentException($"Palette name '{name}' must be lower case letters, digits and dashes, starting with a letter.", nameof(name));
        }
        foreach (var shade in shades)
        {
            try
            {
                KlyColorMath.Parse(shade);
            }
            catch (FormatException e)
            {
                throw new ArgumentException($"Palette '{name}': {e.Message}", nameof(shades), e);
            }
        }
        return new KlyPalette(name, shades);
    }

    internal string Hex(int index) => KlyColorMath.ToHex(_shades[index]);

    /// <summary>Returns the name of the palette.</summary>
    public override string ToString() => Name;
}
