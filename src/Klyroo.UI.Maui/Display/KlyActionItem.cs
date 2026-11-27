namespace Klyroo.UI.Maui;

/// <summary>A button shown in a block, drawn as a small <see cref="KlyButton"/>.</summary>
/// <param name="Label">The button text.</param>
public sealed record KlyActionItem(string Label)
{
    /// <summary>The address the button opens in the browser (or in the app registered for its scheme, such as <c>mailto:</c>) when it is pressed. It is opened before <see cref="OnClick"/> runs.</summary>
    public string? Href { get; init; }

    /// <summary>Where <see cref="Href"/> opens: <see cref="KlyLinkTarget.Blank"/> in the external browser, anything else in the in-app browser when the platform has one.</summary>
    public KlyLinkTarget? Target { get; init; }

    /// <summary>The callback invoked when the button is pressed.</summary>
    public Action? OnClick { get; init; }

    /// <summary>The button color (neutral by default).</summary>
    public KlyColor Color { get; init; } = KlyColor.Neutral;

    /// <summary>The button look (outline by default).</summary>
    public ButtonVariant Variant { get; init; } = ButtonVariant.Outline;
}
