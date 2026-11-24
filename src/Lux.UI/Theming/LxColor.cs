namespace Lux.UI;

/// <summary>
/// Semantic color of a component, rendered as <c>data-color</c>. Built-in colors map to the
/// <c>--lux-*</c> tokens. A custom color only needs CSS rules for its name, for example
/// <c>[data-color="brand"] { --lx-color: ...; --lx-color-contrast: ...; --lx-color-text: ...; }</c>,
/// which works with and without Tailwind in the app.
/// </summary>
public readonly record struct LxColor
{
    public LxColor(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public static LxColor Primary { get; } = new("primary");
    public static LxColor Secondary { get; } = new("secondary");
    public static LxColor Success { get; } = new("success");
    public static LxColor Info { get; } = new("info");
    public static LxColor Warning { get; } = new("warning");
    public static LxColor Error { get; } = new("error");
    public static LxColor Neutral { get; } = new("neutral");

    public static implicit operator LxColor(string value) => new(value);

    public override string ToString() => Value;
}

/// <summary>Look of colored surfaces: badges, alerts.</summary>
public enum LxVariant
{
    Solid,
    Outline,
    Soft,
    Subtle,
}

/// <summary>Look of text fields: input, textarea, select.</summary>
public enum FieldVariant
{
    Outline,
    Soft,
    Subtle,
    Ghost,
    None,
}

public enum LxSize
{
    Xs,
    Sm,
    Md,
    Lg,
    Xl,
}

internal static class VariantValue
{
    public static string Of(LxSize size) => size switch
    {
        LxSize.Xs => "xs",
        LxSize.Sm => "sm",
        LxSize.Md => "md",
        LxSize.Lg => "lg",
        LxSize.Xl => "xl",
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    public static string Of(bool value) => value ? "true" : "false";

    public static string Of(LxVariant variant) => variant switch
    {
        LxVariant.Solid => "solid",
        LxVariant.Outline => "outline",
        LxVariant.Soft => "soft",
        LxVariant.Subtle => "subtle",
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
