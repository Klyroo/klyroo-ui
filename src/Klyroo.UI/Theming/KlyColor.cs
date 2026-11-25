namespace Klyroo.UI;

/// <summary>
/// Semantic color of a component, rendered as <c>data-color</c>. Built-in colors map to the
/// <c>--klyroo-*</c> tokens. A custom color only needs CSS rules for its name, for example
/// <c>[data-color="brand"] { --kly-color: ...; --kly-color-contrast: ...; --kly-color-text: ...; }</c>,
/// which works with and without Tailwind in the app.
/// </summary>
public readonly record struct KlyColor
{
    /// <summary>Creates a color from the name of a built-in or custom semantic color.</summary>
    public KlyColor(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>The name of the color, such as <c>primary</c>.</summary>
    public string Value { get; }

    /// <summary>The primary color of the app.</summary>
    public static KlyColor Primary { get; } = new("primary");
    /// <summary>The secondary color of the app.</summary>
    public static KlyColor Secondary { get; } = new("secondary");
    /// <summary>The color of success messages.</summary>
    public static KlyColor Success { get; } = new("success");
    /// <summary>The color of informational messages.</summary>
    public static KlyColor Info { get; } = new("info");
    /// <summary>The color of warnings.</summary>
    public static KlyColor Warning { get; } = new("warning");
    /// <summary>The color of errors.</summary>
    public static KlyColor Error { get; } = new("error");
    /// <summary>The neutral color, a gray that also drives text, background and border tokens.</summary>
    public static KlyColor Neutral { get; } = new("neutral");

    /// <summary>Converts a color name, such as <c>"brand"</c>, into a color.</summary>
    public static implicit operator KlyColor(string value) => new(value);

    /// <summary>Returns the name of the color.</summary>
    public override string ToString() => Value;
}

/// <summary>Look of colored surfaces: badges, alerts.</summary>
public enum KlyVariant
{
    /// <summary>A filled surface in the semantic color.</summary>
    Solid,
    /// <summary>A border in the semantic color and no fill.</summary>
    Outline,
    /// <summary>A light tint of the semantic color.</summary>
    Soft,
    /// <summary>A light tint with a border.</summary>
    Subtle,
}

/// <summary>Look of text fields: input, textarea, select.</summary>
public enum FieldVariant
{
    /// <summary>A border around the field.</summary>
    Outline,
    /// <summary>A tinted background.</summary>
    Soft,
    /// <summary>A tinted background with a border.</summary>
    Subtle,
    /// <summary>No border and no background until hovered or focused.</summary>
    Ghost,
    /// <summary>A transparent field with no border, for a field placed inside a surface of your own.</summary>
    None,
}

/// <summary>Size of a component, which scales its text, padding and icons.</summary>
public enum KlySize
{
    /// <summary>Extra small.</summary>
    Xs,
    /// <summary>Small.</summary>
    Sm,
    /// <summary>Medium, the default of most components.</summary>
    Md,
    /// <summary>Large.</summary>
    Lg,
    /// <summary>Extra large.</summary>
    Xl,
}

internal static class VariantValue
{
    public static string Of(KlySize size) => size switch
    {
        KlySize.Xs => "xs",
        KlySize.Sm => "sm",
        KlySize.Md => "md",
        KlySize.Lg => "lg",
        KlySize.Xl => "xl",
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    public static string Of(bool value) => value ? "true" : "false";

    public static string Of(KlyVariant variant) => variant switch
    {
        KlyVariant.Solid => "solid",
        KlyVariant.Outline => "outline",
        KlyVariant.Soft => "soft",
        KlyVariant.Subtle => "subtle",
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    public static string Of(FieldVariant variant) => variant switch
    {
        FieldVariant.Outline => "outline",
        FieldVariant.Soft => "soft",
        FieldVariant.Subtle => "subtle",
        FieldVariant.Ghost => "ghost",
        FieldVariant.None => "none",
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };
}
