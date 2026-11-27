namespace Klyroo.UI.Maui;

/// <summary>
/// Semantic color of a control. Built-in colors map to the Klyroo.UI tokens; a custom color needs a
/// <see cref="KlyColorSet"/> registered with <see cref="KlyTheme.Register"/> under its name.
/// </summary>
public readonly record struct KlyColor
{
    /// <summary>Creates a color from the name of a built-in or registered semantic color.</summary>
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
    /// <summary>The neutral color, a gray that follows the text and background tokens.</summary>
    public static KlyColor Neutral { get; } = new("neutral");

    /// <summary>Converts a color name, such as <c>"brand"</c>, into a color.</summary>
    public static implicit operator KlyColor(string value) => new(value);

    /// <summary>Returns the name of the color.</summary>
    public override string ToString() => Value ?? string.Empty;
}

/// <summary>Size of a control, from extra small to extra large.</summary>
public enum KlySize
{
    /// <summary>Extra small.</summary>
    Xs,
    /// <summary>Small.</summary>
    Sm,
    /// <summary>Medium, the default of most controls.</summary>
    Md,
    /// <summary>Large.</summary>
    Lg,
    /// <summary>Extra large.</summary>
    Xl,
}

/// <summary>Look of a <see cref="KlyButton"/>.</summary>
public enum ButtonVariant
{
    /// <summary>A filled button in the semantic color.</summary>
    Solid,
    /// <summary>A border in the semantic color and no fill.</summary>
    Outline,
    /// <summary>A light tint of the semantic color.</summary>
    Soft,
    /// <summary>A light tint with a border.</summary>
    Subtle,
    /// <summary>No border and no fill.</summary>
    Ghost,
    /// <summary>Colored text only, like a link.</summary>
    Link,
}

/// <summary>Look of colored surfaces: <see cref="KlyBadge"/> and <see cref="KlyAlert"/>.</summary>
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

/// <summary>Look of a <see cref="KlyCard"/>.</summary>
public enum CardVariant
{
    /// <summary>A border on the page background.</summary>
    Outline,
    /// <summary>A tinted background and no border.</summary>
    Soft,
    /// <summary>A tinted background with a border.</summary>
    Subtle,
}

/// <summary>Look of a text field: <see cref="KlyInput"/>, <see cref="KlyTextarea"/> and <see cref="KlySelect"/>.</summary>
public enum FieldVariant
{
    /// <summary>A border around the field.</summary>
    Outline,
    /// <summary>A tinted background and no border.</summary>
    Soft,
    /// <summary>A tinted background with a border.</summary>
    Subtle,
    /// <summary>No border and no background.</summary>
    Ghost,
    /// <summary>No border, no background and no focus ring.</summary>
    None,
}
