namespace Lux.UI;

/// <summary>
/// Semantic color of a component, rendered as <c>data-color</c>. Built-in colors map to the
/// <c>--blux-*</c> tokens. A custom color only needs CSS rules for its name, for example
/// <c>[data-color="brand"] { --bx-color: ...; --bx-color-contrast: ...; --bx-color-text: ...; }</c>,
/// which works with and without Tailwind in the app.
/// </summary>
public readonly record struct BxColor
{
    public BxColor(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public static BxColor Primary { get; } = new("primary");
    public static BxColor Secondary { get; } = new("secondary");
    public static BxColor Success { get; } = new("success");
    public static BxColor Info { get; } = new("info");
    public static BxColor Warning { get; } = new("warning");
    public static BxColor Error { get; } = new("error");
    public static BxColor Neutral { get; } = new("neutral");

    public static implicit operator BxColor(string value) => new(value);

    public override string ToString() => Value;
}

/// <summary>Look of colored surfaces: badges, alerts.</summary>
public enum BxVariant
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

public enum BxSize
{
    Xs,
    Sm,
    Md,
    Lg,
    Xl,
}

internal static class VariantValue
{
    public static string Of(BxSize size) => size switch
    {
        BxSize.Xs => "xs",
        BxSize.Sm => "sm",
        BxSize.Md => "md",
        BxSize.Lg => "lg",
        BxSize.Xl => "xl",
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    public static string Of(bool value) => value ? "true" : "false";

    public static string Of(BxVariant variant) => variant switch
    {
        BxVariant.Solid => "solid",
        BxVariant.Outline => "outline",
        BxVariant.Soft => "soft",
        BxVariant.Subtle => "subtle",
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
