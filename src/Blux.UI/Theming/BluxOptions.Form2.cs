using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme SelectMenu { get; } = Themes.SelectMenu();
    public ComponentTheme InputMenu { get; } = Themes.InputMenu();
    public ComponentTheme InputNumber { get; } = Themes.InputNumber();
    public ComponentTheme InputTags { get; } = Themes.InputTags();
    public ComponentTheme PinInput { get; } = Themes.PinInput();
    public ComponentTheme Slider { get; } = Themes.Slider();
    public ComponentTheme CheckboxGroup { get; } = Themes.CheckboxGroup();
    public ComponentTheme Toggle { get; } = Themes.Toggle();
    public ComponentTheme ToggleGroup { get; } = Themes.ToggleGroup();
    public ComponentTheme Listbox { get; } = Themes.Listbox();
    public ComponentTheme InputRating { get; } = Themes.InputRating();
    public ComponentTheme Form { get; } = Themes.Form();
}

public sealed partial class BluxStrings
{
    public string SelectMenuSearch { get; set; } = "Search";
    public string SelectMenuNoResults { get; set; } = "No results";

    /// <summary>Label of the option that adds the typed text as a new value; <c>{0}</c> is the text.</summary>
    public string SelectMenuCreate { get; set; } = "Create \"{0}\"";
    public string InputMenuNoResults { get; set; } = "No results";
    public string InputMenuSuggestions { get; set; } = "Suggestions";
    public string InputNumberIncrement { get; set; } = "Increase";
    public string InputNumberDecrement { get; set; } = "Decrease";

    /// <summary>Label of the button that removes a tag; <c>{0}</c> is the tag.</summary>
    public string InputTagsRemove { get; set; } = "Remove {0}";

    /// <summary>Label of one box of a pin input; <c>{0}</c> is its position, <c>{1}</c> the number of boxes.</summary>
    public string PinInputCharacter { get; set; } = "Character {0} of {1}";
    public string SliderMinimum { get; set; } = "Minimum";
    public string SliderMaximum { get; set; } = "Maximum";

    /// <summary>Label of one star of a rating; <c>{0}</c> is its value, <c>{1}</c> the maximum.</summary>
    public string InputRatingValue { get; set; } = "{0} out of {1}";
    public string FormSummaryTitle { get; set; } = "Please fix the following errors";
}
