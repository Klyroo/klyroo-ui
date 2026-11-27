namespace Klyroo.UI.Maui;

/// <summary>A line of the feature list of a <see cref="KlyPricingPlan"/>. A string converts to one.</summary>
/// <param name="Title">Text of the feature.</param>
public sealed record KlyPricingFeature(string Title)
{
    /// <summary>Name of a built-in icon (see <see cref="KlyIcon"/>) shown instead of the check mark.</summary>
    public string? Icon { get; init; }

    /// <summary>Converts a text into a feature with that title.</summary>
    /// <param name="title">The text of the feature.</param>
    public static implicit operator KlyPricingFeature(string title) => new(title);
}
