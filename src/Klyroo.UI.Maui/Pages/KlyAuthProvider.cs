namespace Klyroo.UI.Maui;

/// <summary>A button of a <see cref="KlyAuthForm"/> that leaves for another address, such as an OAuth provider.</summary>
/// <param name="Label">Text of the button, such as "Continue with GitHub".</param>
/// <param name="Href">Address the button leaves for: reported by <see cref="KlyAuthForm.ProviderClicked"/>.</param>
public sealed record KlyAuthProvider(string Label, string Href);
