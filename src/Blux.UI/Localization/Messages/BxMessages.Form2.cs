namespace Blux.UI;

public sealed partial record BxMessages
{
    public string SelectMenuSearch { get; init; } = "Search";
    public string SelectMenuNoResults { get; init; } = "No results";

    /// <summary>Label of the option that adds the typed text as a new value; <c>{0}</c> is the text.</summary>
    public string SelectMenuCreate { get; init; } = "Create \"{0}\"";
    public string InputMenuNoResults { get; init; } = "No results";
    public string InputMenuSuggestions { get; init; } = "Suggestions";
    public string InputNumberIncrement { get; init; } = "Increase";
    public string InputNumberDecrement { get; init; } = "Decrease";

    /// <summary>Label of the button that removes a tag; <c>{0}</c> is the tag.</summary>
    public string InputTagsRemove { get; init; } = "Remove {0}";

    /// <summary>Label of one box of a pin input; <c>{0}</c> is its position, <c>{1}</c> the number of boxes.</summary>
    public string PinInputCharacter { get; init; } = "Character {0} of {1}";
    public string SliderMinimum { get; init; } = "Minimum";
    public string SliderMaximum { get; init; } = "Maximum";

    /// <summary>Label of one star of a rating; <c>{0}</c> is its value, <c>{1}</c> the maximum.</summary>
    public string InputRatingValue { get; init; } = "{0} out of {1}";
    public string FormSummaryTitle { get; init; } = "Please fix the following errors";
}
