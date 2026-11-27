namespace Klyroo.UI.Maui;

/// <summary>One contact line of a <see cref="KlyUser"/>.</summary>
/// <param name="Value">The text shown and copied, such as an email address.</param>
public sealed record KlyContactItem(string Value)
{
    /// <summary>The name of an icon (see <see cref="KlyIcon"/>) shown before the value, such as <c>"mail"</c>.</summary>
    public string? Icon { get; init; }

    /// <summary>A link for the value, such as a <c>mailto:</c> or <c>tel:</c> address.</summary>
    public string? Href { get; init; }

    /// <summary>Whether to show a copy button (<see langword="true"/> by default).</summary>
    public bool Copy { get; init; } = true;
}
