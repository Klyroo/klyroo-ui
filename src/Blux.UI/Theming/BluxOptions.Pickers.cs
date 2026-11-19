using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme Calendar { get; } = Themes.Calendar();
    public ComponentTheme InputDate { get; } = Themes.InputDate();
    public ComponentTheme InputTime { get; } = Themes.InputTime();
    public ComponentTheme ColorPicker { get; } = Themes.ColorPicker();
    public ComponentTheme FileUpload { get; } = Themes.FileUpload();
    public ComponentTheme Carousel { get; } = Themes.Carousel();
}

public sealed partial class BluxStrings
{
    public string PreviousMonth { get; set; } = "Previous month";
    public string NextMonth { get; set; } = "Next month";
    public string ChooseDate { get; set; } = "Choose date";
    public string Hours { get; set; } = "Hours";
    public string Minutes { get; set; } = "Minutes";
    public string DayPeriod { get; set; } = "AM/PM";
    public string Saturation { get; set; } = "Saturation";
    public string Brightness { get; set; } = "Brightness";
    public string Hue { get; set; } = "Hue";
    public string HexColor { get; set; } = "Hex color";
    public string DropFiles { get; set; } = "Drop files here or click to browse";
    public string RemoveFile { get; set; } = "Remove {0}";
    public string MaximumSize { get; set; } = "Maximum size {0}";
    public string FileTooLarge { get; set; } = "{0} is larger than {1}.";
    public string TooManyFiles { get; set; } = "At most {0} files are allowed.";
    public string FileNotAccepted { get; set; } = "{0} is not an accepted file type.";
    public string PreviousSlide { get; set; } = "Previous slide";
    public string NextSlide { get; set; } = "Next slide";
    public string PauseSlides { get; set; } = "Pause automatic slide show";
    public string PlaySlides { get; set; } = "Start automatic slide show";
    public string GoToSlide { get; set; } = "Go to slide {0}";
    public string SlideOf { get; set; } = "{0} of {1}";
    public string SlideControls { get; set; } = "Choose a slide";
}
