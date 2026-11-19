namespace Blux.UI;

/// <summary>A culture offered by <see cref="BxLocaleSelect"/>.</summary>
/// <param name="Code">The culture name written to the cookie, such as "fr" or "pt-BR". It must be one of the supported cultures of the server.</param>
/// <param name="Label">The name shown to users, in that language ("Français").</param>
public sealed record BxLocale(string Code, string Label)
{
    /// <summary>Shown before the label: a flag emoji, or any short text. Some systems (Windows) draw flag emoji as letters.</summary>
    public string? Flag { get; init; }
}
