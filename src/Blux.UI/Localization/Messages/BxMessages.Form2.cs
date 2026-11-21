namespace Blux.UI;

public sealed partial record BxMessages
{
    public string SelectMenuSearch { get; init; } = "Search";
    public string SelectMenuNoResults { get; init; } = "No results";

    /// <summary>Label of the option that adds the typed text as a new value; <c>{0}</c> is the text.</summary>
    public string SelectMenuCreate { get; init; } = "Create \"{0}\"";
    public string InputMenuNoResults { get; init; } = "No results";
    public string InputMenuSuggestions { get; init; } = "Suggestions";

    /// <summary>Announced politely while a <c>SelectMenu</c> or <c>InputMenu</c> with an <c>OptionsProvider</c> loads its options.</summary>
    public string RemoteOptionsLoading { get; init; } = "Loading...";

    /// <summary>Shown (and announced) when the <c>OptionsProvider</c> of a <c>SelectMenu</c> or <c>InputMenu</c> fails.</summary>
    public string RemoteOptionsError { get; init; } = "The options could not be loaded.";

    /// <summary>Text of the button that asks the <c>OptionsProvider</c> again after a failure.</summary>
    public string RemoteOptionsRetry { get; init; } = "Retry";

    /// <summary>Shown when the typed text is shorter than <c>MinLength</c>; <c>{0}</c> is that length.</summary>
    public string RemoteOptionsMinLength { get; init; } = "Type at least {0} characters to search.";
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
