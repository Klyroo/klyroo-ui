using Blux.UI;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    private static readonly ComponentDoc[] Pickers =
    [
        new("calendar", "Calendar", "Form", "A month grid following the WAI-ARIA date picker pattern: arrow keys, Page Up/Down, Home and End, in single, multiple or range mode.",
            [new("Single date", typeof(Examples.CalendarExample), "Dates outside the minimum and maximum, and the listed ones, are disabled."),
             new("Range and multiple", typeof(Examples.CalendarModesExample), "Month and day names and the first day of the week come from the culture.")],
            [typeof(BxCalendar), typeof(BxDateRange)]),
        new("input-date", "InputDate", "Form", "A date field typed in segments with a calendar popover. The value is always posted as yyyy-MM-dd.",
            [new("Bound", typeof(Examples.InputDateExample)),
             new("In an EditForm", typeof(Examples.InputDateFormExample), "Posts in static rendering too: the input is the browser's own date input.")],
            [typeof(BxInputDate)]),
        new("input-time", "InputTime", "Form", "A time field made of spinbutton segments, with a 12 or 24 hour clock taken from the culture. Posts HH:mm.",
            [new("12 and 24 hours", typeof(Examples.InputTimeExample)),
             new("In an EditForm", typeof(Examples.InputTimeFormExample), "Type digits, use the arrow keys, or Backspace to clear a segment.")],
            [typeof(BxInputTime)]),
        new("color-picker", "ColorPicker", "Form", "A saturation and brightness area, a hue slider and a hex field. The value is a #rrggbb string.",
            [new("Bound", typeof(Examples.ColorPickerExample), "The area is two native sliders (saturation and brightness), so the keyboard works as on any slider."),
             new("In an EditForm", typeof(Examples.ColorPickerFormExample))],
            [typeof(BxColorPicker)]),
        new("file-upload", "FileUpload", "Form", "A drop zone around a native file input, with a file list, remove buttons and size, count and type limits.",
            [new("In a form", typeof(Examples.FileUploadExample),
                "Static rendering: the form posts the files (multipart) and they bind to a property of type IReadOnlyList<IBrowserFile> named like the input. Interactive rendering: OnChange receives them from InputFile."),
             new("Interactive", typeof(Examples.FileUploadInteractiveExample), "OnChange renders Blazor's InputFile, so reading the file needs an interactive render mode.")],
            [typeof(BxFileUpload)]),
        new("carousel", "Carousel", "Data", "Slides in a scroll-snap viewport with previous and next buttons, a slide picker and optional autoplay that can be paused.",
            [new("Loop with buttons", typeof(Examples.CarouselExample), "Arrow keys move between slides while the slides have focus."),
             new("Autoplay", typeof(Examples.CarouselAutoplayExample), "Autoplay pauses on hover and focus and has a pause button; it stays off for visitors who prefer reduced motion.")],
            [typeof(BxCarousel), typeof(BxCarouselItem)]),
    ];
}
