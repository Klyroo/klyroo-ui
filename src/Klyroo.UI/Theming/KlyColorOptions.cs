using System.Text.RegularExpressions;

namespace Klyroo.UI;

/// <summary>
/// Maps the semantic colors to palettes:
/// <c>AddKlyrooUi(o =&gt; { o.Colors.Primary = KlyPalette.Blue; o.Colors.Neutral = KlyPalette.Zinc; o.Colors.Add("tertiary", KlyPalette.Indigo); })</c>.
/// </summary>
/// <remarks>
/// Every semantic color left at <see cref="KlyPalette.Klyroo"/> keeps the hand-tuned colors of <c>klyroo.css</c>. A color mapped to
/// another palette gets its tokens (<c>--klyroo-{name}</c>, <c>-text</c>, <c>-contrast</c>, light and dark) computed from the
/// shades: the shade nearest to 500 (light) or 400 (dark) that keeps text on the fill at 4.5:1, the fill at 3:1 on the
/// page surfaces and the text color at 4.5:1 on the page and on soft backgrounds. <see cref="Neutral"/> also drives the
/// text, background and border tokens. Render <see cref="KlyThemeStyle"/> in <c>&lt;head&gt;</c> to deliver the result.
/// The options become read-only once the first component or <see cref="KlyThemeStyle"/> uses them.
/// </remarks>
public sealed class KlyColorOptions
{
    private static readonly string[] SemanticNames = ["primary", "secondary", "success", "info", "warning", "error", "neutral"];

    private readonly Dictionary<string, KlyPalette> _roles = SemanticNames.ToDictionary(n => n, _ => KlyPalette.Klyroo, StringComparer.Ordinal);
    private readonly Dictionary<string, KlyPalette> _extra = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private volatile bool _frozen;
    private volatile bool _allowLowContrast;

    /// <summary>The names of the built-in semantic colors.</summary>
    public static IReadOnlyList<string> BuiltInNames => SemanticNames;

    public KlyPalette Primary { get => Get("primary"); set => Set("primary", value); }
    public KlyPalette Secondary { get => Get("secondary"); set => Set("secondary", value); }
    public KlyPalette Success { get => Get("success"); set => Set("success", value); }
    public KlyPalette Info { get => Get("info"); set => Set("info", value); }
    public KlyPalette Warning { get => Get("warning"); set => Set("warning", value); }
    public KlyPalette Error { get => Get("error"); set => Set("error", value); }

    /// <summary>Drives the text, background and border tokens as well.</summary>
    public KlyPalette Neutral { get => Get("neutral"); set => Set("neutral", value); }

    /// <summary>
    /// What happens when a palette cannot meet the contrast rules (a flat palette, a neutral whose shades are too close to each
    /// other…). False, the default, throws an <see cref="InvalidOperationException"/> naming the color, the palette and the rule
    /// the first time the stylesheet is resolved; true keeps the best combination found, which may be below the ratios.
    /// </summary>
    public bool AllowLowContrast
    {
        get => _allowLowContrast;
        set
        {
            lock (_gate)
            {
                ThrowIfFrozen();
                _allowLowContrast = value;
            }
        }
    }

    /// <summary>Extra semantic colors added with <see cref="Add"/>, usable as <c>Color="@("tertiary")"</c>.</summary>
    public IReadOnlyDictionary<string, KlyPalette> Extra
    {
        get
        {
            lock (_gate)
            {
                return new Dictionary<string, KlyPalette>(_extra, StringComparer.Ordinal);
            }
        }
    }

    /// <summary>Registers an extra semantic color role (e.g. 'tertiary').</summary>
    /// <exception cref="InvalidOperationException">A component already rendered with these options.</exception>
    public void Add(string name, KlyPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        if (!Regex.IsMatch(name ?? "", "^[a-z][a-z0-9-]*$", RegexOptions.CultureInvariant))
        {
            throw new ArgumentException($"Color name '{name}' must be lower case letters, digits and dashes, starting with a letter.", nameof(name));
        }
        if (SemanticNames.Contains(name!))
        {
            throw new ArgumentException($"'{name}' is a built-in semantic color: assign its palette with the {char.ToUpperInvariant(name![0])}{name[1..]} property.", nameof(name));
        }
        if (name!.StartsWith("chart-", StringComparison.Ordinal))
        {
            throw new ArgumentException("Names starting with 'chart-' are reserved for the chart series colors.", nameof(name));
        }
        if (palette.IsKlyroo)
        {
            throw new ArgumentException("An extra color needs a palette with shades: KlyPalette.Klyroo only describes the built-in colors.", nameof(palette));
        }
        lock (_gate)
        {
            ThrowIfFrozen();
            _extra[name] = palette;
        }
    }

    /// <summary>The palette of a built-in or extra semantic color, or null.</summary>
    internal KlyPalette? PaletteOf(string name)
    {
        lock (_gate)
        {
            return _roles.TryGetValue(name, out var role) ? role : _extra.GetValueOrDefault(name);
        }
    }

    /// <summary>True when nothing differs from the colors compiled into <c>klyroo.css</c>.</summary>
    internal bool IsDefault
    {
        get
        {
            lock (_gate)
            {
                return _extra.Count == 0 && _roles.Values.All(p => p.IsKlyroo);
            }
        }
    }

    internal void Freeze() => _frozen = true;

    private KlyPalette Get(string role)
    {
        lock (_gate)
        {
            return _roles[role];
        }
    }

    private void Set(string role, KlyPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        lock (_gate)
        {
            ThrowIfFrozen();
            _roles[role] = palette;
        }
    }

    private void ThrowIfFrozen()
    {
        if (_frozen)
        {
            throw new InvalidOperationException(
                "The colors are read-only once a component or KlyThemeStyle has rendered. Configure them in AddKlyrooUi, which runs before the first render.");
        }
    }
}

/// <summary>
/// Default <c>Color</c> and <c>Size</c> applied across components.
/// </summary>
/// <remarks>
/// A component whose built-in default is <see cref="KlyColor.Primary"/> uses <see cref="Color"/> when the parameter is not set;
/// components that are neutral by design (icon buttons, separators, kbd…) keep <see cref="KlyColor.Neutral"/>.
/// <see cref="Size"/> applies to the controls whose built-in size is <see cref="KlySize.Md"/>. An explicit parameter always wins.
/// Read-only once a component rendered.
/// </remarks>
public sealed class KlyDefaults
{
    private KlyColor? _color;
    private KlySize? _size;
    private volatile bool _frozen;

    public KlyColor? Color { get => _color; set { ThrowIfFrozen(); _color = value; } }
    public KlySize? Size { get => _size; set { ThrowIfFrozen(); _size = value; } }

    // Reading it from a component freezes the values, like a resolved theme: a late change would apply to some renders only.
    internal KlyColor ColorOr(KlyColor fallback)
    {
        _frozen = true;
        return _color ?? fallback;
    }

    internal KlySize SizeOr(KlySize fallback)
    {
        _frozen = true;
        return _size ?? fallback;
    }

    private void ThrowIfFrozen()
    {
        if (_frozen)
        {
            throw new InvalidOperationException("The defaults are read-only once a component rendered. Configure them in AddKlyrooUi.");
        }
    }
}
