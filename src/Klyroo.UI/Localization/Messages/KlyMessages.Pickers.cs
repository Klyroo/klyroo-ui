namespace Klyroo.UI;

public sealed partial record KlyMessages
{
    /// <summary>Accessible name of the previous month button of <see cref="KlyCalendar"/>.</summary>
    public string PreviousMonth { get; init; } = "Previous month";
    /// <summary>Accessible name of the next month button of <see cref="KlyCalendar"/>.</summary>
    public string NextMonth { get; init; } = "Next month";
    /// <summary>Accessible name of the button of <see cref="KlyInputDate"/> that opens its calendar.</summary>
    public string ChooseDate { get; init; } = "Choose date";
    /// <summary>Accessible name of the hours segment of <see cref="KlyInputTime"/>.</summary>
    public string Hours { get; init; } = "Hours";
    /// <summary>Accessible name of the minutes segment of <see cref="KlyInputTime"/>.</summary>
    public string Minutes { get; init; } = "Minutes";

    /// <summary>Label of the morning segment when the culture has no designator for it.</summary>
    public string TimeAm { get; init; } = "AM";

    /// <summary>Label of the afternoon segment when the culture has no designator for it.</summary>
    public string TimePm { get; init; } = "PM";
    /// <summary>Accessible name of the AM/PM segment of <see cref="KlyInputTime"/>.</summary>
    public string DayPeriod { get; init; } = "AM/PM";
    /// <summary>Accessible name of the saturation control of <see cref="KlyColorPicker"/>.</summary>
    public string Saturation { get; init; } = "Saturation";
    /// <summary>Accessible name of the brightness control of <see cref="KlyColorPicker"/>.</summary>
    public string Brightness { get; init; } = "Brightness";
    /// <summary>Accessible name of the hue slider of <see cref="KlyColorPicker"/>.</summary>
    public string Hue { get; init; } = "Hue";
    /// <summary>Accessible name of the hex field of <see cref="KlyColorPicker"/>.</summary>
    public string HexColor { get; init; } = "Hex color";
    /// <summary>Text of the drop zone of <see cref="KlyFileUpload"/>.</summary>
    public string DropFiles { get; init; } = "Drop files here or click to browse";
    /// <summary>Accessible name of the remove button of a file in <see cref="KlyFileUpload"/>; <c>{0}</c> is the file name.</summary>
    public string RemoveFile { get; init; } = "Remove {0}";
    /// <summary>Hint of <see cref="KlyFileUpload"/> that states the size limit; <c>{0}</c> is the limit, formatted for the locale.</summary>
    public string MaximumSize { get; init; } = "Maximum size {0}";
    /// <summary>Error of <see cref="KlyFileUpload"/> for a file over the size limit; <c>{0}</c> is the file name and <c>{1}</c> the limit.</summary>
    public string FileTooLarge { get; init; } = "{0} is larger than {1}.";
    /// <summary>Error of <see cref="KlyFileUpload"/> when more files than allowed are chosen; <c>{0}</c> is the limit.</summary>
    public string TooManyFiles { get; init; } = "At most {0} files are allowed.";
    /// <summary>Error of <see cref="KlyFileUpload"/> for a file whose type is not accepted; <c>{0}</c> is the file name.</summary>
    public string FileNotAccepted { get; init; } = "{0} is not an accepted file type.";
    /// <summary>Accessible name of the previous button of <see cref="KlyCarousel"/>.</summary>
    public string PreviousSlide { get; init; } = "Previous slide";
    /// <summary>Accessible name of the next button of <see cref="KlyCarousel"/>.</summary>
    public string NextSlide { get; init; } = "Next slide";
    /// <summary>Accessible name of the button of <see cref="KlyCarousel"/> that pauses the automatic rotation.</summary>
    public string PauseSlides { get; init; } = "Pause automatic slide show";
    /// <summary>Accessible name of the button of <see cref="KlyCarousel"/> that starts the automatic rotation.</summary>
    public string PlaySlides { get; init; } = "Start automatic slide show";
    /// <summary>Accessible name of a slide dot of <see cref="KlyCarousel"/>; <c>{0}</c> is the slide number.</summary>
    public string GoToSlide { get; init; } = "Go to slide {0}";
    /// <summary>Default accessible name of a slide of <see cref="KlyCarousel"/>; <c>{0}</c> is its number and <c>{1}</c> the count.</summary>
    public string SlideOf { get; init; } = "{0} of {1}";
    /// <summary>Spoken by screen readers for the carousel itself (<c>aria-roledescription</c>).</summary>
    public string CarouselRoleDescription { get; init; } = "carousel";

    /// <summary>Spoken by screen readers for each slide (<c>aria-roledescription</c>).</summary>
    public string CarouselSlideRoleDescription { get; init; } = "slide";
    /// <summary>Accessible name of the group of slide dots of <see cref="KlyCarousel"/>.</summary>
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
