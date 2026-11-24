using System.Text.RegularExpressions;
using Blux.UI.Theming;

namespace Blux.UI;

/// <summary>
/// A palette of 11 shades (50, 100, 200 … 900, 950) that a semantic color is mapped to, like the palettes of Tailwind CSS
/// Use the Tailwind v4 defaults (<see cref="Blue"/>, <see cref="Zinc"/> …), <see cref="Custom(string, IReadOnlyDictionary{int, string})"/>
/// for your own, or <see cref="Blux"/>, the hand-tuned colors Blux UI ships with.
/// </summary>
/// <remarks>
/// Shade values are <c>#rgb</c>, <c>#rrggbb</c> or <c>oklch(L C H)</c>. The tokens of a semantic color (the fill, the text color
/// for light surfaces and the text color on the fill) are picked from the shades so that the contrast rules of the library
/// hold; see <see cref="BxColorOptions"/>.
/// </remarks>
public sealed partial class BxPalette
{
    /// <summary>The shade numbers of a palette, lightest first.</summary>
    public static IReadOnlyList<int> ShadeNumbers { get; } = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950];

    // Not a static field: the generated partial declares static palettes whose initializers run Create, in an unspecified order.
    private const string NamePattern = "^[a-z][a-z0-9-]*$";

    private readonly string[] _shades;

    private BxPalette(string name, string[] shades)
    {
        Name = name;
        _shades = shades;
    }

    /// <summary>
    /// The colors Blux UI ships with, tuned by hand for contrast (the signal yellow primary, a teal secondary…). Assigned to a
    /// semantic color it keeps that color's default; it has no shades.
    /// </summary>
    public static BxPalette Blux { get; } = new("blux", []);

    public string Name { get; }

    /// <summary>True for <see cref="Blux"/>, which carries no shades.</summary>
    public bool IsBlux => _shades.Length == 0;

    /// <summary>The shades as given, lightest first (empty for <see cref="Blux"/>).</summary>
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
            return IsBlux ? throw new InvalidOperationException("BxPalette.Blux has no shades.") : _shades[index];
        }
    }

    /// <summary>A palette from all 11 shades, keyed by shade number (50 to 950).</summary>
    public static BxPalette Custom(string name, IReadOnlyDictionary<int, string> shades)
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
    public static BxPalette Custom(string name, params string[] shades)
    {
        ArgumentNullException.ThrowIfNull(shades);
        if (shades.Length != ShadeNumbers.Count)
        {
            throw new ArgumentException($"A palette has {ShadeNumbers.Count} shades (50 to 950), got {shades.Length}.", nameof(shades));
        }
        return Create(name, [.. shades]);
    }

    private static BxPalette Tailwind(string name, params string[] shades) => Create(name, shades);

    private static BxPalette Create(string name, string[] shades)
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
                BxColorMath.Parse(shade);
            }
            catch (FormatException e)
            {
                throw new ArgumentException($"Palette '{name}': {e.Message}", nameof(shades), e);
            }
        }
        return new BxPalette(name, shades);
    }

    internal string Hex(int index) => BxColorMath.ToHex(_shades[index]);

    public override string ToString() => Name;
}
