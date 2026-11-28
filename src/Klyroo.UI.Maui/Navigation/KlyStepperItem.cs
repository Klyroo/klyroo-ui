namespace Klyroo.UI.Maui;

/// <summary>A step of a <see cref="KlyStepper"/>.</summary>
/// <param name="Title">Title of the step.</param>
public sealed record KlyStepperItem(string Title)
{
    /// <summary>Text under the title.</summary>
    public string? Description { get; init; }

    /// <summary>Replaces the step number (and the check of a completed step): a Lucide name such as <see cref="KlyIconNames.Star"/>.</summary>
    public string? Icon { get; init; }

    /// <summary>Shown below the list while this step is the current one.</summary>
    public View? Content { get; init; }

    /// <summary>Cannot be selected when the stepper is interactive.</summary>
    public bool Disabled { get; init; }
}
