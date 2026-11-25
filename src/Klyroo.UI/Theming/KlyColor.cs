namespace Klyroo.UI;

/// <summary>
/// Semantic color of a component, rendered as <c>data-color</c>. Built-in colors map to the
/// <c>--klyroo-*</c> tokens. A custom color only needs CSS rules for its name, for example
/// <c>[data-color="brand"] { --kly-color: ...; --kly-color-contrast: ...; --kly-color-text: ...; }</c>,
/// which works with and without Tailwind in the app.
/// </summary>
public readonly record struct KlyColor
{
    public KlyColor(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public static KlyColor Primary { get; } = new("primary");
    public static KlyColor Secondary { get; } = new("secondary");
    public static KlyColor Success { get; } = new("success");
    public static KlyColor Info { get; } = new("info");
    public static KlyColor Warning { get; } = new("warning");
    public static KlyColor Error { get; } = new("error");
    public static KlyColor Neutral { get; } = new("neutral");

    public static implicit operator KlyColor(string value) => new(value);

    public override string ToString() => Value;
}

/// <summary>Look of colored surfaces: badges, alerts.</summary>
public enum KlyVariant
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

public enum KlySize
{
    Xs,
    Sm,
    Md,
    Lg,
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
