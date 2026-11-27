namespace Klyroo.UI.Maui;

/// <summary>One label and value row of a <see cref="KlyFacts"/>.</summary>
/// <param name="Label">The row label.</param>
public sealed record KlyFact(string Label)
{
    /// <summary>The row value. An empty value with no <see cref="Badge"/> shows a dash.</summary>
    public string? Value { get; init; }

    /// <summary>A short line of help shown under the label.</summary>
    public string? Hint { get; init; }

    /// <summary>A badge shown instead of the value.</summary>
    public KlyBadgeItem? Badge { get; init; }

    /// <summary>A color for the value, such as <see cref="KlyColor.Success"/>. It applies to plain values; a link keeps the link color and a badge its own.</summary>
    public KlyColor? Color { get; init; }

    /// <summary>Shows a copy button that copies <see cref="CopyText"/>, or <see cref="Value"/> when that is not set.</summary>
    public bool Copy { get; init; }

    /// <summary>The text copied by the copy button. Setting it also shows the button.</summary>
    public string? CopyText { get; init; }

    /// <summary>Shows the value in a monospace font.</summary>
    public bool Mono { get; init; }

    /// <summary>Shows the value in bold.</summary>
    public bool Emphasis { get; init; }

    /// <summary>The name of an icon (see <see cref="KlyIcon"/>) shown before the label.</summary>
    public string? Icon { get; init; }

    /// <summary>Shows the value as a link to this address.</summary>
    public string? Href { get; init; }

    /// <summary>Shows a placeholder instead of the value.</summary>
    public bool Loading { get; init; }
}
