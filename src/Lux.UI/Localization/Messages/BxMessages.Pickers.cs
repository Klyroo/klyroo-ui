namespace Lux.UI;

public sealed partial record BxMessages
{
    public string PreviousMonth { get; init; } = "Previous month";
    public string NextMonth { get; init; } = "Next month";
    public string ChooseDate { get; init; } = "Choose date";
    public string Hours { get; init; } = "Hours";
    public string Minutes { get; init; } = "Minutes";

    /// <summary>Label of the morning segment when the culture has no designator for it.</summary>
    public string TimeAm { get; init; } = "AM";

    /// <summary>Label of the afternoon segment when the culture has no designator for it.</summary>
    public string TimePm { get; init; } = "PM";
    public string DayPeriod { get; init; } = "AM/PM";
    public string Saturation { get; init; } = "Saturation";
    public string Brightness { get; init; } = "Brightness";
    public string Hue { get; init; } = "Hue";
    public string HexColor { get; init; } = "Hex color";
    public string DropFiles { get; init; } = "Drop files here or click to browse";
    public string RemoveFile { get; init; } = "Remove {0}";
    public string MaximumSize { get; init; } = "Maximum size {0}";
    public string FileTooLarge { get; init; } = "{0} is larger than {1}.";
    public string TooManyFiles { get; init; } = "At most {0} files are allowed.";
    public string FileNotAccepted { get; init; } = "{0} is not an accepted file type.";
    public string PreviousSlide { get; init; } = "Previous slide";
    public string NextSlide { get; init; } = "Next slide";
    public string PauseSlides { get; init; } = "Pause automatic slide show";
    public string PlaySlides { get; init; } = "Start automatic slide show";
    public string GoToSlide { get; init; } = "Go to slide {0}";
    public string SlideOf { get; init; } = "{0} of {1}";
    /// <summary>Spoken by screen readers for the carousel itself (<c>aria-roledescription</c>).</summary>
    public string CarouselRoleDescription { get; init; } = "carousel";

    /// <summary>Spoken by screen readers for each slide (<c>aria-roledescription</c>).</summary>
    public string CarouselSlideRoleDescription { get; init; } = "slide";
    public string SlideControls { get; init; } = "Choose a slide";

    /// <summary>A size in bytes (<c>{0}</c> is the number, formatted for the culture of the locale).</summary>
    public string FileSizeBytes { get; init; } = "{0} B";

    /// <summary>A size in kilobytes.</summary>
    public string FileSizeKilobytes { get; init; } = "{0} KB";

    /// <summary>A size in megabytes.</summary>
    public string FileSizeMegabytes { get; init; } = "{0} MB";

    /// <summary>A size in gigabytes.</summary>
    public string FileSizeGigabytes { get; init; } = "{0} GB";
}
